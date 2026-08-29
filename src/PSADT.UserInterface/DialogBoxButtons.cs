using Windows.Win32.UI.WindowsAndMessaging;

namespace PSADT.UserInterface
{
    /// <summary>
    /// Specifies the set of buttons to display in a message box.
    /// </summary>
    /// <remarks>The members are the <c>MB_*</c> button constants, which Win32 packs into the low nibble of a
    /// message box's style word as a small integer rather than as independent bits. They are therefore mutually
    /// exclusive: a caller chooses one. The style word is assembled by casting this, an icon and a default button to
    /// <c>MESSAGEBOX_STYLE</c> and combining those, so nothing needs to combine two members of this type - and
    /// marking it as flags would suggest otherwise while quietly turning <c>OkCancel | AbortRetryIgnore</c> into
    /// <c>YesNoCancel</c>.</remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1008:Enums should have zero value", Justification = "The zero value is MB_OK, which is named as per the Win32 API rather than as 'None'.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1028:Enum Storage should be Int32", Justification = "This is typed as per the Win32 API.")]
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
