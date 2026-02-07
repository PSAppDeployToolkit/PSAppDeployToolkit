using System;
using Windows.Win32.UI.WindowsAndMessaging;

namespace PSADT.UserInterface
{
    /// <summary>
    /// Specifies the set of buttons to display in a message box.
    /// </summary>
    /// <remarks>This enumeration is used to define the button options available in a message box, such as "OK", "Cancel", "Yes", "No", etc. It supports a combination of values due to the <see cref="FlagsAttribute"/> applied to the enumeration.</remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1008:Enums should have zero value", Justification = "All values are named as per the Win32 API.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1028:Enum Storage should be Int32", Justification = "This is typed as per the Win32 API.")]
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
}
