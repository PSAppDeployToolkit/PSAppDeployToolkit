using System;
using System.Diagnostics;

namespace PSADT.Diagnostics.StackTraces
{
    /// <summary>
    /// Represents stack trace context information, including filtered stack frames and the original exception.
    /// </summary>
    public class StackTraceContext
    {
        /// <summary>
        /// Gets the caller context containing method, file, and line number information.
        /// </summary>
        public CallerContext CallerInfo { get; }

        /// <summary>
        /// Gets the filtered stack frames that passed the filtering criteria.
        /// </summary>
        public StackFrame[]? FilteredStackFrames { get; }

        /// <summary>
        /// Gets the filtered stack trace as a string.
        /// </summary>
        public string? FilteredStackTrace { get; }

        /// <summary>
        /// Gets the original exception associated with the stack trace.
        /// </summary>
        public Exception OriginalException { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StackTraceContext"/> class.
        /// </summary>
        /// <param name="callerInfo">The caller context.</param>
        /// <param name="filteredStackFrames">The filtered stack frames.</param>
        /// <param name="filteredStackTrace">The filtered stack trace as a string.</param>
        /// <param name="originalException">The original exception associated with the stack trace.</param>
        public StackTraceContext(
            CallerContext callerInfo,
            StackFrame[] filteredStackFrames,
            string filteredStackTrace,
            Exception originalException)
        {
            CallerInfo = callerInfo ?? throw new ArgumentNullException(nameof(callerInfo));
            FilteredStackFrames = filteredStackFrames;
            FilteredStackTrace = filteredStackTrace;
            OriginalException = originalException ?? throw new ArgumentNullException(nameof(originalException));
        }

        /// <summary>
        /// Rethrows a new exception with the filtered stack trace while preserving the original exception.
        /// </summary>
        /// <param name="messageFormat">The format string used to combine the exception message and the filtered stack trace. Default is "{0}\n{1}".</param>
        public void RethrowWithFilteredStackTrace(string messageFormat = "{0}\n{1}")
        {
            string combinedMessage = CombineExceptionMessageAndFilteredTrace(messageFormat);
            throw new PSADTExceptionHandler(combinedMessage, OriginalException);
        }

        /// <summary>
        /// Combines the original exception message with the filtered stack trace for detailed logging or rethrowing.
        /// </summary>
        /// <param name="messageFormat">The format string used to combine the exception message and the filtered stack trace. Default is "{0}\n{1}".</param>
        /// <returns>Combined exception message and filtered stack trace.</returns>
        public string CombineExceptionMessageAndFilteredTrace(string messageFormat = "{0}\n{1}")
        {
            return string.Format(messageFormat, OriginalException.Message, FilteredStackTrace);
        }
    }
}
