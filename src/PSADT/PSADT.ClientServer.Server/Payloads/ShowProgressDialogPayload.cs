using System.Runtime.Serialization;
using PSADT.UserInterface;
using PSADT.UserInterface.DialogOptions;

namespace PSADT.ClientServer.Payloads
{
    /// <summary>
    /// Payload for the ShowProgressDialog command.
    /// </summary>
    [DataContract]
    internal sealed record ShowProgressDialogPayload : IPayload
    {
        /// <summary>
        /// The style of the dialog.
        /// </summary>
        [DataMember]
        internal readonly DialogStyle DialogStyle;

        /// <summary>
        /// The progress dialog options.
        /// </summary>
        [DataMember]
        internal readonly ProgressDialogOptions Options;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShowProgressDialogPayload"/> class.
        /// </summary>
        /// <param name="dialogStyle">The style of the dialog.</param>
        /// <param name="options">The progress dialog options.</param>
        internal ShowProgressDialogPayload(DialogStyle dialogStyle, ProgressDialogOptions options)
        {
            DialogStyle = dialogStyle;
            Options = options;
        }
    }
}
