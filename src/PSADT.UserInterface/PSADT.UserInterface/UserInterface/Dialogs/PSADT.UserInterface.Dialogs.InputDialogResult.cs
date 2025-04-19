namespace PSADT.UserInterface.Dialogs
{
    /// <summary>
    /// Represents the result of an input dialog.
    /// </summary>
    public sealed class InputDialogResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InputDialogResult"/> class.
        /// </summary>
        /// <param name="result"></param>
        /// <param name="text"></param>
        public InputDialogResult(string result, string? text)
        {
            Result = result;
            Text = !string.IsNullOrWhiteSpace(text) ? text : null;
        }

        /// <summary>
        /// Gets the result of the dialog.
        /// </summary>
        public readonly string Result;

        /// <summary>
        /// Gets the text entered by the user.
        /// </summary>
        public readonly string? Text;
    }
}
