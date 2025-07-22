using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation.Runspaces;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using PSADT.Extensions;
using PSADT.FileSystem;
using PSADT.LibraryInterfaces;
using PSADT.Module;
using PSADT.Security;
using Windows.Win32.System.Services;

namespace PSADT.ProcessManagement
{
    /// <summary>
    /// Provides functionality to manage and retrieve information about running processes.
    /// </summary>
    /// <remarks>The <see cref="ProcessUtilities"/> class offers methods to identify and retrieve details about processes running on the system. It allows filtering and matching processes based on user-defined criteria, such as process names, command-line arguments, and custom filters. Processes that cannot be accessed due to insufficient privileges are automatically skipped.</remarks>
    public static class ProcessUtilities
    {
        /// <summary>
        /// Retrieves a list of running processes that match the specified process definitions.
        /// </summary>
        /// <remarks>This method identifies running processes by comparing their names and command-line arguments against the provided process definitions. If a process definition includes a filter, only processes that satisfy the filter are included in the result. Processes that cannot be accessed due to insufficient privileges are skipped.</remarks>
        /// <param name="processDefinitions">An array of <see cref="ProcessDefinition"/> objects that define the processes to search for. Each definition specifies the name, optional description, and an optional filter to match processes.</param>
        /// <returns>A read-only list of <see cref="RunningProcess"/> objects representing the processes that match the given definitions. The list is ordered by the description of the running processes.</returns>
        public static IReadOnlyList<RunningProcess> GetRunningProcesses(IReadOnlyList<ProcessDefinition> processDefinitions)
        {
            // Set up some caches for performance.
            var ntPathLookupTable = FileSystemUtilities.GetNtPathLookupTable();
            Dictionary<Process, string[]> processCommandLines = [];

            // Ensure we have a PowerShell runspace available for command execution here.
            if (processDefinitions.Any(p => p.Filter is not null) && Runspace.DefaultRunspace is null)
            {
                Runspace.DefaultRunspace = ModuleDatabase.GetRunspace();
            }

            // Inline lambda to get the command line from the given process.
            static string[] GetCommandLine(Process process, Dictionary<Process, string[]> processCommandLines, ReadOnlyDictionary<string, string> ntPathLookupTable)
            {
                // Get the command line from the cache if we have it.
                if (processCommandLines.TryGetValue(process, out var commandLine))
                {
                    return commandLine;
                }

                // Get the image path for this process. We use this instead of what we get
                // from GetProcessCommandLine() because POSIX applications render incorrectly.
                var imagePath = ProcessTools.GetProcessImageName(process.Id, ntPathLookupTable);

                // Get the command line for the process. If this fails due to lack
                // of privileges, we simply just return the image path and that's it.
                try
                {
                    commandLine = Shell32.CommandLineToArgv(ProcessTools.GetProcessCommandLine(process.Id));
                    commandLine[0] = imagePath;
                }
                catch
                {
                    commandLine = [imagePath];
                }
                processCommandLines.Add(process, commandLine);
                return commandLine;
            }

            // Pre-cache running processes and start looping through to find matches.
            var processNames = processDefinitions.Select(p => (Path.IsPathRooted(p.Name) ? Path.GetFileNameWithoutExtension(p.Name) : p.Name).ToLower());
            var allProcesses = Process.GetProcesses().Where(p => processNames.Contains(p.ProcessName.ToLower()));
            List<RunningProcess> runningProcesses = [];
            foreach (var processDefinition in processDefinitions)
            {
                // Loop through each process and check if it matches the definition.
                foreach (var process in allProcesses)
                {
                    // Try to get the command line. If we can't, skip this process.
                    string[] commandLine;
                    try
                    {
                        commandLine = GetCommandLine(process, processCommandLines, ntPathLookupTable);
                    }
                    catch (ArgumentException)
                    {
                        continue;
                    }

                    // Continue if this isn't our process or it's ended since we cached it.
                    if (Path.IsPathRooted(processDefinition.Name))
                    {
                        if (!commandLine[0].Equals(processDefinition.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                    }
                    else
                    {
                        if (!process.ProcessName.Equals(processDefinition.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                    }

                    // Calculate a description for the running application.
                    string procDescription;
                    if (!string.IsNullOrWhiteSpace(processDefinition.Description))
                    {
                        procDescription = processDefinition.Description!;
                    }
                    else if (File.Exists(commandLine[0]) && FileVersionInfo.GetVersionInfo(commandLine[0]) is FileVersionInfo fileInfo && !string.IsNullOrWhiteSpace(fileInfo.FileDescription))
                    {
                        procDescription = fileInfo.FileDescription;
                    }
                    else if (PrivilegeManager.HasPrivilege(SE_PRIVILEGE.SeDebugPrivilege) && ProcessVersionInfo.GetVersionInfo(process, commandLine[0]) is ProcessVersionInfo procInfo && !string.IsNullOrWhiteSpace(procInfo.FileDescription))
                    {
                        procDescription = procInfo.FileDescription!;
                    }
                    else
                    {
                        procDescription = process.ProcessName;
                    }

                    // Store the process information.
                    RunningProcess runningProcess = new(process, procDescription, commandLine[0], commandLine.Length > 1 ? commandLine.Skip(1) : null);
                    if ((null == processDefinition.Filter) || processDefinition.Filter(runningProcess))
                    {
                        runningProcesses.Add(runningProcess);
                    }
                }
            }

            // Return an ordered list of running processes to the caller.
            return runningProcesses.OrderBy(runningProcess => runningProcess.Description).ToList().AsReadOnly();
        }

        /// <summary>
        /// Converts a list of command-line arguments into a single command-line string.
        /// </summary>
        /// <remarks>This method handles quoting and escaping according to standard command-line parsing
        /// rules: - Arguments containing whitespace or quotes are enclosed in quotes. - Backslashes preceding a quote
        /// are doubled to ensure correct parsing. - A closing quote followed by another quote is treated as a literal
        /// quote.</remarks>
        /// <param name="argv">A read-only list of command-line arguments to be converted.</param>
        /// <returns>A command-line string that represents the concatenated arguments, with necessary quoting and escaping
        /// applied. Returns <see langword="null"/> if the resulting command-line string is empty or consists only of
        /// whitespace.</returns>
        public static string? ArgvToCommandLine(IEnumerable<string> argv)
        {
            // Internal worker to test the argument for whitespace or quotes.
            const char Backslash = '\\'; const char Quote = '\"'; const char Space = ' ';
            static bool ContainsNoWhitespaceOrQuotes(string s)
            {
                for (int i = 0; i < s.Length; i++)
                {
                    char c = s[i];
                    if (char.IsWhiteSpace(c) || c == Quote)
                    {
                        return false;
                    }
                }
                return true;
            }

            // Build out the command line string.
            StringBuilder stringBuilder = new();
            foreach (string argument in argv.Select(static a => a.TrimRemoveNull()))
            {
                // Continue if the argument is null or empty.
                if (string.IsNullOrWhiteSpace(argument))
                {
                    continue;
                }

                // Quote the argument and escape and quotes/backslashes.
                if (!ContainsNoWhitespaceOrQuotes(argument))
                {
                    stringBuilder.Append(Quote); int idx = 0;
                    while (idx < argument.Length)
                    {
                        char c = argument[idx++];
                        if (c == Backslash)
                        {
                            int numBackSlash = 1;
                            while (idx < argument.Length && argument[idx] == Backslash)
                            {
                                idx++;
                                numBackSlash++;
                            }

                            if (idx == argument.Length)
                            {
                                // We'll emit an end quote after this so must double the number of backslashes.
                                stringBuilder.Append(Backslash, numBackSlash * 2);
                            }
                            else if (argument[idx] == Quote)
                            {
                                // Backslashes will be followed by a quote. Must double the number of backslashes.
                                stringBuilder.Append(Backslash, numBackSlash * 2 + 1);
                                stringBuilder.Append(Quote);
                                idx++;
                            }
                            else
                            {
                                // Backslash will not be followed by a quote, so emit as normal characters.
                                stringBuilder.Append(Backslash, numBackSlash);
                            }
                            continue;
                        }

                        if (c == Quote)
                        {
                            // Escape the quote so it appears as a literal. This also guarantees that we won't end up generating a closing quote followed
                            // by another quote (which parses differently pre-2008 vs. post-2008.)
                            stringBuilder.Append(Backslash);
                            stringBuilder.Append(Quote);
                            continue;
                        }
                        stringBuilder.Append(c);
                    }
                    stringBuilder.Append(Quote);
                }
                else
                {
                    // Argument can just be added.
                    stringBuilder.Append(argument);
                }
                stringBuilder.Append(Space);
            }

            // Return the built command line string.
            return stringBuilder.ToString().Trim() is string arguments && !string.IsNullOrWhiteSpace(arguments) ? arguments + '\0' : null;
        }

        /// <summary>
        /// Parses a command line string into an array of arguments.
        /// </summary>
        /// <param name="commandLine">The command line string to be parsed into arguments. Cannot be null or empty.</param>
        /// <returns>A read-only list of strings, each representing an individual argument parsed from the command line.</returns>
        /// <exception cref="ArgumentException">Thrown if the command line string cannot be parsed into arguments.</exception>
        public static IReadOnlyList<string> CommandLineToArgv(string commandLine) => !string.IsNullOrWhiteSpace(commandLine) ? new ReadOnlyCollection<string>(Shell32.CommandLineToArgv(commandLine)) : throw new ArgumentNullException("Specified CommandLine was null or empty.", nameof(commandLine));

        /// <summary>
        /// Retrieves the process identifier (PID) of the specified service.
        /// </summary>
        /// <remarks>This method queries the service control manager to obtain the process ID of the
        /// service.  Ensure that the service is running before calling this method, as it will only return a valid
        /// process ID for active services.</remarks>
        /// <param name="service">The <see cref="ServiceController"/> representing the service for which to obtain the process ID.</param>
        /// <returns>The process ID of the specified service.</returns>
        public static uint GetServiceProcessId(ServiceController service)
        {
            using (var scm = AdvApi32.OpenSCManager(null, null, SC_MANAGER_ACCESS.SC_MANAGER_CONNECT))
            using (var svc = AdvApi32.OpenService(scm, service.ServiceName, SERVICE_ACCESS_RIGHTS.SERVICE_QUERY_STATUS))
            {
                Span<byte> buffer = stackalloc byte[Marshal.SizeOf<SERVICE_STATUS_PROCESS>()];
                AdvApi32.QueryServiceStatusEx(svc, SC_STATUS_TYPE.SC_STATUS_PROCESS_INFO, buffer, out _);
                if (MemoryMarshal.Read<SERVICE_STATUS_PROCESS>(buffer).dwProcessId is uint dwProcessId && dwProcessId == 0)
                {
                    throw new InvalidOperationException($"The service [{service.ServiceName}] is not running or does not have a valid process ID.");
                }
                return dwProcessId;
            }
        }
    }
}
