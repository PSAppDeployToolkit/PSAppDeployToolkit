using System;

namespace PSADT.ClientServer
{
    /// <summary>
    /// Represents an exception that occurs on the server side during application execution.
    /// </summary>
    /// <remarks>This exception is typically thrown to indicate an error condition specific to server-side
    /// operations. It extends <see cref="InvalidOperationException"/> to provide additional context for server-related
    /// errors.</remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "The constructors we have are fine for our internal usage.")]
    internal class ServerException : InvalidOperationException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServerException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        internal ServerException(string message) : base(message)
        {
        }

        /// <summary>
        /// Represents an exception that occurs on the server side during application execution.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that caused the current exception, or <see langword="null"/> if no inner exception is
        /// specified.</param>
        internal ServerException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
