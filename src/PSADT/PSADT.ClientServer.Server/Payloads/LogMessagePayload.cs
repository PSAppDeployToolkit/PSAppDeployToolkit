using System;
using System.Runtime.Serialization;
using PSAppDeployToolkit.Logging;

namespace PSADT.ClientServer.Payloads
{
    /// <summary>
    /// Represents a log message sent from the client to the server.
    /// </summary>
    [DataContract]
    internal sealed record LogMessagePayload : IClientServerPayload
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
            Message = !string.IsNullOrWhiteSpace(message) ? message : throw new ArgumentNullException(nameof(message), "Message cannot be null or whitespace.");
            Source = !string.IsNullOrWhiteSpace(source) ? source : throw new ArgumentNullException(nameof(source), "Source cannot be null or whitespace.");
            Severity = severity;
        }
    }
}
