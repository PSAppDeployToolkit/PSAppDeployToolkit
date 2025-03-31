using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.LibraryLoader;
using Windows.Win32.System.SystemInformation;

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
        internal static unsafe BOOL IsWow64Process2(SafeHandle hProcess, out IMAGE_FILE_MACHINE pProcessMachine, out IMAGE_FILE_MACHINE pNativeMachine)
        {
            IMAGE_FILE_MACHINE pNativeMachineInternal;
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
    }
}
