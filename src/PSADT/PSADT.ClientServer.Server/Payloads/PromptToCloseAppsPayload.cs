using System;
using System.Text.Json.Serialization;

namespace PSADT.ClientServer.Payloads
{
    /// <summary>
    /// Payload for the PromptToCloseApps command.
    /// </summary>
    internal sealed record PromptToCloseAppsPayload : IPayload
    {
        /// <summary>
        /// The timeout duration for the prompt.
        /// </summary>
        [JsonInclude]
        internal readonly TimeSpan Timeout;

        /// <summary>
        /// Initializes a new instance of the <see cref="PromptToCloseAppsPayload"/> class.
        /// </summary>
        /// <param name="timeout">The timeout duration for the prompt.</param>
        [JsonConstructor]
        internal PromptToCloseAppsPayload(TimeSpan timeout)
        {
            Timeout = timeout;
        }
    }
}
