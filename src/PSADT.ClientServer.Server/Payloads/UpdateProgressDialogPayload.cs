using System;
using System.Runtime.Serialization;
using PSADT.UserInterface;

namespace PSADT.ClientServer.Payloads
{
    /// <summary>
    /// Payload for the UpdateProgressDialog command.
    /// </summary>
    [DataContract]
    internal sealed record class UpdateProgressDialogPayload : IClientServerPayload
    {
        /// <summary>
        /// The main progress message.
        /// </summary>
        /// <remarks>When null, the existing message is retained.</remarks>
        [DataMember]
        internal readonly string? Message;

        /// <summary>
        /// The detail progress message.
        /// </summary>
        /// <remarks>When null, the existing detail message is retained.</remarks>
        [DataMember]
        internal readonly string? DetailMessage;

        /// <summary>
        /// The progress percentage (0-100).
        /// </summary>
        /// <remarks>When null, the existing percentage is retained.</remarks>
        [DataMember]
        internal readonly double? Percentage;

        /// <summary>
        /// The message alignment.
        /// </summary>
        /// <remarks>When null, the existing alignment is retained.</remarks>
        [DataMember]
        internal readonly DialogMessageAlignment? Alignment;

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateProgressDialogPayload"/> class.
        /// </summary>
        /// <param name="message">The main progress message.</param>
        /// <param name="detailMessage">The detail progress message.</param>
        /// <param name="percentage">The progress percentage (0-100).</param>
        /// <param name="alignment">The message alignment.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="percentage"/> has a value that is not between 0 and 100.</exception>
        internal UpdateProgressDialogPayload(string? message = null, string? detailMessage = null, double? percentage = null, DialogMessageAlignment? alignment = null)
        {
            if (message is not null)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(message);
            }
            if (detailMessage is not null)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(detailMessage);
            }
            if (percentage is double value && (double.IsNaN(value) || value is < 0.0 or > 100.0))
            {
                throw new ArgumentOutOfRangeException(nameof(percentage), value, "The progress percentage must be between 0 and 100.");
            }
            Message = message;
            DetailMessage = detailMessage;
            Percentage = percentage;
            Alignment = alignment;
        }

        /// <summary>
        /// Confirms the payload's invariants once it has been read off the wire.
        /// </summary>
        /// <remarks>DataContractSerializer allocates without running a constructor, so a member the sender left
        /// out keeps its CLR default and what the constructor refuses arrives unrefused. Every member here is
        /// optional, so each is checked only where the sender gave one.</remarks>
        /// <param name="context">The streaming context, which is not used.</param>
        /// <exception cref="SerializationException">Thrown if the payload arrived with a blank message or a percentage outside 0 to 100.</exception>
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if (Message is not null && string.IsNullOrWhiteSpace(Message))
            {
                throw new SerializationException($"The deserialized {nameof(UpdateProgressDialogPayload)} has a blank message.");
            }
            if (DetailMessage is not null && string.IsNullOrWhiteSpace(DetailMessage))
            {
                throw new SerializationException($"The deserialized {nameof(UpdateProgressDialogPayload)} has a blank detail message.");
            }
            if (Percentage is double value && (double.IsNaN(value) || value is < 0.0 or > 100.0))
            {
                throw new SerializationException($"The deserialized {nameof(UpdateProgressDialogPayload)} has a progress percentage that is not between 0 and 100.");
            }
        }
    }
}
