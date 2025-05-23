using System;
using System.Collections;
using PSADT.UserInterface.Dialogs;

namespace PSADT.UserInterface.DialogOptions
{
    /// <summary>
    /// Options for the ProgressDialog.
    /// </summary>
    public sealed record ProgressDialogOptions : BaseOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressDialogOptions"/> class.
        /// </summary>
        /// <param name="options"></param>
        public ProgressDialogOptions(Hashtable options) : base(options)
        {
            // Nothing here is allowed to be null.
            if (options["ProgressMessageText"] is not string progressMessageText || string.IsNullOrWhiteSpace(progressMessageText))
            {
                throw new ArgumentNullException("ProgressMessageText value is null or invalid.", (Exception?)null);
            }
            if (options["ProgressDetailMessageText"] is not string progressDetailMessageText || string.IsNullOrWhiteSpace(progressDetailMessageText))
            {
                throw new ArgumentNullException("ProgressDetailMessageText value is null or invalid.", (Exception?)null);
            }

            // Test and set optional values.
            if (options.ContainsKey("MessageAlignment"))
            {
                if (options["MessageAlignment"] is not DialogMessageAlignment messageAlignment)
                {
                    throw new ArgumentOutOfRangeException("MessageAlignment value is not valid.", (Exception?)null);
                }
                MessageAlignment = messageAlignment;
            }

            // The hashtable was correctly defined, assign the remaining values.
            ProgressMessageText = progressMessageText;
            ProgressDetailMessageText = progressDetailMessageText;
        }

        /// <summary>
        /// The message to be displayed in the progress dialog, indicating the current status or action being performed.
        /// </summary>
        public readonly string ProgressMessageText;

        /// <summary>
        /// The detailed message to be displayed in the progress dialog, providing more context or information about the current action.
        /// </summary>
        public readonly string ProgressDetailMessageText;

        /// <summary>
        /// The alignment of the message text in the dialog.
        /// </summary>
        public readonly DialogMessageAlignment? MessageAlignment;
    }
}
