using System;

namespace PSADT.UserInterface.Dialogs
{
    /// <summary>
    /// Represents a dialog interface for deployment operations, providing a result and the ability to close the dialog.
    /// </summary>
    /// <remarks>This interface is intended to be implemented by classes that manage deployment-related dialogs. It provides a mechanism to retrieve the result of the dialog and to close it when the operation is complete.</remarks>
    internal interface IModalDialog : IDialogBase
    {
        /// <summary>
        /// Displays a modal dialog box to the user.
        /// </summary>
        /// <remarks>This method blocks execution until the dialog box is closed by the user. Use this method to present information or gather input that requires immediate attention.</remarks>
        void ShowDialog();

        /// <summary>
        /// Gets the result of the operation.
        /// </summary>
        object DialogResult { get; }
    }
}
