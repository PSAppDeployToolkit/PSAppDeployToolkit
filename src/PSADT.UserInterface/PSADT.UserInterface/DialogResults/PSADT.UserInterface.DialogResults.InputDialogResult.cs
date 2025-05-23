namespace PSADT.UserInterface.DialogResults
{
    /// <summary>
    /// Represents the result of an input dialog.
    /// </summary>
    public sealed record InputDialogResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InputDialogResult"/> class.
        /// </summary>
        /// <param name="result"></param>
        /// <param name="text"></param>
        internal InputDialogResult(string result, string? text = null)
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
