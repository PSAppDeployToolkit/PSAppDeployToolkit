using System;
using System.Collections;

namespace PSADT.UserInterface.DialogOptions
{
    /// <summary>
    /// Options for the ProgressDialog.
    /// </summary>
    public sealed class ProgressDialogOptions : BaseOptions
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
                throw new ArgumentNullException("ProgressMessageText cannot be null.", (Exception?)null);
            }
            if (options["ProgressDetailMessageText"] is not string progressDetailMessageText || string.IsNullOrWhiteSpace(progressDetailMessageText))
            {
                throw new ArgumentNullException("ProgressDetailMessageText cannot be null.", (Exception?)null);
            }

            // The hashtable was correctly defined, so we can assign the values and continue onwards.
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
    }
}
