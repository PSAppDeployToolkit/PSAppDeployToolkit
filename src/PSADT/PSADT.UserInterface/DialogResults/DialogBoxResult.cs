using Windows.Win32.UI.WindowsAndMessaging;

namespace PSADT.UserInterface.DialogResults
{
    /// <summary>
    /// Specifies the possible results of a message box operation.
    /// </summary>
    /// <remarks>This enumeration represents the various outcomes of a message box interaction, such as the button selected by the user or other conditions like a timeout. Each value corresponds to a specific Windows API constant from MESSAGEBOX_RESULT. These results are typically used to determine the user's response to a prompt or dialog.</remarks>
    public enum DialogBoxResult
    {
        /// <summary>
        /// Represents the result of a message box operation where the user selects the "OK" option.
        /// </summary>
        /// <remarks>This value corresponds to the Windows API constant MESSAGEBOX_RESULT.IDOK. It is typically used to indicate that the user acknowledged the message or completed an action in response to a prompt.</remarks>"
        OK = MESSAGEBOX_RESULT.IDOK,

        /// <summary>
        /// Represents the result of a message box operation where the user selects the "Cancel" option.
        /// </summary>
        /// <remarks>This value corresponds to the Windows API constant MESSAGEBOX_RESULT.IDCANCEL. It is typically used to indicate that the user canceled the operation or dismissed the message box without making a selection.</remarks>
        Cancel = MESSAGEBOX_RESULT.IDCANCEL,

        /// <summary>
        /// Represents the result of a message box operation where the user selected the "Abort" option.
        /// </summary>
        /// <remarks>This value corresponds to the Windows API constant MESSAGEBOX_RESULT.IDABORT. It is typically used to indicate that the user chose to abort an operation in response to a message box prompt.</remarks>
        Abort = MESSAGEBOX_RESULT.IDABORT,

        /// <summary>
        /// Represents the result of a message box operation where the user selects the "Retry" option.
        /// </summary>
        /// <remarks>This value corresponds to the Windows API constant MESSAGEBOX_RESULT.IDRETRY. It is typically used to indicate that the user has chosen to retry an operation after encountering an error or prompt.</remarks>
        Retry = MESSAGEBOX_RESULT.IDRETRY,

        /// <summary>
        /// Represents the result of a message box operation where the user selects "Ignore."
        /// </summary>
        /// <remarks>This value corresponds to the Windows API constant MESSAGEBOX_RESULT.IDIGNORE. It is typically used to handle scenarios where the user chooses to ignore a warning or error.</remarks>
        Ignore = MESSAGEBOX_RESULT.IDIGNORE,

        /// <summary>
        /// Represents a result indicating that the user selected "Yes" in a message box.
        /// </summary>
        /// <remarks>This value corresponds to the Windows API constant MESSAGEBOX_RESULT.IDYES. It is typically used to indicate that the user has confirmed an action or answered "Yes" to a prompt.</remarks>
        Yes = MESSAGEBOX_RESULT.IDYES,

        /// <summary>
        /// Represents the result of a message box operation where the user selected "No".
        /// </summary>
        /// <remarks>This value corresponds to the Windows API constant MESSAGEBOX_RESULT.IDNO. It is typically used to indicate that the user declined an action or answered "No" to a prompt.</remarks>
        No = MESSAGEBOX_RESULT.IDNO,

        /// <summary>
        /// Represents the result of a message box when the Close button is selected.
        /// </summary>
        /// <remarks>This value corresponds to the Close button being clicked or the message box being dismissed without selecting any other option. It is typically used to handle scenarios where the user close the message box without making a specific choice.</remarks>
        Close = MESSAGEBOX_RESULT.IDCLOSE,

        /// <summary>
        /// Represents the result indicating that the user selected the "Try Again" option in a message box.
        /// </summary>
        /// <remarks>This value corresponds to the Windows API constant MESSAGEBOX_RESULT.IDTRYAGAIN. It is typically used to handle scenarios where the user opts to retry an operation after a failure.</remarks>
        TryAgain = MESSAGEBOX_RESULT.IDTRYAGAIN,

        /// <summary>
        /// Represents the result of a message box operation where the user selects "Continue."
        /// </summary>
        /// <remarks>This value corresponds to the "Continue" button in a message box, typically used to indicate that the user has chosen to proceed with the operation.</remarks>
        Continue = MESSAGEBOX_RESULT.IDCONTINUE,

        /// <summary>
        /// Represents the result of a message box operation when the operation times out.
        /// </summary>
        /// <remarks>This value is returned when a message box is displayed with a timeout and the timeout period elapses before the user interacts with the message box.</remarks>
        Timeout = MESSAGEBOX_RESULT.IDTIMEOUT,
    }
}
