using System;
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
        /// Add this flag to indicate a patch file. Combine with another mode using the + operator.
        /// </summary>
        /// <remarks>
        /// The Win32 API defines MSIDBOPEN_PATCHFILE as the integer 16. When combined with a PCWSTR mode
        /// (e.g., MSIDBOPEN_READONLY + MSIDBOPEN_PATCHFILE), char* pointer arithmetic is used to
        /// produce the final mode value: (char*)0 + 16 = 32 (since sizeof(char) = 2 in UTF-16).
        /// </remarks>
        internal static readonly unsafe MSI_PERSISTENCE_MODE MSIDBOPEN_PATCHFILE = new((char*)Windows.Win32.PInvoke.MSIDBOPEN_PATCHFILE);

        /// <summary>
        /// Initializes a new instance of the <see cref="MSI_PERSISTENCE_MODE"/> class with the specified value.
        /// </summary>
        /// <param name="value">A PCWSTR representing the persistence mode value to assign.</param>
        /// <param name="name">The name of the constant, automatically captured from the calling member.</param>
        private MSI_PERSISTENCE_MODE(PCWSTR value, [System.Runtime.CompilerServices.CallerMemberName] string name = null!) : base(value, name)
        {
        }

        /// <summary>
        /// Combines two persistence modes using char* pointer arithmetic.
        /// </summary>
        /// <remarks>
        /// This operator enables Win32-style mode combination such as MSIDBOPEN_READONLY + MSIDBOPEN_PATCHFILE.
        /// One operand must be MSIDBOPEN_PATCHFILE (integer-based), and the other must be a PCWSTR-based mode.
        /// Pointer + pointer arithmetic is not permitted; one side must be MSIDBOPEN_PATCHFILE.
        /// </remarks>
        /// <param name="left">The first persistence mode.</param>
        /// <param name="right">The second persistence mode.</param>
        /// <returns>A new <see cref="MSI_PERSISTENCE_MODE"/> representing the combined mode.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="left"/> or <paramref name="right"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when neither operand is MSIDBOPEN_PATCHFILE.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Converting this to conditional expression just makes a mess.")]
        public static MSI_PERSISTENCE_MODE operator +(MSI_PERSISTENCE_MODE left, MSI_PERSISTENCE_MODE right)
        {
            // Validate that neither operand is null.
            if (left is null)
            {
                throw new ArgumentNullException(nameof(left));
            }
            if (right is null)
            {
                throw new ArgumentNullException(nameof(right));
            }

            // Determine which operand is MSIDBOPEN_PATCHFILE (the integer offset) and which is the base pointer
            bool leftIsPatchFile = ReferenceEquals(left, MSIDBOPEN_PATCHFILE);
            bool rightIsPatchFile = ReferenceEquals(right, MSIDBOPEN_PATCHFILE);
            if (!leftIsPatchFile && !rightIsPatchFile)
            {
                throw new InvalidOperationException("Pointer + pointer arithmetic is not permitted. One operand must be MSIDBOPEN_PATCHFILE.");
            }

            // Use the PATCHFILE value as the offset and the other as the base pointer,
            // then use char* pointer arithmetic: (char*)base + offset.
            MSI_PERSISTENCE_MODE baseMode, offsetMode;
            if (leftIsPatchFile)
            {
                baseMode = right;
                offsetMode = left;
            }
            else
            {
                baseMode = left;
                offsetMode = right;
            }
            unsafe
            {
                return new((char*)baseMode.ToIntPtr() + (int)offsetMode, $"{baseMode}, {offsetMode}");
            }
        }
    }

    /// <summary>
    /// Specifies the various installation states recognized by the Windows Installer for applications, features, or
    /// components.
    /// </summary>
    /// <remarks>Use this enumeration to determine or specify the current installation status of a product,
    /// feature, or component within the Windows Installer framework. Each value corresponds to a distinct state that
    /// may be encountered during installation, maintenance, repair, or removal operations. This enumeration is
    /// typically used when querying or managing installation states to handle different scenarios such as incomplete
    /// installations, missing sources, or advertised applications.</remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "This is how they're named in the Win32 API.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1712:Do not prefix enum values with type name", Justification = "This is how they're named in the Win32 API.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1069:Enums values should not be duplicated", Justification = "This duplication is within the Win32 API")]
    public enum INSTALLSTATE
    {
        /// <summary>
        /// Represents an installation state indicating that the installation is not used or not applicable.
        /// </summary>
        /// <remarks>This constant is part of the Windows Installer API and is typically used to specify
        /// that a particular installation state does not apply to the product, feature, or component being queried. It
        /// is useful when checking or modifying installation states to identify items that are not relevant to the
        /// current context.</remarks>
        INSTALLSTATE_NOTUSED = Windows.Win32.System.ApplicationInstallationAndServicing.INSTALLSTATE.INSTALLSTATE_NOTUSED,

        /// <summary>
        /// Represents an installation state indicating that the configuration is invalid or corrupted.
        /// </summary>
        /// <remarks>This value is used to identify scenarios where the installation configuration is not
        /// set up correctly, which may prevent successful installation, repair, or removal operations. Applications can
        /// use this state to detect and handle configuration issues appropriately.</remarks>
        INSTALLSTATE_BADCONFIG = Windows.Win32.System.ApplicationInstallationAndServicing.INSTALLSTATE.INSTALLSTATE_BADCONFIG,

        /// <summary>
        /// Represents the installation state indicating that the installation is incomplete.
        /// </summary>
        /// <remarks>This state is typically used to indicate that an installation process was started but
        /// did not complete successfully, possibly due to an error or interruption.</remarks>
        INSTALLSTATE_INCOMPLETE = Windows.Win32.System.ApplicationInstallationAndServicing.INSTALLSTATE.INSTALLSTATE_INCOMPLETE,

        /// <summary>
        /// Represents the state indicating that the installation source is not available.
        /// </summary>
        /// <remarks>Use this value to determine when the source files required for installation or
        /// servicing are missing or inaccessible. This state may require user intervention to provide the necessary
        /// source media or network location.</remarks>
        INSTALLSTATE_SOURCEABSENT = Windows.Win32.System.ApplicationInstallationAndServicing.INSTALLSTATE.INSTALLSTATE_SOURCEABSENT,

        /// <summary>
        /// Represents the installation state indicating that additional data is available and required to continue the
        /// installation process.
        /// </summary>
        /// <remarks>This value is typically used in scenarios where the installation process cannot
        /// proceed without further information or input. It is part of the INSTALLSTATE enumeration in the Windows API
        /// and may be encountered when an installation requires user intervention or supplemental data.</remarks>
        INSTALLSTATE_MOREDATA = Windows.Win32.System.ApplicationInstallationAndServicing.INSTALLSTATE.INSTALLSTATE_MOREDATA,

        /// <summary>
        /// Represents an installation state indicating that an invalid argument was provided to an installation
        /// operation.
        /// </summary>
        /// <remarks>Use this value to identify scenarios where an installation function receives
        /// arguments that do not meet the required criteria. It is typically used in error handling to signal that the
        /// input parameters are not valid for the requested operation.</remarks>
        INSTALLSTATE_INVALIDARG = Windows.Win32.System.ApplicationInstallationAndServicing.INSTALLSTATE.INSTALLSTATE_INVALIDARG,

        /// <summary>
        /// Represents an unknown installation state for an application.
        /// </summary>
        /// <remarks>This constant is used to indicate that the installation state of an application
        /// cannot be determined. It is typically used in scenarios where the installation status is queried but the
        /// result is inconclusive.</remarks>
        INSTALLSTATE_UNKNOWN = Windows.Win32.System.ApplicationInstallationAndServicing.INSTALLSTATE.INSTALLSTATE_UNKNOWN,

        /// <summary>
        /// Represents the installation state indicating that the application is broken and cannot be used.
        /// </summary>
        /// <remarks>This state typically occurs when the application is improperly installed or has
        /// missing components. It is important to address the underlying issues to restore the application's
        /// functionality.</remarks>
        INSTALLSTATE_BROKEN = Windows.Win32.System.ApplicationInstallationAndServicing.INSTALLSTATE.INSTALLSTATE_BROKEN,

        /// <summary>
        /// Represents the installation state of an application as advertised, indicating that the application is
        /// available for installation but not yet installed.
        /// </summary>
        /// <remarks>This state is typically used in scenarios where the application is registered with
        /// the system but not fully installed. It allows for the application to be installed on demand.</remarks>
        INSTALLSTATE_ADVERTISED = Windows.Win32.System.ApplicationInstallationAndServicing.INSTALLSTATE.INSTALLSTATE_ADVERTISED,

        /// <summary>
        /// Indicates that the application has been removed from the system.
        /// </summary>
        /// <remarks>This value is part of the INSTALLSTATE enumeration, which represents the various
        /// installation states recognized by the Windows Installer. Use this state to determine if an application is no
        /// longer present on the system.</remarks>
        INSTALLSTATE_REMOVED = Windows.Win32.System.ApplicationInstallationAndServicing.INSTALLSTATE.INSTALLSTATE_REMOVED,

        /// <summary>
        /// Represents the installation state indicating that the application is not present on the system.
        /// </summary>
        /// <remarks>Use this value to check for the absence of an application during installation or
        /// maintenance operations. This constant corresponds to the Windows Installer state for an absent product or
        /// feature.</remarks>
        INSTALLSTATE_ABSENT = Windows.Win32.System.ApplicationInstallationAndServicing.INSTALLSTATE.INSTALLSTATE_ABSENT,

        /// <summary>
        /// Represents the installation state indicating that the application is installed locally on the system.
        /// </summary>
        /// <remarks>This constant is used to check the installation status of an application within the
        /// Windows Installer framework.</remarks>
        INSTALLSTATE_LOCAL = Windows.Win32.System.ApplicationInstallationAndServicing.INSTALLSTATE.INSTALLSTATE_LOCAL,

        /// <summary>
        /// Indicates that the source location for the installation is available.
        /// </summary>
        /// <remarks>This value is part of the INSTALLSTATE enumeration and is used to determine whether
        /// the original installation source for an application is accessible. It is typically used when assessing the
        /// state of an application's installation, such as during repair or maintenance operations.</remarks>
        INSTALLSTATE_SOURCE = Windows.Win32.System.ApplicationInstallationAndServicing.INSTALLSTATE.INSTALLSTATE_SOURCE,

        /// <summary>
        /// Represents the default installation state for an application as defined by the system.
        /// </summary>
        /// <remarks>Use this value to indicate that the installation state should be determined by the
        /// system's default behavior. This is typically used when querying or setting installation states where a
        /// specific state is not required.</remarks>
        INSTALLSTATE_DEFAULT = Windows.Win32.System.ApplicationInstallationAndServicing.INSTALLSTATE.INSTALLSTATE_DEFAULT,
    }
}
