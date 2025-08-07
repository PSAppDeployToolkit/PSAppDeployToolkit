using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation.Runspaces;
using System.Runtime.InteropServices;
using System.ServiceProcess;
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

                // Get the command line for the process. Failing that, just get the image path.
                string? imageName = null;
                try
                {
                    commandLine = CommandLineUtilities.CommandLineToArgumentList(ProcessTools.GetProcessCommandLine(process.Id)).ToArray();
                }
                catch
                {
                    try
                    {
                        commandLine = [imageName = ProcessTools.GetProcessImageName(process.Id, ntPathLookupTable)];
                    }
                    catch
                    {
                        if (!process.HasExited)
                        {
                            throw;
                        }
                        return [];
                    }
                }

                // If the command line process path isn't fully qualified, try to resolve it using the process image name.
                if (!Path.IsPathRooted(commandLine[0]) && null == imageName)
                {
                    commandLine[0] = ProcessTools.GetProcessImageName(process.Id, ntPathLookupTable);
                }

                // Cache and return the command line.
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
                    // Skip this process if it doesn't match the name.
                    if (!Path.IsPathRooted(processDefinition.Name) && !process.ProcessName.Equals(processDefinition.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    // Skip this process if it's not running anymore.
                    if (process.HasExited)
                    {
                        continue;
                    }

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

                    // If we couldn't get the command line, skip this process.
                    if (commandLine.Length == 0)
                    {
                        continue;
                    }

                    // Continue if this isn't our process or it's ended since we cached it.
                    if (Path.IsPathRooted(processDefinition.Name) && !commandLine[0].Equals(processDefinition.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
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
                    else if (PrivilegeManager.HasPrivilege(SE_PRIVILEGE.SeDebugPrivilege) && !process.HasExited && ProcessVersionInfo.GetVersionInfo(process, commandLine[0]) is ProcessVersionInfo procInfo && !string.IsNullOrWhiteSpace(procInfo.FileDescription))
                    {
                        procDescription = procInfo.FileDescription!;
                    }
                    else
                    {
                        procDescription = process.ProcessName;
                    }

                    // Store the process information.
                    RunningProcess runningProcess = new(process, procDescription, commandLine[0], commandLine.Length > 1 ? commandLine.Skip(1) : null);
                    if (!process.HasExited && ((null == processDefinition.Filter) || processDefinition.Filter(runningProcess)))
                    {
                        runningProcesses.Add(runningProcess);
                    }
                }
            }

            // Return an ordered list of running processes to the caller.
            return runningProcesses.OrderBy(runningProcess => runningProcess.Description).ToList().AsReadOnly();
        }

        /// <summary>
        /// Retrieves the process identifier (PID) of the specified service.
        /// </summary>
        /// <remarks>This method queries the service control manager to obtain the process ID of the
        /// service. Ensure that the service is running before calling this method, as it will only return a valid
        /// process ID for active services.</remarks>
        /// <param name="service">The <see cref="ServiceController"/> representing the service for which to obtain the process ID.</param>
        /// <returns>The process ID of the specified service.</returns>
        public static uint GetServiceProcessId(ServiceController service)
        {
            using var scm = AdvApi32.OpenSCManager(null, null, SC_MANAGER_ACCESS.SC_MANAGER_CONNECT);
            using var svc = AdvApi32.OpenService(scm, service.ServiceName, SERVICE_ACCESS_RIGHTS.SERVICE_QUERY_STATUS);
            Span<byte> buffer = stackalloc byte[Marshal.SizeOf<SERVICE_STATUS_PROCESS>()];
            AdvApi32.QueryServiceStatusEx(svc, SC_STATUS_TYPE.SC_STATUS_PROCESS_INFO, buffer, out _);
            if (MemoryMarshal.Read<SERVICE_STATUS_PROCESS>(buffer).dwProcessId is uint dwProcessId && dwProcessId == 0)
            {
                throw new InvalidOperationException($"The service [{service.ServiceName}] is not running or does not have a valid process ID.");
            }
            return dwProcessId;
        }

        /// <summary>
        /// Retrieves the parent process of the specified process.
        /// </summary>
        /// <remarks>This method uses system-level information to identify the parent process. The caller
        /// must ensure that the provided process is valid and accessible.</remarks>
        /// <param name="proc">The process for which to retrieve the parent process. Must not be null.</param>
        /// <returns>A <see cref="System.Diagnostics.Process"/> object representing the parent process of the specified process.</returns>
        public static Process GetParentProcess(Process proc)
        {
            NtDll.NtQueryInformationProcess(proc.Handle, out var pbi);
            return Process.GetProcessById((int)pbi.InheritedFromUniqueProcessId);
        }

        /// <summary>
        /// Retrieves the parent process of the current process.
        /// </summary>
        /// <remarks>The returned <see cref="Process"/> object should be disposed of by the caller when it
        /// is no longer needed.</remarks>
        /// <returns>A <see cref="Process"/> object representing the parent process of the current process.</returns>
        public static Process GetParentProcess()
        {
            using (var proc = Process.GetCurrentProcess())
            {
                return GetParentProcess(proc);
            }
        }

        /// <summary>
        /// Retrieves a list of parent processes for the current process, starting from the immediate parent and
        /// continuing up the hierarchy until no further parent processes are found.
        /// </summary>
        /// <remarks>This method iteratively determines the parent process of the current process and
        /// continues up the hierarchy until no further parent processes can be identified or a circular reference is
        /// detected.</remarks>
        /// <returns>A list of <see cref="Process"/> objects representing the parent processes of the current process. The list
        /// is ordered from the immediate parent to the top-level ancestor. If no parent processes are found, the list
        /// will be empty.</returns>
        public static IReadOnlyList<Process> GetParentProcesses()
        {
            var proc = Process.GetCurrentProcess();
            List<Process> procs = [];
            while (true)
            {
                try
                {
                    if (procs.Contains(proc = GetParentProcess(proc)))
                    {
                        break;
                    }
                    procs.Add(proc);
                }
                catch
                {
                    break;
                }
            }
            return procs.AsReadOnly();
        }
    }
}
