using System.Text.Json.Serialization;
using PSADT.WindowManagement;

namespace PSADT.ClientServer.Payloads
{
    /// <summary>
    /// Payload for the GetProcessWindowInfo command.
    /// </summary>
    internal sealed record GetProcessWindowInfoPayload : IPayload
    {
        /// <summary>
        /// The window info options.
        /// </summary>
        [JsonInclude]
        internal readonly WindowInfoOptions Options;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetProcessWindowInfoPayload"/> class.
        /// </summary>
        /// <param name="options">The window info options.</param>
        [JsonConstructor]
        internal GetProcessWindowInfoPayload(WindowInfoOptions options)
        {
            Options = options;
        }
    }
}
