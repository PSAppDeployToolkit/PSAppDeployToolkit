using System;
using System.Collections.Generic;
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
    public static class ExceptionUtilities
    {
        /// <summary>
        /// Removes redundant inner exception stack trace marker lines from an exception
        /// string where the preceding line is not a stack trace entry.
        /// </summary>
        /// <param name="exceptionText">The exception string to clean up.</param>
        /// <returns>The cleaned exception string with redundant markers removed.</returns>
        public static string CollapseInnerExceptionTraceMarkers(string exceptionText)
        {
            // Internal worker method to determine if a line is an inner exception marker line.
            static bool IsInnerExceptionMarker(string line)
            {
                ReadOnlySpan<char> trimmed = line.AsSpan().Trim();
                return trimmed.Length > 6
                    && trimmed.StartsWith("---".AsSpan(), StringComparison.Ordinal)
                    && trimmed.EndsWith("---".AsSpan(), StringComparison.Ordinal)
                    && !trimmed.StartsWith("--- >".AsSpan(), StringComparison.Ordinal);
            }

            // Remove all invalid inner exception marker lines from the exception text and return the result.
            if (string.IsNullOrWhiteSpace(exceptionText))
            {
                throw new ArgumentNullException(nameof(exceptionText));
            }
            string[] lines = exceptionText.Split(["\r\n", "\n"], StringSplitOptions.None);
            List<string> result = new(lines.Length);
            for (int i = 0; i < lines.Length; i++)
            {
                if (!IsInnerExceptionMarker(lines[i]) || !(result.Count == 0 || !result[result.Count - 1].TrimStart().StartsWith("at ", StringComparison.Ordinal)))
                {
                    result.Add(lines[i]);
                }
            }
            return string.Join(Environment.NewLine, result);
        }

        /// <summary>
        /// Retrieves the last Win32 error code that occurred in the calling thread.
        /// </summary>
        /// <remarks>This method is typically used after a Win32 API call fails to obtain the specific
        /// error code that indicates the reason for the failure. It is important to call this method immediately after
        /// the failure, as subsequent API calls may overwrite the error code.</remarks>
        /// <returns>A <see cref="WIN32_ERROR"/> representing the last Win32 error code, which can be used to diagnose the cause
        /// of a failure in Win32 API calls.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0030:Do not use banned APIs", Justification = "This is our only permitted use case, to wrap the call.")]
        internal static WIN32_ERROR GetLastWin32Error()
        {
            return unchecked((WIN32_ERROR)Marshal.GetLastWin32Error());
        }

        /// <summary>
        /// Gets the exception for the last Win32 error.
        /// </summary>
        /// <returns></returns>
        internal static Exception GetExceptionForLastWin32Error()
        {
            return GetException(GetLastWin32Error());
        }

        /// <summary>
        /// Creates a new exception that represents the specified Win32 error code.
        /// </summary>
        /// <remarks>The exception message is formatted to ensure it ends with a single period, providing
        /// consistent error messages.</remarks>
        /// <param name="win32Error">The Win32 error code that identifies the error condition.</param>
        /// <param name="message">An optional custom message to include in the exception. If not provided, a default message based on the Win32 error code is used.</param>
        /// <returns>An instance of the <see cref="Exception"/> class that describes the specified Win32 error.</returns>
        internal static Exception GetException(WIN32_ERROR win32Error, string? message = null)
        {
            // Create the exception given Win32 error code. Trim the trailing period from the message and add it back to ensure consistent formatting.
            Win32Exception win32Exception = new(unchecked((int)win32Error), !string.IsNullOrWhiteSpace(message) ? message : GetMessageForWin32Error(win32Error));

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
                // Build out the Win32Exception, then see if there's a managed exception for it, otherwise return the Win32Exception.
                Win32Exception win32Exception = new(GetMessageForWin32Error(win32Error), ntStatusException);
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
        /// Retrieves the error message associated with a specified Windows error code.
        /// </summary>
        /// <remarks>This method uses the Win32Exception class to obtain the error message, ensuring that
        /// it is properly formatted by trimming any trailing periods.</remarks>
        /// <param name="win32Error">The Windows error code for which to retrieve the corresponding error message.</param>
        /// <param name="disableSuffix">Indicates whether to omit the suffix in the error message.</param>
        /// <returns>A string containing the error message associated with the specified Windows error code.</returns>
        internal static string GetMessageForWin32Error(WIN32_ERROR win32Error, bool disableSuffix = false)
        {
            string message = $"{new Win32Exception(unchecked((int)win32Error)).Message.TrimEnd('.')}.{(!disableSuffix ? $" {$"(Exception from WIN32_ERROR: 0x{unchecked((int)win32Error):X8} ({win32Error}))"}" : null)}";
            PInvoke.SetLastError(win32Error); return message;
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
