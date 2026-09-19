using System;
using System.Runtime.Serialization;

namespace PSADT.ClientServer.Payloads
{
    /// <summary>
    /// Payload for the UpdateNotifyIconPayload command.
    /// </summary>
    [DataContract]
    internal sealed record class UpdateNotifyIconPayload : IClientServerPayload
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

        /// <summary>
        /// Confirms the payload's invariants once it has been read off the wire.
        /// </summary>
        /// <remarks>DataContractSerializer allocates without running a constructor, so a member the sender left
        /// out keeps its CLR default and what the constructor refuses arrives unrefused.</remarks>
        /// <param name="context">The streaming context, which is not used.</param>
        /// <exception cref="SerializationException">Thrown if the payload arrived without any message text.</exception>
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if (string.IsNullOrWhiteSpace(MessageText))
            {
                throw new SerializationException($"The deserialized {nameof(UpdateNotifyIconPayload)} has no message text.");
            }
        }
    }
}
