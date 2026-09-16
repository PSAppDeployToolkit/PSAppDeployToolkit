using System;
using System.Runtime.Serialization;
using PSAppDeployToolkit.Logging;

namespace PSADT.ClientServer.Payloads
{
    /// <summary>
    /// Represents a log message sent from the client to the server.
    /// </summary>
    [DataContract]
    internal sealed record class LogMessagePayload : IClientServerPayload
    {
        /// <summary>
        /// The log message text.
        /// </summary>
        [DataMember]
        internal readonly string Message;

        /// <summary>
        /// The log severity level.
        /// </summary>
        [DataMember]
        internal readonly LogSeverity Severity;

        /// <summary>
        /// The source of the log message. Typically matches the PowerShell function's name.
        /// </summary>
        [DataMember]
        internal readonly string Source;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogMessagePayload"/> class.
        /// </summary>
        /// <param name="message">The log message text.</param>
        /// <param name="severity">The log severity level.</param>
        /// <param name="source">The source of the log message.</param>
        internal LogMessagePayload(string message, LogSeverity severity, string source)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(message);
            ArgumentException.ThrowIfNullOrWhiteSpace(source);
            Message = message;
            Severity = severity;
            Source = source;
        }

        /// <summary>
        /// Confirms the payload's invariants once it has been read off the wire.
        /// </summary>
        /// <remarks>DataContractSerializer allocates without running a constructor and assigns the members it
        /// finds, so a member the sender simply left out keeps its CLR default and everything the constructor
        /// refuses arrives unrefused. The sender is the client, which runs in the logged-on user's session and
        /// is therefore theirs, so this is the only point at which what it chose is tested at all.</remarks>
        /// <param name="context">The streaming context, which is not used.</param>
        /// <exception cref="SerializationException">Thrown if the payload arrived without a message or a source.</exception>
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if (string.IsNullOrWhiteSpace(Message))
            {
                throw new SerializationException($"The deserialized {nameof(LogMessagePayload)} has no message.");
            }
            if (string.IsNullOrWhiteSpace(Source))
            {
                throw new SerializationException($"The deserialized {nameof(LogMessagePayload)} has no source.");
            }
        }
    }
}
