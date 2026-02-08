using System;

namespace PSADT.UserInterface
{
    /// <summary>
    /// Represents a dialog interface for deployment operations, providing a result and the ability to close the dialog.
    /// </summary>
    /// <remarks>This interface is intended to be implemented by classes that manage deployment-related dialogs. It provides a mechanism to retrieve the result of the dialog and to close it when the operation is complete.</remarks>
    internal interface IDialogBase : IDisposable
    {
        /// <summary>
        /// Closes the currently active dialog, if one is open.
        /// </summary>
        /// <remarks>Gracefully closes out the dialog.</remarks>
        void CloseDialog();
    }
}
