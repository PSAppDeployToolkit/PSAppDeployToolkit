using System.Runtime.InteropServices;

namespace PSADT.ProcessManagement
{
    /// <summary>
    /// Represents an exception that occurs during process management operations, providing additional context through the associated ProcessResult.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="result">The ProcessResult object containing additional context about the process execution.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Roslynator", "RCS1194:Implement exception constructors", Justification = "This is a highly specific exception for process management.")]
    public sealed class ProcessException(string message, ProcessResult result) : ExternalException(message)
    {
        /// <summary>
        /// Gets the error code of the process that caused the exception, if available.
        /// </summary>
        public override int ErrorCode => Result.ExitCode;

        /// <summary>
        /// Gets the ProcessResult associated with the process exception, if available.
        /// </summary>
        public ProcessResult Result { get; } = result;
    }
}
