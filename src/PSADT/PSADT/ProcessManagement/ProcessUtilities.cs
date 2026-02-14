using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.ServiceProcess;
using Microsoft.Win32.SafeHandles;
using PSADT.Extensions;
using PSADT.FileSystem;
using PSADT.LibraryInterfaces;
using PSADT.LibraryInterfaces.Extensions;
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
        /// Retrieves the parent process of the specified process.
        /// </summary>
        /// <remarks>This method uses system-level information to identify the parent process. The caller
        /// must ensure that the provided process is valid and accessible.</remarks>
        /// <param name="process">The process for which to retrieve the parent process. Must not be null.</param>
        /// <returns>A <see cref="Process"/> object representing the parent process of the specified process.</returns>
        public static Process GetParentProcess(Process process)
        {
            if (process is null)
            {
                throw new ArgumentNullException(nameof(process), "Process cannot be null.");
            }
            using SafeFileHandle hProcess = Kernel32.OpenProcess(PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION, false, (uint)process.Id);
            return GetParentProcess(hProcess);
        }

        /// <summary>
        /// Retrieves the parent process of the specified process by its process identifier.
        /// </summary>
        /// <param name="processId">The identifier of the process whose parent process is to be retrieved. Must correspond to a running process.</param>
        /// <returns>A <see cref="Process"/> object representing the parent process of the specified process. Returns <c>null</c>
        /// if the parent process cannot be determined.</returns>
        public static Process GetParentProcess(int processId)
        {
            using Process process = Process.GetProcessById(processId);
            return GetParentProcess(process);
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
        /// Determines whether the specified process has exited using minimal privileges.
        /// </summary>
        /// <remarks>This method opens the process with <c>PROCESS_QUERY_LIMITED_INFORMATION</c> access
        /// and checks the exit code to determine if the process is still running. This is more reliable than
        /// using <see cref="Process.HasExited"/> which relies on a cached handle that may have been opened
        /// with broader access rights that could fail on protected processes.</remarks>
        /// <param name="process">The process to check. Must not be null.</param>
        /// <returns><see langword="true"/> if the process has exited; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="process"/> is <see langword="null"/>.</exception>
        public static bool HasProcessExited(Process process)
        {
            return process is null
                ? throw new ArgumentNullException(nameof(process), "Process cannot be null.")
                : HasProcessExited((uint)process.Id);
        }

        /// <summary>
        /// Determines whether the process with the specified identifier has exited.
        /// </summary>
        /// <remarks>This method checks the exit status of the process identified by the given process ID.
        /// It is important to ensure that the process ID is valid before calling this method to avoid unexpected
        /// results.</remarks>
        /// <param name="processId">The identifier of the process to check. Must be a valid process ID.</param>
        /// <returns>true if the process has exited; otherwise, false.</returns>
        public static bool HasProcessExited(int processId)
        {
            return HasProcessExited((uint)processId);
        }

        /// <summary>
        /// Retrieves the security identifier (SID) of the user associated with the specified process.
        /// </summary>
        /// <remarks>This method opens the process with <c>PROCESS_QUERY_LIMITED_INFORMATION</c> access
        /// and queries the process token to retrieve the user SID. This is more reliable than using
        /// <see cref="Process.SafeHandle"/> which may have been opened with broader access rights that
        /// could fail on protected processes.</remarks>
        /// <param name="process">The process for which to retrieve the SID. Must not be null.</param>
        /// <returns>A <see cref="SecurityIdentifier"/> representing the user SID of the process owner.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="process"/> is <see langword="null"/>.</exception>
        public static SecurityIdentifier GetProcessSid(Process process)
        {
            return process is null
                ? throw new ArgumentNullException(nameof(process), "Process cannot be null.")
                : GetProcessSid((uint)process.Id);
        }

        /// <summary>
        /// Retrieves the security identifier (SID) of the user associated with the specified process.
        /// </summary>
        /// <remarks>This method opens the process with <c>PROCESS_QUERY_LIMITED_INFORMATION</c> access
        /// and queries the process token to retrieve the user SID.</remarks>
        /// <param name="processId">The identifier of the process for which to retrieve the SID. Must be a valid process ID.</param>
        /// <returns>A <see cref="SecurityIdentifier"/> representing the user SID of the process owner.</returns>
        public static SecurityIdentifier GetProcessSid(int processId)
        {
            return GetProcessSid((uint)processId);
        }

        /// <summary>
        /// Retrieves the full command-line string used to start the specified process.
        /// </summary>
        /// <remarks>This method requires that the caller has sufficient permissions to query information
        /// about the target process. If the process has already exited or access is denied, the returned string may be
        /// empty.</remarks>
        /// <param name="process">The process for which to obtain the command-line arguments. Must not be null.</param>
        /// <returns>A string containing the complete command-line used to launch the specified process. Returns an empty string
        /// if the command-line cannot be retrieved.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="process"/> is <see langword="null"/>.</exception>
        public static string GetProcessCommandLine(Process process)
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
            ref readonly UNICODE_STRING unicodeString = ref buffer.AsReadOnlyStructure<UNICODE_STRING>();
            return unicodeString.ToManagedString().RemoveNull();
        }

        /// <summary>
        /// Retrieves the command-line string used to start the process with the specified process ID.
        /// </summary>
        /// <remarks>This method may require elevated permissions to access information about certain
        /// processes. If the process has already exited or access is denied, the result may be null.</remarks>
        /// <param name="processId">The unique identifier of the process whose command-line arguments are to be retrieved. Must refer to a
        /// currently running process.</param>
        /// <returns>A string containing the full command-line used to start the specified process, or null if the command-line
        /// cannot be determined.</returns>
        public static string GetProcessCommandLine(int processId)
        {
            using Process process = Process.GetProcessById(processId);
            return GetProcessCommandLine(process);
        }

        /// <summary>
        /// Retrieves the full file system path of the executable image for the specified process.
        /// </summary>
        /// <param name="process">The process for which to obtain the image file path. Must not be null.</param>
        /// <returns>A string containing the full path to the process's executable image. Returns an empty string if the image
        /// name cannot be determined.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="process"/> is null.</exception>
        public static string GetProcessImageName(Process process)
        {
            return process is null ? throw new ArgumentNullException(nameof(process), "Process cannot be null.") : GetProcessImageName((uint)process.Id);
        }

        /// <summary>
        /// Retrieves the file name of the main executable module for the process specified by its identifier.
        /// </summary>
        /// <remarks>If the process has already exited or the identifier does not correspond to an
        /// existing process, an exception will be thrown. This method accesses process information and may require
        /// appropriate permissions.</remarks>
        /// <param name="processId">The unique identifier of the process whose image file name is to be retrieved. Must refer to a running
        /// process.</param>
        /// <returns>A string containing the full path to the executable file of the specified process.</returns>
        public static string GetProcessImageName(int processId)
        {
            return GetProcessImageName((uint)processId);
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
        internal static Process GetParentProcess(SafeHandle hProcess)
        {
            Span<byte> buffer = stackalloc byte[Marshal.SizeOf<PROCESS_BASIC_INFORMATION>()];
            _ = NtDll.NtQueryInformationProcess(hProcess, PROCESSINFOCLASS.ProcessBasicInformation, buffer, out _);
            ref readonly PROCESS_BASIC_INFORMATION pbi = ref buffer.AsReadOnlyStructure<PROCESS_BASIC_INFORMATION>();
            return Process.GetProcessById((int)pbi.InheritedFromUniqueProcessId);
        }

        /// <summary>
        /// Determines whether the process with the specified identifier has exited using minimal privileges.
        /// </summary>
        /// <remarks>This method opens the process with <c>PROCESS_QUERY_LIMITED_INFORMATION</c> access
        /// and checks the exit code to determine if the process is still running. A process is considered
        /// to have exited if <c>GetExitCodeProcess</c> returns an exit code other than <c>STATUS_PENDING</c> (259).</remarks>
        /// <param name="processId">The unique identifier of the process to check.</param>
        /// <returns><see langword="true"/> if the process has exited or cannot be opened; otherwise, <see langword="false"/>.</returns>
        internal static bool HasProcessExited(uint processId)
        {
            try
            {
                using SafeFileHandle hProcess = Kernel32.OpenProcess(PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION, false, processId);
                return Kernel32.GetExitCodeProcess(hProcess, out uint exitCode) && exitCode != NTSTATUS.STATUS_PENDING;
            }
            catch
            {
                return true;
                throw;
            }
        }

        /// <summary>
        /// Retrieves the security identifier (SID) of the user associated with the specified process.
        /// </summary>
        /// <remarks>This method opens the process with <c>PROCESS_QUERY_LIMITED_INFORMATION</c> access
        /// and queries the process token with <c>TOKEN_QUERY</c> to retrieve the user SID.</remarks>
        /// <param name="processId">The unique identifier of the process for which to retrieve the SID.</param>
        /// <returns>A <see cref="SecurityIdentifier"/> representing the user SID of the process owner.</returns>
        internal static SecurityIdentifier GetProcessSid(uint processId)
        {
            using SafeFileHandle hProcess = Kernel32.OpenProcess(PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION, false, processId);
            _ = AdvApi32.OpenProcessToken(hProcess, TOKEN_ACCESS_MASK.TOKEN_QUERY, out SafeFileHandle hToken);
            using (hToken)
            {
                return TokenUtilities.GetTokenSid(hToken);
            }
        }

        /// <summary>
        /// Retrieves the full image file name of a process specified by its process identifier (PID), using multiple
        /// fallback methods to maximize compatibility across Windows versions and process types.
        /// </summary>
        /// <remarks>This method attempts several approaches to obtain the process image name, including
        /// kernel and user-mode APIs, to ensure compatibility with different Windows versions and process
        /// architectures. The returned path may be in NT device format or DOS drive letter format, depending on which
        /// method succeeds. Callers should be prepared to handle either format.</remarks>
        /// <param name="processId">The identifier of the process whose image file name is to be retrieved.</param>
        /// <param name="ntPathLookupTable">A read-only dictionary used to translate NT device paths to DOS drive letter paths, if applicable. Can be
        /// empty if no translation is required.</param>
        /// <returns>A string containing the full path to the process's executable image. The path format may vary depending on
        /// the method used and system configuration.</returns>
        /// <exception cref="AggregateException">Thrown if all available methods for retrieving the process image name fail. The exception contains details
        /// of each failure encountered during the retrieval attempts.</exception>
        internal static string GetProcessImageName(uint processId, ReadOnlyDictionary<string, string>? ntPathLookupTable = null)
        {
            // Get the process image name via a waterfall approach.
            ntPathLookupTable ??= FileSystemUtilities.MakeNtPathLookupTable();
            try
            {
                // Attempt to get the ImageName via a kernel API first as it's reliable for native processes.
                return QuerySystemProcessIdInformationImageName(processId, ntPathLookupTable);
            }
            catch (Exception ex1) when (ex1.Message is not null)
            {
                // The kernel API call failed. This can occur when the caller is a 32-bit process on a 64-bit system, etc.
                try
                {
                    // Open a handle to the target process. If this fails, something is seriously wrong and we cannot continue.
                    using SafeFileHandle hProcess = Kernel32.OpenProcess(PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION, false, processId);
                    try
                    {
                        // QueryFullProcessImageName is the standard API for this purpose.
                        return QueryFullProcessImageName(hProcess);
                    }
                    catch (Exception ex3) when (ex3.Message is not null)
                    {
                        // That failed. Fall back to the Windows XP-era API.
                        try
                        {
                            // Unlike the above, this provides the path in NT device format.
                            return GetProcessImageFileName(hProcess, ntPathLookupTable);
                        }
                        catch (Exception ex4) when (ex4.Message is not null)
                        {
                            // That failed too. Go back down to the kernel API level and see how we go.
                            try
                            {
                                // This leverages the documented ProcessImageFileNameWin32 info class.
                                return QueryProcessImageFileNameWin32(hProcess);

                            }
                            catch (Exception ex5) when (ex5.Message is not null)
                            {
                                // The Win32 API call failed. Try the NT API directly as the last resort.
                                try
                                {
                                    // The NT device path will get translated internally for us.
                                    return QueryProcessImageFileName(hProcess, ntPathLookupTable);
                                }
                                catch (Exception ex6) when (ex6.Message is not null)
                                {
                                    throw new AggregateException($"Failed to retrieve the process image name for process ID [{processId}] via all available methods.", ex1, ex3, ex4, ex5, ex6);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex2) when (ex2.Message is not null)
                {
                    throw new AggregateException($"Failed to open process ID [{processId}] for querying the image name after the kernel API call failed.", ex1, ex2);
                }
            }
        }

        /// <summary>
        /// Retrieves the full image file name of a process specified by its process identifier (PID), using multiple
        /// fallback methods to maximize compatibility across Windows versions and process types.
        /// </summary>
        /// <remarks>This method attempts several approaches to obtain the process image name, including
        /// kernel and user-mode APIs, to ensure compatibility with different Windows versions and process
        /// architectures. The returned path may be in NT device format or DOS drive letter format, depending on which
        /// method succeeds. Callers should be prepared to handle either format.</remarks>
        /// <param name="processId">The identifier of the process whose image file name is to be retrieved.</param>
        /// <param name="ntPathLookupTable">A read-only dictionary used to translate NT device paths to DOS drive letter paths, if applicable. Can be
        /// empty if no translation is required.</param>
        /// <returns>A string containing the full path to the process's executable image. The path format may vary depending on
        /// the method used and system configuration.</returns>
        /// <exception cref="AggregateException">Thrown if all available methods for retrieving the process image name fail. The exception contains details
        /// of each failure encountered during the retrieval attempts.</exception>
        internal static string GetProcessImageName(int processId, ReadOnlyDictionary<string, string>? ntPathLookupTable)
        {
            return GetProcessImageName((uint)processId, ntPathLookupTable);
        }

        /// <summary>
        /// Retrieves the full path of the executable file for the specified process.
        /// </summary>
        /// <param name="hProcess">A handle to the process. The handle must have the PROCESS_QUERY_LIMITED_INFORMATION access right.</param>
        /// <returns>A string containing the full path to the executable file of the process.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the process image name cannot be retrieved or the result is null or empty.</exception>
        internal static string QueryFullProcessImageName(SafeHandle hProcess)
        {
            Span<char> buffer = stackalloc char[1024];
            _ = Kernel32.QueryFullProcessImageName(hProcess, PROCESS_NAME_FORMAT.PROCESS_NAME_WIN32, buffer, out uint requiredLength);
            string result = buffer.Slice(0, (int)requiredLength).ToString().TrimRemoveNull();
            return string.IsNullOrWhiteSpace(result)
                ? throw new InvalidOperationException("The QueryFullProcessImageName() call returned a null or empty result.")
                : result;
        }

        /// <summary>
        /// Retrieves the full Win32 file system path of the executable image for the specified process.
        /// </summary>
        /// <remarks>The returned path is translated from the native NT device path to a Win32 file system
        /// path using the provided lookup table. This method does not validate whether the process is still running or
        /// whether the returned path points to an existing file.</remarks>
        /// <param name="hProcess">A handle to the process whose image file name is to be retrieved. The handle must have the required access
        /// rights to query information about the process.</param>
        /// <param name="ntPathLookupTable">A read-only dictionary used to translate NT device paths to Win32 file system paths. The method uses this
        /// table to convert the native NT path returned by the system to a standard Win32 path.</param>
        /// <returns>A string containing the full Win32 file system path of the process's executable image.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the underlying system call does not return a valid image file name.</exception>
        internal static string GetProcessImageFileName(SafeHandle hProcess, ReadOnlyDictionary<string, string> ntPathLookupTable)
        {
            Span<char> buffer = stackalloc char[1024];
            uint requiredLength = PsApi.GetProcessImageFileName(hProcess, buffer);
            string result = buffer.Slice(0, (int)requiredLength).ToString().TrimRemoveNull();
            return string.IsNullOrWhiteSpace(result)
                ? throw new InvalidOperationException("The GetProcessImageFileName() call returned a null or empty result.")
                : TranslateNtPathToWin32Path(result, ntPathLookupTable);
        }

        /// <summary>
        /// Retrieves the Win32 path of the executable image for the specified process.
        /// </summary>
        /// <param name="hProcess">A handle to the process whose image file name is to be retrieved. The handle must have the required access
        /// rights to query information about the process.</param>
        /// <returns>A string containing the Win32 path of the process's executable image, or null if the path cannot be
        /// determined.</returns>
        internal static string QueryProcessImageFileNameWin32(SafeHandle hProcess)
        {
            return QueryProcessImageFileNameCommon(hProcess, PROCESSINFOCLASS.ProcessImageFileNameWin32);
        }

        /// <summary>
        /// Retrieves the full Win32 file system path of the executable image for the specified process.
        /// </summary>
        /// <remarks>If the process is running under a different user context or with restricted
        /// permissions, the returned path may be inaccessible or incomplete depending on the caller's
        /// privileges.</remarks>
        /// <param name="hProcess">A handle to the process whose image file name is to be queried. The handle must have the required access
        /// rights to query process information.</param>
        /// <param name="ntPathLookupTable">A read-only dictionary used to map NT device paths to Win32 file system paths. This table is applied to
        /// translate the native path format returned by the system.</param>
        /// <returns>A string containing the full Win32 path to the executable image of the specified process.</returns>
        internal static string QueryProcessImageFileName(SafeHandle hProcess, ReadOnlyDictionary<string, string> ntPathLookupTable)
        {
            return TranslateNtPathToWin32Path(QueryProcessImageFileNameCommon(hProcess, PROCESSINFOCLASS.ProcessImageFileName), ntPathLookupTable);
        }

        /// <summary>
        /// Retrieves the image file path of the process associated with the specified process ID, returning the path in
        /// Win32 format.
        /// </summary>
        /// <remarks>This method uses the NtQuerySystemInformation API with the SystemProcessIdInformation
        /// class to obtain the process image path. The returned path is translated from the NT device path to a Win32
        /// path using the provided lookup table. This method is not supported when called from a 32-bit process on a
        /// 64-bit system.</remarks>
        /// <param name="processId">The identifier of the process whose image file path is to be retrieved.</param>
        /// <param name="ntPathLookupTable">A read-only dictionary used to translate NT device paths to Win32 file system paths.</param>
        /// <returns>The Win32-formatted image file path of the specified process.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the method is called from a 32-bit process on a 64-bit operating system, if the image name query
        /// returns a null or empty result, or if the retrieved image name is not a valid NT path.</exception>
        internal static string QuerySystemProcessIdInformationImageName(uint processId, ReadOnlyDictionary<string, string> ntPathLookupTable)
        {
            // Throw if we're a 32-bit process on a 64-bit system as we cannot query the image name in that case.
            if (RuntimeInformation.ProcessArchitecture != RuntimeInformation.OSArchitecture)
            {
                throw new InvalidOperationException("A 32-bit process cannot call NtQuerySystemInformation() with the [SystemProcessIdInformation] information class on a 64-bit system.");
            }

            // Set up initial buffer that we need to query the process information. We must clear the buffer ourselves as stackalloc buffers are undefined.
            Span<byte> processIdInfoPtr = stackalloc byte[NtDll.SystemInfoClassSizes[SYSTEM_INFORMATION_CLASS.SystemProcessIdInformation]]; processIdInfoPtr.Clear();
            ref SYSTEM_PROCESS_ID_INFORMATION processIdInfo = ref Unsafe.As<byte, SYSTEM_PROCESS_ID_INFORMATION>(ref MemoryMarshal.GetReference(processIdInfoPtr));
            processIdInfo.ProcessId = (nint)processId;

            // Perform initial query so we can get the required ImageName buffer length.
            _ = NtDll.NtQuerySystemInformation(SYSTEM_INFORMATION_CLASS.SystemProcessIdInformation, processIdInfoPtr, out _, retrievingLength: true);
            Span<char> imageNamePtr = stackalloc char[((processIdInfo.ImageName.MaximumLength + 2) / sizeof(char)) + 1]; imageNamePtr.Clear();

            // Assign the ImageName buffer and perform the query again.
            string imageName;
            unsafe
            {
                fixed (char* pImageName = imageNamePtr)
                {
                    processIdInfo.ImageName = new() { Length = 0, MaximumLength = checked((ushort)(imageNamePtr.Length * 2)), Buffer = pImageName };
                    _ = NtDll.NtQuerySystemInformation(SYSTEM_INFORMATION_CLASS.SystemProcessIdInformation, processIdInfoPtr, out _);
                    imageName = processIdInfo.ImageName.ToManagedString().RemoveNull();
                }
            }

            // Throw if the value doesn't start with \Device\ (indicating an NT path).
            if (!imageName.StartsWith(@"\Device\", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"The image name [{imageName}] for process ID [{processId}] is not a valid NT path.");
            }

            // Return the path in the Win32 path format as the NT device path is inappropriate for most uses.
            return TranslateNtPathToWin32Path(imageName, ntPathLookupTable);
        }

        /// <summary>
        /// Translates an NT device path to its corresponding Win32 path using the specified lookup table.
        /// </summary>
        /// <remarks>This method is intended for scenarios where conversion between NT device paths and
        /// standard Win32 paths is required, such as when working with low-level system APIs or logs. The lookup table
        /// must contain all NT device names that may appear in the input paths.</remarks>
        /// <param name="ntPath">The NT device path to translate. This should be a fully qualified NT path, such as
        /// "\Device\HarddiskVolume1\Windows\System32".</param>
        /// <param name="ntPathLookupTable">A read-only dictionary that maps NT device names (e.g., "\Device\HarddiskVolume1") to their corresponding
        /// Win32 drive letters (e.g., "C:").</param>
        /// <returns>A string containing the Win32 path equivalent of the specified NT device path.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the NT device name derived from the specified path does not exist in the lookup table.</exception>
        internal static string TranslateNtPathToWin32Path(string ntPath, ReadOnlyDictionary<string, string> ntPathLookupTable)
        {
            string ntDeviceName = $@"\{string.Join(@"\", ntPath.Split(['\\'], StringSplitOptions.RemoveEmptyEntries).Take(2))}";
            return !ntPathLookupTable.TryGetValue(ntDeviceName, out string? driveLetter)
                ? throw new InvalidOperationException($"Unable to find drive letter for NT device [{ntDeviceName}], derived from NT path [{ntPath}].")
                : ntPath.Replace(ntDeviceName, driveLetter);
        }

        /// <summary>
        /// Retrieves the process identifier (PID) of the specified service.
        /// </summary>
        /// <remarks>This method queries the service control manager to obtain the process ID of the
        /// service. Ensure that the service is running before calling this method, as it will only return a valid
        /// process ID for active services.</remarks>
        /// <param name="service">The <see cref="ServiceController"/> representing the service for which to obtain the process ID.</param>
        /// <returns>The process ID of the specified service.</returns>
        internal static uint GetServiceProcessId(ServiceController service)
        {
            if (service is null)
            {
                throw new ArgumentNullException(nameof(service), "Service cannot be null.");
            }
            using CloseServiceHandleSafeHandle scm = AdvApi32.OpenSCManager(SC_MANAGER_ACCESS.SC_MANAGER_CONNECT);
            using CloseServiceHandleSafeHandle svc = AdvApi32.OpenService(scm, service.ServiceName, SERVICE_ACCESS_RIGHTS.SERVICE_QUERY_STATUS);
            Span<byte> buffer = stackalloc byte[Marshal.SizeOf<SERVICE_STATUS_PROCESS>()];
            _ = AdvApi32.QueryServiceStatusEx(svc, SC_STATUS_TYPE.SC_STATUS_PROCESS_INFO, buffer, out _);
            ref readonly SERVICE_STATUS_PROCESS serviceStatus = ref buffer.AsReadOnlyStructure<SERVICE_STATUS_PROCESS>();
            return serviceStatus.dwProcessId is uint dwProcessId && dwProcessId == 0
                ? throw new InvalidOperationException($"The service [{service.ServiceName}] is not running or does not have a valid process ID.")
                : dwProcessId;
        }

        /// <summary>
        /// Retrieves the image file name of a process using the specified process information class.
        /// </summary>
        /// <param name="hProcess">A handle to the process for which to query the image file name. The handle must have appropriate access
        /// rights for the requested information.</param>
        /// <param name="processInfoClass">The type of process information to query, specifying how the image file name should be retrieved.</param>
        /// <returns>A string containing the image file name of the specified process.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the query returns a null or empty result for the specified process information class.</exception>
        private static string QueryProcessImageFileNameCommon(SafeHandle hProcess, PROCESSINFOCLASS processInfoClass)
        {
            // Determine required buffer size.
            _ = NtDll.NtQueryInformationProcess(hProcess, processInfoClass, null, out uint requiredLength);
            Span<byte> buffer = stackalloc byte[(int)requiredLength];

            // Perform the query.
            _ = NtDll.NtQueryInformationProcess(hProcess, processInfoClass, buffer, out _);
            ref readonly UNICODE_STRING unicodeString = ref buffer.AsReadOnlyStructure<UNICODE_STRING>();
            return unicodeString.ToManagedString().RemoveNull();
        }
    }
}
