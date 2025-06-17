using System;
using System.Runtime.Serialization;

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
    [Serializable]
    internal class ClientException : InvalidOperationException
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
        /// <param name="exitCode">The exit code associated with the exception, which is used to set the <see cref="HResult"/> property.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or <see langword="null"/> if no inner exception is specified.</param>
        internal ClientException(string message, ClientExitCode exitCode, Exception innerException) : base(message, innerException)
        {
            HResult = (int)exitCode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientException"/> class with serialized data.
        /// </summary>
        /// <remarks>This constructor is used during deserialization to reconstruct the exception object
        /// transmitted over a stream.</remarks>
        /// <param name="info">The <see cref="SerializationInfo"/> object that holds the serialized object data about the exception being
        /// thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> object that contains contextual information about the source or
        /// destination.</param>
        #if NET8_0_OR_GREATER
        [Obsolete(DiagnosticId = "SYSLIB0051")]
        #endif
        protected ClientException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        /// <summary>
        /// Populates a <see cref="SerializationInfo"/> object with the data needed to serialize the current object.
        /// </summary>
        /// <remarks>This method overrides the base implementation to provide serialization support for
        /// the current object.</remarks>
        /// <param name="info">The <see cref="SerializationInfo"/> object to populate with serialization data. Cannot be <see
        /// langword="null"/>.</param>
        /// <param name="context">The <see cref="StreamingContext"/> structure that contains the source and destination of the serialized
        /// stream.</param>
        #if NET8_0_OR_GREATER
        [Obsolete(DiagnosticId = "SYSLIB0051")]
        #endif
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }
    }
}
