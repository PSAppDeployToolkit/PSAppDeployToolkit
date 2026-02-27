using System;
using System.Collections;
using System.Runtime.Serialization;
using PSADT.Interop.Extensions;

namespace PSADT.UserInterface.DialogOptions
{
    /// <summary>
    /// Options for all dialogs.
    /// </summary>
    [DataContract]
    public sealed record DialogBoxOptions : IDialogOptions
    {
        /// <summary>
        /// Initializes a new instance of the DialogBoxOptions class using the specified configuration options.
        /// </summary>
        /// <remarks>If a required key is missing from the options dictionary, a default value is used for
        /// that setting. The expected keys are "AppTitle", "MessageText", "DialogButtons", "DialogDefaultButton",
        /// "DialogIcon", "DialogTopMost", and "DialogExpiryDuration". Ensure that the values associated with each key
        /// are of the appropriate type.</remarks>
        /// <param name="options">A dictionary containing key-value pairs that specify dialog box settings, such as the application title,
        /// message text, button configuration, default button, icon, topmost behavior, and expiry duration.</param>
        /// <exception cref="ArgumentNullException">Thrown if the options dictionary is null.</exception>
        public DialogBoxOptions(IDictionary options) : this(
            (options ?? throw new ArgumentNullException(nameof(options)))["AppTitle"] as string ?? null!,
            options["MessageText"] as string ?? null!,
            options["DialogButtons"] as DialogBoxButtons? ?? (DialogBoxButtons)uint.MaxValue,
            options["DialogDefaultButton"] as DialogBoxDefaultButton? ?? (DialogBoxDefaultButton)uint.MaxValue,
            options["DialogIcon"] as DialogBoxIcon?,
            options["DialogTopMost"] as bool? ?? false,
            options["DialogExpiryDuration"] as uint? ?? uint.MaxValue)
        {
        }

        /// <summary>
        /// Represents the configuration options for a dialog box, including its title, message, buttons, default
        /// button, icon, topmost behavior, and expiry duration.
        /// </summary>
        /// <remarks>This constructor is intended for internal use and is marked as private. It
        /// initializes all properties required to configure a dialog box.</remarks>
        /// <param name="appTitle">The title of the application displayed in the dialog box.</param>
        /// <param name="messageText">The message text displayed in the dialog box.</param>
        /// <param name="dialogButtons">The set of buttons displayed in the dialog box, such as OK, Cancel, or Yes/No.</param>
        /// <param name="dialogDefaultButton">The default button that is pre-selected when the dialog box is displayed.</param>
        /// <param name="dialogIcon">The icon displayed in the dialog box, such as an information, warning, or error icon.</param>
        /// <param name="dialogTopMost">A value indicating whether the dialog box should appear as the topmost window. <see langword="true"/> if the
        /// dialog box is topmost; otherwise, <see langword="false"/>.</param>
        /// <param name="dialogExpiryDuration">The duration after which the dialog box will automatically close if no user action is taken.</param>
        private DialogBoxOptions(string appTitle, string messageText, DialogBoxButtons dialogButtons, DialogBoxDefaultButton dialogDefaultButton, DialogBoxIcon? dialogIcon, bool dialogTopMost, uint dialogExpiryDuration)
        {
            if ((uint)dialogButtons == uint.MaxValue)
            {
                throw new ArgumentNullException(nameof(dialogButtons), "DialogButtons value is null or invalid.");
            }
            if ((uint)dialogDefaultButton == uint.MaxValue)
            {
                throw new ArgumentNullException(nameof(dialogDefaultButton), "DialogDefaultButton value is null or invalid.");
            }
            if (dialogExpiryDuration == uint.MaxValue)
            {
                throw new ArgumentNullException(nameof(dialogExpiryDuration), "DialogExpiryDuration value is null or invalid.");
            }
            AppTitle = appTitle.ThrowIfNullOrWhiteSpace();
            MessageText = messageText.ThrowIfNullOrWhiteSpace();
            DialogButtons = dialogButtons;
            DialogDefaultButton = dialogDefaultButton;
            DialogIcon = dialogIcon;
            DialogTopMost = dialogTopMost;
            DialogExpiryDuration = dialogExpiryDuration;
        }

        /// <summary>
        /// The title of the application or process being displayed in the dialog.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly string AppTitle;

        /// <summary>
        /// Gets the text of the message.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly string MessageText;

        /// <summary>
        /// Gets the set of buttons to display in the message box dialog.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly DialogBoxButtons DialogButtons;

        /// <summary>
        /// Gets the default button that is selected in the dialog box when it is displayed.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly DialogBoxDefaultButton DialogDefaultButton;

        /// <summary>
        /// Gets the icon displayed in the dialog box.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly DialogBoxIcon? DialogIcon;

        /// <summary>
        /// Indicates whether the dialog should be displayed as a top-most window.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly bool DialogTopMost;

        /// <summary>
        /// The duration for which the dialog will be displayed before it automatically closes.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly uint DialogExpiryDuration;
    }
}
