using System.Collections;

namespace PSADT.UserInterface.Dialogs
{
    /// <summary>
    /// Options for all dialogs.
    /// </summary>
    public abstract class DialogOptions
    {
        public DialogOptions(Hashtable options)
        {
            AppTitle = options["AppTitle"] as string;
            Subtitle = options["Subtitle"] as string;
            AppIconImage = options["AppIconImage"] as string;
            DialogPosition = options["DialogPosition"] as DialogPosition?;
            DialogAllowMove = options["DialogAllowMove"] as bool?;
            DialogTopMost = options["DialogTopMost"] as bool?;
            DialogAccentColor = options["DialogAccentColor"] as string;
            DialogExpiryDuration = options["DialogExpiryDuration"] as TimeSpan?;
        }

        /// <summary>
        /// The title of the application or process being displayed in the dialog.
        /// </summary>
        public readonly string? AppTitle;

        /// <summary>
        /// The subtitle of the dialog, providing additional context or information.
        /// </summary>
        public readonly string? Subtitle;

        /// <summary>
        /// The image file path for the application icon to be displayed in the dialog.
        /// </summary>
        public readonly string? AppIconImage;

        /// <summary>
        /// The position of the dialog on the screen.
        /// </summary>
        public readonly DialogPosition? DialogPosition;

        /// <summary>
        /// Indicates whether the dialog allows the user to move it around the screen.
        /// </summary>
        public readonly bool? DialogAllowMove;

        /// <summary>
        /// Indicates whether the dialog should be displayed as a top-most window.
        /// </summary>
        public readonly bool? DialogTopMost;

        /// <summary>
        /// The accent color for the dialog.
        /// </summary>
        public readonly string? DialogAccentColor;

        /// <summary>
        /// The duration for which the dialog will be displayed before it automatically closes.
        /// </summary>
        public readonly TimeSpan? DialogExpiryDuration;
    }
}
