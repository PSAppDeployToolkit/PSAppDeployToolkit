using System.Collections;

namespace PSADT.UserInterface.DialogOptions
{
    /// <summary>
    /// Options for the InputDialog.
    /// </summary>
    public sealed class InputDialogOptions : CustomDialogOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InputDialogOptions"/> class.
        /// </summary>
        /// <param name="options"></param>
        public InputDialogOptions(Hashtable options) : base(options)
        {
            // Just set our one and only field.
            InitialInputText = options["InitialInputText"] is string initialInputText && !string.IsNullOrWhiteSpace(initialInputText) ? initialInputText : null;
        }

        /// <summary>
        /// The initial text to be displayed in the input field.
        /// </summary>
        public readonly string? InitialInputText;
    }
}
