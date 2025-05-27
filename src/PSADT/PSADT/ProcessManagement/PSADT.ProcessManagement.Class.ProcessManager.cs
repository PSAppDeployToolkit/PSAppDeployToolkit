using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Management.Automation.Runspaces;
using PSADT.Module;
using PSADT.LibraryInterfaces;
using PSADT.Utilities;

namespace PSADT.ProcessManagement
{
    /// <summary>
    /// Provides functionality to manage and retrieve information about running processes.
    /// </summary>
    /// <remarks>The <see cref="ProcessManager"/> class offers methods to identify and retrieve details about processes running on the system. It allows filtering and matching processes based on user-defined criteria, such as process names, command-line arguments, and custom filters. Processes that cannot be accessed due to insufficient privileges are automatically skipped.</remarks>
    public static class ProcessManager
    {
        /// <summary>
        /// Retrieves a list of running processes that match the specified process definitions.
        /// </summary>
        /// <remarks>This method identifies running processes by comparing their names and command-line arguments against the provided process definitions. If a process definition includes a filter, only processes that satisfy the filter are included in the result. Processes that cannot be accessed due to insufficient privileges are skipped.</remarks>
        /// <param name="processDefinitions">An array of <see cref="ProcessDefinition"/> objects that define the processes to search for. Each definition specifies the name, optional description, and an optional filter to match processes.</param>
        /// <returns>A read-only list of <see cref="RunningProcess"/> objects representing the processes that match the given definitions. The list is ordered by the description of the running processes.</returns>
        public static IReadOnlyList<RunningProcess> GetRunningProcesses(ProcessDefinition[] processDefinitions)
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
            string[] GetCommandLine(Process process)
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
                        commandLine = GetCommandLine(process);
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
                    else
                    {
                        var procInfo = FileVersionInfo.GetVersionInfo(commandLine[0]);
                        if (!string.IsNullOrWhiteSpace(procInfo.FileDescription))
                        {
                            procDescription = procInfo.FileDescription;
                        }
                        else
                        {
                            procDescription = process.ProcessName;
                        }
                    }

                    // Store the process information.
                    var runningProcess = new RunningProcess(process, procDescription, commandLine[0], commandLine.Length > 1 ? string.Join(" ", commandLine.Skip(1)) : null);
                    if ((null == processDefinition.Filter) || processDefinition.Filter(runningProcess))
                    {
                        runningProcesses.Add(runningProcess);
                    }
                }
            }

            // Return an ordered list of running processes to the caller.
            return runningProcesses.OrderBy(runningProcess => runningProcess.Description).ToList().AsReadOnly();
        }
    }
}
