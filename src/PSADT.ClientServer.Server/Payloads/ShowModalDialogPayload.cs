using System.Runtime.Serialization;
using PSADT.UserInterface;
using PSADT.UserInterface.DialogOptions;

namespace PSADT.ClientServer.Payloads
{
    /// <summary>
    /// Payload for the ShowModalDialog command.
    /// </summary>
    /// <param name="DialogType">The type of dialog to display.</param>
    /// <param name="DialogStyle">The style of the dialog.</param>
    /// <param name="Options">The options for the dialog.</param>
    [DataContract]
    [KnownType(typeof(CloseAppsDialogOptions))]
    [KnownType(typeof(CustomDialogOptions))]
    [KnownType(typeof(DialogBoxOptions))]
    [KnownType(typeof(HelpConsoleOptions))]
    [KnownType(typeof(InputDialogOptions))]
    [KnownType(typeof(ProgressDialogOptions))]
    [KnownType(typeof(RestartDialogOptions))]
    internal sealed record class ShowModalDialogPayload(DialogType DialogType, DialogStyle DialogStyle, IDialogOptions Options) : IClientServerPayload
    {
        /// <summary>
        /// The type of dialog to display.
        /// </summary>
        [DataMember]
        internal readonly DialogType DialogType = DialogType;

        /// <summary>
        /// The style of the dialog.
        /// </summary>
        [DataMember]
        internal readonly DialogStyle DialogStyle = DialogStyle;

        /// <summary>
        /// The options for the dialog.
        /// </summary>
        /// <remarks>The concrete type depends on the <see cref="DialogType"/>.</remarks>
        [DataMember]
        internal readonly IDialogOptions Options = Options;
    }
}
