using System.Runtime.Serialization;
using PSADT.UserInterface;
using PSADT.UserInterface.DialogOptions;

namespace PSADT.ClientServer.Payloads
{
    /// <summary>
    /// Payload for the ShowProgressDialog command.
    /// </summary>
    /// <param name="DialogStyle">The style of the dialog.</param>
    /// <param name="Options">The progress dialog options.</param>
    [DataContract]
    internal sealed record class ShowProgressDialogPayload(DialogStyle DialogStyle, ProgressDialogOptions Options) : IClientServerPayload
    {
        /// <summary>
        /// The style of the dialog.
        /// </summary>
        [DataMember]
        internal readonly DialogStyle DialogStyle = DialogStyle;

        /// <summary>
        /// The progress dialog options.
        /// </summary>
        [DataMember]
        internal readonly ProgressDialogOptions Options = Options;
    }
}
