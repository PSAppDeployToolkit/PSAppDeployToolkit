using System;
using System.Runtime.Serialization;

namespace PSADT.ClientServer.Payloads
{
    /// <summary>
    /// Payload for the PromptToCloseApps command.
    /// </summary>
    /// <param name="Timeout">The timeout duration for the prompt.</param>
    [DataContract]
    internal sealed record class PromptToCloseAppsPayload(TimeSpan Timeout) : IClientServerPayload
    {
        /// <summary>
        /// The timeout duration for the prompt.
        /// </summary>
        [DataMember]
        internal readonly TimeSpan Timeout = Timeout;
    }
}
