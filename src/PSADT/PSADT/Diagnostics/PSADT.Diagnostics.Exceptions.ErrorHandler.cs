using System;
using System.Linq;
using System.ComponentModel;
using System.Management.Automation;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using PSADT.PInvokes;

namespace PSADT.Diagnostics.Exceptions
{
    /// <summary>
    /// Provides utility methods for handling and interpreting HRESULT, Win32, .NET, and PowerShell errors.
    /// </summary>
    public static class ErrorHandler
    {
        private static readonly System.Management.Automation.MethodException _noException = new MethodException();

        public static Exception NoMethodException { get; } = _noException;

        /// <summary>
        /// Checks whether the specified HRESULT indicates a failure.
        /// </summary>
        /// <param name="hresult">The HRESULT value to check.</param>
        /// <returns>True if the HRESULT indicates a failure; otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsFailure(int hresult) => hresult < 0;

        /// <summary>
        /// Checks whether the specified HRESULT indicates success.
        /// </summary>
        /// <param name="hresult">The HRESULT value to check.</param>
        /// <returns>True if the HRESULT indicates success; otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSuccess(int hresult) => hresult >= 0;

        #region Win32 and COM Errors

        /// <summary>
        /// Unified entry point to retrieve an exception for both Win32 and COM errors.
        /// </summary>
        /// <param name="message">Optional message to include in the exception.</param>
        /// <param name="errorType">The type of error to retrieve: "Win32" or "COM".</param>
        /// <returns>The corresponding exception for the error or <see cref="NoMethodException"/> if no error was found.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Exception GetSystemError([Optional, DefaultParameterValue(null)] string? message, SystemErrorType errorType)
        {
            if (errorType == SystemErrorType.COM)
            {
                return GetLastComError(message);
            }
            else if (errorType == SystemErrorType.Win32)
            {
                return GetLastWin32Error(message);
            }

            // If unknown error type is provided, return NoMethodException
            return _noException;
        }

        /// <summary>
        /// Unified entry point to throw an exception for both Win32 and COM errors.
        /// </summary>
        /// <param name="message">Optional message to include in the exception.</param>
        /// <param name="errorType">The type of error to throw: "Win32" or "COM".</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowSystemError([Optional, DefaultParameterValue(null)] string? message, SystemErrorType errorType)
        {
            Exception systemException = GetSystemError(message, errorType);
            if (systemException != _noException)
            {
                throw systemException;
            }
        }

        /// <summary>
        /// Retrieves an exception based on the last Win32 error, with an optional custom message.
        /// </summary>
        /// <param name="message">Optional message to include in the exception.</param>
        /// <returns>The corresponding exception for the last Win32 error or <see cref="NoMethodException"/> if no error was found.</returns>
        private static Exception GetLastWin32Error(string? message = null)
        {
            int errorCode = Marshal.GetLastWin32Error();
            int hresult = HRESULT_FROM_WIN32(errorCode);

            if (IsFailure(hresult))
            {
                Exception? innerWin32Exception = Marshal.GetExceptionForHR(hresult);

                string? innerExceptionMessage = !string.IsNullOrWhiteSpace(innerWin32Exception?.Message)
                    ? innerWin32Exception!.Message
                    : null;

                return new Exception(
                    $"{message ?? (innerExceptionMessage == null ? "Unknown error occurred." : $"Exception Message: {innerExceptionMessage}.")} Error Code [{errorCode}].",
                    innerWin32Exception ?? new Win32Exception(errorCode)
                );
            }

            // Return NoMethodException to indicate no error was found.
            return _noException;
        }

        /// <summary>
        /// Retrieves an exception based on the last COM error (HRESULT) with additional information from IErrorInfo if available.
        /// </summary>
        /// <param name="message">Optional message to include in the exception.</param>
        /// <returns>The corresponding exception for the last COM error or <see cref="NoMethodException"/> if no error was found.</returns>
        private static Exception GetLastComError(string? message = null)
        {
            int hresult = Marshal.GetLastWin32Error();

            if (IsFailure(hresult))
            {
                using SafeErrorInfoHandle errorInfoHandle = GetCurrentErrorInfo();

                // Convert HRESULT to an Exception, considering IErrorInfo if available
                Exception? innerCOMException = Marshal.GetExceptionForHR(hresult, errorInfoHandle.DangerousGetHandle());

                string? innerExceptionMessage = !string.IsNullOrWhiteSpace(innerCOMException?.Message)
                    ? innerCOMException!.Message
                    : null;

                return new Exception(
                    $"{message ?? (innerExceptionMessage == null ? "Unknown COM error occurred." : $"Exception Message: {innerExceptionMessage}.")} COM Error Code [0x{hresult:X8}].",
                    innerCOMException ?? new COMException($"Unknown COM Error [0x{hresult:X8}]", hresult)
                );
            }

            // Return NoMethodException to indicate no error was found.
            return _noException;
        }

        #endregion

        #region .NET and PowerShell Exceptions


        /// <summary>
        /// Retrieves an exception based on either a .NET exception or PowerShell ErrorRecord.
        /// Handles AggregateException for .NET exceptions automatically.
        /// </summary>
        /// <param name="error">The object to process, either an Exception or ErrorRecord.</param>
        /// <param name="message">Optional message to include in the exception.</param>
        /// <returns>The corresponding exception or <see cref="NoMethodException"/> if no error was found.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Exception GetException(object? error, string? message = null)
        {
            if (error is Exception exception)
            {
                return GetNetException(exception, message);
            }
            else if (error is ErrorRecord errorRecord)
            {
                return GetPowerShellError(errorRecord, message);
            }

            // Return NoMethodException to indicate no error was found.
            return _noException;
        }

        /// <summary>
        /// Retrieves a .NET exception, handling both regular and AggregateExceptions.
        /// </summary>
        /// <param name="exception">The .NET exception to process.</param>
        /// <param name="message">Optional message to include in the exception.</param>
        /// <returns>The corresponding exception or <see cref="NoMethodException"/> if no error was found.</returns>
        private static Exception GetNetException(Exception exception, string? message = null)
        {
            // Check if it's an AggregateException and flatten it.
            if (exception is AggregateException aggregateException)
            {
                aggregateException = aggregateException.Flatten();
                var innerMessages = string.Join("; ", aggregateException.InnerExceptions.Select(e => e.Message));
                return new Exception($"{message ?? "AggregateException occurred."} Inner Exceptions: {innerMessages}.", aggregateException);
            }

            // Handle regular exceptions
            string exceptionMessage = string.IsNullOrWhiteSpace(exception.Message) ? "Unknown .NET error occurred." : exception.Message;
            return new Exception($"{message ?? exceptionMessage}", exception);
        }

        /// <summary>
        /// Retrieves an exception based on a PowerShell ErrorRecord.
        /// </summary>
        /// <param name="errorRecord">The PowerShell ErrorRecord to process.</param>
        /// <param name="message">Optional message to include in the exception.</param>
        /// <returns>The corresponding exception based on the ErrorRecord.</returns>
        private static Exception GetPowerShellError(ErrorRecord errorRecord, string? message = null)
        {
            return new Exception($"{message ?? errorRecord.Exception.Message} PowerShell Error: {errorRecord}", errorRecord.Exception);
        }

        /// <summary>
        /// Throws an exception based on either a .NET exception or PowerShell ErrorRecord.
        /// </summary>
        /// <param name="error">The object to process, either an Exception or ErrorRecord.</param>
        /// <param name="message">Optional message to include in the exception.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowException(object? error, string? message = null)
        {
            Exception exception = GetException(error, message);
            if (exception != _noException)
            {
                throw exception;
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Retrieves the current IErrorInfo for the current thread.
        /// </summary>
        /// <returns>A <see cref="SafeErrorInfoHandle"/> to the IErrorInfo interface or <see cref="SafeErrorInfoHandle"/> if no error info is available.</returns>
        private static SafeErrorInfoHandle GetCurrentErrorInfo()
        {
            NativeMethods.GetErrorInfo(0, out SafeErrorInfoHandle errorInfoHandle);

            return errorInfoHandle;
        }


        /// <summary>
        /// Converts a Win32 error code to an HRESULT value.
        /// </summary>
        /// <param name="win32ErrorCode">The Win32 error code.</param>
        /// <returns>The corresponding HRESULT value.</returns>
        private static int HRESULT_FROM_WIN32(int win32ErrorCode)
        {
            return win32ErrorCode <= 0
                ? win32ErrorCode
                : unchecked((int)(0x80070000 | ((uint)win32ErrorCode & 0xFFFF)));
        }

        #endregion
    }
}
