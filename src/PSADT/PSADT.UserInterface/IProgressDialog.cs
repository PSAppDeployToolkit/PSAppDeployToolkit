namespace PSADT.UserInterface
{
    /// <summary>
    /// Represents a dialog interface for deployment operations, providing a result and the ability to close the dialog.
    /// </summary>
    /// <remarks>This interface is intended to be implemented by classes that manage deployment-related dialogs. It provides a mechanism to retrieve the result of the dialog and to close it when the operation is complete.</remarks>
    internal interface IProgressDialog : IDialogBase
    {
        /// <summary>
        /// Displays the current content or state of the object to the user.
        /// </summary>
        /// <remarks>The specific behavior of this method depends on the implementation. It may render content to a user interface, log information, or perform other display-related actions.</remarks>
        void Show();

        /// <summary>
        /// Updates the progress of an operation with an optional message, detailed message, and percentage completion.
        /// </summary>
        /// <param name="progressMessage">An optional message describing the current progress. Can be <see langword="null"/> if no message is provided.</param>
        /// <param name="progressMessageDetail">An optional detailed message providing additional context about the progress. Can be <see langword="null"/> if no detailed message is provided.</param>
        /// <param name="progressPercentage">An optional value representing the percentage of completion, ranging from 0.0 to 100.0. Can be <see langword="null"/> if the percentage is not specified.</param>
        /// <param name="messageAlignment">An optional value specifying the alignment of the message. Can be <see langword="null"/> if no specific alignment is required.</param>
        void UpdateProgress(string? progressMessage = null, string? progressMessageDetail = null, double? progressPercentage = null, DialogMessageAlignment? messageAlignment = null);
    }
}
