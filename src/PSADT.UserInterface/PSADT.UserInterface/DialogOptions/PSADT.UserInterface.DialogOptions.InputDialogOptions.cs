using System;
using System.Collections;

namespace PSADT.UserInterface.DialogOptions
{
    /// <summary>
    /// Options for the InputDialog.
    /// </summary>
    public sealed record InputDialogOptions : CustomDialogOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InputDialogOptions"/> class.
        /// </summary>
        /// <param name="options"></param>
        public InputDialogOptions(Hashtable options) : base(options)
        {
            // Just set our one and only field.
            if (options.ContainsKey("InitialInputText"))
            {
                if (options["InitialInputText"] is not string initialInputText || string.IsNullOrWhiteSpace(initialInputText))
                {
                    throw new ArgumentOutOfRangeException("InitialInputText value is not valid.", (Exception?)null);
                }
                InitialInputText = initialInputText;
            }
        }

        /// <summary>
        /// The initial text to be displayed in the input field.
        /// </summary>
        public readonly string? InitialInputText;
    }
}
