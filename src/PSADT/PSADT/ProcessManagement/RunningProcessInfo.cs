using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Security.Principal;
using PSADT.Extensions;
using PSADT.FileSystem;
using PSADT.Interop;
using PSADT.Security;

namespace PSADT.ProcessManagement
{
    /// <summary>
    /// Represents a running process.
    /// </summary>
    public sealed record class RunningProcessInfo
    {
        /// <summary>
        /// Retrieves a list of running processes that match the specified process definitions.
        /// </summary>
        /// <remarks>This method identifies running processes by comparing their names and command-line arguments against the provided process definitions. If a process definition includes a filter, only processes that satisfy the filter are included in the result. Processes that cannot be accessed due to insufficient privileges are skipped.</remarks>
        /// <param name="processDefinitions">An array of <see cref="ProcessDefinition"/> objects that define the processes to search for. Each definition specifies the name, optional description, and an optional filter to match processes.</param>
        /// <returns>A read-only list of <see cref="RunningProcessInfo"/> objects representing the processes that match the given definitions. The list is ordered by the description of the running processes.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S3236:Caller information arguments should not be provided explicitly", Justification = "This is intentional as we're testing a parameter member.")]
        public static IReadOnlyList<RunningProcessInfo> Get(IReadOnlyList<ProcessDefinition> processDefinitions)
        {
            // Set up some caches for performance.
            ArgumentNullException.ThrowIfNull(processDefinitions);
            ArgumentOutOfRangeException.ThrowIfZero(processDefinitions.Count, nameof(processDefinitions));
            ReadOnlyDictionary<string, string> ntPathLookupTable = FileSystemUtilities.MakeNtPathLookupTable();
            Dictionary<Process, string> processFilePathMap = []; Dictionary<Process, string[]> processArgvMap = [];

            // Inline lambda to get the file path from the given process.
            static string? GetProcessFilePath(Process process, Dictionary<Process, string> processFilePathMap, ReadOnlyDictionary<string, string> ntPathLookupTable)
            {
                // Get the file path from the cache if we have it.
                if (processFilePathMap.TryGetValue(process, out string? filePath))
                {
                    return filePath;
                }

                // Get the file path for the process.
                if (ProcessUtilities.HasProcessExited(process))
                {
                    return null;
                }
                try
                {
                    filePath = process.GetFilePath(ntPathLookupTable).FullName;
                }
                catch (Exception ex) when (ex.Message is not null)
                {
                    if (!ProcessUtilities.HasProcessExited(process))
                    {
                        ExceptionDispatchInfo.Capture(ex).Throw();
                        throw;
                    }
                    return null;
                }

                // Cache and return the file path.
                processFilePathMap.Add(process, filePath);
                return filePath;
            }

            // Inline lambda to get the command line from the given process.
            static string[] GetProcessArgv(Process process, Dictionary<Process, string> processFilePathMap, Dictionary<Process, string[]> processArgvMap, ReadOnlyDictionary<string, string> ntPathLookupTable)
            {
                // Get the command line from the cache if we have it.
                if (processArgvMap.TryGetValue(process, out string[]? argv))
                {
                    return argv;
                }

                // Get the command line for the process. Failing that, just get the image path.
                string? commandLine;
                if (ProcessUtilities.HasProcessExited(process))
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
                string? filePath = GetProcessFilePath(process, processFilePathMap, ntPathLookupTable);
                if (filePath is null)
                {
                    return [];
                }
                if (argv?.Length > 0)
                {
                    if (!argv[0].Contains(process.ProcessName, StringComparison.OrdinalIgnoreCase) && !argv[0].EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        argv = [.. (new[] { filePath }).Concat(argv)];
                    }
                    else
                    {
                        argv[0] = filePath;
                    }
                }
                else
                {
                    argv = [filePath];
                }

                // Cache and return the command line.
                processArgvMap.Add(process, argv);
                return argv;
            }

            // Pre-cache running processes and start looping through to find matches.
            Process[] allProcesses = [.. Process.GetProcesses().Where(p => p.Id > 0 && processDefinitions.Any(pd => pd.ProcessNameIsMatch(p.ProcessName)))]; List<RunningProcessInfo> runningProcesses = [];
            foreach (ProcessDefinition processDefinition in processDefinitions)
            {
                // Loop through each process and check if it matches the definition.
                bool nameIsFullyQualifiedPath = processDefinition.NameIsFullyQualifiedPath();
                foreach (Process process in allProcesses)
                {
                    // Skip this process if it doesn't match the name.
                    if (!processDefinition.ProcessNameIsMatch(process.ProcessName))
                    {
                        continue;
                    }

                    // Skip this process if it's not running anymore.
                    if (ProcessUtilities.HasProcessExited(process))
                    {
                        continue;
                    }

                    // Only throw if the ProcessDefinition's name doesn't contain a wildcard character.
                    try
                    {
                        // Continue if this isn't our process or it's ended since we cached it.
                        if (nameIsFullyQualifiedPath && (GetProcessFilePath(process, processFilePathMap, ntPathLookupTable) is not string filePath || !processDefinition.IsNameMatch(filePath)))
                        {
                            continue;
                        }

                        // Try to get the command line. If we can't, skip this process.
                        string[] argv;
                        try
                        {
                            if (ProcessUtilities.HasProcessExited(process))
                            {
                                continue;
                            }
                            argv = GetProcessArgv(process, processFilePathMap, processArgvMap, ntPathLookupTable);
                        }
                        catch (ArgumentException)
                        {
                            continue;
                        }

                        // If we couldn't get the command line, skip this process.
                        if (argv.Length is 0)
                        {
                            continue;
                        }

                        // Calculate a description for the running application.
                        string description = processDefinition.Description is string defDescription && !string.IsNullOrWhiteSpace(defDescription)
                            ? defDescription
                            : File.Exists(argv[0]) && FileVersionInfo.GetVersionInfo(argv[0]).FileDescription is string fileDescription && !string.IsNullOrWhiteSpace(fileDescription)
                            ? fileDescription
                            : PrivilegeManager.HasPrivilege(SE_PRIVILEGE.SeDebugPrivilege) && !ProcessUtilities.HasProcessExited(process) && ProcessVersionInfo.GetVersionInfo(process, argv[0]).FileDescription is string procDescription && !string.IsNullOrWhiteSpace(procDescription)
                            ? procDescription
                            : process.ProcessName;

                        // Grab the process owner if we can.
                        SecurityIdentifier? sid = null;
                        if (!ProcessUtilities.HasProcessExited(process))
                        {
                            try
                            {
                                sid = ProcessUtilities.GetProcessSid(process);
                            }
                            catch (Exception ex) when (ex.Message is not null)
                            {
                                sid = null;
                            }
                        }

                        // Store the process information.
                        if (!ProcessUtilities.HasProcessExited(process))
                        {
                            runningProcesses.Add(new(process, description, argv[0], argv.Skip(1), sid));
                        }
                    }
                    catch when (processDefinition.Name.Contains('*', StringComparison.Ordinal))
                    {
                        continue;
                        throw;
                    }
                }
            }

            // Return an ordered list of running processes to the caller.
            return new ReadOnlyCollection<RunningProcessInfo>([.. runningProcesses.OrderBy(static runningProcess => runningProcess.Description, StringComparer.OrdinalIgnoreCase).ThenBy(static runningProcess => runningProcess.Description, StringComparer.Ordinal)]);
        }

        /// <summary>
        /// Initializes a new instance of the RunningProcessInfo class with the specified process, description, file
        /// name, argument list, and optional username.
        /// </summary>
        /// <param name="process">The Process object representing the running process. Cannot be null.</param>
        /// <param name="description">A descriptive string for the process. Cannot be null or empty.</param>
        /// <param name="fileName">The file name of the executable associated with the process. Cannot be null or empty.</param>
        /// <param name="argumentList">A collection of arguments passed to the process. Any null or whitespace-only arguments are ignored.</param>
        /// <param name="sid">The security identifier (SID) associated with the user account, or null if not specified.</param>
        /// <exception cref="ArgumentNullException">Thrown if process is null, or if description or fileName is null or empty.</exception>
        private RunningProcessInfo(Process process, string description, string fileName, IEnumerable<string> argumentList, SecurityIdentifier? sid)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(description);
            ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
            Process = process;
            Description = description;
            FileName = new(fileName);
            ArgumentList = new ReadOnlyCollection<string>([.. argumentList.Where(static a => !string.IsNullOrWhiteSpace(a))]);
            SID = sid;
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
        public FileInfo FileName { get; }

        /// <summary>
        /// Gets the arguments passed to the running process.
        /// </summary>
        public IReadOnlyList<string> ArgumentList { get; }

        /// <summary>
        /// Gets the security identifier (SID) associated with the object.
        /// </summary>
        /// <remarks>The SID uniquely identifies a security principal, such as a user or group, for
        /// security-related operations. This property returns null if no SID is associated with the object.</remarks>
        public SecurityIdentifier? SID { get; }
    }
}
