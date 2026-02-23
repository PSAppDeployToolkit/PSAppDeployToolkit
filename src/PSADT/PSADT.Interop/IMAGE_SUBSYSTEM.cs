namespace PSADT.Interop
{
    /// <summary>
    /// The subsystem of the executable.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1028:Enum Storage should be Int32", Justification = "The type is correct for the underlying Win32 API.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "These values are precisely as they're defined in the Win32 API.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1712:Do not prefix enum values with type name", Justification = "These values are precisely as they're defined in the Win32 API.")]
    public enum IMAGE_SUBSYSTEM : ushort
    {
        /// <summary>
        /// Unknown
        /// </summary>
        IMAGE_SUBSYSTEM_UNKNOWN = Windows.Win32.System.Diagnostics.Debug.IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_UNKNOWN,

        /// <summary>
        /// Native
        /// </summary>
        IMAGE_SUBSYSTEM_NATIVE = Windows.Win32.System.Diagnostics.Debug.IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_NATIVE,

        /// <summary>
        /// Windows GUI
        /// </summary>
        IMAGE_SUBSYSTEM_WINDOWS_GUI = Windows.Win32.System.Diagnostics.Debug.IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_WINDOWS_GUI,

        /// <summary>
        /// Windows CUI
        /// </summary>
        IMAGE_SUBSYSTEM_WINDOWS_CUI = Windows.Win32.System.Diagnostics.Debug.IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_WINDOWS_CUI,

        /// <summary>
        /// OS/2 CUI
        /// </summary>
        IMAGE_SUBSYSTEM_OS2_CUI = Windows.Win32.System.Diagnostics.Debug.IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_OS2_CUI,

        /// <summary>
        /// POSIX CUI
        /// </summary>
        IMAGE_SUBSYSTEM_POSIX_CUI = Windows.Win32.System.Diagnostics.Debug.IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_POSIX_CUI,

        /// <summary>
        /// Windows CE GUI
        /// </summary>
        IMAGE_SUBSYSTEM_WINDOWS_CE_GUI = Windows.Win32.System.Diagnostics.Debug.IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_WINDOWS_CE_GUI,

        /// <summary>
        /// EFI Application
        /// </summary>
        IMAGE_SUBSYSTEM_EFI_APPLICATION = Windows.Win32.System.Diagnostics.Debug.IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_EFI_APPLICATION,

        /// <summary>
        /// EFI Boot Service Driver
        /// </summary>
        IMAGE_SUBSYSTEM_EFI_BOOT_SERVICE_DRIVER = Windows.Win32.System.Diagnostics.Debug.IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_EFI_BOOT_SERVICE_DRIVER,

        /// <summary>
        /// EFI Runtime Driver
        /// </summary>
        IMAGE_SUBSYSTEM_EFI_RUNTIME_DRIVER = Windows.Win32.System.Diagnostics.Debug.IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_EFI_RUNTIME_DRIVER,

        /// <summary>
        /// EFI ROM
        /// </summary>
        IMAGE_SUBSYSTEM_EFI_ROM = Windows.Win32.System.Diagnostics.Debug.IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_EFI_ROM,

        /// <summary>
        /// Xbox
        /// </summary>
        IMAGE_SUBSYSTEM_XBOX = Windows.Win32.System.Diagnostics.Debug.IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_XBOX,

        /// <summary>
        /// Windows boot application
        /// </summary>
        IMAGE_SUBSYSTEM_WINDOWS_BOOT_APPLICATION = Windows.Win32.System.Diagnostics.Debug.IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_WINDOWS_BOOT_APPLICATION,
    }
}
