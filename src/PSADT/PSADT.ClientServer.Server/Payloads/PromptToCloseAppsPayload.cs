using System;
using System.Runtime.Serialization;

namespace PSADT.ClientServer.Payloads
{
    /// <summary>
    /// Payload for the PromptToCloseApps command.
    /// </summary>
    [DataContract]
    internal sealed record PromptToCloseAppsPayload : IClientServerPayload
    {
        /// <summary>
        /// The timeout duration for the prompt.
        /// </summary>
        [DataMember]
        internal readonly TimeSpan Timeout;

        /// <summary>
        /// Initializes a new instance of the <see cref="PromptToCloseAppsPayload"/> class.
        /// </summary>
        /// <param name="timeout">The timeout duration for the prompt.</param>
        internal PromptToCloseAppsPayload(TimeSpan timeout)
        {
            Timeout = timeout;
        }
    }
}
