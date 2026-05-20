using System;
using System.Runtime.Serialization;

namespace PSADT.ClientServer.Payloads
{
    /// <summary>
    /// Payload for the UpdateNotifyIconPayload command.
    /// </summary>
    [DataContract]
    internal sealed record UpdateNotifyIconPayload : IClientServerPayload
    {
        /// <summary>
        /// The text content of the message.
        /// </summary>
        [DataMember]
        internal readonly string MessageText;

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateNotifyIconPayload"/> class with the specified message
        /// text.
        /// </summary>
        /// <param name="messageText">The message text to display.</param>
        internal UpdateNotifyIconPayload(string messageText)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(messageText);
            MessageText = messageText;
        }
    }
}
