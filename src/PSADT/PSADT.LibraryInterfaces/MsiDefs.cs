using Windows.Win32.Foundation;

namespace PSADT.LibraryInterfaces
{
    /// <summary>
    /// Specifies property identifiers used to access standard metadata properties in Windows Installer (MSI) files.
    /// </summary>
    /// <remarks>These property IDs correspond to common document metadata fields, such as title, author, and
    /// creation date, and are typically used when reading or writing summary information streams in MSI packages. The
    /// values align with standard property IDs defined by the Windows property system.</remarks>
    internal enum MSI_PROPERTY_ID : uint
    {
        /// <summary>
        /// The property ID for "Codepage".
        /// </summary>
        PID_CODEPAGE = Windows.Win32.PInvoke.PID_CODEPAGE,

        /// <summary>
        /// The property ID for "Title".
        /// </summary>
        PID_TITLE = Windows.Win32.PInvoke.PID_TITLE,

        /// <summary>
        /// The property ID for "Subject".
        /// </summary>
        PID_SUBJECT = Windows.Win32.PInvoke.PID_SUBJECT,

        /// <summary>
        /// The property ID for "Author".
        /// </summary>
        PID_AUTHOR = Windows.Win32.PInvoke.PID_AUTHOR,

        /// <summary>
        /// The property ID for "Keywords".
        /// </summary>
        PID_KEYWORDS = Windows.Win32.PInvoke.PID_KEYWORDS,

        /// <summary>
        /// The property ID for "Comments".
        /// </summary>
        PID_COMMENTS = Windows.Win32.PInvoke.PID_COMMENTS,

        /// <summary>
        /// The property ID for "Template".
        /// </summary>
        PID_TEMPLATE = Windows.Win32.PInvoke.PID_TEMPLATE,

        /// <summary>
        /// The property ID for "Last Author".
        /// </summary>
        PID_LASTAUTHOR = Windows.Win32.PInvoke.PID_LASTAUTHOR,

        /// <summary>
        /// The property ID for "Revision Number".
        /// </summary>
        PID_REVNUMBER = Windows.Win32.PInvoke.PID_REVNUMBER,

        /// <summary>
        /// The property ID for "Last Printed".
        /// </summary>
        PID_LASTPRINTED = Windows.Win32.PInvoke.PID_LASTPRINTED,

        /// <summary>
        /// The property ID for "Create Time/Date".
        /// </summary>
        PID_CREATE_DTM = Windows.Win32.PInvoke.PID_CREATE_DTM,

        /// <summary>
        /// The property ID for "Last Save Time/Date".
        /// </summary>
        PID_LASTSAVE_DTM = Windows.Win32.PInvoke.PID_LASTSAVE_DTM,

        /// <summary>
        /// The property ID for "Page Count".
        /// </summary>
        PID_PAGECOUNT = Windows.Win32.PInvoke.PID_PAGECOUNT,

        /// <summary>
        /// The property ID for "Word Count".
        /// </summary>
        PID_WORDCOUNT = Windows.Win32.PInvoke.PID_WORDCOUNT,

        /// <summary>
        /// The property ID for "Character Count".
        /// </summary>
        PID_CHARCOUNT = Windows.Win32.PInvoke.PID_CHARCOUNT,

        /// <summary>
        /// The property ID for "Creating Application".
        /// </summary>
        PID_APPNAME = Windows.Win32.PInvoke.PID_APPNAME,

        /// <summary>
        /// The property ID for "Security".
        /// </summary>
        PID_SECURITY = Windows.Win32.PInvoke.PID_SECURITY,
    }

    /// <summary>
    /// Represents a persistence mode for opening or creating Windows Installer (MSI) databases, encapsulating the mode
    /// value used by native MSI APIs.
    /// </summary>
    /// <remarks>This class provides type safety and convenience when specifying database persistence
    /// modes in interop scenarios with Windows Installer APIs. It includes predefined values for common modes such as
    /// direct, transactional, and read-only access, as well as support for patch files. Use the provided static fields
    /// to select the appropriate mode when working with MSI database operations. This type is intended for internal use
    /// and is not intended to be used directly in application code.</remarks>
    internal sealed class MSI_PERSISTENCE_MODE : TypedConstant<MSI_PERSISTENCE_MODE>
    {
        /// <summary>
        /// Open a database read-only, no persistent changes.
        /// </summary>
        internal static readonly MSI_PERSISTENCE_MODE MSIDBOPEN_READONLY = new(Windows.Win32.PInvoke.MSIDBOPEN_READONLY);

        /// <summary>
        /// Open a database read/write in transaction mode.
        /// </summary>
        internal static readonly MSI_PERSISTENCE_MODE MSIDBOPEN_TRANSACT = new(Windows.Win32.PInvoke.MSIDBOPEN_TRANSACT);

        /// <summary>
        /// Open a database direct read/write without transaction.
        /// </summary>
        internal static readonly MSI_PERSISTENCE_MODE MSIDBOPEN_DIRECT = new(Windows.Win32.PInvoke.MSIDBOPEN_DIRECT);

        /// <summary>
        /// Create a new database, transact mode read/write.
        /// </summary>
        internal static readonly MSI_PERSISTENCE_MODE MSIDBOPEN_CREATE = new(Windows.Win32.PInvoke.MSIDBOPEN_CREATE);

        /// <summary>
        /// Create a new database, direct mode read/write.
        /// </summary>
        internal static readonly MSI_PERSISTENCE_MODE MSIDBOPEN_CREATEDIRECT = new(Windows.Win32.PInvoke.MSIDBOPEN_CREATEDIRECT);

        /// <summary>
        /// Add this flag to indicate a patch file.
        /// </summary>
        internal static readonly MSI_PERSISTENCE_MODE MSIDBOPEN_PATCHFILE = new(Windows.Win32.PInvoke.MSIDBOPEN_PATCHFILE * 2);

        /// <summary>
        /// Initializes a new instance of the <see cref="MSI_PERSISTENCE_MODE"/> class with the specified value.
        /// </summary>
        /// <param name="value">A PCWSTR representing the persistence mode value to assign.</param>
        /// <param name="name">The name of the constant, automatically captured from the calling member.</param>
        private MSI_PERSISTENCE_MODE(PCWSTR value, [System.Runtime.CompilerServices.CallerMemberName] string name = null!) : base(value, name)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MSI_PERSISTENCE_MODE"/> class with the specified integer value.
        /// </summary>
        /// <param name="value">The integer value to assign to the persistence mode.</param>
        /// <param name="name">The name of the constant, automatically captured from the calling member.</param>
        private MSI_PERSISTENCE_MODE(int value, [System.Runtime.CompilerServices.CallerMemberName] string name = null!) : base(value, name)
        {
        }
    }
}
