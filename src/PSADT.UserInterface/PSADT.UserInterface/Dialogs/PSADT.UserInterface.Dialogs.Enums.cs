using System;
using Windows.Win32.UI.WindowsAndMessaging;

namespace PSADT.UserInterface.Dialogs
{
    /// <summary>
    /// Defines the type of dialog to be displayed.
    /// </summary>
    public enum DialogStyle
    {
        /// <summary>
        /// Presents a dialog using the classic interface.
        /// </summary>
        Classic,

        /// <summary>
        /// Presents a dialog using the fluent interface.
        /// </summary>
        Fluent,
    }

    public enum DialogType
    {
        /// <summary>
        /// Represents the CloseAppsDialog type.
        /// </summary>
        CloseAppsDialog,

        /// <summary>
        /// Represents the CustomDialog type.
        /// </summary>
        CustomDialog,

        /// <summary>
        /// Represents a Windows 9x-style message box.
        /// </summary>
        DialogBox,

        /// <summary>
        /// Represents the InputDialog type.
        /// </summary>
        InputDialog,

        /// <summary>
        /// Represents the ProgressDialog type.
        /// </summary>
        ProgressDialog,

        /// <summary>
        /// Represents the RestartDialog type.
        /// </summary>
        RestartDialog,
    }

    /// <summary>
    /// Defines the position of the dialog window on the screen
    /// </summary>
    public enum DialogPosition
    {
        /// <summary>
        /// Represents the top-left corner of the screen.
        /// </summary>
        TopLeft,

        /// <summary>
        /// Represents the top-middle area of the screen.
        /// </summary>
        Top,

        /// <summary>
        /// Represents the top-right corner of the screen.
        /// </summary>
        TopRight,

        /// <summary>
        /// Represents the top-middle area of the screen, half way between the top and center.
        /// </summary>
        TopCenter,

        /// <summary>
        /// Represents the center of the screen.
        /// </summary>
        Center,

        /// <summary>
        /// Represents the bottom-left corner of the screen.
        /// </summary>
        BottomLeft,

        /// <summary>
        /// Represents the bottom-middle area of the screen.
        /// </summary>
        Bottom,

        /// <summary>
        /// Represents the bottom-right corner of the screen.
        /// </summary>
        BottomRight,

        /// <summary>
        /// Represents the bottom-middle area of the screen, half way between the bottom and center.
        /// </summary>
        BottomCenter,
    }

    /// <summary>
    /// Defines the alignment of the message text in the dialog.
    /// </summary>
    public enum DialogMessageAlignment
    {
        /// <summary>
        /// Aligns the message text to the left
        /// </summary>
        Left,

        /// <summary>
        /// Aligns the message text to the center
        /// </summary>
        Center,

        /// <summary>
        /// Aligns the message text to the right
        /// </summary>
        Right,
    }

    /// <summary>
    /// Defines the system icons that can be used in the dialog.
    /// </summary>
    public enum DialogSystemIcon
    {
        /// <summary>
        /// Icon for generic application
        /// </summary>
        Application,

        /// <summary>
        /// Icon for asterisk
        /// </summary>
        Asterisk,

        /// <summary>
        /// Icon for error
        /// </summary>
        Error,

        /// <summary>
        /// Icon for exclamation
        /// </summary>
        Exclamation,

        /// <summary>
        /// Icon for hand
        /// </summary>
        Hand,

        /// <summary>
        /// Icon for information
        /// </summary>
        Information,

        /// <summary>
        /// Icon for question
        /// </summary>
        Question,

        /// <summary>
        /// Icon for shield
        /// </summary>
        Shield,

        /// <summary>
        /// Icon for stop
        /// </summary>
        Warning,

        /// <summary>
        /// Icon for the Windows logo
        /// </summary>
        WinLogo,
    }

    /// <summary>
    /// Specifies the set of buttons to display in a message box.
    /// </summary>
    /// <remarks>This enumeration is used to define the button options available in a message box, such as "OK", "Cancel", "Yes", "No", etc. It supports a combination of values due to the <see cref="FlagsAttribute"/> applied to the enumeration.</remarks>
    [Flags]
    public enum DialogBoxButtons : uint
    {
        /// <summary>
        /// OK button only (default). This member is equivalent to the Visual Basic constant vbOKOnly.
        /// </summary>
        Ok = MESSAGEBOX_STYLE.MB_OK,

        /// <summary>
        /// OK and Cancel buttons. This member is equivalent to the Visual Basic constant vbOKCancel.
        /// </summary>
        OkCancel = MESSAGEBOX_STYLE.MB_OKCANCEL,

        /// <summary>
        /// Abort, Retry, and Ignore buttons. This member is equivalent to the Visual Basic constant vbAbortRetryIgnore.
        /// </summary>
        AbortRetryIgnore = MESSAGEBOX_STYLE.MB_ABORTRETRYIGNORE,

        /// <summary>
        /// Yes, No, and Cancel buttons. This member is equivalent to the Visual Basic constant vbYesNoCancel.
        /// </summary>
        YesNoCancel = MESSAGEBOX_STYLE.MB_YESNOCANCEL,

        /// <summary>
        /// Yes and No buttons. This member is equivalent to the Visual Basic constant vbYesNo.
        /// </summary>
        YesNo = MESSAGEBOX_STYLE.MB_YESNO,

        /// <summary>
        /// Retry and Cancel buttons. This member is equivalent to the Visual Basic constant vbRetryCancel.
        /// </summary>
        RetryCancel = MESSAGEBOX_STYLE.MB_RETRYCANCEL,

        /// <summary>
        /// Represents a message box style that displays Cancel, Try Again, and Continue buttons.
        /// </summary>
        CancelTryContinue = MESSAGEBOX_STYLE.MB_CANCELTRYCONTINUE,
    }

    /// <summary>
    /// Specifies the icon to display in a message box to convey the nature of the message.
    /// </summary>
    /// <remarks>This enumeration is used to indicate the type of message being displayed in a message box, such as an error, warning, or informational message. The icon helps users quickly understand the context or severity of the message. Multiple values can be combined using a bitwise OR  operation due to the <see cref="FlagsAttribute"/> applied to this enumeration.</remarks>
    [Flags]
    public enum DialogBoxIcon : uint
    {
        /// <summary>
        /// Represents the absence of any specific value or state.
        /// </summary>
        None = 0,

        /// <summary>
        /// Critical message. This member is equivalent to the Visual Basic constant vbCritical.
        /// </summary>
        Stop = MESSAGEBOX_STYLE.MB_ICONSTOP,

        /// <summary>
        /// Warning query. This member is equivalent to the Visual Basic constant vbQuestion.
        /// </summary>
        Question = MESSAGEBOX_STYLE.MB_ICONQUESTION,

        /// <summary>
        /// Warning message. This member is equivalent to the Visual Basic constant vbExclamation.
        /// </summary>
        Exclamation = MESSAGEBOX_STYLE.MB_ICONEXCLAMATION,

        /// <summary>
        /// Information message. This member is equivalent to the Visual Basic constant vbInformation.
        /// </summary>
        Information = MESSAGEBOX_STYLE.MB_ICONINFORMATION,
    }

    /// <summary>
    /// Specifies the default button for a message box displayed to the user.
    /// </summary>
    /// <remarks>This enumeration is used to indicate which button in a message box is preselected by default when the dialog is displayed. The default button is typically activated when the user presses the Enter key without explicitly selecting a button.</remarks>
    [Flags]
    public enum DialogBoxDefaultButton : uint
    {
        /// <summary>
        /// Default button is the first button in the dialog box.
        /// </summary>
        First = MESSAGEBOX_STYLE.MB_DEFBUTTON1,

        /// <summary>
        /// Default button is the second button in the dialog box.
        /// </summary>
        Second = MESSAGEBOX_STYLE.MB_DEFBUTTON2,

        /// <summary>
        /// Default button is the third button in the dialog box.
        /// </summary>
        Third = MESSAGEBOX_STYLE.MB_DEFBUTTON3,
    }

    /// <summary>
    /// Specifies the possible results of a message box operation.
    /// </summary>
    /// <remarks>This enumeration represents the various outcomes of a message box interaction, such as the button selected by the user or other conditions like a timeout. Each value corresponds to a specific Windows API constant from <see cref="DialogBox_RESULT" />. These results are typically used to determine the user's response to a prompt or dialog.</remarks>
    public enum DialogBoxResult
    {
        /// <summary>
        /// Represents the result of a message box operation where the user selects the "OK" option.
        /// </summary>
        /// <remarks>This value corresponds to the Windows API constant <see cref="MESSAGEBOX_RESULT.IDOK"/>. It is typically used to indicate that the user acknowledged the message or completed an action in response to a prompt.</remarks>"
        OK = MESSAGEBOX_RESULT.IDOK,

        /// <summary>
        /// Represents the result of a message box operation where the user selects the "Cancel" option.
        /// </summary>
        /// <remarks>This value corresponds to the Windows API constant <see cref="MESSAGEBOX_RESULT.IDCANCEL"/>. It is typically used to indicate that the user canceled the operation or dismissed the message box without making a selection.</remarks>
        Cancel = MESSAGEBOX_RESULT.IDCANCEL,

        /// <summary>
        /// Represents the result of a message box operation where the user selected the "Abort" option.
        /// </summary>
        /// <remarks>This value corresponds to the Windows API constant <see cref="MESSAGEBOX_RESULT.IDABORT"/>. It is typically used to indicate that the user chose to abort an operation in response to a message box prompt.</remarks>
        Abort = MESSAGEBOX_RESULT.IDABORT,

        /// <summary>
        /// Represents the result of a message box operation where the user selects the "Retry" option.
        /// </summary>
        /// <remarks>This value corresponds to the Windows API constant <see cref="MESSAGEBOX_RESULT.IDRETRY"/>. It is typically used to indicate that the user has chosen to retry an operation after encountering an error or prompt.</remarks>
        Retry = MESSAGEBOX_RESULT.IDRETRY,

        /// <summary>
        /// Represents the result of a message box operation where the user selects "Ignore."
        /// </summary>
        /// <remarks>This value corresponds to the Windows API constant <see cref="MESSAGEBOX_RESULT.IDIGNORE"/>. It is typically used to handle scenarios where the user chooses to ignore a warning or error.</remarks>
        Ignore = MESSAGEBOX_RESULT.IDIGNORE,

        /// <summary>
        /// Represents a result indicating that the user selected "Yes" in a message box.
        /// </summary>
        /// <remarks>This value corresponds to the Windows API constant <see cref="MESSAGEBOX_RESULT.IDYES"/>. It is typically used to indicate that the user has confirmed an action or answered "Yes" to a prompt.</remarks>
        Yes = MESSAGEBOX_RESULT.IDYES,

        /// <summary>
        /// Represents the result of a message box operation where the user selected "No".
        /// </summary>
        /// <remarks>This value corresponds to the Windows API constant <see cref="MESSAGEBOX_RESULT.IDNO"/>. It is typically used to indicate that the user declined an action or answered "No" to a prompt.</remarks>
        No = MESSAGEBOX_RESULT.IDNO,

        /// <summary>
        /// Represents the result of a message box when the Close button is selected.
        /// </summary>
        /// <remarks>This value corresponds to the Close button being clicked or the message box being dismissed without selecting any other option. It is typically used to handle scenarios where the user close the message box without making a specific choice.</remarks>
        Close = MESSAGEBOX_RESULT.IDCLOSE,

        /// <summary>
        /// Represents the result indicating that the user selected the "Try Again" option in a message box.
        /// </summary>
        /// <remarks>This value corresponds to the Windows API constant <see cref="MESSAGEBOX_RESULT.IDTRYAGAIN"/>. It is typically used to handle scenarios where the user opts to retry an operation after a failure.</remarks>
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
