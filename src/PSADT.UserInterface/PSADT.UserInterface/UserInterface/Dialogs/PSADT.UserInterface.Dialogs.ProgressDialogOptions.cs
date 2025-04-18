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
            ProgressMessage = options["ProgressMessage"] as string;
            ProgressDetailMessage = options["ProgressDetailMessage"] as string;
        }

        /// <summary>
        /// The message to be displayed in the progress dialog, indicating the current status or action being performed.
        /// </summary>
        public readonly string? ProgressMessage;

        /// <summary>
        /// The detailed message to be displayed in the progress dialog, providing more context or information about the current action.
        /// </summary>
        public readonly string? ProgressDetailMessage;
    }
}
