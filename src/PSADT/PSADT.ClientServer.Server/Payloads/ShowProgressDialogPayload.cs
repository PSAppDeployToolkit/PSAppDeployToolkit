using System.Text.Json.Serialization;
using PSADT.UserInterface;
using PSADT.UserInterface.DialogOptions;

namespace PSADT.ClientServer.Payloads
{
    /// <summary>
    /// Payload for the ShowProgressDialog command.
    /// </summary>
    internal sealed record ShowProgressDialogPayload : IPayload
    {
        /// <summary>
        /// The style of the dialog.
        /// </summary>
        [JsonInclude]
        internal readonly DialogStyle DialogStyle;

        /// <summary>
        /// The progress dialog options.
        /// </summary>
        [JsonInclude]
        internal readonly ProgressDialogOptions Options;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShowProgressDialogPayload"/> class.
        /// </summary>
        /// <param name="dialogStyle">The style of the dialog.</param>
        /// <param name="options">The progress dialog options.</param>
        [JsonConstructor]
        internal ShowProgressDialogPayload(DialogStyle dialogStyle, ProgressDialogOptions options)
        {
            DialogStyle = dialogStyle;
            Options = options;
        }
    }
}
