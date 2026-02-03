using System.Text.Json.Serialization;
using PSADT.UserInterface.Dialogs;

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
        [JsonInclude]
        internal readonly DialogType DialogType;

        /// <summary>
        /// The style of the dialog.
        /// </summary>
        [JsonInclude]
        internal readonly DialogStyle DialogStyle;

        /// <summary>
        /// The options for the dialog.
        /// </summary>
        /// <remarks>The concrete type depends on the <see cref="DialogType"/>.</remarks>
        [JsonInclude]
        internal readonly object Options;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShowModalDialogPayload"/> class.
        /// </summary>
        /// <param name="dialogType">The type of dialog to display.</param>
        /// <param name="dialogStyle">The style of the dialog.</param>
        /// <param name="options">The options for the dialog.</param>
        [JsonConstructor]
        internal ShowModalDialogPayload(DialogType dialogType, DialogStyle dialogStyle, object options)
        {
            DialogType = dialogType;
            DialogStyle = dialogStyle;
            Options = options;
        }
    }
}
