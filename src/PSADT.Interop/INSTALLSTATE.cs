namespace PSADT.Interop
{
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
