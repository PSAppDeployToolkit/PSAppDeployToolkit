using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Threading;
using Microsoft.Win32.SafeHandles;
using PSADT.SafeHandles;
using PSADT.Utilities;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.Storage.FileSystem;
using Windows.Win32.System.JobObjects;
using Windows.Win32.System.LibraryLoader;
using Windows.Win32.System.Power;
using Windows.Win32.System.SystemInformation;
using Windows.Win32.System.Threading;

namespace PSADT.LibraryInterfaces
{
    /// <summary>
    /// CsWin32 P/Invoke wrappers for the kernel32.dll library.
    /// </summary>
    public static class Kernel32
    {
        /// <summary>
        /// Tests whether the current device has completed its Out-of-Box Experience (OOBE).
        /// </summary>
        /// <param name="isOOBEComplete"></param>
        /// <returns></returns>
        internal static BOOL OOBEComplete(out BOOL isOOBEComplete)
        {
            var res = PInvoke.OOBEComplete(out isOOBEComplete);
            if (!res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
        }

        /// <summary>
        /// Wrapper around LoadLibraryEx to manage error handling.
        /// </summary>
        /// <param name="lpLibFileName"></param>
        /// <param name="dwFlags"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal static FreeLibrarySafeHandle LoadLibraryEx(string lpLibFileName, LOAD_LIBRARY_FLAGS dwFlags)
        {
            var res = PInvoke.LoadLibraryEx(lpLibFileName, dwFlags);
            if (null == res || res.IsInvalid)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
        }

        /// <summary>
        /// Wrapper around GetProcAddress to manage error handling.
        /// </summary>
        /// <param name="hModule"></param>
        /// <param name="lpProcName"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal static FARPROC GetProcAddress(SafeHandle hModule, string lpProcName)
        {
            var res = PInvoke.GetProcAddress(hModule, lpProcName);
            if (res.IsNull)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
        }

        /// <summary>
        /// Wrapper around GetPrivateProfileSectionNames to manage error handling.
        /// </summary>
        /// <param name="lpReturnedString"></param>
        /// <param name="lpFileName"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        /// <exception cref="OverflowException"></exception>
        internal static uint GetPrivateProfileSectionNames(Span<char> lpReturnedString, string lpFileName)
        {
            var res = PInvoke.GetPrivateProfileSectionNames(lpReturnedString, lpFileName);
            if (res == lpReturnedString.Length - 1)
            {
                throw new OverflowException("Buffer was too small. Value was truncated.");
            }
            return res;
        }

        /// <summary>
        /// Wrapper around GetPrivateProfileSection to manage error handling.
        /// </summary>
        /// <param name="lpAppName"></param>
        /// <param name="lpReturnedString"></param>
        /// <param name="lpFileName"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        /// <exception cref="OverflowException"></exception>
        internal static uint GetPrivateProfileSection(string lpAppName, Span<char> lpReturnedString, string lpFileName)
        {
            var res = PInvoke.GetPrivateProfileSection(lpAppName, lpReturnedString, lpFileName);
            if (res == lpReturnedString.Length - 1)
            {
                throw new OverflowException("Buffer was too small. Value was truncated.");
            }
            return res;
        }

        /// <summary>
        /// Wrapper around GetPrivateProfileString to manage error handling.
        /// </summary>
        /// <param name="lpAppName"></param>
        /// <param name="lpKeyName"></param>
        /// <param name="lpDefault"></param>
        /// <param name="lpReturnedString"></param>
        /// <param name="lpFileName"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        /// <exception cref="OverflowException"></exception>
        internal static uint GetPrivateProfileString(string lpAppName, string lpKeyName, string? lpDefault, Span<char> lpReturnedString, string lpFileName)
        {
            var res = PInvoke.GetPrivateProfileString(lpAppName, lpKeyName, lpDefault, lpReturnedString, lpFileName);
            if (res == 0 && ((WIN32_ERROR)Marshal.GetLastWin32Error() is WIN32_ERROR lastWin32Error) && lastWin32Error != WIN32_ERROR.NO_ERROR)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error(lastWin32Error);
            }
            else if (res == lpReturnedString.Length - 1)
            {
                throw new OverflowException("Buffer was too small. Value was truncated.");
            }
            return res;
        }

        /// <summary>
        /// Wrapper around WritePrivateProfileSection to manage error handling.
        /// </summary>
        /// <param name="lpAppName"></param>
        /// <param name="lpString"></param>
        /// <param name="lpFileName"></param>
        /// <exception cref="Win32Exception"></exception>
        internal static BOOL WritePrivateProfileSection(string lpAppName, string? lpString, string lpFileName)
        {
            var res = PInvoke.WritePrivateProfileSection(lpAppName, lpString, lpFileName);
            if (!res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
        }

        /// <summary>
        /// Wrapper around WritePrivateProfileString to manage error handling.
        /// </summary>
        /// <param name="lpAppName"></param>
        /// <param name="lpKeyName"></param>
        /// <param name="lpString"></param>
        /// <param name="lpFileName"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal static BOOL WritePrivateProfileString(string lpAppName, string? lpKeyName, string? lpString, string lpFileName)
        {
            var res = PInvoke.WritePrivateProfileString(lpAppName, lpKeyName, lpString, lpFileName);
            if (!res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
        }

        /// <summary>
        /// Wrapper around CreateIoCompletionPort to manage error handling.
        /// </summary>
        /// <param name="FileHandle"></param>
        /// <param name="ExistingCompletionPort"></param>
        /// <param name="CompletionKey"></param>
        /// <param name="NumberOfConcurrentThreads"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal static SafeFileHandle CreateIoCompletionPort(SafeHandle FileHandle, SafeHandle ExistingCompletionPort, nuint CompletionKey, uint NumberOfConcurrentThreads)
        {
            var res = PInvoke.CreateIoCompletionPort(FileHandle, ExistingCompletionPort, CompletionKey, NumberOfConcurrentThreads);
            if (null == res || res.IsInvalid)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
        }

        /// <summary>
        /// Wrapper around CreateJobObject to manage error handling.
        /// </summary>
        /// <param name="lpJobAttributes"></param>
        /// <param name="lpName"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal static SafeFileHandle CreateJobObject(SECURITY_ATTRIBUTES? lpJobAttributes, string? lpName)
        {
            var res = PInvoke.CreateJobObject(lpJobAttributes, lpName);
            if (null == res || res.IsInvalid)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
        }

        /// <summary>
        /// Wrapper around AssignProcessToJobObject to manage error handling.
        /// </summary>
        /// <param name="hJob"></param>
        /// <param name="JobObjectInformationClass"></param>
        /// <param name="lpJobObjectInformation"></param>
        /// <param name="cbJobObjectInformationLength"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        private unsafe static BOOL SetInformationJobObject(SafeHandle hJob, JOBOBJECTINFOCLASS JobObjectInformationClass, void* lpJobObjectInformation, uint cbJobObjectInformationLength)
        {
            var res = PInvoke.SetInformationJobObject(hJob, JobObjectInformationClass, lpJobObjectInformation, cbJobObjectInformationLength);
            if (!res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
        }

        /// <summary>
        /// Wrapper around SetInformationJobObject to provide a managed interface for JOBOBJECT_ASSOCIATE_COMPLETION_PORT setups.
        /// </summary>
        /// <param name="hJob"></param>
        /// <param name="JobObjectInformationClass"></param>
        /// <param name="lpJobObjectInformation"></param>
        /// <returns></returns>
        internal unsafe static BOOL SetInformationJobObject(SafeHandle hJob, JOBOBJECTINFOCLASS JobObjectInformationClass, JOBOBJECT_ASSOCIATE_COMPLETION_PORT lpJobObjectInformation)
        {
            return SetInformationJobObject(hJob, JobObjectInformationClass, &lpJobObjectInformation, (uint)sizeof(JOBOBJECT_ASSOCIATE_COMPLETION_PORT));
        }

        /// <summary>
        /// Wrapper around SetInformationJobObject to provide a managed interface for JOBOBJECT_EXTENDED_LIMIT_INFORMATION setups.
        /// </summary>
        /// <param name="hJob"></param>
        /// <param name="JobObjectInformationClass"></param>
        /// <param name="lpJobObjectInformation"></param>
        /// <returns></returns>
        internal unsafe static BOOL SetInformationJobObject(SafeHandle hJob, JOBOBJECTINFOCLASS JobObjectInformationClass, JOBOBJECT_EXTENDED_LIMIT_INFORMATION lpJobObjectInformation)
        {
            return SetInformationJobObject(hJob, JobObjectInformationClass, &lpJobObjectInformation, (uint)sizeof(JOBOBJECT_EXTENDED_LIMIT_INFORMATION));
        }

        /// <summary>
        /// Wrapper around CreateProcess to manage error handling.
        /// </summary>
        /// <param name="lpApplicationName"></param>
        /// <param name="lpCommandLine"></param>
        /// <param name="lpProcessAttributes"></param>
        /// <param name="lpThreadAttributes"></param>
        /// <param name="bInheritHandles"></param>
        /// <param name="dwCreationFlags"></param>
        /// <param name="lpEnvironment"></param>
        /// <param name="lpCurrentDirectory"></param>
        /// <param name="lpStartupInfo"></param>
        /// <param name="lpProcessInformation"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal unsafe static BOOL CreateProcess(string? lpApplicationName, ref Span<char> lpCommandLine, SECURITY_ATTRIBUTES? lpProcessAttributes, SECURITY_ATTRIBUTES? lpThreadAttributes, BOOL bInheritHandles, PROCESS_CREATION_FLAGS dwCreationFlags, SafeEnvironmentBlockHandle lpEnvironment, string? lpCurrentDirectory, in STARTUPINFOW lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation)
        {
            if (lpEnvironment is null || lpEnvironment.IsClosed)
            {
                throw new ArgumentNullException(nameof(lpEnvironment));
            }

            bool lpEnvironmentAddRef = false;
            try
            {
                lpEnvironment.DangerousAddRef(ref lpEnvironmentAddRef);
                var res = PInvoke.CreateProcess(lpApplicationName, ref lpCommandLine, lpProcessAttributes, lpThreadAttributes, bInheritHandles, dwCreationFlags, lpEnvironment.DangerousGetHandle().ToPointer(), lpCurrentDirectory, lpStartupInfo, out lpProcessInformation);
                if (!res)
                {
                    throw ExceptionUtilities.GetExceptionForLastWin32Error();
                }
                return res;
            }
            finally
            {
                if (lpEnvironmentAddRef)
                {
                    lpEnvironment.DangerousRelease();
                }
            }
        }

        /// <summary>
        /// Wrapper around AssignProcessToJobObject to manage error handling.
        /// </summary>
        /// <param name="hJob"></param>
        /// <param name="hProcess"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal static BOOL AssignProcessToJobObject(SafeHandle hJob, SafeHandle hProcess)
        {
            var res = PInvoke.AssignProcessToJobObject(hJob, hProcess);
            if (!res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
        }

        /// <summary>
        /// Wrapper around ResumeThread to manage error handling.
        /// </summary>
        /// <param name="hThread"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal static uint ResumeThread(SafeHandle hThread)
        {
            var res = PInvoke.ResumeThread(hThread);
            if (res == uint.MaxValue)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
        }

        /// <summary>
        /// Wrapper around GetQueuedCompletionStatus to manage error handling.
        /// </summary>
        /// <param name="CompletionPort"></param>
        /// <param name="lpCompletionCode"></param>
        /// <param name="lpCompletionKey"></param>
        /// <param name="lpOverlapped"></param>
        /// <param name="dwMilliseconds"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal unsafe static BOOL GetQueuedCompletionStatus(SafeHandle CompletionPort, out uint lpCompletionCode, out nuint lpCompletionKey, out nuint lpOverlapped, uint dwMilliseconds)
        {
            var res = PInvoke.GetQueuedCompletionStatus(CompletionPort, out lpCompletionCode, out lpCompletionKey, out var pOverlapped, dwMilliseconds);
            if (!res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            lpOverlapped = (nuint)pOverlapped;
            return res;
        }

        /// <summary>
        /// Wrapper around GetExitCodeProcess to manage error handling.
        /// </summary>
        /// <param name="hProcess"></param>
        /// <param name="lpExitCode"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal static BOOL GetExitCodeProcess(SafeHandle hProcess, out uint lpExitCode)
        {
            var res = PInvoke.GetExitCodeProcess(hProcess, out lpExitCode);
            if (!res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
        }

        /// <summary>
        /// Terminates a job object and all child processes under it.
        /// </summary>
        /// <param name="hJob"></param>
        /// <param name="uExitCode"></param>
        /// <returns></returns>
        internal static BOOL TerminateJobObject(SafeHandle hJob, uint uExitCode)
        {
            var res = PInvoke.TerminateJobObject(hJob, uExitCode);
            if (!res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
        }

        /// <summary>
        /// Wrapper around GetProcessId to manage error handling.
        /// </summary>
        /// <param name="Process"></param>
        /// <returns></returns>
        internal static uint GetProcessId(SafeHandle Process)
        {
            var res = PInvoke.GetProcessId(Process);
            if (res == 0)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
        }

        /// <summary>
        /// Wrapper around DuplicateHandle to manage error handling.
        /// </summary>
        /// <param name="hSourceProcessHandle"></param>
        /// <param name="hSourceHandle"></param>
        /// <param name="hTargetProcessHandle"></param>
        /// <param name="lpTargetHandle"></param>
        /// <param name="dwDesiredAccess"></param>
        /// <param name="bInheritHandle"></param>
        /// <param name="dwOptions"></param>
        /// <returns></returns>
        internal static BOOL DuplicateHandle(SafeHandle hSourceProcessHandle, SafeHandle hSourceHandle, SafeHandle hTargetProcessHandle, out SafeFileHandle lpTargetHandle, PROCESS_ACCESS_RIGHTS dwDesiredAccess, BOOL bInheritHandle, DUPLICATE_HANDLE_OPTIONS dwOptions)
        {
            var res = PInvoke.DuplicateHandle(hSourceProcessHandle, hSourceHandle, hTargetProcessHandle, out lpTargetHandle, (uint)dwDesiredAccess, bInheritHandle, dwOptions);
            if (!res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
        }

        /// <summary>
        /// Wrapper around OpenProcess to manage error handling.
        /// </summary>
        /// <param name="dwDesiredAccess"></param>
        /// <param name="bInheritHandle"></param>
        /// <param name="dwProcessId"></param>
        /// <returns></returns>
        internal static SafeFileHandle OpenProcess(PROCESS_ACCESS_RIGHTS dwDesiredAccess, BOOL bInheritHandle, uint dwProcessId)
        {
            var res = PInvoke.OpenProcess_SafeHandle(dwDesiredAccess, bInheritHandle, dwProcessId);
            if (null == res || res.IsInvalid)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
        }

        /// <summary>
        /// Wrapper around QueryDosDevice to manage error handling.
        /// </summary>
        /// <param name="lpDeviceName"></param>
        /// <param name="lpTargetPath"></param>
        /// <returns></returns>
        internal static uint QueryDosDevice(string lpDeviceName, Span<char> lpTargetPath)
        {
            var res = PInvoke.QueryDosDevice(lpDeviceName, lpTargetPath);
            if (res == 0)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
        }

        /// <summary>
        /// Wrapper around GetExitCodeThread to manage error handling.
        /// </summary>
        /// <param name="hThread"></param>
        /// <param name="lpExitCode"></param>
        /// <returns></returns>
        internal static BOOL GetExitCodeThread(SafeHandle hThread, out uint lpExitCode)
        {
            var res = PInvoke.GetExitCodeThread(hThread, out lpExitCode);
            if (!res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
        }

        /// <summary>
        /// Wrapper around LoadLibrary to manage error handling.
        /// </summary>
        /// <param name="lpLibFileName"></param>
        /// <returns></returns>
        internal static FreeLibrarySafeHandle LoadLibrary(string lpLibFileName)
        {
            var res = PInvoke.LoadLibrary(lpLibFileName);
            if (null == res || res.IsInvalid)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
        }

        /// <summary>
        /// Retrieves a safe handle representing the current process.
        /// </summary>
        /// <remarks>The returned handle is safe to use with Windows API functions that require a process
        /// handle. It is the caller's responsibility to ensure proper disposal of the handle to release system
        /// resources.</remarks>
        /// <returns>A <see cref="SafeProcessHandle"/> that encapsulates a handle to the current process.</returns>
        internal static SafeProcessHandle GetCurrentProcess()
        {
            var res = PInvoke.GetCurrentProcess();
            if (res != (nint)(-1))
            {
                throw new InvalidOperationException("Failed to retrieve handle for current process.");
            }
            return new(res, true);
        }

        /// <summary>
        /// Retrieves the session ID associated with a specified process ID.
        /// </summary>
        /// <param name="dwProcessId">The process ID for which to retrieve the session ID.</param>
        /// <param name="pSessionId">When this method returns, contains the session ID associated with the specified process ID.</param>
        /// <returns><see langword="true"/> if the session ID was successfully retrieved; otherwise, <see langword="false"/>.</returns>
        internal static BOOL ProcessIdToSessionId(uint dwProcessId, out uint pSessionId)
        {
            var res = PInvoke.ProcessIdToSessionId(dwProcessId, out pSessionId);
            if (!res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
        }

        /// <summary>
        /// Determines whether the system is currently in Terminal Services application installation mode.
        /// </summary>
        /// <remarks>Terminal Services application installation mode is used to install applications in a
        /// way that supports multiple users on a terminal server. This method can be used to check the current mode 
        /// before performing operations that depend on the installation mode.</remarks>
        /// <returns><see langword="true"/> if the system is in Terminal Services application installation mode; otherwise, <see
        /// langword="false"/>.</returns>
        [DllImport("kernel32.dll", SetLastError = false, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool TermsrvAppInstallMode();

        /// <summary>
        /// Retrieves system firmware table data for the specified firmware table provider and table ID.
        /// </summary>
        /// <remarks>This method retrieves firmware table data from the system using the specified
        /// provider and table ID. If the buffer provided in <paramref name="pFirmwareTableBuffer"/> is too small to
        /// hold the data, an <see cref="OverflowException"/> is thrown.</remarks>
        /// <param name="FirmwareTableProviderSignature">The signature of the firmware table provider. This identifies the type of firmware table to retrieve.</param>
        /// <param name="FirmwareTableID">The identifier of the specific firmware table to retrieve.</param>
        /// <param name="pFirmwareTableBuffer">A buffer to store the retrieved firmware table data. The buffer must be large enough to hold the data.</param>
        /// <returns>The size, in bytes, of the firmware table data retrieved.</returns>
        /// <exception cref="OverflowException">Thrown if the buffer provided in <paramref name="pFirmwareTableBuffer"/> is too small to hold the firmware
        /// table data.</exception>
        internal static uint GetSystemFirmwareTable(FIRMWARE_TABLE_PROVIDER FirmwareTableProviderSignature, uint FirmwareTableID, Span<byte> pFirmwareTableBuffer)
        {
            var res = PInvoke.GetSystemFirmwareTable(FirmwareTableProviderSignature, FirmwareTableID, pFirmwareTableBuffer);
            if (res == 0)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            if (pFirmwareTableBuffer.Length != 0 && res > pFirmwareTableBuffer.Length)
            {
                throw new OverflowException("Buffer was too small. Value was truncated.");
            }
            return res;
        }

        /// <summary>
        /// Retrieves the current system power status, including battery and AC power information.
        /// </summary>
        /// <remarks>This method wraps a call to the native Win32 API function
        /// <c>GetSystemPowerStatus</c>. It throws an exception if the underlying API call fails.</remarks>
        /// <param name="lpSystemPowerStatus">When the method returns, contains a <see cref="SYSTEM_POWER_STATUS"/> structure with details about the
        /// system's power status.</param>
        /// <returns><see langword="true"/> if the operation succeeds; otherwise, <see langword="false"/>.</returns>
        internal static BOOL GetSystemPowerStatus(out SYSTEM_POWER_STATUS lpSystemPowerStatus)
        {
            var res = PInvoke.GetSystemPowerStatus(out lpSystemPowerStatus);
            if (res == 0)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
        }

        /// <summary>
        /// Determines whether a specified process is running within a specified job.
        /// </summary>
        /// <param name="ProcessHandle">A handle to the process to be checked. This handle must have the PROCESS_QUERY_INFORMATION or
        /// PROCESS_QUERY_LIMITED_INFORMATION access right.</param>
        /// <param name="JobHandle">A handle to the job. If this parameter is <see langword="null"/>, the function checks if the process is
        /// running in any job.</param>
        /// <param name="Result">When this method returns, contains a <see langword="true"/> if the process is in the specified job;
        /// otherwise, <see langword="false"/>.</param>
        /// <returns><see langword="true"/> if the function succeeds; otherwise, <see langword="false"/>.</returns>
        internal static BOOL IsProcessInJob(SafeHandle ProcessHandle, SafeHandle? JobHandle, out BOOL Result)
        {
            var res = PInvoke.IsProcessInJob(ProcessHandle, JobHandle, out Result);
            if (!res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
        }

        /// <summary>
        /// Queries information about the specified job object.
        /// </summary>
        /// <remarks>This method is a wrapper around the native Windows API function
        /// <c>QueryInformationJobObject</c>. It is used to retrieve various types of information about a job object,
        /// such as accounting information, limits, and process information.</remarks>
        /// <param name="hJob">A handle to the job object. This handle must have the <see cref="JobObjectAccessRights.Query"/> access
        /// right.</param>
        /// <param name="JobObjectInformationClass">The information class for the job object. This parameter specifies the type of information to be queried.</param>
        /// <param name="lpJobObjectInformation">A buffer that receives the information. The format of this data depends on the value of the <paramref
        /// name="JobObjectInformationClass"/> parameter.</param>
        /// <param name="lpReturnLength">When this method returns, contains the size of the data returned in the <paramref
        /// name="lpJobObjectInformation"/> buffer, in bytes.</param>
        /// <returns><see langword="true"/> if the function succeeds; otherwise, <see langword="false"/>.</returns>
        internal unsafe static BOOL QueryInformationJobObject(SafeHandle? hJob, JOBOBJECTINFOCLASS JobObjectInformationClass, SafeHGlobalHandle lpJobObjectInformation, out uint lpReturnLength)
        {
            bool lpJobObjectInformationAddRef = false;
            try
            {
                lpJobObjectInformation.DangerousAddRef(ref lpJobObjectInformationAddRef);
                fixed (uint* pReturnLength = &lpReturnLength)
                {
                    var res = PInvoke.QueryInformationJobObject(hJob, JobObjectInformationClass, lpJobObjectInformation.DangerousGetHandle().ToPointer(), (uint)lpJobObjectInformation.Length, pReturnLength);
                    if (!res)
                    {
                        throw ExceptionUtilities.GetExceptionForLastWin32Error();
                    }
                    return res;
                }
            }
            finally
            {
                if (lpJobObjectInformationAddRef)
                {
                    lpJobObjectInformation.DangerousRelease();
                }
            }
        }

        /// <summary>
        /// Retrieves the Application User Model ID (AUMID) for a specified process.
        /// </summary>
        /// <remarks>This method wraps a PInvoke call to retrieve the AUMID, and throws an exception if
        /// the operation is unsuccessful.</remarks>
        /// <param name="hProcess">A handle to the process for which the AUMID is being retrieved. This handle must have the necessary access
        /// rights.</param>
        /// <param name="applicationUserModelIdLength">On input, specifies the size of the <paramref name="applicationUserModelId"/> buffer. On output, receives
        /// the length of the AUMID, including the null terminator.</param>
        /// <param name="applicationUserModelId">A buffer that receives the AUMID as a null-terminated string.</param>
        /// <returns>A <see cref="WIN32_ERROR"/> code indicating the result of the operation. Returns <see
        /// cref="WIN32_ERROR.NO_ERROR"/> if successful.</returns>
        internal static WIN32_ERROR GetApplicationUserModelId(SafeHandle hProcess, ref uint applicationUserModelIdLength, Span<char> applicationUserModelId)
        {
            var res = PInvoke.GetApplicationUserModelId(hProcess, ref applicationUserModelIdLength, applicationUserModelId);
            if (res != WIN32_ERROR.ERROR_SUCCESS)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error(res);
            }
            return res;
        }

        /// <summary>
        /// Initializes a list of attributes for process and thread creation.
        /// </summary>
        /// <param name="lpAttributeList">A pointer to a buffer that receives the updated attribute list.</param>
        /// <param name="dwAttributeCount">The number of attributes to be added to the list.</param>
        /// <param name="lpSize">On input, specifies the size of the lpAttributeList buffer. On output, receives the required buffer size if
        /// the function fails.</param>
        /// <returns><see langword="true"/> if the attribute list is successfully initialized; otherwise, <see
        /// langword="false"/>.</returns>
        internal static BOOL InitializeProcThreadAttributeList(LPPROC_THREAD_ATTRIBUTE_LIST lpAttributeList, uint dwAttributeCount, ref nuint lpSize)
        {
            var res = PInvoke.InitializeProcThreadAttributeList(lpAttributeList, dwAttributeCount, ref lpSize);
            if (!res && ((WIN32_ERROR)Marshal.GetLastWin32Error() is WIN32_ERROR lastWin32Error) && (lastWin32Error != WIN32_ERROR.ERROR_INSUFFICIENT_BUFFER || lpAttributeList != default))
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error(lastWin32Error);
            }
            return res;
        }

        /// <summary>
        /// Updates the attributes of a specified process or thread.
        /// </summary>
        /// <param name="lpAttributeList">A pointer to an attribute list created by the <c>InitializeProcThreadAttributeList</c> function.</param>
        /// <param name="Attribute">The attribute key to update. This specifies which attribute to modify in the list.</param>
        /// <param name="lpValue">A pointer to the attribute value. The type and meaning of this value depend on the attribute key specified
        /// by <paramref name="Attribute"/>.</param>
        /// <param name="cbSize">The size in bytes of the attribute value specified by <paramref name="lpValue"/>.</param>
        /// <param name="lpPreviousValue">A pointer to a buffer that receives the previous value of the attribute. This parameter can be <see
        /// langword="null"/> if the previous value is not required.</param>
        /// <param name="lpReturnSize">A pointer to a variable that receives the size of the attribute value. This parameter can be <see
        /// langword="null"/> if the size is not required.</param>
        /// <returns><see langword="true"/> if the function succeeds; otherwise, <see langword="false"/>.</returns>
        internal unsafe static BOOL UpdateProcThreadAttribute(SafeProcThreadAttributeListHandle lpAttributeList, PROC_THREAD_ATTRIBUTE Attribute, SafeHGlobalHandle lpValue, IntPtr? lpPreviousValue = null, nuint? lpReturnSize = null)
        {
            bool lpAttributeListAddRef = false;
            bool lpValueAddRef = false;
            try
            {
                lpAttributeList.DangerousAddRef(ref lpAttributeListAddRef);
                lpValue.DangerousAddRef(ref lpValueAddRef);
                var res = PInvoke.UpdateProcThreadAttribute((LPPROC_THREAD_ATTRIBUTE_LIST)lpAttributeList.DangerousGetHandle(), 0, (nuint)Attribute, lpValue.DangerousGetHandle().ToPointer(), (nuint)lpValue.Length, lpPreviousValue.HasValue && lpPreviousValue.Value != IntPtr.Zero ? lpPreviousValue.Value.ToPointer() : null, lpReturnSize);
                if (!res)
                {
                    throw ExceptionUtilities.GetExceptionForLastWin32Error();
                }
                return res;
            }
            finally
            {
                if (lpValueAddRef)
                {
                    lpValue.DangerousRelease();
                }
                if (lpAttributeListAddRef)
                {
                    lpAttributeList.DangerousRelease();
                }
            }
        }

        /// <summary>
        /// Reads data from an area of memory in a specified process. The process is identified by a handle.
        /// </summary>
        /// <remarks>This method wraps the PInvoke call to ReadProcessMemory and throws an exception if
        /// the operation fails. Ensure that the buffer is large enough to hold the data being read to avoid an <see
        /// cref="OverflowException"/>.</remarks>
        /// <param name="hProcess">A handle to the process with memory that is being read. The handle must have PROCESS_VM_READ access.</param>
        /// <param name="lpBaseAddress">A pointer to the base address in the specified process from which to read.</param>
        /// <param name="lpBuffer">A pointer to a buffer that receives the contents from the address space of the specified process.</param>
        /// <param name="nSize">The number of bytes to be read from the specified process.</param>
        /// <param name="lpNumberOfBytesRead">A pointer to a variable that receives the number of bytes transferred into the specified buffer. This
        /// parameter can be null.</param>
        /// <returns>A <see cref="BOOL"/> indicating whether the operation succeeded.</returns>
        /// <exception cref="OverflowException">Thrown if the buffer was too small and the value was truncated.</exception>
        internal unsafe static BOOL ReadProcessMemory(SafeHandle hProcess, IntPtr lpBaseAddress, SafeMemoryHandle lpBuffer, out nuint lpNumberOfBytesRead)
        {
            bool lpBufferAddRef = false;
            fixed (nuint* pNumberOfBytesRead = &lpNumberOfBytesRead)
            {
                try
                {
                    lpBuffer.DangerousAddRef(ref lpBufferAddRef);
                    var res = PInvoke.ReadProcessMemory(hProcess, lpBaseAddress.ToPointer(), lpBuffer.DangerousGetHandle().ToPointer(), (nuint)lpBuffer.Length, pNumberOfBytesRead);
                    if (!res)
                    {
                        throw ExceptionUtilities.GetExceptionForLastWin32Error();
                    }
                    return res;
                }
                finally
                {
                    if (lpBufferAddRef)
                    {
                        lpBuffer.DangerousRelease();
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves a human-readable name for a specified language identifier.
        /// </summary>
        /// <remarks>This method wraps a call to a native function and throws an exception if the
        /// operation fails. Ensure that <paramref name="szLang"/> is sufficiently large to avoid truncation.</remarks>
        /// <param name="wLang">The language identifier for which the name is to be retrieved.</param>
        /// <param name="szLang">A span of characters that receives the language name. The buffer must be large enough to hold the name.</param>
        /// <returns>The number of characters written to <paramref name="szLang"/>, excluding the null terminator.</returns>
        /// <exception cref="OverflowException">Thrown if the buffer provided by <paramref name="szLang"/> is too small to hold the language name.</exception>
        internal static uint VerLanguageName(uint wLang, Span<char> szLang)
        {
            var res = PInvoke.VerLanguageName(wLang, szLang);
            if (res == 0)
            {
                throw new Win32Exception("Failed to retrieve language name.");
            }
            if (res > szLang.Length)
            {
                throw new OverflowException("Buffer was too small. Value was truncated.");
            }
            return res;
        }

        /// <summary>
        /// Frees a block of memory allocated by the LocalAlloc function.
        /// </summary>
        /// <param name="hMem"></param>
        /// <returns></returns>
        internal static HLOCAL LocalFree(HLOCAL hMem)
        {
            var res = PInvoke.LocalFree(hMem);
            if (!res.IsNull)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return hMem;
        }

        /// <summary>
        /// Posts a completion packet to a specified completion port.
        /// </summary>
        /// <param name="CompletionPort"></param>
        /// <param name="dwNumberOfBytesTransferred"></param>
        /// <param name="dwCompletionKey"></param>
        /// <param name="lpOverlapped"></param>
        /// <returns></returns>
        internal static BOOL PostQueuedCompletionStatus(SafeHandle CompletionPort, uint dwNumberOfBytesTransferred, nuint dwCompletionKey, NativeOverlapped? lpOverlapped)
        {
            var res = PInvoke.PostQueuedCompletionStatus(CompletionPort, dwNumberOfBytesTransferred, dwCompletionKey, lpOverlapped);
            if (!res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
        }

        /// <summary>
        /// Creates or opens a file or I/O device.
        /// </summary>
        /// <param name="lpFileName"></param>
        /// <param name="dwDesiredAccess"></param>
        /// <param name="dwShareMode"></param>
        /// <param name="lpSecurityAttributes"></param>
        /// <param name="dwCreationDisposition"></param>
        /// <param name="dwFlagsAndAttributes"></param>
        /// <param name="hTemplateFile"></param>
        /// <returns></returns>
        internal static SafeFileHandle CreateFile(string lpFileName, FileSystemRights dwDesiredAccess, FILE_SHARE_MODE dwShareMode, SECURITY_ATTRIBUTES? lpSecurityAttributes, FILE_CREATION_DISPOSITION dwCreationDisposition, FILE_FLAGS_AND_ATTRIBUTES dwFlagsAndAttributes, SafeHandle? hTemplateFile = null)
        {
            var res = PInvoke.CreateFile(lpFileName, (uint)dwDesiredAccess, dwShareMode, lpSecurityAttributes, dwCreationDisposition, dwFlagsAndAttributes, hTemplateFile);
            if (res.IsInvalid)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
        }
    }
}
