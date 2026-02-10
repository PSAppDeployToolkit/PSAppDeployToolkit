using System;
using System.Collections;
using System.Runtime.Serialization;

namespace PSADT.UserInterface.DialogOptions
{
    /// <summary>
    /// Options for all dialogs.
    /// </summary>
    [DataContract]
    public sealed record DialogBoxOptions : IDialogOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DialogBoxOptions"/> class with the specified options.
        /// This accepts a hashtable of parameters to ease construction on the PowerShell side of things.
        /// </summary>
        /// <param name="options"></param>
        public DialogBoxOptions(Hashtable options) : this(
            (options ?? throw new ArgumentNullException(nameof(options)))["AppTitle"] is string appTitle ? appTitle : string.Empty,
            options["MessageText"] is string messageText ? messageText : string.Empty,
            options["DialogButtons"] is DialogBoxButtons dialogButtons ? dialogButtons : (DialogBoxButtons)uint.MaxValue,
            options["DialogDefaultButton"] is DialogBoxDefaultButton dialogDefaultButton ? dialogDefaultButton : (DialogBoxDefaultButton)uint.MaxValue,
            options["DialogIcon"] is DialogBoxIcon dialogIcon ? dialogIcon : null,
            options["DialogTopMost"] is bool dialogTopMost && dialogTopMost,
            options["DialogExpiryDuration"] is uint dialogExpiryDuration ? dialogExpiryDuration : null)
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
        private DialogBoxOptions(string appTitle, string messageText, DialogBoxButtons dialogButtons, DialogBoxDefaultButton dialogDefaultButton, DialogBoxIcon? dialogIcon, bool dialogTopMost, uint? dialogExpiryDuration)
        {
            if (string.IsNullOrWhiteSpace(appTitle))
            {
                throw new ArgumentNullException(nameof(appTitle), "AppTitle value is null or invalid.");
            }
            if (string.IsNullOrWhiteSpace(messageText))
            {
                throw new ArgumentNullException(nameof(messageText), "MessageText value is null or invalid.");
            }
            if ((uint)dialogButtons == uint.MaxValue)
            {
                throw new ArgumentNullException(nameof(dialogButtons), "DialogButtons value is null or invalid.");
            }
            if ((uint)dialogDefaultButton == uint.MaxValue)
            {
                throw new ArgumentNullException(nameof(dialogDefaultButton), "DialogDefaultButton value is null or invalid.");
            }
            if (dialogExpiryDuration is null)
            {
                throw new ArgumentNullException(nameof(dialogExpiryDuration), "DialogExpiryDuration value is null or invalid.");
            }

            AppTitle = appTitle;
            MessageText = messageText;
            DialogButtons = dialogButtons;
            DialogDefaultButton = dialogDefaultButton;
            DialogIcon = dialogIcon;
            DialogTopMost = dialogTopMost;
            DialogExpiryDuration = dialogExpiryDuration.Value;
        }

        /// <summary>
        /// The title of the application or process being displayed in the dialog.
        /// </summary>
        [DataMember]
        public string AppTitle { get; private set; }

        /// <summary>
        /// Gets the text of the message.
        /// </summary>
        [DataMember]
        public string MessageText { get; private set; }

        /// <summary>
        /// Gets the set of buttons to display in the message box dialog.
        /// </summary>
        [DataMember]
        public DialogBoxButtons DialogButtons { get; private set; }

        /// <summary>
        /// Gets the default button that is selected in the dialog box when it is displayed.
        /// </summary>
        [DataMember]
        public DialogBoxDefaultButton DialogDefaultButton { get; private set; }

        /// <summary>
        /// Gets the icon displayed in the dialog box.
        /// </summary>
        [DataMember]
        public DialogBoxIcon? DialogIcon { get; private set; }

        /// <summary>
        /// Indicates whether the dialog should be displayed as a top-most window.
        /// </summary>
        [DataMember]
        public bool DialogTopMost { get; private set; }

        /// <summary>
        /// The duration for which the dialog will be displayed before it automatically closes.
        /// </summary>
        [DataMember]
        public uint DialogExpiryDuration { get; private set; }
    }
}
