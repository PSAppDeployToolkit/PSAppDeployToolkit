using System.Collections;

namespace PSADT.UserInterface.Dialogs
{
    /// <summary>
    /// Options for the InputDialog.
    /// </summary>
    public sealed class InputDialogOptions : DialogOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InputDialogOptions"/> class.
        /// </summary>
        /// <param name="options"></param>
        public InputDialogOptions(Hashtable options) : base(options)
        {
            InitialInputText = options["InitialInputText"] as string;
            ButtonLeftText = options["ButtonLeftText"] as string;
            ButtonMiddleText = options["ButtonMiddleText"] as string;
            ButtonRightText = options["ButtonRightText"] as string;
            CustomMessage = options["CustomMessage"] as string;
        }

        /// <summary>
        /// The initial text to be displayed in the input field.
        /// </summary>
        public readonly string? InitialInputText;

        /// <summary>
        /// The text for the left button.
        /// </summary>
        public readonly string? ButtonLeftText;

        /// <summary>
        /// The text for the middle button.
        /// </summary>
        public readonly string? ButtonMiddleText;

        /// <summary>
        /// The text for the right button.
        /// </summary>
        public readonly string? ButtonRightText;

        /// <summary>
        /// The custom message to be displayed in the dialog.
        /// </summary>
        public readonly string? CustomMessage;
    }
}
