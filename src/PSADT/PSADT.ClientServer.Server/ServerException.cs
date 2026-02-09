using System;
using System.Runtime.Serialization;
using PSADT.ProcessManagement;

namespace PSADT.ClientServer
{
    /// <summary>
    /// Represents an exception that occurs on the server side during application execution.
    /// </summary>
    /// <remarks>This exception is typically thrown to indicate an error condition specific to server-side
    /// operations. It extends <see cref="InvalidOperationException"/> to provide additional context for server-related
    /// errors.</remarks>
    [Serializable]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "The constructors we have are fine for our internal usage.")]
    internal sealed class ServerException : InvalidOperationException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServerException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        internal ServerException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ServerException class with a specified error message and the associated
        /// client process.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="clientProcess">The process handle representing the client process related to the exception.</param>
        internal ServerException(string message, ProcessHandle clientProcess) : base(message)
        {
            _clientProcess = clientProcess;
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
            _clientProcess = clientProcess;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerException"/> class with serialized data.
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
        private ServerException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        /// <summary>
        /// Populates a <see cref="SerializationInfo"/> object with the data needed to serialize the current object.
        /// </summary>
        /// <remarks>This method overrides the base implementation to provide serialization support for
        /// the current object. The <see cref="ClientProcess"/> property is intentionally not serialized
        /// as it is a runtime-only handle that cannot cross process boundaries.</remarks>
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

        /// <summary>
        /// Gets the handle to the client process associated with the current context.
        /// </summary>
        /// <remarks>This property is not serialized as it represents a runtime-only handle
        /// that cannot cross process boundaries.</remarks>
        public ProcessHandle? ClientProcess => _clientProcess;

        /// <summary>
        /// Represents the handle to the client process associated with this instance.
        /// </summary>
        /// <remarks>This field is not serialized when the containing object is serialized. It is intended
        /// for internal use and should not be accessed directly by consumers of the class.</remarks>
        [NonSerialized]
        private readonly ProcessHandle? _clientProcess;
    }
}
