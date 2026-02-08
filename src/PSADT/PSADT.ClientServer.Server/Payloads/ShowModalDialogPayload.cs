using PSADT.UserInterface;
using PSADT.UserInterface.DialogOptions;
using Newtonsoft.Json;

namespace PSADT.ClientServer.Payloads
{
    /// <summary>
    /// Payload for the ShowModalDialog command.
    /// </summary>
    internal sealed record ShowModalDialogPayload : IPayload
    {
        /// <summary>
        /// The type of dialog to display.
        /// </summary>
        [JsonProperty]
        internal readonly DialogType DialogType;

        /// <summary>
        /// The style of the dialog.
        /// </summary>
        [JsonProperty]
        internal readonly DialogStyle DialogStyle;

        /// <summary>
        /// The options for the dialog.
        /// </summary>
        /// <remarks>The concrete type depends on the <see cref="DialogType"/>.</remarks>
        [JsonProperty]
        internal readonly IDialogOptions Options;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShowModalDialogPayload"/> class.
        /// </summary>
        /// <param name="dialogType">The type of dialog to display.</param>
        /// <param name="dialogStyle">The style of the dialog.</param>
        /// <param name="options">The options for the dialog.</param>
        [JsonConstructor]
        internal ShowModalDialogPayload(DialogType dialogType, DialogStyle dialogStyle, IDialogOptions options)
        {
            DialogType = dialogType;
            DialogStyle = dialogStyle;
            Options = options;
        }
    }
}
