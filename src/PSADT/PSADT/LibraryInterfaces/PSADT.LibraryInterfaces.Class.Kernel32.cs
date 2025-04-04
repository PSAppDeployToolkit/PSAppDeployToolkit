using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using PSADT.Utilities;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.System.JobObjects;
using Windows.Win32.System.LibraryLoader;
using Windows.Win32.System.Threading;

namespace PSADT.LibraryInterfaces
{
    /// <summary>
    /// Public P/Invokes from the kernel32.dll library.
    /// </summary>
    public static class Kernel32
    {
        /// <summary>
        /// Tests whether the current device has completed its Out-of-Box Experience (OOBE).
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        public static bool IsOOBEComplete()
        {
            if (!PInvoke.OOBEComplete(out var isOobeComplete))
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return isOobeComplete;
        }

        /// <summary>
        /// Gets the Session Id for the given Process Id.
        /// </summary>
        /// <param name="processId"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        public static uint ProcessIdToSessionId(uint processId)
        {
            if (!PInvoke.ProcessIdToSessionId(processId, out uint sessionId))
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return sessionId;
        }

        /// <summary>
        /// Wrapper around LoadLibraryEx to manage error handling.
        /// </summary>
        /// <param name="lpLibFileName"></param>
        /// <param name="dwFlags"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal static unsafe FreeLibrarySafeHandle LoadLibraryEx(string lpLibFileName, LOAD_LIBRARY_FLAGS dwFlags)
        {
            var res = PInvoke.LoadLibraryEx(lpLibFileName, dwFlags);
            if (null == res)
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
        internal static unsafe FARPROC GetProcAddress(SafeHandle hModule, string lpProcName)
        {
            var res = PInvoke.GetProcAddress(hModule, lpProcName);
            if (null == res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
        }

        /// <summary>
        /// Wrapper around IsWow64Process2 to manage error handling.
        /// </summary>
        /// <param name="hProcess"></param>
        /// <param name="pProcessMachine"></param>
        /// <param name="pNativeMachine"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal static unsafe BOOL IsWow64Process2(SafeHandle hProcess, out Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE pProcessMachine, out Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE pNativeMachine)
        {
            Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE pNativeMachineInternal;
            var res = PInvoke.IsWow64Process2(hProcess, out pProcessMachine, &pNativeMachineInternal);
            if (null == res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            pNativeMachine = pNativeMachineInternal;
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
        internal static uint GetPrivateProfileString(string lpAppName, string lpKeyName, string lpDefault, Span<char> lpReturnedString, string lpFileName)
        {
            var res = PInvoke.GetPrivateProfileString(lpAppName, lpKeyName, lpDefault, lpReturnedString, lpFileName);
            if (res == 0)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            else if (res == lpReturnedString.Length - 1)
            {
                throw new OverflowException("Buffer was too small. Value was truncated.");
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
        internal static unsafe BOOL WritePrivateProfileString(string lpAppName, string lpKeyName, string lpString, string lpFileName)
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
        internal static HANDLE CreateIoCompletionPort(HANDLE FileHandle, HANDLE ExistingCompletionPort, nuint CompletionKey, uint NumberOfConcurrentThreads)
        {
            var res = PInvoke.CreateIoCompletionPort(FileHandle, ExistingCompletionPort, CompletionKey, NumberOfConcurrentThreads);
            if (null == res || res.IsNull)
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
        internal static unsafe HANDLE CreateJobObject([Optional] SECURITY_ATTRIBUTES? lpJobAttributes, PCWSTR lpName)
        {
            SECURITY_ATTRIBUTES lpJobAttributesLocal = lpJobAttributes ?? default(SECURITY_ATTRIBUTES);
            var res = PInvoke.CreateJobObject(lpJobAttributes.HasValue ? &lpJobAttributesLocal : null, (lpName.Value != null && *lpName.Value != '\0') ? lpName : null);
            if (null == res || res.IsNull)
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
        internal static unsafe BOOL SetInformationJobObject(HANDLE hJob, JOBOBJECTINFOCLASS JobObjectInformationClass, IntPtr lpJobObjectInformation, uint cbJobObjectInformationLength)
        {
            var res = PInvoke.SetInformationJobObject(hJob, JobObjectInformationClass, lpJobObjectInformation.ToPointer(), cbJobObjectInformationLength);
            if (!res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
        }

        internal static unsafe BOOL SetInformationJobObject(HANDLE hJob, JOBOBJECTINFOCLASS JobObjectInformationClass, JOBOBJECT_ASSOCIATE_COMPLETION_PORT lpJobObjectInformation)
        {
            return SetInformationJobObject(hJob, JobObjectInformationClass, new IntPtr(&lpJobObjectInformation), (uint)sizeof(JOBOBJECT_ASSOCIATE_COMPLETION_PORT));
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
        internal static unsafe BOOL CreateProcess(string? lpApplicationName, string lpCommandLine, [Optional] SECURITY_ATTRIBUTES? lpProcessAttributes, [Optional] SECURITY_ATTRIBUTES? lpThreadAttributes, BOOL bInheritHandles, PROCESS_CREATION_FLAGS dwCreationFlags, [Optional] IntPtr lpEnvironment, string? lpCurrentDirectory, in STARTUPINFOW lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation)
        {
            string lpCurrentDirectoryLocal = !string.IsNullOrWhiteSpace(lpCurrentDirectory) ? lpCurrentDirectory! : string.Empty;
            string lpApplicationNameLocal = !string.IsNullOrWhiteSpace(lpApplicationName) ? lpApplicationName! : string.Empty;
            fixed (char* pApplicationName = lpApplicationNameLocal, pCommandLine = lpCommandLine, pCurrentDirectory = lpCurrentDirectoryLocal)
            fixed (PROCESS_INFORMATION* pProcessInformation = &lpProcessInformation)
            fixed (STARTUPINFOW* pStartupInfo = &lpStartupInfo)
            {
                SECURITY_ATTRIBUTES lpProcessAttributesLocal = lpProcessAttributes ?? default(SECURITY_ATTRIBUTES);
                SECURITY_ATTRIBUTES lpThreadAttributesLocal = lpThreadAttributes ?? default(SECURITY_ATTRIBUTES);
                var res = PInvoke.CreateProcess(lpApplicationNameLocal.Length != 0 ? pApplicationName : null, pCommandLine, lpProcessAttributes.HasValue ? &lpProcessAttributesLocal : null, lpThreadAttributes.HasValue ? &lpThreadAttributesLocal : null, bInheritHandles, dwCreationFlags, lpEnvironment.ToPointer(), lpCurrentDirectoryLocal.Length != 0 ? pCurrentDirectory : null, pStartupInfo, pProcessInformation);
                if (!res)
                {
                    throw ExceptionUtilities.GetExceptionForLastWin32Error();
                }
                return res;
            }
        }

        /// <summary>
        /// Wrapper around CreateProcessAsUser to manage error handling.
        /// </summary>
        /// <param name="hToken"></param>
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
        internal static unsafe BOOL CreateProcessAsUser(HANDLE hToken, string? lpApplicationName, string lpCommandLine, [Optional] SECURITY_ATTRIBUTES? lpProcessAttributes, [Optional] SECURITY_ATTRIBUTES? lpThreadAttributes, BOOL bInheritHandles, PROCESS_CREATION_FLAGS dwCreationFlags, [Optional] IntPtr lpEnvironment, string? lpCurrentDirectory, in STARTUPINFOW lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation)
        {
            string lpCurrentDirectoryLocal = !string.IsNullOrWhiteSpace(lpCurrentDirectory) ? lpCurrentDirectory! : string.Empty;
            string lpApplicationNameLocal = !string.IsNullOrWhiteSpace(lpApplicationName) ? lpApplicationName! : string.Empty;
            fixed (char* pApplicationName = lpApplicationNameLocal, pCommandLine = lpCommandLine, pCurrentDirectory = lpCurrentDirectoryLocal)
            fixed (PROCESS_INFORMATION* pProcessInformation = &lpProcessInformation)
            fixed (STARTUPINFOW* pStartupInfo = &lpStartupInfo)
            {
                SECURITY_ATTRIBUTES lpProcessAttributesLocal = lpProcessAttributes ?? default(SECURITY_ATTRIBUTES);
                SECURITY_ATTRIBUTES lpThreadAttributesLocal = lpThreadAttributes ?? default(SECURITY_ATTRIBUTES);
                var res = PInvoke.CreateProcessAsUser(hToken, lpApplicationNameLocal.Length != 0 ? pApplicationName : null, pCommandLine, lpProcessAttributes.HasValue ? &lpProcessAttributesLocal : null, lpThreadAttributes.HasValue ? &lpThreadAttributesLocal : null, bInheritHandles, dwCreationFlags, lpEnvironment.ToPointer(), lpCurrentDirectoryLocal.Length != 0 ? pCurrentDirectory : null, pStartupInfo, pProcessInformation);
                if (!res)
                {
                    throw ExceptionUtilities.GetExceptionForLastWin32Error();
                }
                return res;
            }
        }

        /// <summary>
        /// Wrapper around AssignProcessToJobObject to manage error handling.
        /// </summary>
        /// <param name="hJob"></param>
        /// <param name="hProcess"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal static BOOL AssignProcessToJobObject(HANDLE hJob, HANDLE hProcess)
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
        internal static uint ResumeThread(HANDLE hThread)
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
        internal static BOOL CloseHandle(ref HANDLE hObject)
        {
            if (null == hObject || hObject == default || hObject.IsNull)
            {
                return true;
            }
            var res = PInvoke.CloseHandle(hObject);
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
        internal static unsafe BOOL GetQueuedCompletionStatus(HANDLE CompletionPort, out uint lpCompletionCode, out nuint lpCompletionKey, out NativeOverlapped lpOverlapped, uint dwMilliseconds)
        {
            fixed (uint* pCompletionCode = &lpCompletionCode)
            fixed (nuint* pCompletionKey = &lpCompletionKey)
            fixed (NativeOverlapped* pOverlapped = &lpOverlapped)
            {
                var res = PInvoke.GetQueuedCompletionStatus(CompletionPort, pCompletionCode, pCompletionKey, &pOverlapped, dwMilliseconds);
                if (!res)
                {
                    throw ExceptionUtilities.GetExceptionForLastWin32Error();
                }
                return res;
            }
        }

        /// <summary>
        /// Wrapper around GetExitCodeProcess to manage error handling.
        /// </summary>
        /// <param name="hProcess"></param>
        /// <param name="lpExitCode"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal static unsafe BOOL GetExitCodeProcess(HANDLE hProcess, out uint lpExitCode)
        {
            fixed (uint* pExitCode = &lpExitCode)
            {
                var res = PInvoke.GetExitCodeProcess(hProcess, pExitCode);
                if (!res)
                {
                    throw ExceptionUtilities.GetExceptionForLastWin32Error();
                }
                return res;
            }
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
        internal static unsafe BOOL CreatePipe(out HANDLE hReadPipe, out HANDLE hWritePipe, [Optional] SECURITY_ATTRIBUTES? lpPipeAttributes, uint nSize = 0)
        {
            fixed (HANDLE* pReadPipe = &hReadPipe, pWritePipe = &hWritePipe)
            {
                SECURITY_ATTRIBUTES lpPipeAttributesLocal = lpPipeAttributes ?? default(SECURITY_ATTRIBUTES);
                var res = PInvoke.CreatePipe(pReadPipe, pWritePipe, lpPipeAttributes.HasValue ? &lpPipeAttributesLocal : null, nSize);
                if (!res)
                {
                    throw ExceptionUtilities.GetExceptionForLastWin32Error();
                }
                return res;
            }
        }

        /// <summary>
        /// Wrapper around SetHandleInformation to manage error handling.
        /// </summary>
        /// <param name="hObject"></param>
        /// <param name="dwMask"></param>
        /// <param name="dwFlags"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal static BOOL SetHandleInformation(HANDLE hObject, uint dwMask, HANDLE_FLAGS dwFlags)
        {
            var res = PInvoke.SetHandleInformation(hObject, dwMask, dwFlags);
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
        internal static unsafe BOOL ReadFile(HANDLE hFile, [Optional] byte[] lpBuffer, [Optional] out uint lpNumberOfBytesRead, [Optional] ref NativeOverlapped lpOverlapped)
        {
            fixed (byte* pBuffer = lpBuffer)
            fixed (uint* pNumberOfBytesRead = &lpNumberOfBytesRead)
            fixed (NativeOverlapped* pOverlapped = &lpOverlapped)
            {
                var res = PInvoke.ReadFile(hFile, pBuffer, (uint)lpBuffer.Length, pNumberOfBytesRead, pOverlapped);
                if (!res)
                {
                    throw ExceptionUtilities.GetExceptionForLastWin32Error();
                }
                return res;
            }
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
        internal static unsafe BOOL ReadFile(HANDLE hFile, [Optional] byte[] lpBuffer, [Optional] out uint lpNumberOfBytesRead)
        {
            fixed (byte* pBuffer = lpBuffer)
            fixed (uint* pNumberOfBytesRead = &lpNumberOfBytesRead)
            {
                var res = PInvoke.ReadFile(hFile, pBuffer, (uint)lpBuffer.Length, pNumberOfBytesRead, null);
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
        internal static BOOL SetPriorityClass(HANDLE hProcess, PROCESS_CREATION_FLAGS dwPriorityClass)
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
        internal static BOOL SetPriorityClass(HANDLE hProcess, ProcessPriorityClass dwPriorityClass)
        {
            return SetPriorityClass(hProcess, (PROCESS_CREATION_FLAGS)dwPriorityClass);
        }
    }
}
