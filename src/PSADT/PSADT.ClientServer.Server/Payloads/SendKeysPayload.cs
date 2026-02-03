using System.Text.Json.Serialization;
using PSADT.Types;

namespace PSADT.ClientServer.Payloads
{
    /// <summary>
    /// Payload for the SendKeys command.
    /// </summary>
    internal sealed record SendKeysPayload : IPayload
    {
        /// <summary>
        /// The send keys options.
        /// </summary>
        [JsonInclude]
        internal readonly SendKeysOptions Options;

        /// <summary>
        /// Initializes a new instance of the <see cref="SendKeysPayload"/> class.
        /// </summary>
        /// <param name="options">The send keys options.</param>
        [JsonConstructor]
        internal SendKeysPayload(SendKeysOptions options)
        {
            Options = options;
        }
    }
}
