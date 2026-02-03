using System;
using System.Text.Json.Serialization;
using PSAppDeployToolkit.Logging;

namespace PSADT.ClientServer.Payloads
{
    /// <summary>
    /// Represents a log message sent from the client to the server.
    /// </summary>
    internal sealed record LogMessagePayload : IPayload
    {
        /// <summary>
        /// The log message text.
        /// </summary>
        [JsonInclude]
        internal readonly string Message;

        /// <summary>
        /// The log severity level.
        /// </summary>
        [JsonInclude]
        internal readonly LogSeverity Severity;

        /// <summary>
        /// The source of the log message. Typically matches the PowerShell function's name.
        /// </summary>
        [JsonInclude]
        internal readonly string Source;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogMessagePayload"/> class.
        /// </summary>
        /// <param name="message">The log message text.</param>
        /// <param name="severity">The log severity level.</param>
        /// <param name="source">The source of the log message.</param>
        [JsonConstructor]
        internal LogMessagePayload(string message, LogSeverity severity, string source)
        {
            Message = !string.IsNullOrWhiteSpace(message) ? message : throw new ArgumentNullException(nameof(message), "Message cannot be null or whitespace.");
            Source = !string.IsNullOrWhiteSpace(source) ? source : throw new ArgumentNullException(nameof(source), "Source cannot be null or whitespace.");
            Severity = severity;
        }
    }
}
