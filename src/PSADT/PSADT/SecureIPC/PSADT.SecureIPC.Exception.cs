﻿using System;
using System.Runtime.Serialization;

namespace PSADT.SecureIPC
{
    /// <summary>
    /// Represents errors that occur during secure named pipe operations.
    /// </summary>
    [Serializable]
    public class SecureNamedPipeException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SecureNamedPipeException"/> class.
        /// </summary>
        public SecureNamedPipeException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SecureNamedPipeException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public SecureNamedPipeException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SecureNamedPipeException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="inner">The exception that is the cause of the current exception.</param>
        public SecureNamedPipeException(string message, Exception inner)
            : base(message, inner)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SecureNamedPipeException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data.</param>
        /// <param name="context">The StreamingContext that contains contextual information.</param>
        protected SecureNamedPipeException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
