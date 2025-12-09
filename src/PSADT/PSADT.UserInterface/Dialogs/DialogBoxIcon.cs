using System;
using Windows.Win32.UI.WindowsAndMessaging;

namespace PSADT.UserInterface.Dialogs
{
    /// <summary>
    /// Specifies the icon to display in a message box to convey the nature of the message.
    /// </summary>
    /// <remarks>This enumeration is used to indicate the type of message being displayed in a message box, such as an error, warning, or informational message. The icon helps users quickly understand the context or severity of the message. Multiple values can be combined using a bitwise OR operation due to the <see cref="FlagsAttribute"/> applied to this enumeration.</remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1028:Enum Storage should be Int32", Justification = "This is typed as per the Win32 API.")]
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
}
