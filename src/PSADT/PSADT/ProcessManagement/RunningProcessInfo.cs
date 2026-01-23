using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;
using PSADT.FileSystem;
using PSADT.LibraryInterfaces;
using PSADT.Security;
using Windows.Win32.Foundation;
using Windows.Win32.Security;

namespace PSADT.ProcessManagement
{
    /// <summary>
    /// Represents a running process.
    /// </summary>
    public sealed record RunningProcessInfo
    {
        /// <summary>
        /// Retrieves a list of running processes that match the specified process definitions.
        /// </summary>
        /// <remarks>This method identifies running processes by comparing their names and command-line arguments against the provided process definitions. If a process definition includes a filter, only processes that satisfy the filter are included in the result. Processes that cannot be accessed due to insufficient privileges are skipped.</remarks>
        /// <param name="processDefinitions">An array of <see cref="ProcessDefinition"/> objects that define the processes to search for. Each definition specifies the name, optional description, and an optional filter to match processes.</param>
        /// <returns>A read-only list of <see cref="RunningProcessInfo"/> objects representing the processes that match the given definitions. The list is ordered by the description of the running processes.</returns>
        public static IReadOnlyList<RunningProcessInfo> Get(params IReadOnlyList<ProcessDefinition> processDefinitions)
        {
            // Set up some caches for performance.
            if (!(processDefinitions?.Count > 0))
            {
                throw new ArgumentNullException(nameof(processDefinitions), "Process definitions cannot be null or empty.");
            }
            ReadOnlyDictionary<string, string> ntPathLookupTable = FileSystemUtilities.GetNtPathLookupTable();
            Dictionary<Process, string[]> processArgvMap = [];

            // Inline lambda to get the command line from the given process.
            static string[] GetProcessArgv(Process process, Dictionary<Process, string[]> processArgvMap, ReadOnlyDictionary<string, string> ntPathLookupTable)
            {
                // Inline lambda to get the file path from the given process.
                static string GetProcessFilePath(Process process, ReadOnlyDictionary<string, string> ntPathLookupTable)
                {
                    // Try and get the file path from the MainModule first, falling back to the image name if we can't.
                    try
                    {
                        if (process.MainModule is not null)
                        {
                            return process.MainModule.FileName;
                        }
                    }
                    catch (Exception ex) when (ex.Message is not null)
                    {
                        return ProcessUtilities.GetProcessImageName(process, ntPathLookupTable);
                    }
                    return ProcessUtilities.GetProcessImageName(process, ntPathLookupTable);
                }

                // Get the command line from the cache if we have it.
                if (processArgvMap.TryGetValue(process, out string[]? argv))
                {
                    return argv;
                }

                // Get the command line for the process. Failing that, just get the image path.
                string? commandLine;
                if (process.HasExited)
                {
                    return [];
                }
                try
                {
                    commandLine = ProcessUtilities.GetProcessCommandLine(process);
                }
                catch (Exception ex) when (ex.Message is not null)
                {
                    commandLine = null;
                }

                // Convert the command line into an argument array.
                if (commandLine is not null)
                {
                    argv = [.. CommandLineUtilities.CommandLineToArgumentList(commandLine)];
                }

                // If we couldn't get the command line or the file path is malformed, try and get the process's image name.
                if (process.HasExited)
                {
                    return [];
                }
                try
                {
                    if (argv?.Length > 0)
                    {
                        if (!argv[0].Contains(process.ProcessName, StringComparison.OrdinalIgnoreCase) && !argv[0].EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                        {
                            argv = [.. (new[] { GetProcessFilePath(process, ntPathLookupTable) }).Concat(argv)];
                        }
                        else
                        {
                            argv[0] = GetProcessFilePath(process, ntPathLookupTable);
                        }
                    }
                    else
                    {
                        argv = [GetProcessFilePath(process, ntPathLookupTable)];
                    }
                }
                catch
                {
                    if (!process.HasExited)
                    {
                        throw;
                    }
                    return [];
                }

                // Cache and return the command line.
                processArgvMap.Add(process, argv);
                return argv;
            }

            // Pre-cache running processes and start looping through to find matches.
            string[] processNames = [.. processDefinitions.Select(p => (Path.IsPathRooted(p.Name) ? Path.GetFileNameWithoutExtension(p.Name) : p.Name).ToUpperInvariant())];
            Process[] allProcesses = [.. Process.GetProcesses().Where(p => processNames.Contains(p.ProcessName.ToUpperInvariant()))];
            List<RunningProcessInfo> runningProcesses = [];
            foreach (ProcessDefinition processDefinition in processDefinitions)
            {
                // Loop through each process and check if it matches the definition.
                foreach (Process process in allProcesses)
                {
                    // Skip this process if it doesn't match the name.
                    if (!Path.IsPathRooted(processDefinition.Name) && !process.ProcessName.Equals(processDefinition.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    // Skip this process if it's not running anymore.
                    try
                    {
                        if (process.HasExited)
                        {
                            continue;
                        }
                    }
                    catch (Win32Exception ex) when (ex.NativeErrorCode == (int)WIN32_ERROR.ERROR_ACCESS_DENIED)
                    {
                        // If we can't access the process, skip it. We only need to test this
                        // once here, it shouldn't be an issue for the remainder of the loop.
                        continue;
                    }

                    // Try to get the command line. If we can't, skip this process.
                    string[] argv;
                    try
                    {
                        if (process.HasExited)
                        {
                            continue;
                        }
                        argv = GetProcessArgv(process, processArgvMap, ntPathLookupTable);
                    }
                    catch (ArgumentException)
                    {
                        continue;
                    }

                    // If we couldn't get the command line, skip this process.
                    if (argv.Length == 0)
                    {
                        continue;
                    }

                    // Continue if this isn't our process or it's ended since we cached it.
                    if (Path.IsPathRooted(processDefinition.Name) && !argv[0].Equals(processDefinition.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    // Calculate a description for the running application.
                    string procDescription = !string.IsNullOrWhiteSpace(processDefinition.Description)
                        ? processDefinition.Description!
                        : File.Exists(argv[0]) && FileVersionInfo.GetVersionInfo(argv[0]) is FileVersionInfo fileInfo && !string.IsNullOrWhiteSpace(fileInfo.FileDescription)
                            ? fileInfo.FileDescription
                            : PrivilegeManager.HasPrivilege(SE_PRIVILEGE.SeDebugPrivilege) && !process.HasExited && ProcessVersionInfo.GetVersionInfo(process, argv[0]) is ProcessVersionInfo procInfo && !string.IsNullOrWhiteSpace(procInfo.FileDescription)
                                ? procInfo.FileDescription!
                                : process.ProcessName;

                    // Grab the process owner if we can.
                    NTAccount? username = null;
                    if (!process.HasExited)
                    {
                        // Users can only get the username for their own processes, whereas admins can get anyone's.
                        try
                        {
                            // We're caching the process, so don't dispose of its SafeHande as .NET caches it also...
                            _ = AdvApi32.OpenProcessToken(process.SafeHandle, TOKEN_ACCESS_MASK.TOKEN_QUERY, out SafeFileHandle hToken);
                            using (hToken)
                            {
                                username = TokenUtilities.GetTokenSid(hToken).Translate(typeof(NTAccount)) as NTAccount;
                            }
                        }
                        catch (Exception ex) when (ex.Message is not null)
                        {
                            username = null;
                        }
                    }

                    // Store the process information.
                    RunningProcessInfo runningProcess = new(process, procDescription, argv[0], argv.Skip(1), username);
                    if (!process.HasExited && ((processDefinition.Filter is null) || processDefinition.Filter(runningProcess)))
                    {
                        runningProcesses.Add(runningProcess);
                    }
                }
            }

            // Return an ordered list of running processes to the caller.
            return new ReadOnlyCollection<RunningProcessInfo>([.. runningProcesses.OrderBy(runningProcess => runningProcess.Description)]);
        }

        /// <summary>
        /// Initializes a new instance of the RunningProcessInfo class with the specified process, description, file
        /// name, argument list, and optional username.
        /// </summary>
        /// <param name="process">The Process object representing the running process. Cannot be null.</param>
        /// <param name="description">A descriptive string for the process. Cannot be null or empty.</param>
        /// <param name="fileName">The file name of the executable associated with the process. Cannot be null or empty.</param>
        /// <param name="argumentList">A collection of arguments passed to the process. Any null or whitespace-only arguments are ignored.</param>
        /// <param name="username">The user account under which the process is running, or null if not specified.</param>
        /// <exception cref="ArgumentNullException">Thrown if process is null, or if description or fileName is null or empty.</exception>
        private RunningProcessInfo(Process process, string description, string fileName, IEnumerable<string> argumentList, NTAccount? username)
        {
            Process = process ?? throw new ArgumentNullException("Process cannot be null.", (Exception?)null);
            Description = !string.IsNullOrWhiteSpace(description) ? description : throw new ArgumentNullException("Description cannot be null or empty.", (Exception?)null);
            FileName = !string.IsNullOrWhiteSpace(fileName) ? fileName : throw new ArgumentNullException("FileName cannot be null or empty.", (Exception?)null);
            ArgumentList = new ReadOnlyCollection<string>([.. argumentList.Where(static a => !string.IsNullOrWhiteSpace(a))]);
            if (username is not null)
            {
                Username = username;
            }
        }

        /// <summary>
        /// Gets the process associated with the running process.
        /// </summary>
        public Process Process { get; }

        /// <summary>
        /// Gets the description of the running process.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets the file path of the running process.
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// Gets the arguments passed to the running process.
        /// </summary>
        public IReadOnlyList<string> ArgumentList { get; }

        /// <summary>
        /// Represents the username associated with a Windows NT account.
        /// </summary>
        /// <remarks>The <see cref="NTAccount"/> class provides a way to work with Windows NT account
        /// names, including translating them to and from security identifiers (SIDs). This field is
        /// read-only.</remarks>
        public NTAccount? Username { get; }
    }
}
