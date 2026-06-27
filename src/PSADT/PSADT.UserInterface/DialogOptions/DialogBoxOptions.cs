using System;
using System.Collections;
using System.Runtime.Serialization;

namespace PSADT.UserInterface.DialogOptions
{
    /// <summary>
    /// Options for all dialogs.
    /// </summary>
    [DataContract]
    public sealed record class DialogBoxOptions : IDialogOptions
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
            (string?)(options ?? throw new ArgumentNullException(nameof(options)))["AppTitle"] ?? throw new ArgumentNullException(nameof(options), "The specified key 'AppTitle' is missing."),
            (string?)options["MessageText"] ?? throw new ArgumentNullException(nameof(options), "The specified key 'MessageText' is missing."),
            (DialogBoxButtons?)options["DialogButtons"] ?? throw new ArgumentNullException(nameof(options), "The specified key 'DialogButtons' is missing."),
            (DialogBoxDefaultButton?)options["DialogDefaultButton"] ?? throw new ArgumentNullException(nameof(options), "The specified key 'DialogDefaultButton' is missing."),
            (DialogBoxIcon?)options["DialogIcon"],
            (bool?)options["DialogTopMost"] ?? throw new ArgumentNullException(nameof(options), "The specified key 'DialogTopMost' is missing."),
            (uint?)options["DialogExpiryDuration"] ?? throw new ArgumentNullException(nameof(options), "The specified key 'DialogExpiryDuration' is missing."))
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
            ArgumentException.ThrowIfNullOrWhiteSpace(appTitle);
            ArgumentException.ThrowIfNullOrWhiteSpace(messageText);
            AppTitle = appTitle;
            MessageText = messageText;
            DialogButtons = dialogButtons;
            DialogDefaultButton = dialogDefaultButton;
            DialogIcon = dialogIcon;
            DialogTopMost = dialogTopMost;
            DialogExpiryDuration = dialogExpiryDuration;
        }

        /// <summary>
        /// The title of the application or process being displayed in the dialog.
        /// </summary>
        [DataMember]
        public readonly string AppTitle;

        /// <summary>
        /// Gets the text of the message.
        /// </summary>
        [DataMember]
        public readonly string MessageText;

        /// <summary>
        /// Gets the set of buttons to display in the message box dialog.
        /// </summary>
        [DataMember]
        public readonly DialogBoxButtons DialogButtons;

        /// <summary>
        /// Gets the default button that is selected in the dialog box when it is displayed.
        /// </summary>
        [DataMember]
        public readonly DialogBoxDefaultButton DialogDefaultButton;

        /// <summary>
        /// Gets the icon displayed in the dialog box.
        /// </summary>
        [DataMember]
        public readonly DialogBoxIcon? DialogIcon;

        /// <summary>
        /// Indicates whether the dialog should be displayed as a top-most window.
        /// </summary>
        [DataMember]
        public readonly bool DialogTopMost;

        /// <summary>
        /// The duration for which the dialog will be displayed before it automatically closes.
        /// </summary>
        [DataMember]
        public readonly uint DialogExpiryDuration;
    }
}
