namespace PSADT.LibraryInterfaces
{
    /// <summary>
    /// Specifies the length of the strings for the package's identity fields. The length is measured in characters and may or may not include space for the NULL terminator.
    /// </summary>
    /// <remarks>Constants in the form: APPLICATION_USER_MODEL_ID_*_LENGTH and PACKAGE_RELATIVE_APPLICATION_ID_*_LENGTH include space for a NULL terminator, but constants in the form: PACKAGE_*_LENGTH do not include space for a NULL terminator.</remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1069:Enums values should not be duplicated", Justification = "These values are precisely as they're defined in the Win32 API.")]
    internal enum APPX_IDENTITY : uint
    {
        /// <summary>
        /// The maximum length of the application user model ID. Space is included for the NULL terminator.
        /// </summary>
        APPLICATION_USER_MODEL_ID_MAX_LENGTH = Windows.Win32.PInvoke.APPLICATION_USER_MODEL_ID_MAX_LENGTH,

        /// <summary>
        /// The minimum length of the application user model ID. Space is included for the NULL terminator.
        /// </summary>
        APPLICATION_USER_MODEL_ID_MIN_LENGTH = Windows.Win32.PInvoke.APPLICATION_USER_MODEL_ID_MIN_LENGTH,

        /// <summary>
        /// The maximum length of the architecture. No space included for the NULL terminator.
        /// </summary>
        PACKAGE_ARCHITECTURE_MAX_LENGTH = Windows.Win32.PInvoke.PACKAGE_ARCHITECTURE_MAX_LENGTH,

        /// <summary>
        /// The minimum length of the architecture. No space included for the NULL terminator.
        /// </summary>
        PACKAGE_ARCHITECTURE_MIN_LENGTH = Windows.Win32.PInvoke.PACKAGE_ARCHITECTURE_MIN_LENGTH,

        /// <summary>
        /// The maximum length of the family name. No space included for the NULL terminator.
        /// </summary>
        PACKAGE_FAMILY_NAME_MAX_LENGTH = Windows.Win32.PInvoke.PACKAGE_FAMILY_NAME_MAX_LENGTH,

        /// <summary>
        /// The minimum length of the family name. No space included for the NULL terminator.
        /// </summary>
        PACKAGE_FAMILY_NAME_MIN_LENGTH = Windows.Win32.PInvoke.PACKAGE_FAMILY_NAME_MIN_LENGTH,

        /// <summary>
        /// The maximum length of the full name. No space included for the NULL terminator.
        /// </summary>
        PACKAGE_FULL_NAME_MAX_LENGTH = Windows.Win32.PInvoke.PACKAGE_FULL_NAME_MAX_LENGTH,

        /// <summary>
        /// The minimum length of the full name. No space included for the NULL terminator.
        /// </summary>
        PACKAGE_FULL_NAME_MIN_LENGTH = Windows.Win32.PInvoke.PACKAGE_FULL_NAME_MIN_LENGTH,

        /// <summary>
        /// The maximum length of the name. No space included for the NULL terminator.
        /// </summary>
        PACKAGE_NAME_MAX_LENGTH = Windows.Win32.PInvoke.PACKAGE_NAME_MAX_LENGTH,

        /// <summary>
        /// The minimum length of the name. No space included for the NULL terminator.
        /// </summary>
        PACKAGE_NAME_MIN_LENGTH = Windows.Win32.PInvoke.PACKAGE_NAME_MIN_LENGTH,

        /// <summary>
        /// The maximum length of the publisher ID. No space included for the NULL terminator.
        /// </summary>
        PACKAGE_PUBLISHERID_MAX_LENGTH = Windows.Win32.PInvoke.PACKAGE_PUBLISHERID_MAX_LENGTH,

        /// <summary>
        /// The minimum length of the publisher ID. No space included for the NULL terminator.
        /// </summary>
        PACKAGE_PUBLISHERID_MIN_LENGTH = Windows.Win32.PInvoke.PACKAGE_PUBLISHERID_MIN_LENGTH,

        /// <summary>
        /// The maximum length of the publisher. For example, CN=publisher. No space included for the NULL terminator.
        /// </summary>
        PACKAGE_PUBLISHER_MAX_LENGTH = Windows.Win32.PInvoke.PACKAGE_PUBLISHER_MAX_LENGTH,

        /// <summary>
        /// The minimum length of the publisher. No space included for the NULL terminator.
        /// </summary>
        PACKAGE_PUBLISHER_MIN_LENGTH = Windows.Win32.PInvoke.PACKAGE_PUBLISHER_MIN_LENGTH,

        /// <summary>
        /// The maximum length of the package relative application ID. Space is included for the NULL terminator.
        /// </summary>
        PACKAGE_RELATIVE_APPLICATION_ID_MAX_LENGTH = Windows.Win32.PInvoke.PACKAGE_RELATIVE_APPLICATION_ID_MAX_LENGTH,

        /// <summary>
        /// The minimum length of the package relative application ID. Space is included for the NULL terminator.
        /// </summary>
        PACKAGE_RELATIVE_APPLICATION_ID_MIN_LENGTH = Windows.Win32.PInvoke.PACKAGE_RELATIVE_APPLICATION_ID_MIN_LENGTH,

        /// <summary>
        /// The maximum length of the resource ID. No space included for the NULL terminator.
        /// </summary>
        PACKAGE_RESOURCEID_MAX_LENGTH = Windows.Win32.PInvoke.PACKAGE_RESOURCEID_MAX_LENGTH,

        /// <summary>
        /// The minimum length of the resource ID. No space included for the NULL terminator.
        /// </summary>
        PACKAGE_RESOURCEID_MIN_LENGTH = Windows.Win32.PInvoke.PACKAGE_RESOURCEID_MIN_LENGTH,

        /// <summary>
        /// The maximum length of the version. For example, xxxxx.xxxxx.xxxxx.xxxxx. No space included for the NULL terminator/
        /// </summary>
        PACKAGE_VERSION_MAX_LENGTH = Windows.Win32.PInvoke.PACKAGE_VERSION_MAX_LENGTH,

        /// <summary>
        /// The minimum length of the version. For example, x.x.x.x. No space included for the NULL terminator.
        /// </summary>
        PACKAGE_VERSION_MIN_LENGTH = Windows.Win32.PInvoke.PACKAGE_VERSION_MIN_LENGTH,
    }
}
