using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.ServiceProcess;
using Microsoft.Win32.SafeHandles;
using PSADT.Extensions;
using PSADT.FileSystem;
using PSADT.LibraryInterfaces;
using PSADT.Security;
using Windows.Wdk.System.Threading;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.System.Services;
using Windows.Win32.System.Threading;

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
                        return GetProcessImageName(process, ntPathLookupTable);
                    }
                    return GetProcessImageName(process, ntPathLookupTable);
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
                    commandLine = GetProcessCommandLine(process);
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
            List<RunningProcess> runningProcesses = [];
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
                    RunningProcess runningProcess = new(process, procDescription, argv[0], argv.Skip(1), username);
                    if (!process.HasExited && ((processDefinition.Filter is null) || processDefinition.Filter(runningProcess)))
                    {
                        runningProcesses.Add(runningProcess);
                    }
                }
            }

            // Return an ordered list of running processes to the caller.
            return new ReadOnlyCollection<RunningProcess>([.. runningProcesses.OrderBy(runningProcess => runningProcess.Description)]);
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
            if (service is null)
            {
                throw new ArgumentNullException(nameof(service), "Service cannot be null.");
            }
            using CloseServiceHandleSafeHandle scm = AdvApi32.OpenSCManager(SC_MANAGER_ACCESS.SC_MANAGER_CONNECT);
            using CloseServiceHandleSafeHandle svc = AdvApi32.OpenService(scm, service.ServiceName, SERVICE_ACCESS_RIGHTS.SERVICE_QUERY_STATUS);
            Span<byte> buffer = stackalloc byte[Marshal.SizeOf<SERVICE_STATUS_PROCESS>()];
            _ = AdvApi32.QueryServiceStatusEx(svc, SC_STATUS_TYPE.SC_STATUS_PROCESS_INFO, buffer, out _);
            ref SERVICE_STATUS_PROCESS serviceStatus = ref buffer.AsStructure<SERVICE_STATUS_PROCESS>();
            return serviceStatus.dwProcessId is uint dwProcessId && dwProcessId == 0
                ? throw new InvalidOperationException($"The service [{service.ServiceName}] is not running or does not have a valid process ID.")
                : dwProcessId;
        }

        /// <summary>
        /// Retrieves the parent process of the specified process handle.
        /// </summary>
        /// <remarks>This method uses the NtQueryInformationProcess function to retrieve information about
        /// the specified process. Ensure that the provided process handle is valid and has the required
        /// permissions.</remarks>
        /// <param name="hProcess">A <see cref="SafeHandle"/> representing the handle to the process whose parent process is to be retrieved.
        /// The handle must have the necessary access rights to query process information.</param>
        /// <returns>A <see cref="Process"/> object representing the parent process of the specified process.</returns>
        public static Process GetParentProcess(SafeHandle hProcess)
        {
            Span<byte> buffer = stackalloc byte[Marshal.SizeOf<PROCESS_BASIC_INFORMATION>()];
            _ = NtDll.NtQueryInformationProcess(hProcess, PROCESSINFOCLASS.ProcessBasicInformation, buffer, out _);
            ref PROCESS_BASIC_INFORMATION pbi = ref buffer.AsStructure<PROCESS_BASIC_INFORMATION>();
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
            using SafeProcessHandle hProcess = Kernel32.GetCurrentProcess();
            return GetParentProcess(hProcess);
        }

        /// <summary>
        /// Retrieves the parent process of the specified process.
        /// </summary>
        /// <remarks>This method uses system-level information to identify the parent process. The caller
        /// must ensure that the provided process is valid and accessible.</remarks>
        /// <param name="proc">The process for which to retrieve the parent process. Must not be null.</param>
        /// <returns>A <see cref="Process"/> object representing the parent process of the specified process.</returns>
        public static Process GetParentProcess(Process proc)
        {
            // We don't own the process, so don't dispose of its SafeHande as .NET caches it...
            return proc is null ? throw new ArgumentNullException(nameof(proc), "Process cannot be null.") : GetParentProcess(proc.SafeHandle);
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
            Process proc = Process.GetCurrentProcess();
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
                    throw;
                }
            }
            return procs.AsReadOnly();
        }

        /// <summary>
        /// Retrieves the command line arguments of a process given its process ID.
        /// </summary>
        /// <param name="process"></param>
        /// <returns></returns>
        internal static string GetProcessCommandLine(Process process)
        {
            // Open the process's handle with the relevant access rights and get the required length we need for the buffer.
            if (process is null)
            {
                throw new ArgumentNullException(nameof(process), "Process cannot be null.");
            }
            using SafeFileHandle hProc = Kernel32.OpenProcess(PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION, false, (uint)process.Id);
            _ = NtDll.NtQueryInformationProcess(hProc, PROCESSINFOCLASS.ProcessCommandLineInformation, null, out uint requiredLength);

            // Fill the buffer, then retrieve the actual command line string.
            Span<byte> buffer = stackalloc byte[(int)requiredLength];
            _ = NtDll.NtQueryInformationProcess(hProc, PROCESSINFOCLASS.ProcessCommandLineInformation, buffer, out _);
            ref UNICODE_STRING unicodeString = ref buffer.AsStructure<UNICODE_STRING>();
            return unicodeString.Buffer.ToString().TrimRemoveNull();
        }

        /// <summary>
        /// Retrieves the command line arguments of a process given its process ID.
        /// </summary>
        /// <param name="processId"></param>
        /// <returns></returns>
        public static string GetProcessCommandLine(int processId)
        {
            using Process process = Process.GetProcessById(processId);
            return GetProcessCommandLine(process);
        }

        /// <summary>
        /// Retrieves the image name of a process given its process ID.
        /// </summary>
        /// <param name="process"></param>
        /// <param name="ntPathLookupTable"></param>
        /// <returns></returns>
        internal static string GetProcessImageName(Process process, ReadOnlyDictionary<string, string>? ntPathLookupTable = null)
        {
            // Set up initial buffer that we need to query the process information. We must clear the buffer ourselves as stackalloc buffers are undefined.
            Span<byte> processIdInfoPtr = stackalloc byte[NtDll.SystemInfoClassSizes[SYSTEM_INFORMATION_CLASS.SystemProcessIdInformation]]; processIdInfoPtr.Clear();
            ref SYSTEM_PROCESS_ID_INFORMATION processIdInfo = ref processIdInfoPtr.AsStructure<SYSTEM_PROCESS_ID_INFORMATION>();
            processIdInfo.ProcessId = new(process.Id);

            // Perform initial query so we can reallocate with the required length.
            _ = NtDll.NtQuerySystemInformation(SYSTEM_INFORMATION_CLASS.SystemProcessIdInformation, processIdInfoPtr, out _);
            Span<byte> imageNamePtr = stackalloc byte[processIdInfo.ImageName.MaximumLength + 2]; imageNamePtr.Clear();

            // Assign the ImageName buffer and perform the query again.
            string imageName;
            unsafe
            {
                fixed (byte* pImageName = imageNamePtr)
                {
                    processIdInfo.ImageName.Buffer = (char*)pImageName;
                    _ = NtDll.NtQuerySystemInformation(SYSTEM_INFORMATION_CLASS.SystemProcessIdInformation, processIdInfoPtr, out _);
                    imageName = processIdInfo.ImageName.Buffer.ToString().TrimRemoveNull();
                    processIdInfo.ImageName.Buffer = null;
                }
            }

            // If we have a lookup table, replace the NT path with the drive letter before returning.
            if (ntPathLookupTable is not null)
            {
                string ntDeviceName = $@"\{string.Join(@"\", imageName.Split(['\\'], StringSplitOptions.RemoveEmptyEntries).Take(2))}";
                return !ntPathLookupTable.TryGetValue(ntDeviceName, out string? driveLetter)
                    ? throw new InvalidOperationException($"Unable to find drive letter for NT device [{ntDeviceName}], derived from image name [{imageName}].")
                    : imageName.Replace(ntDeviceName, driveLetter);
            }
            return imageName;
        }

        /// <summary>
        /// Retrieves the image name of a process given its process ID.
        /// </summary>
        /// <param name="processId"></param>
        /// <returns></returns>
        public static string GetProcessImageName(int processId)
        {
            using Process process = Process.GetProcessById(processId);
            return GetProcessImageName(process, null);
        }
    }
}
