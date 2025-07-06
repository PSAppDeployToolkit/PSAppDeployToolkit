using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;
using PSADT.SafeHandles;
using PSADT.Utilities;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.System.JobObjects;
using Windows.Win32.System.LibraryLoader;
using Windows.Win32.System.Memory;
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
        internal static unsafe BOOL OOBEComplete(out BOOL isOOBEComplete)
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
        internal static unsafe BOOL WritePrivateProfileString(string lpAppName, string? lpKeyName, string? lpString, string lpFileName)
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
        internal static unsafe BOOL SetInformationJobObject(SafeHandle hJob, JOBOBJECTINFOCLASS JobObjectInformationClass, IntPtr lpJobObjectInformation, uint cbJobObjectInformationLength)
        {
            var res = PInvoke.SetInformationJobObject(hJob, JobObjectInformationClass, lpJobObjectInformation.ToPointer(), cbJobObjectInformationLength);
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
        internal static unsafe BOOL SetInformationJobObject(SafeHandle hJob, JOBOBJECTINFOCLASS JobObjectInformationClass, JOBOBJECT_ASSOCIATE_COMPLETION_PORT lpJobObjectInformation)
        {
            return SetInformationJobObject(hJob, JobObjectInformationClass, new IntPtr(&lpJobObjectInformation), (uint)sizeof(JOBOBJECT_ASSOCIATE_COMPLETION_PORT));
        }

        /// <summary>
        /// Wrapper around SetInformationJobObject to provide a managed interface for JOBOBJECT_EXTENDED_LIMIT_INFORMATION setups.
        /// </summary>
        /// <param name="hJob"></param>
        /// <param name="JobObjectInformationClass"></param>
        /// <param name="lpJobObjectInformation"></param>
        /// <returns></returns>
        internal static unsafe BOOL SetInformationJobObject(SafeHandle hJob, JOBOBJECTINFOCLASS JobObjectInformationClass, JOBOBJECT_EXTENDED_LIMIT_INFORMATION lpJobObjectInformation)
        {
            return SetInformationJobObject(hJob, JobObjectInformationClass, new IntPtr(&lpJobObjectInformation), (uint)sizeof(JOBOBJECT_EXTENDED_LIMIT_INFORMATION));
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
        internal static unsafe BOOL CreateProcess(string? lpApplicationName, string lpCommandLine, SECURITY_ATTRIBUTES? lpProcessAttributes, SECURITY_ATTRIBUTES? lpThreadAttributes, BOOL bInheritHandles, PROCESS_CREATION_FLAGS dwCreationFlags, SafeEnvironmentBlockHandle lpEnvironment, string? lpCurrentDirectory, in STARTUPINFOW lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation)
        {
            if (lpEnvironment is not object || lpEnvironment.IsClosed)
            {
                throw new ArgumentNullException(nameof(lpEnvironment));
            }

            bool lpEnvironmentAddRef = false;
            try
            {
                lpEnvironment.DangerousAddRef(ref lpEnvironmentAddRef);
                Span<char> lpCommandLineSpan = lpCommandLine.ToCharArray();
                var res = PInvoke.CreateProcess(lpApplicationName, ref lpCommandLineSpan, lpProcessAttributes, lpThreadAttributes, bInheritHandles, dwCreationFlags, lpEnvironment.DangerousGetHandle().ToPointer(), lpCurrentDirectory, lpStartupInfo, out lpProcessInformation);
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
        /// Wrapper around CloseHandle to manage error handling.
        /// </summary>
        /// <param name="hObject"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal static BOOL CloseHandle(ref IntPtr hObject)
        {
            if (hObject == default || IntPtr.Zero != hObject)
            {
                return true;
            }
            var res = PInvoke.CloseHandle((HANDLE)hObject);
            if (!res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            hObject = default;
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
        internal static unsafe BOOL GetQueuedCompletionStatus(SafeHandle CompletionPort, out uint lpCompletionCode, out nuint lpCompletionKey, out IntPtr lpOverlapped, uint dwMilliseconds)
        {
            var res = PInvoke.GetQueuedCompletionStatus(CompletionPort, out lpCompletionCode, out lpCompletionKey, out var pOverlapped, dwMilliseconds);
            if (!res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            lpOverlapped = new IntPtr(pOverlapped);
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
        /// Wrapper around CreatePipe to manage error handling.
        /// </summary>
        /// <param name="hReadPipe"></param>
        /// <param name="hWritePipe"></param>
        /// <param name="lpPipeAttributes"></param>
        /// <param name="nSize"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal static BOOL CreatePipe(out SafeFileHandle hReadPipe, out SafeFileHandle hWritePipe, SECURITY_ATTRIBUTES? lpPipeAttributes, uint nSize = 0)
        {
            var res = PInvoke.CreatePipe(out hReadPipe, out hWritePipe, lpPipeAttributes, nSize);
            if (!res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
        }

        /// <summary>
        /// Wrapper around SetHandleInformation to manage error handling.
        /// </summary>
        /// <param name="hObject"></param>
        /// <param name="dwMask"></param>
        /// <param name="dwFlags"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal static BOOL SetHandleInformation(SafeHandle hObject, HANDLE_FLAGS dwMask, HANDLE_FLAGS dwFlags)
        {
            var res = PInvoke.SetHandleInformation(hObject, (uint)dwMask, dwFlags);
            if (!res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
        }

        /// <summary>
        /// Wrapper around ReadFile to manage error handling.
        /// </summary>
        /// <param name="hFile"></param>
        /// <param name="lpBuffer"></param>
        /// <param name="lpNumberOfBytesRead"></param>
        /// <param name="lpOverlapped"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal static unsafe BOOL ReadFile(SafeHandle hFile, Span<byte> lpBuffer, out uint lpNumberOfBytesRead, IntPtr lpOverlapped)
        {
            fixed (uint* pNumberOfBytesRead = &lpNumberOfBytesRead)
            {
                var res = PInvoke.ReadFile(hFile, lpBuffer, pNumberOfBytesRead, (NativeOverlapped*)lpOverlapped);
                if (!res)
                {
                    throw ExceptionUtilities.GetExceptionForLastWin32Error();
                }
                return res;
            }
        }

        /// <summary>
        /// Wrapper around WriteFile to manage error handling.
        /// </summary>
        /// <param name="hProcess"></param>
        /// <param name="dwPriorityClass"></param>
        /// <returns></returns>
        internal static BOOL SetPriorityClass(SafeHandle hProcess, PROCESS_CREATION_FLAGS dwPriorityClass)
        {
            var res = PInvoke.SetPriorityClass(hProcess, dwPriorityClass);
            if (!res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
        }

        /// <summary>
        /// Wrapper around WriteFile to manage error handling.
        /// </summary>
        /// <param name="hProcess"></param>
        /// <param name="dwPriorityClass"></param>
        /// <returns></returns>
        internal static BOOL SetPriorityClass(SafeHandle hProcess, ProcessPriorityClass dwPriorityClass)
        {
            return SetPriorityClass(hProcess, (PROCESS_CREATION_FLAGS)dwPriorityClass);
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
        /// Wrapper around WaitForMultipleObjects to manage error handling.
        /// </summary>
        /// <param name="lpHandles"></param>
        /// <param name="bWaitAll"></param>
        /// <param name="dwMilliseconds"></param>
        /// <returns></returns>
        internal static WAIT_EVENT WaitForMultipleObjects(ReadOnlySpan<HANDLE> lpHandles, BOOL bWaitAll, uint dwMilliseconds)
        {
            var res = PInvoke.WaitForMultipleObjects(lpHandles, bWaitAll, dwMilliseconds);
            if (res == WAIT_EVENT.WAIT_FAILED)
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
        /// Gets the elapsed amount of milliseconds since system boot as a 64-bit unsigned integer.
        /// </summary>
        /// <returns></returns>
        public static ulong GetTickCount64()
        {
            return PInvoke.GetTickCount64();
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
        /// Wrapper around VirtualAlloc to manage error handling.
        /// </summary>
        /// <param name="lpAddress"></param>
        /// <param name="dwSize"></param>
        /// <param name="flAllocationType"></param>
        /// <param name="flProtect"></param>
        /// <returns></returns>
        internal static unsafe SafeVirtualAllocHandle VirtualAlloc(IntPtr lpAddress, nuint dwSize, VIRTUAL_ALLOCATION_TYPE flAllocationType, PAGE_PROTECTION_FLAGS flProtect)
        {
            var res = PInvoke.VirtualAlloc(lpAddress.ToPointer(), dwSize, flAllocationType, flProtect);
            if (null == res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return new((IntPtr)res, true);
        }

        /// <summary>
        /// Wrapper around VirtualFree to manage error handling.
        /// </summary>
        /// <param name="lpAddress"></param>
        /// <param name="dwSize"></param>
        /// <param name="dwFreeType"></param>
        /// <returns></returns>
        internal static unsafe BOOL VirtualFree(IntPtr lpAddress, nuint dwSize, VIRTUAL_FREE_TYPE dwFreeType)
        {
            if (IntPtr.Zero == lpAddress)
            {
                return true;
            }
            var res = PInvoke.VirtualFree(lpAddress.ToPointer(), dwSize, dwFreeType);
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
        /// Allocates a specified number of bytes in the local heap.
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
            return res;
        }

        /// <summary>
        /// Determines whether the system is currently in Terminal Services application installation mode.
        /// </summary>
        /// <remarks>Terminal Services application installation mode is used to install applications in a
        /// way that  supports multiple users on a terminal server. This method can be used to check the current mode 
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
    }
}
