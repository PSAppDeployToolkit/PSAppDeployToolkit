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
    /// <remarks>This structure provides type safety and convenience when specifying database persistence
    /// modes in interop scenarios with Windows Installer APIs. It includes predefined values for common modes such as
    /// direct, transactional, and read-only access, as well as support for patch files. Use the provided static fields
    /// to select the appropriate mode when working with MSI database operations. This type is intended for internal use
    /// and is not intended to be used directly in application code.</remarks>
    internal readonly record struct MSI_PERSISTENCE_MODE
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
        /// Initializes a new instance of the MSI_PERSISTENCE_MODE structure with the specified value.
        /// </summary>
        /// <param name="value">A PCWSTR representing the persistence mode value to assign. Must reference a valid, null-terminated Unicode
        /// string.</param>
        private MSI_PERSISTENCE_MODE(PCWSTR value)
        {
            unsafe
            {
                Value = (nint)value.Value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the MSI_PERSISTENCE_MODE structure with the specified integer value.
        /// </summary>
        /// <param name="value">The integer value to assign to the persistence mode. This value determines the mode represented by the
        /// instance.</param>
        private MSI_PERSISTENCE_MODE(int value)
        {
            Value = value;
        }

        /// <summary>
        /// Defines an explicit conversion from an instance of the MSI_PERSISTENCE_MODE structure to an nint.
        /// </summary>
        /// <remarks>Use this operator to obtain the underlying pointer value represented by the
        /// MSI_PERSISTENCE_MODE instance. This conversion is explicit and must be cast by the caller.</remarks>
        /// <param name="h">The MSI_PERSISTENCE_MODE instance to convert to an nint.</param>
        public static explicit operator nint(MSI_PERSISTENCE_MODE h)
        {
            return h.Value;
        }

        /// <summary>
        /// Defines an explicit conversion from an MSI_PERSISTENCE_MODE value to a PCWSTR pointer.
        /// </summary>
        /// <remarks>Use this operator when you need to obtain the underlying PCWSTR pointer
        /// representation of an MSI_PERSISTENCE_MODE value. The caller is responsible for ensuring that the conversion
        /// is valid in the context of the API being used.</remarks>
        /// <param name="h">The MSI_PERSISTENCE_MODE value to convert.</param>
        public static explicit operator PCWSTR(MSI_PERSISTENCE_MODE h)
        {
            unsafe
            {
                return (PCWSTR)(void*)h.Value;
            }
        }

        /// <summary>
        /// Converts an instance of the MSI_PERSISTENCE_MODE structure to its underlying 32-bit unsigned integer value.
        /// </summary>
        /// <param name="h">The MSI_PERSISTENCE_MODE instance to convert.</param>
        public static explicit operator uint(MSI_PERSISTENCE_MODE h)
        {
            return (uint)h.Value;
        }

        /// <summary>
        /// Determines whether the specified pointer and persistence mode represent different values.
        /// </summary>
        /// <param name="h1">The pointer to compare.</param>
        /// <param name="h2">The persistence mode to compare.</param>
        /// <returns>true if the value of h1 does not equal the value represented by h2; otherwise, false.</returns>
        public static bool operator !=(nint h1, MSI_PERSISTENCE_MODE h2)
        {
            return h1 != h2.Value;
        }

        /// <summary>
        /// Determines whether the specified pointer and persistence mode value are equal.
        /// </summary>
        /// <param name="h1">The pointer to compare.</param>
        /// <param name="h2">The persistence mode value to compare.</param>
        /// <returns>true if the value of h1 is equal to the value of h2; otherwise, false.</returns>
        public static bool operator ==(nint h1, MSI_PERSISTENCE_MODE h2)
        {
            return h1 == h2.Value;
        }

        /// <summary>
        /// Determines whether a specified PCWSTR value and MSI_PERSISTENCE_MODE value are not equal.
        /// </summary>
        /// <param name="h1">The PCWSTR value to compare.</param>
        /// <param name="h2">The MSI_PERSISTENCE_MODE value to compare.</param>
        /// <returns>true if the values are not equal; otherwise, false.</returns>
        public static bool operator !=(PCWSTR h1, MSI_PERSISTENCE_MODE h2)
        {
            unsafe
            {
                return (nint)h1.Value != h2.Value;
            }
        }

        /// <summary>
        /// Determines whether the specified PCWSTR value and MSI_PERSISTENCE_MODE value are equal.
        /// </summary>
        /// <param name="h1">The PCWSTR value to compare.</param>
        /// <param name="h2">The MSI_PERSISTENCE_MODE value to compare.</param>
        /// <returns>true if the underlying values of h1 and h2 are equal; otherwise, false.</returns>
        public static bool operator ==(PCWSTR h1, MSI_PERSISTENCE_MODE h2)
        {
            unsafe
            {
                return (nint)h1.Value == h2.Value;
            }
        }

        /// <summary>
        /// Determines whether a specified unsigned integer value and an MSI_PERSISTENCE_MODE value are not equal.
        /// </summary>
        /// <param name="h1">The unsigned integer value to compare.</param>
        /// <param name="h2">The MSI_PERSISTENCE_MODE value to compare.</param>
        /// <returns>true if the values are not equal; otherwise, false.</returns>
        public static bool operator !=(uint h1, MSI_PERSISTENCE_MODE h2)
        {
            return (nint)h1 != h2;
        }

        /// <summary>
        /// Determines whether the specified unsigned integer value is equal to the specified MSI_PERSISTENCE_MODE
        /// value.
        /// </summary>
        /// <remarks>This operator enables direct comparison between a uint and an MSI_PERSISTENCE_MODE
        /// value. The comparison is performed by converting the uint to an nint and comparing it to the
        /// MSI_PERSISTENCE_MODE value.</remarks>
        /// <param name="h1">The unsigned integer value to compare.</param>
        /// <param name="h2">The MSI_PERSISTENCE_MODE value to compare.</param>
        /// <returns>true if the values are equal; otherwise, false.</returns>
        public static bool operator ==(uint h1, MSI_PERSISTENCE_MODE h2)
        {
            return (nint)h1 == h2;
        }

        /// <summary>
        /// Represents the underlying pointer value of the MSI_PERSISTENCE_MODE instance.
        /// </summary>
        private readonly nint Value;
    }
}
