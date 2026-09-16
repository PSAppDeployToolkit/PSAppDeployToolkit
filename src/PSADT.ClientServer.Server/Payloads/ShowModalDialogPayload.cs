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

        /// <summary>
        /// Confirms the payload's invariants once it has been read off the wire.
        /// </summary>
        /// <remarks>DataContractSerializer allocates without running a constructor and assigns the members it
        /// finds, so a member the sender left out keeps its CLR default and arrives as nothing at all. Every
        /// member below is declared non-nullable, and this is the only thing holding it to that.</remarks>
        /// <param name="context">The streaming context, which is not used.</param>
        /// <exception cref="SerializationException">Thrown if the payload arrived without its options.</exception>
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if (Options is null)
            {
                throw new SerializationException($"The deserialized {nameof(ShowModalDialogPayload)} has no options.");
            }
        }
    }
}
