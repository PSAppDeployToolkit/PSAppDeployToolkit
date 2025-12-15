using System;
using System.Collections;
using PSADT.UserInterface.Dialogs;
using Newtonsoft.Json;

namespace PSADT.UserInterface.DialogOptions
{
    /// <summary>
    /// Options for all dialogs.
    /// </summary>
    public sealed record DialogBoxOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DialogBoxOptions"/> class with the specified options.
        /// This accepts a hashtable of parameters to ease construction on the PowerShell side of things.
        /// </summary>
        /// <param name="options"></param>
        public DialogBoxOptions(Hashtable options)
        {
            // Nothing here is allowed to be null.
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            if (options["AppTitle"] is not string appTitle || string.IsNullOrWhiteSpace(appTitle))
            {
                throw new ArgumentNullException("AppTitle value is null or invalid.", (Exception?)null);
            }
            if (options["MessageText"] is not string messageText || string.IsNullOrWhiteSpace(messageText))
            {
                throw new ArgumentNullException("MessageText value is null or invalid.", (Exception?)null);
            }
            if (options["DialogButtons"] is not DialogBoxButtons dialogButtons)
            {
                throw new ArgumentNullException("DialogButtons value is null or invalid.", (Exception?)null);
            }
            if (options["DialogDefaultButton"] is not DialogBoxDefaultButton dialogDefaultButton)
            {
                throw new ArgumentNullException("DialogDefaultButton value is null or invalid.", (Exception?)null);
            }
            if (options["DialogIcon"] is not DialogBoxIcon dialogIcon)
            {
                throw new ArgumentNullException("DialogIcon value is null or invalid.", (Exception?)null);
            }
            if (options["DialogTopMost"] is not bool dialogTopMost)
            {
                throw new ArgumentNullException("DialogTopMost value is null or invalid.", (Exception?)null);
            }
            if (options["DialogExpiryDuration"] is not TimeSpan dialogExpiryDuration)
            {
                throw new ArgumentNullException("DialogExpiryDuration value is null or invalid.", (Exception?)null);
            }

            // The hashtable was correctly defined, assign the remaining values.
            AppTitle = appTitle;
            MessageText = messageText;
            DialogButtons = dialogButtons;
            DialogDefaultButton = dialogDefaultButton;
            DialogIcon = dialogIcon;
            DialogTopMost = dialogTopMost;
            DialogExpiryDuration = dialogExpiryDuration;
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0051:Remove unused private members", Justification = "This constructor is used for deserialisation.")]
        [JsonConstructor]
        private DialogBoxOptions(string appTitle, string messageText, DialogBoxButtons dialogButtons, DialogBoxDefaultButton dialogDefaultButton, DialogBoxIcon dialogIcon, bool dialogTopMost, TimeSpan dialogExpiryDuration)
        {
            // Assign the values with null checks to catch deserialization mismatches.
            AppTitle = appTitle ?? throw new ArgumentNullException(nameof(appTitle));
            MessageText = messageText ?? throw new ArgumentNullException(nameof(messageText));
            DialogButtons = dialogButtons;
            DialogDefaultButton = dialogDefaultButton;
            DialogIcon = dialogIcon;
            DialogTopMost = dialogTopMost;
            DialogExpiryDuration = dialogExpiryDuration;
        }

        /// <summary>
        /// The title of the application or process being displayed in the dialog.
        /// </summary>
        [JsonProperty]
        public string AppTitle { get; }

        /// <summary>
        /// Gets the text of the message.
        /// </summary>
        [JsonProperty]
        public string MessageText { get; }

        /// <summary>
        /// Gets the set of buttons to display in the message box dialog.
        /// </summary>
        [JsonProperty]
        public DialogBoxButtons DialogButtons { get; }

        /// <summary>
        /// Gets the default button that is selected in the dialog box when it is displayed.
        /// </summary>
        [JsonProperty]
        public DialogBoxDefaultButton DialogDefaultButton { get; }

        /// <summary>
        /// Gets the icon displayed in the dialog box.
        /// </summary>
        [JsonProperty]
        public DialogBoxIcon DialogIcon { get; }

        /// <summary>
        /// Indicates whether the dialog should be displayed as a top-most window.
        /// </summary>
        [JsonProperty]
        public bool DialogTopMost { get; }

        /// <summary>
        /// The duration for which the dialog will be displayed before it automatically closes.
        /// </summary>
        [JsonProperty]
        public TimeSpan DialogExpiryDuration { get; }
    }
}
