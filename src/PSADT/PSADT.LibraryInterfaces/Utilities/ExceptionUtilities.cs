using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Diagnostics.Debug;

namespace PSADT.LibraryInterfaces.Utilities
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
            WIN32_ERROR win32ErrorCode = lastWin32Error ?? unchecked((WIN32_ERROR)Marshal.GetLastWin32Error());
            Exception? marshalException = Marshal.GetExceptionForHR(HRESULT_FROM_WIN32(win32ErrorCode).Value);
            return marshalException is null || (marshalException is COMException && win32ErrorCode != WIN32_ERROR.ERROR_MR_MID_NOT_FOUND)
                ? new Win32Exception(unchecked((int)win32ErrorCode))
                : marshalException;
        }

        /// <summary>
        /// Converts a Win32 error code to its corresponding HRESULT value.
        /// </summary>
        /// <remarks>Use this method to translate Win32 error codes into HRESULT values for
        /// interoperability with APIs that require HRESULT-based error handling.</remarks>
        /// <param name="win32Error">The Win32 error code to convert. This value should be a valid member of the WIN32_ERROR enumeration.</param>
        /// <returns>An HRESULT value that represents the specified Win32 error code.</returns>
        internal static HRESULT HRESULT_FROM_WIN32(WIN32_ERROR win32Error)
        {
            return PInvoke.HRESULT_FROM_WIN32(win32Error);
        }
    }
}
