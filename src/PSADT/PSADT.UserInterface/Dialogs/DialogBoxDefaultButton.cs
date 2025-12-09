using System;
using Windows.Win32.UI.WindowsAndMessaging;

namespace PSADT.UserInterface.Dialogs
{
    /// <summary>
    /// Specifies the default button for a message box displayed to the user.
    /// </summary>
    /// <remarks>This enumeration is used to indicate which button in a message box is preselected by default when the dialog is displayed. The default button is typically activated when the user presses the Enter key without explicitly selecting a button.</remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1008:Enums should have zero value", Justification = "All values are named as per the Win32 API.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1028:Enum Storage should be Int32", Justification = "This is typed as per the Win32 API.")]
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
}
