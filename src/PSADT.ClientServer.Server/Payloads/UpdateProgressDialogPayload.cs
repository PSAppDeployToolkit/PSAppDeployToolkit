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
    }
}
