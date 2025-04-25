using System;
using System.Collections;
using PSADT.UserInterface.Dialogs;

namespace PSADT.UserInterface.DialogOptions
{
    /// <summary>
    /// Options for the CustomDialog.
    /// </summary>
    public class CustomDialogOptions : BaseOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CustomDialogOptions"/> class.
        /// </summary>
        /// <param name="options"></param>
        public CustomDialogOptions(Hashtable options) : base(options)
        {
            // Nothing here is allowed to be null.
            if (options["MessageText"] is not string messageText || string.IsNullOrWhiteSpace(messageText))
            {
                throw new ArgumentNullException("MessageText cannot be null.", (Exception?)null);
            }
            if (options["MessageAlignment"] is not string messageAlignment || string.IsNullOrWhiteSpace(messageAlignment))
            {
                throw new ArgumentNullException("MessageAlignment cannot be null.", (Exception?)null);
            }

            // Test that messageAlignment is valid.
            if (messageAlignment != "Left" && messageAlignment != "Center" && messageAlignment != "Right")
            {
                throw new ArgumentOutOfRangeException("MessageAlignment must be Left, Center, or Right.", (Exception?)null);
            }

            // The hashtable was correctly defined, so we can assign the values and continue onwards.
            MessageText = messageText;
            MessageAlignment = messageAlignment;
            ButtonLeftText = options["ButtonLeftText"] is string buttonLeftText && !string.IsNullOrWhiteSpace(buttonLeftText) ? buttonLeftText : null;
            ButtonMiddleText = options["ButtonMiddleText"] is string buttonMiddleText && !string.IsNullOrWhiteSpace(buttonMiddleText) ? buttonMiddleText : null;
            ButtonRightText = options["ButtonRightText"] is string buttonRightText && !string.IsNullOrWhiteSpace(buttonRightText) ? buttonRightText : null;
            Icon = options["Icon"] is DialogSystemIcon icon ? icon : null;

            // At least one button must be defined before we finish.
            if (string.IsNullOrWhiteSpace(ButtonLeftText) && string.IsNullOrWhiteSpace(ButtonMiddleText) && string.IsNullOrWhiteSpace(ButtonRightText))
            {
                throw new ArgumentNullException("At least one button must be defined.", (Exception?)null);
            }
        }

        /// <summary>
        /// The custom message to be displayed in the dialog.
        /// </summary>
        public readonly string MessageText;

        /// <summary>
        /// The alignment of the message text in the dialog.
        /// </summary>
        public readonly string MessageAlignment;

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
        /// The icon to be displayed in the dialog.
        /// </summary>
        public readonly DialogSystemIcon? Icon;
    }
}
