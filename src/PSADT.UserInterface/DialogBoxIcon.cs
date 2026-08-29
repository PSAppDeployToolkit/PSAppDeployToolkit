using Windows.Win32.UI.WindowsAndMessaging;

namespace PSADT.UserInterface
{
    /// <summary>
    /// Specifies the icon to display in a message box to convey the nature of the message.
    /// </summary>
    /// <remarks>This enumeration indicates the type of message being displayed in a message box, such as an error, warning, or informational message. The members are mutually exclusive - a message box shows one icon - so this is not a flags enumeration, even though Win32 does place them in their own bits of the style word so they can be combined with a button set and a default button. There is no member for "no icon": passing zero is what suppresses it, which is why the options type holds this as a nullable.</remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1027:Mark enums with FlagsAttribute", Justification = "The values are spaced as bits because Win32 packs them into their own part of the style word, not because they compose. A caller picks exactly one, and marking this as flags would make combining two of them look meaningful.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1028:Enum Storage should be Int32", Justification = "This is typed as per the Win32 API.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1008:Enums should have zero value", Justification = "There's no zero value in the Win32 API; a message box with no icon is asked for by not supplying one.")]
    public enum DialogBoxIcon : uint
    {
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
