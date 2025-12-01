using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;

namespace PSADT.Utilities
{
    /// <summary>
    /// Provides error handling for Win32 errors.
    /// </summary>
    internal static class ExceptionUtilities
    {
        /// <summary>
        /// Gets the exception for the last Win32 error.
        /// </summary>
        /// <param name="lastWin32Error"></param>
        /// <returns></returns>
        internal static Exception GetExceptionForLastWin32Error(WIN32_ERROR? lastWin32Error = null)
        {
            var win32ErrorCode = lastWin32Error.HasValue ? unchecked((int)lastWin32Error.Value) : Marshal.GetLastWin32Error();
            var marshalException = Marshal.GetExceptionForHR(GetHRForWin32ErrorCode(win32ErrorCode));
            return marshalException is not null && marshalException is not COMException ? marshalException : new Win32Exception(win32ErrorCode);
        }

        /// <summary>
        /// Gets the HRESULT for the last Win32 error.
        /// </summary>
        /// <param name="win32ErrorCode"></param>
        /// <returns></returns>
        private static int GetHRForWin32ErrorCode(int win32ErrorCode) => (win32ErrorCode & 0x80000000u) != 2147483648u ? win32ErrorCode & 0xFFFF | -2147024896 : win32ErrorCode;
    }
}
