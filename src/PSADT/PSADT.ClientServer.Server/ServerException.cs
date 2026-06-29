using System;
using PSADT.ProcessManagement;

namespace PSADT.ClientServer
{
    /// <summary>
    /// Represents an exception that occurs on the server side during application execution.
    /// </summary>
    /// <remarks>This exception is typically thrown to indicate an error condition specific to server-side
    /// operations. It extends <see cref="InvalidOperationException"/> to provide additional context for server-related
    /// errors.</remarks>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that caused the current exception, or <see langword="null"/> if no inner exception is
    /// specified.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "The constructors we have are fine for our internal usage.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Roslynator", "RCS1194:Implement exception constructors", Justification = "The constructors we have are fine for our internal usage.")]
    internal sealed class ServerException(string message, Exception? innerException) : InvalidOperationException(message, innerException)
    {
        /// <summary>
        /// Initializes a new instance of the ServerException class with a specified error message and the associated
        /// client process.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="clientProcess">The process handle representing the client process related to the exception.</param>
        internal ServerException(string message, ProcessHandle clientProcess) : this(message, (Exception?)null)
        {
            ClientProcess = clientProcess;
        }

        /// <summary>
        /// Initializes a new instance of the ServerException class with a specified error message, a reference to the
        /// inner exception that is the cause of this exception, and the associated client process.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is
        /// specified.</param>
        /// <param name="clientProcess">The process handle representing the client process associated with this exception.</param>
        internal ServerException(string message, Exception innerException, ProcessHandle clientProcess) : this(message, innerException)
        {
            ClientProcess = clientProcess;
        }

        /// <summary>
        /// Gets the handle to the client process associated with the current context.
        /// </summary>
        /// <remarks>This property is not serialized as it represents a runtime-only handle
        /// that cannot cross process boundaries.</remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0137:Use 'Async' suffix when a method returns an awaitable type", Justification = "This is OK here.")]
        public ProcessHandle? ClientProcess { get; }
    }
}
