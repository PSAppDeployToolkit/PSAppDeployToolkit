using System.Runtime.Serialization;
using PSADT.UserInterface;
using PSADT.UserInterface.DialogOptions;

namespace PSADT.ClientServer.Payloads
{
    /// <summary>
    /// Payload for the ShowModalDialog command.
    /// </summary>
    [DataContract]
    [KnownType(typeof(CloseAppsDialogOptions))]
    [KnownType(typeof(CustomDialogOptions))]
    [KnownType(typeof(DialogBoxOptions))]
    [KnownType(typeof(HelpConsoleOptions))]
    [KnownType(typeof(InputDialogOptions))]
    [KnownType(typeof(ProgressDialogOptions))]
    [KnownType(typeof(RestartDialogOptions))]
    internal sealed record ShowModalDialogPayload : IClientServerPayload
    {
        /// <summary>
        /// The type of dialog to display.
        /// </summary>
        [DataMember]
        internal readonly DialogType DialogType;

        /// <summary>
        /// The style of the dialog.
        /// </summary>
        [DataMember]
        internal readonly DialogStyle DialogStyle;

        /// <summary>
        /// The options for the dialog.
        /// </summary>
        /// <remarks>The concrete type depends on the <see cref="DialogType"/>.</remarks>
        [DataMember]
        internal readonly IDialogOptions Options;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShowModalDialogPayload"/> class.
        /// </summary>
        /// <param name="dialogType">The type of dialog to display.</param>
        /// <param name="dialogStyle">The style of the dialog.</param>
        /// <param name="options">The options for the dialog.</param>
        internal ShowModalDialogPayload(DialogType dialogType, DialogStyle dialogStyle, IDialogOptions options)
        {
            DialogType = dialogType;
            DialogStyle = dialogStyle;
            Options = options;
        }
    }
}
