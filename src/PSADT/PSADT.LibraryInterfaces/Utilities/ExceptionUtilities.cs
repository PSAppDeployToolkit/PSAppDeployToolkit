using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using PSADT.LibraryInterfaces.Exceptions;
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
        /// <returns></returns>
        internal static Exception GetExceptionForLastWin32Error()
        {
            return GetException(unchecked((WIN32_ERROR)Marshal.GetLastWin32Error()));
        }

        /// <summary>
        /// Creates a new exception that represents the specified Win32 error code.
        /// </summary>
        /// <remarks>The exception message is formatted to ensure it ends with a single period, providing
        /// consistent error messages.</remarks>
        /// <param name="win32Error">The Win32 error code that identifies the error condition.</param>
        /// <returns>An instance of the <see cref="Exception"/> class that describes the specified Win32 error.</returns>
        internal static Exception GetException(WIN32_ERROR win32Error)
        {
            // Create the exception given Win32 error code. Trim the trailing period from the message and add it back to ensure consistent formatting.
            Win32Exception win32Exception = new(unchecked((int)win32Error), new Win32Exception((int)win32Error).Message.TrimEnd('.') + '.');

            // Try for an ManagedException > Win32Exception based on the WIN32_ERROR code, falling back as appripriate.
            if (GetException(HRESULT_FROM_WIN32(win32Error), win32Exception) is Exception hrException)
            {
                // There was a managed exception for the HRESULT corresponding to the WIN32_ERROR code, return that instead of the Win32Exception.
                return hrException;
            }
            else
            {
                // Just return the Win32Exception with the message from FormatMessage for the WIN32_ERROR code.
                return win32Exception;
            }
        }

        /// <summary>
        /// Creates an exception that best represents the error condition indicated by the specified NTSTATUS code.
        /// </summary>
        /// <remarks>This method attempts to map the NTSTATUS code to a more user-friendly exception type,
        /// prioritizing managed exceptions and Win32Exception when possible. If no suitable mapping is found, it
        /// returns an NtStatusException. The exception message is formatted consistently for clarity.</remarks>
        /// <param name="ntStatus">The NTSTATUS code that identifies the error to be converted into an exception.</param>
        /// <returns>An exception corresponding to the provided NTSTATUS code. The returned exception may be a Win32Exception,
        /// NtStatusException, or another managed exception, depending on the mapping.</returns>
        internal static Exception GetException(NTSTATUS ntStatus)
        {
            // Generate the base NtStatusException for use throughout the method.
            NtStatusException ntStatusException = new(ntStatus);

            // Try for an ManagedException > Win32Exception > NtStatusException based on the NTSTATUS code, falling back as appripriate.
            if (WIN32_FROM_NT(ntStatus) is WIN32_ERROR win32Error)
            {
                // Trim the trailing period from the message and add it back to ensure consistent formatting. It's
                // crucial we call SetLastError() after as the first Win32Exception call can clobber the last error.
                string message = new Win32Exception((int)win32Error).Message.TrimEnd('.') + '.'; PInvoke.SetLastError(win32Error);
                Win32Exception win32Exception = new(message, ntStatusException);
                return GetException(HRESULT_FROM_WIN32(win32Error), win32Exception) is Exception hrException ? hrException : win32Exception;
            }
            else if (GetException(HRESULT_FROM_NT(ntStatus), ntStatusException) is Exception hrException)
            {
                // There was no suitable Win32Exception, however there was a managed exception for the HRESULT corresponding to the NTSTATUS code.
                return hrException;
            }
            else
            {
                // Just return an NtStatusException with the message from FormatMessage for the NTSTATUS code.
                return ntStatusException;
            }
        }

        /// <summary>
        /// Converts an NTSTATUS code to the corresponding Windows error code, if available.
        /// </summary>
        /// <remarks>If the NTSTATUS code cannot be mapped to a Windows error code, the method returns
        /// null instead of WIN32_ERROR.ERROR_MR_MID_NOT_FOUND.</remarks>
        /// <param name="ntStatus">The NTSTATUS code to convert to a Windows error code.</param>
        /// <returns>A WIN32_ERROR value that corresponds to the specified NTSTATUS code, or null if the NTSTATUS code does not
        /// map to a known Windows error code.</returns>
        internal static WIN32_ERROR? WIN32_FROM_NT(NTSTATUS ntStatus)
        {
            WIN32_ERROR win32Error = (WIN32_ERROR)PInvoke.RtlNtStatusToDosError(ntStatus);
            return win32Error != WIN32_ERROR.ERROR_MR_MID_NOT_FOUND ? win32Error : null;
        }

        /// <summary>
        /// Converts an NTSTATUS value to an HRESULT. Equivalent to the HRESULT_FROM_NT macro.
        /// </summary>
        /// <param name="ntStatus">The NTSTATUS value to convert.</param>
        /// <returns>The corresponding HRESULT value.</returns>
        internal static HRESULT HRESULT_FROM_NT(NTSTATUS ntStatus)
        {
            return new HRESULT(unchecked((int)(unchecked((uint)ntStatus.Value) | (uint)FACILITY_CODE.FACILITY_NT_BIT)));
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

        /// <summary>
        /// Creates an exception instance that corresponds to the specified HRESULT value, optionally associating it
        /// with an inner exception for additional context.
        /// </summary>
        /// <remarks>This method attempts to map the HRESULT to a known exception type and inject the
        /// provided inner exception. If the HRESULT does not correspond to a recognized exception or if instantiation
        /// fails, null is returned.</remarks>
        /// <param name="hResult">The HRESULT value that identifies the error condition for which an exception should be created.</param>
        /// <param name="innerException">An optional exception that provides additional context and will be set as the InnerException of the created
        /// exception.</param>
        /// <returns>An exception instance that represents the specified HRESULT value, or null if no suitable exception can be
        /// created.</returns>
        private static Exception? GetException(HRESULT hResult, Exception innerException)
        {
            // Return early if there's no suitable exception to get for the HRESULT value.
            if (Marshal.GetExceptionForHR(hResult) is not Exception hrException || hrException is COMException)
            {
                return null;
            }

            // Create a new instance of the exception using reflection so we can inject an InnerException.
            try
            {
                if (Activator.CreateInstance(hrException.GetType(), hrException.Message, innerException) is Exception createdException)
                {
                    return createdException;
                }
            }
            catch
            {
                return null;
                throw;
            }
            return null;
        }
    }
}
