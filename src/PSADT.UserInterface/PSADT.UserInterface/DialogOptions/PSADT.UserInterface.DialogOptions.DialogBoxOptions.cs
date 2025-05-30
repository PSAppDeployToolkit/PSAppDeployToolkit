using System;
using System.Collections;
using System.Runtime.Serialization;
using PSADT.UserInterface.Dialogs;

namespace PSADT.UserInterface.DialogOptions
{
    /// <summary>
    /// Options for all dialogs.
    /// </summary>
    [DataContract]
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
            if (options["AppTitle"] is not string appTitle || string.IsNullOrWhiteSpace(appTitle))
            {
                throw new ArgumentNullException("AppTitle value is null or invalid.", (Exception?)null);
            }
            if (options["MessageText"] is not string messageText || string.IsNullOrWhiteSpace(messageText))
            {
                throw new ArgumentNullException("MessageText value is null or invalid.", (Exception?)null);
            }
            if (options["DialogButtons"] is not MessageBoxButtons dialogButtons)
            {
                throw new ArgumentNullException("DialogButtons value is null or invalid.", (Exception?)null);
            }
            if (options["DialogDefaultButton"] is not MessageBoxDefaultButton dialogDefaultButton)
            {
                throw new ArgumentNullException("DialogDefaultButton value is null or invalid.", (Exception?)null);
            }
            if (options["DialogIcon"] is not MessageBoxIcon dialogIcon)
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
        public readonly MessageBoxButtons DialogButtons;

        /// <summary>
        /// Gets the default button that is selected in the dialog box when it is displayed.
        /// </summary>
        [DataMember]
        public readonly MessageBoxDefaultButton DialogDefaultButton;

        /// <summary>
        /// Gets the icon displayed in the dialog box.
        /// </summary>
        [DataMember]
        public readonly MessageBoxIcon DialogIcon;

        /// <summary>
        /// Indicates whether the dialog should be displayed as a top-most window.
        /// </summary>
        [DataMember]
        public readonly bool DialogTopMost;

        /// <summary>
        /// The duration for which the dialog will be displayed before it automatically closes.
        /// </summary>
        [DataMember]
        public readonly TimeSpan DialogExpiryDuration;
    }
}
