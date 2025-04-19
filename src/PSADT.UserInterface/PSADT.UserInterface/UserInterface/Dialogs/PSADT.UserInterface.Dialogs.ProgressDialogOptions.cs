using System.Collections;

namespace PSADT.UserInterface.Dialogs
{
    /// <summary>
    /// Options for the ProgressDialog.
    /// </summary>
    public sealed class ProgressDialogOptions : DialogOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressDialogOptions"/> class.
        /// </summary>
        /// <param name="options"></param>
        public ProgressDialogOptions(Hashtable options) : base(options)
        {
            // Nothing here is allowed to be null.
            if (options["ProgressMessage"] is not string progressMessage || string.IsNullOrWhiteSpace(progressMessage))
            {
                throw new ArgumentNullException("ProgressMessage cannot be null.", (Exception?)null);
            }
            if (options["ProgressDetailMessage"] is not string progressDetailMessage || string.IsNullOrWhiteSpace(progressDetailMessage))
            {
                throw new ArgumentNullException("ProgressDetailMessage cannot be null.", (Exception?)null);
            }

            // The hashtable was correctly defined, so we can assign the values and continue onwards.
            ProgressMessage = progressMessage;
            ProgressDetailMessage = progressDetailMessage;
        }

        /// <summary>
        /// The message to be displayed in the progress dialog, indicating the current status or action being performed.
        /// </summary>
        public readonly string ProgressMessage;

        /// <summary>
        /// The detailed message to be displayed in the progress dialog, providing more context or information about the current action.
        /// </summary>
        public readonly string ProgressDetailMessage;
    }
}
