using Windows.Win32.UI.WindowsAndMessaging;

namespace PSADT.UserInterface
{
    /// <summary>
    /// Specifies the default button for a message box displayed to the user.
    /// </summary>
    /// <remarks>This enumeration indicates which button in a message box is preselected when the dialog is displayed; the default button is activated when the user presses Enter without selecting one. The members are mutually exclusive - a caller chooses one - so this is not a flags enumeration, even though Win32 does place them in their own bits of the style word so they can be combined with a button set and an icon.</remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1008:Enums should have zero value", Justification = "The zero value is MB_DEFBUTTON1, which is named as per the Win32 API rather than as 'None'.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1027:Mark enums with FlagsAttribute", Justification = "The values are spaced as bits because Win32 packs them into their own part of the style word, not because they compose. A caller picks exactly one, and marking this as flags would make combining two of them look meaningful.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1028:Enum Storage should be Int32", Justification = "This is typed as per the Win32 API.")]
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
}
