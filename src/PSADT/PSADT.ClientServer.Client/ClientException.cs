using System;

namespace PSADT.ClientServer
{
    /// <summary>
    /// Represents an exception that occurs during program execution, providing an error message and an associated
    /// exit code.
    /// </summary>
    /// <remarks>The <see cref="ClientException"/> class is used to signal errors that occur during
    /// program execution, with an optional exit code that is set to the <see cref="Exception.HResult"/> property.
    /// This allows the exception to convey both the error details and a numeric code that can be used for
    /// programmatic handling  or process termination.</remarks>
    internal class ClientException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ClientException"/> class with a specified error message
        /// and exit code.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="exitCode">The exit code associated with the exception, which is used to set the <see cref="HResult"/> property.</param>
        internal ClientException(string message, ClientExitCode exitCode) : base(message)
        {
            HResult = (int)exitCode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientException"/> class with a specified error message, 
        /// a reference to the inner exception that caused this exception, and an exit code.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or <see langword="null"/> if no inner exception is specified.</param>
        /// <param name="exitCode">The exit code associated with the exception, which is used to set the <see cref="HResult"/> property.</param>
        internal ClientException(string message, Exception innerException, ClientExitCode exitCode) : base(message, innerException)
        {
            HResult = (int)exitCode;
        }
    }
}
