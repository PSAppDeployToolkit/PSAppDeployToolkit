using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using PSADT.UserInterface.ProcessManagement;
using PSADT.UserInterface.LibraryInterfaces;
using PSADT.UserInterface.Utilities;

namespace PSADT.UserInterface.Processes
{
    public sealed class RunningProcessService
    {
        public static ReadOnlyCollection<RunningProcess> GetRunningProcesses(ProcessDefinition[] processDefinitions)
        {
            // Set up some caches for performance.
            ReadOnlyDictionary<string, string>? ntPathLookupTable = FileSystemUtilities.GetNtPathLookupTable();
            Dictionary<Process, string[]> processCommandLines = [];

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
                var imagePath = ProcessUtilities.GetProcessImageName(process.Id, ntPathLookupTable);

                // Get the command line for the process. If this fails due to lack
                // of privileges, we simply just return the image path and that's it.
                try
                {
                    commandLine = Shell32.CommandLineToArgv(ProcessUtilities.GetProcessCommandLine(process.Id));
                    commandLine[0] = imagePath;
                }
                catch
                {
                    commandLine = [imagePath];
                }
                processCommandLines[process] = commandLine;
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
                    // Continue if the process has since terminated.
                    if (process.HasExited)
                    {
                        continue;
                    }

                    // Continue if this isn't our process or it's ended since we cached it.
                    var commandLine = GetCommandLine(process);
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
                    var procInfo = FileVersionInfo.GetVersionInfo(commandLine[0]); string procDescription;
                    if (!string.IsNullOrWhiteSpace(processDefinition.Description))
                    {
                        procDescription = processDefinition.Description!;
                    }
                    else if (!string.IsNullOrWhiteSpace(procInfo.FileDescription))
                    {
                        procDescription = procInfo.FileDescription;
                    }
                    else
                    {
                        procDescription = process.ProcessName;
                    }

                    // Store the process information.
                    var runningProcess = new RunningProcess(process, procDescription, commandLine[0], commandLine.Length > 1 ? string.Join(" ", commandLine.Skip(1)) : null);
                    if ((null == processDefinition.Filter) || processDefinition.Filter(runningProcess))
                    {
                        runningProcesses.Add(runningProcess);
                    }
                }
            }

            // Return a list of running processes to the caller.
            return runningProcesses.OrderBy(runningProc => runningProc.Description).ToList().AsReadOnly();
        }
    }
}
