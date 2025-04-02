using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.System.JobObjects;
using Windows.Win32.System.LibraryLoader;
using Windows.Win32.System.SystemInformation;
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
                throw new Win32Exception(Marshal.GetLastWin32Error());
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
                throw new Win32Exception(Marshal.GetLastWin32Error());
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
                throw new Win32Exception(Marshal.GetLastWin32Error());
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
                throw new Win32Exception(Marshal.GetLastWin32Error());
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
                throw new Win32Exception(Marshal.GetLastWin32Error());
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
                throw new Win32Exception(Marshal.GetLastWin32Error());
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
                throw new Win32Exception(Marshal.GetLastWin32Error());
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
                throw new Win32Exception(Marshal.GetLastWin32Error());
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
        internal static unsafe HANDLE CreateJobObject([Optional] out SECURITY_ATTRIBUTES lpJobAttributes, PCWSTR lpName)
        {
            fixed (SECURITY_ATTRIBUTES* pJobAttributes = &lpJobAttributes)
            {
                var res = PInvoke.CreateJobObject(pJobAttributes, lpName);
                if (null == res || res.IsNull)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
                return res;
            }
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
        internal static unsafe BOOL SetInformationJobObject(HANDLE hJob, JOBOBJECTINFOCLASS JobObjectInformationClass, ref JOBOBJECT_ASSOCIATE_COMPLETION_PORT lpJobObjectInformation, uint cbJobObjectInformationLength)
        {
            fixed (JOBOBJECT_ASSOCIATE_COMPLETION_PORT* pJobObjectInformation = &lpJobObjectInformation)
            {
                var res = PInvoke.SetInformationJobObject(hJob, JobObjectInformationClass, pJobObjectInformation, cbJobObjectInformationLength);
                if (!res)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
                return res;
            }
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
        internal static unsafe BOOL CreateProcess(string? lpApplicationName, Span<char> lpCommandLine, SECURITY_ATTRIBUTES? lpProcessAttributes, SECURITY_ATTRIBUTES? lpThreadAttributes, BOOL bInheritHandles, PROCESS_CREATION_FLAGS dwCreationFlags, IntPtr lpEnvironment, string? lpCurrentDirectory, in STARTUPINFOW lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation)
        {
            var res = PInvoke.CreateProcess(lpApplicationName, ref lpCommandLine, lpProcessAttributes, lpThreadAttributes, bInheritHandles, dwCreationFlags, lpEnvironment.ToPointer(), (!string.IsNullOrWhiteSpace(lpCurrentDirectory) ? lpCurrentDirectory : null), lpStartupInfo, out lpProcessInformation);
            if (!res)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            return res;
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
                throw new Win32Exception(Marshal.GetLastWin32Error());
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
                throw new Win32Exception(Marshal.GetLastWin32Error());
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
                throw new Win32Exception(Marshal.GetLastWin32Error());
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
            uint lpCompletionCodeLocal; nuint lpCompletionKeyLocal; NativeOverlapped lpOverlappedLocal; NativeOverlapped* lpOverlappedLocalPointer = &lpOverlappedLocal;
            var res = PInvoke.GetQueuedCompletionStatus(CompletionPort, &lpCompletionCodeLocal, &lpCompletionKeyLocal, &lpOverlappedLocalPointer, dwMilliseconds);
            if (!res)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            lpCompletionCode = lpCompletionCodeLocal;
            lpCompletionKey = lpCompletionKeyLocal;
            lpOverlapped = lpOverlappedLocal;
            return res;
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
            uint lpExitCodeLocal;
            var res = PInvoke.GetExitCodeProcess(hProcess, &lpExitCodeLocal);
            if (!res)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            lpExitCode = lpExitCodeLocal;
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
        internal static unsafe BOOL CreatePipe(out HANDLE hReadPipe, out HANDLE hWritePipe, [Optional] SECURITY_ATTRIBUTES lpPipeAttributes, uint nSize)
        {
            HANDLE hReadPipeLocal; HANDLE hWritePipeLocal;
            var res = PInvoke.CreatePipe(&hReadPipeLocal, &hWritePipeLocal, &lpPipeAttributes, nSize);
            if (!res)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            hReadPipe = hReadPipeLocal;
            hWritePipe = hWritePipeLocal;
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
        internal static BOOL SetHandleInformation(HANDLE hObject, uint dwMask, HANDLE_FLAGS dwFlags)
        {
            var res = PInvoke.SetHandleInformation(hObject, dwMask, dwFlags);
            if (!res)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            return res;
        }

        /// <summary>
        /// Wrapper around ReadFile to manage error handling.
        /// </summary>
        /// <param name="hFile"></param>
        /// <param name="lpBuffer"></param>
        /// <param name="nNumberOfBytesToRead"></param>
        /// <param name="lpNumberOfBytesRead"></param>
        /// <param name="lpOverlapped"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal static unsafe BOOL ReadFile(HANDLE hFile, [Optional] byte[] lpBuffer, [Optional] out uint lpNumberOfBytesRead, [Optional] out NativeOverlapped lpOverlapped)
        {
            fixed (byte* pBuffer = lpBuffer)
            {
                fixed (NativeOverlapped* pOverlapped = &lpOverlapped)
                {
                    fixed (uint* pNumberOfBytesRead = &lpNumberOfBytesRead)
                    {
                        var res = PInvoke.ReadFile(hFile, pBuffer, (uint)lpBuffer.Length, pNumberOfBytesRead, pOverlapped);
                        if (!res && (WIN32_ERROR)Marshal.GetLastWin32Error() is WIN32_ERROR error && error != WIN32_ERROR.NO_ERROR && error != WIN32_ERROR.ERROR_BROKEN_PIPE)
                        {
                            throw new Win32Exception(((int)error));
                        }
                        return res;
                    }
                }
            }
        }
    }
}
