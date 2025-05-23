using System;
using Microsoft.VisualBasic;

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

    internal enum DialogType
    {
        /// <summary>
        /// Represents the CloseAppsDialog type.
        /// </summary>
        CloseApps,

        /// <summary>
        /// Represents the CustomDialog type.
        /// </summary>
        Custom,

        /// <summary>
        /// Represents the InputDialog type.
        /// </summary>
        Input,

        /// <summary>
        /// Represents the ProgressDialog type.
        /// </summary>
        Progress,

        /// <summary>
        /// Represents the RestartDialog type.
        /// </summary>
        Restart,
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

        /// <summary>
        /// Represents the center of the screen.
        /// </summary>
        Center,
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
    public enum MessageBoxButtons
    {
        /// <summary>
        /// OK button only (default). This member is equivalent to the Visual Basic constant vbOKOnly.
        /// </summary>
        Ok = MsgBoxStyle.OkOnly,

        /// <summary>
        /// OK and Cancel buttons. This member is equivalent to the Visual Basic constant vbOKCancel.
        /// </summary>
        OkCancel = MsgBoxStyle.OkCancel,

        /// <summary>
        /// Abort, Retry, and Ignore buttons. This member is equivalent to the Visual Basic constant vbAbortRetryIgnore.
        /// </summary>
        AbortRetryIgnore = MsgBoxStyle.AbortRetryIgnore,

        /// <summary>
        /// Yes, No, and Cancel buttons. This member is equivalent to the Visual Basic constant vbYesNoCancel.
        /// </summary>
        YesNoCancel = MsgBoxStyle.YesNoCancel,

        /// <summary>
        /// Yes and No buttons. This member is equivalent to the Visual Basic constant vbYesNo.
        /// </summary>
        YesNo = MsgBoxStyle.YesNo,

        /// <summary>
        /// Retry and Cancel buttons. This member is equivalent to the Visual Basic constant vbRetryCancel.
        /// </summary>
        RetryCancel = MsgBoxStyle.RetryCancel,
    }

    /// <summary>
    /// Specifies the icon to display in a message box to convey the nature of the message.
    /// </summary>
    /// <remarks>This enumeration is used to indicate the type of message being displayed in a message box, such as an error, warning, or informational message. The icon helps users quickly understand the context or severity of the message. Multiple values can be combined using a bitwise OR  operation due to the <see cref="FlagsAttribute"/> applied to this enumeration.</remarks>
    [Flags]
    public enum MessageBoxIcon
    {
        /// <summary>
        /// Represents the absence of any specific value or state.
        /// </summary>
        None = 0,

        /// <summary>
        /// Critical message. This member is equivalent to the Visual Basic constant vbCritical.
        /// </summary>
        Stop = MsgBoxStyle.Critical,

        /// <summary>
        /// Warning query. This member is equivalent to the Visual Basic constant vbQuestion.
        /// </summary>
        Question = MsgBoxStyle.Question,

        /// <summary>
        /// Warning message. This member is equivalent to the Visual Basic constant vbExclamation.
        /// </summary>
        Exclamation = MsgBoxStyle.Exclamation,

        /// <summary>
        /// Information message. This member is equivalent to the Visual Basic constant vbInformation.
        /// </summary>
        Information = MsgBoxStyle.Information,
    }

    /// <summary>
    /// Specifies the default button for a message box displayed to the user.
    /// </summary>
    /// <remarks>This enumeration is used to indicate which button in a message box is preselected by default when the dialog is displayed. The default button is typically activated when the user presses the Enter key without explicitly selecting a button.</remarks>
    [Flags]
    public enum MessageBoxDefaultButton
    {
        /// <summary>
        /// Default button is the first button in the dialog box.
        /// </summary>
        First = MsgBoxStyle.DefaultButton1,

        /// <summary>
        /// Default button is the second button in the dialog box.
        /// </summary>
        Second = MsgBoxStyle.DefaultButton2,

        /// <summary>
        /// Default button is the third button in the dialog box.
        /// </summary>
        Third = MsgBoxStyle.DefaultButton3,
    }
}
