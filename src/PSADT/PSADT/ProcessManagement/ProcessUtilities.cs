using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using Microsoft.Win32.SafeHandles;
using PSADT.Extensions;
using PSADT.LibraryInterfaces;
using Windows.Wdk.System.Threading;
using Windows.Win32;
using Windows.Win32.Foundation;
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
