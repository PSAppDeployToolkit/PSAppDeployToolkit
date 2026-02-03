using System.Text.Json.Serialization;
using PSADT.ClientServer.Payloads;

namespace PSADT.ClientServer
{
    /// <summary>
    /// Represents a request sent from the server to the client through the pipe communication channel.
    /// </summary>
    /// <remarks>This class encapsulates all information needed to execute a command on the client side,
    /// including the command type, optional payload data, and logging source information.</remarks>
    internal sealed record PipeRequest
    {
        /// <summary>
        /// The command to execute on the client.
        /// </summary>
        [JsonInclude]
        internal readonly PipeCommand Command;

        /// <summary>
        /// The optional payload data for the command.
        /// </summary>
        /// <remarks>The payload type varies depending on the command. For commands that don't require
        /// additional data, this field may be null.</remarks>
        [JsonInclude]
        internal readonly object? Payload;

        /// <summary>
        /// Initializes a new instance of the <see cref="PipeRequest"/> class.
        /// </summary>
        /// <param name="command">The command to execute on the client.</param>
        /// <param name="payload">The optional payload data for the command.</param>
        [JsonConstructor]
        internal PipeRequest(PipeCommand command, IPayload? payload = null)
        {
            Command = command;
            Payload = payload;
        }
    }
}
