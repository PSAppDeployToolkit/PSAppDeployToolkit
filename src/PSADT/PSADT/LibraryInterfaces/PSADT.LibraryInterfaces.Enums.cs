namespace PSADT.LibraryInterfaces
{
    /// <summary>
    /// The architecture of the executable.
    /// </summary>
    public enum IMAGE_FILE_MACHINE : ushort
    {
        /// <summary>
        /// Alpha
        /// </summary>
        IMAGE_FILE_MACHINE_AXP64 = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_AXP64,

        /// <summary>
        /// x86
        /// </summary>
        IMAGE_FILE_MACHINE_I386 = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_I386,

        /// <summary>
        /// Itanium
        /// </summary>
        IMAGE_FILE_MACHINE_IA64 = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_IA64,

        /// <summary>
        /// AMD64
        /// </summary>
        IMAGE_FILE_MACHINE_AMD64 = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_AMD64,

        /// <summary>
        /// Unknown
        /// </summary>
        IMAGE_FILE_MACHINE_UNKNOWN = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_UNKNOWN,

        /// <summary>
        /// Target host
        /// </summary>
        IMAGE_FILE_MACHINE_TARGET_HOST = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_TARGET_HOST,

        /// <summary>
        /// R3000
        /// </summary>
        IMAGE_FILE_MACHINE_R3000 = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_R3000,

        /// <summary>
        /// R4000
        /// </summary>
        IMAGE_FILE_MACHINE_R4000 = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_R4000,

        /// <summary>
        /// R10000
        /// </summary>
        IMAGE_FILE_MACHINE_R10000 = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_R10000,

        /// <summary>
        /// Windows CE MIPS
        /// </summary>
        IMAGE_FILE_MACHINE_WCEMIPSV2 = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_WCEMIPSV2,

        /// <summary>
        /// Alpha
        /// </summary>
        IMAGE_FILE_MACHINE_ALPHA = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_ALPHA,

        /// <summary>
        /// SH3
        /// </summary>
        IMAGE_FILE_MACHINE_SH3 = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_SH3,

        /// <summary>
        /// SH3DSP
        /// </summary>
        IMAGE_FILE_MACHINE_SH3DSP = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_SH3DSP,

        /// <summary>
        /// SH3E
        /// </summary>
        IMAGE_FILE_MACHINE_SH3E = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_SH3E,

        /// <summary>
        /// SH4
        /// </summary>
        IMAGE_FILE_MACHINE_SH4 = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_SH4,

        /// <summary>
        /// SH5
        /// </summary>
        IMAGE_FILE_MACHINE_SH5 = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_SH5,

        /// <summary>
        /// ARM
        /// </summary>
        IMAGE_FILE_MACHINE_ARM = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_ARM,

        /// <summary>
        /// Thumb
        /// </summary>
        IMAGE_FILE_MACHINE_THUMB = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_THUMB,

        /// <summary>
        /// ARMNT
        /// </summary>
        IMAGE_FILE_MACHINE_ARMNT = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_ARMNT,

        /// <summary>
        /// AM33
        /// </summary>
        IMAGE_FILE_MACHINE_AM33 = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_AM33,

        /// <summary>
        /// PowerPC
        /// </summary>
        IMAGE_FILE_MACHINE_POWERPC = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_POWERPC,

        /// <summary>
        /// PowerPCFP
        /// </summary>
        IMAGE_FILE_MACHINE_POWERPCFP = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_POWERPCFP,

        /// <summary>
        /// MIPS16
        /// </summary>
        IMAGE_FILE_MACHINE_MIPS16 = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_MIPS16,

        /// <summary>
        /// Alpha64
        /// </summary>
        IMAGE_FILE_MACHINE_ALPHA64 = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_ALPHA64,

        /// <summary>
        /// MIPSFPU
        /// </summary>
        IMAGE_FILE_MACHINE_MIPSFPU = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_MIPSFPU,

        /// <summary>
        /// MIPSFPU16
        /// </summary>
        IMAGE_FILE_MACHINE_MIPSFPU16 = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_MIPSFPU16,

        /// <summary>
        /// Tricore
        /// </summary>
        IMAGE_FILE_MACHINE_TRICORE = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_TRICORE,

        /// <summary>
        /// CEF
        /// </summary>
        IMAGE_FILE_MACHINE_CEF = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_CEF,

        /// <summary>
        /// EBC
        /// </summary>
        IMAGE_FILE_MACHINE_EBC = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_EBC,

        /// <summary>
        /// M32R
        /// </summary>
        IMAGE_FILE_MACHINE_M32R = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_M32R,

        /// <summary>
        /// ARM64
        /// </summary>
        IMAGE_FILE_MACHINE_ARM64 = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_ARM64,

        /// <summary>
        /// CEE
        /// </summary>
        IMAGE_FILE_MACHINE_CEE = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_CEE,
    }

    /// <summary>
    /// The subsystem of the executable.
    /// </summary>
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
