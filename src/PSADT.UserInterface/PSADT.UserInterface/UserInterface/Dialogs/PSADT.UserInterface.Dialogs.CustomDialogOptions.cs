using System.Collections;

namespace PSADT.UserInterface.Dialogs
{
    /// <summary>
    /// Options for the CustomDialog.
    /// </summary>
    public sealed class CustomDialogOptions : DialogOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CustomDialogOptions"/> class.
        /// </summary>
        /// <param name="options"></param>
        public CustomDialogOptions(Hashtable options) : base(options)
        {
            ButtonLeftText = options["ButtonLeftText"] as string;
            ButtonMiddleText = options["ButtonMiddleText"] as string;
            ButtonRightText = options["ButtonRightText"] as string;
            CustomMessage = options["CustomMessage"] as string;
        }

        /// <summary>
        /// The text for the left button in the dialog.
        /// </summary>
        public readonly string? ButtonLeftText;

        /// <summary>
        /// The text for the middle button in the dialog.
        /// </summary>
        public readonly string? ButtonMiddleText;

        /// <summary>
        /// The text for the right button in the dialog.
        /// </summary>
        public readonly string? ButtonRightText;

        /// <summary>
        /// The custom message to be displayed in the dialog.
        /// </summary>
        public readonly string? CustomMessage;
    }
}
