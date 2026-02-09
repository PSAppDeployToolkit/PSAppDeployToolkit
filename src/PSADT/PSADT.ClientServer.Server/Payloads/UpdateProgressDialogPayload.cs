using System.Runtime.Serialization;
using PSADT.UserInterface;

namespace PSADT.ClientServer.Payloads
{
    /// <summary>
    /// Payload for the UpdateProgressDialog command.
    /// </summary>
    [DataContract]
    internal sealed record UpdateProgressDialogPayload : IPayload
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
        internal UpdateProgressDialogPayload(string? message = null, string? detailMessage = null, double? percentage = null, DialogMessageAlignment? alignment = null)
        {
            Message = message;
            DetailMessage = detailMessage;
            Percentage = percentage;
            Alignment = alignment;
        }
    }
}
