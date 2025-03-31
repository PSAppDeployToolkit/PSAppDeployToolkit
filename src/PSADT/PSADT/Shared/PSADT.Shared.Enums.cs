using PSADT.LibraryInterfaces;

namespace PSADT.Shared
{
    /// <summary>
    /// Windows PE architecture values. This is a public clone of IMAGE_FILE_MACHINE from CsWin32.
    /// </summary>
    public enum SystemArchitecture : ushort
    {
        /// <summary>
        /// Unknown
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Interacts with the host and not a WOW64 guest. <note>This constant is available starting with Windows 10, version 1607 and
        /// Windows Server 2016.</note>
        /// </summary>
        TargetHost = 0x0001,

        /// <summary>
        /// Intel 386
        /// </summary>
        i386 = 0x014c,

        /// <summary>
        /// MIPS little-endian, 0x160 big-endian
        /// </summary>
        R3000 = 0x0162,

        /// <summary>
        /// MIPS little-endian
        /// </summary>
        R4000 = 0x0166,

        /// <summary>
        /// MIPS little-endian
        /// </summary>
        R10000 = 0x0168,

        /// <summary>
        /// MIPS little-endian WCE v2
        /// </summary>
        WCEMIPSV2 = 0x0169,

        /// <summary>
        /// Alpha_AXP
        /// </summary>
        Alpha = 0x0184,

        /// <summary>
        /// SH3 little-endian
        /// </summary>
        SH3 = 0x01a2,

        /// <summary>
        /// SH3DSP
        /// </summary>
        SH3DSP = 0x01a3,

        /// <summary>
        /// SH3E little-endian
        /// </summary>
        SH3E = 0x01a4,

        /// <summary>
        /// SH4 little-endian
        /// </summary>
        SH4 = 0x01a6,

        /// <summary>
        /// SH5
        /// </summary>
        SH5 = 0x01a8,

        /// <summary>
        /// ARM Little-Endian
        /// </summary>
        ARM = 0x01c0,

        /// <summary>
        /// ARM Thumb/Thumb-2 Little-Endian
        /// </summary>
        THUMB = 0x01c2,

        /// <summary>
        /// ARM Thumb-2 Little-Endian <note>This constant is available starting with Windows 7 and Windows Server 2008 R2.</note>
        /// </summary>
        ARMNT = 0x01c4,

        /// <summary>
        /// TAM33BD
        /// </summary>
        AM33 = 0x01d3,

        /// <summary>
        /// IBM PowerPC Little-Endian
        /// </summary>
        PowerPC = 0x01F0,

        /// <summary>
        /// POWERPCFP
        /// </summary>
        PowerPCFP = 0x01f1,

        /// <summary>
        /// Intel 64
        /// </summary>
        IA64 = 0x0200,

        /// <summary>
        /// MIPS
        /// </summary>
        MIPS16 = 0x0266,

        /// <summary>
        /// ALPHA64
        /// </summary>
        Alpha64 = 0x0284,

        /// <summary>
        /// MIPS
        /// </summary>
        MIPSFPU = 0x0366,

        /// <summary>
        /// MIPS
        /// </summary>
        MIPSFPU16 = 0x0466,

        /// <summary>
        /// AXP64
        /// </summary>
        AXP64 = 0x0284,

        /// <summary>
        /// Infineon
        /// </summary>
        TriCore = 0x0520,

        /// <summary>
        /// CEF
        /// </summary>
        CEF = 0x0CEF,

        /// <summary>
        /// EFI Byte Code
        /// </summary>
        EBC = 0x0EBC,

        /// <summary>
        /// AMD64 (K8)
        /// </summary>
        AMD64 = 0x8664,

        /// <summary>
        /// M32R little-endian
        /// </summary>
        M32R = 0x9041,

        /// <summary>
        /// ARM64 Little-Endian <note>This constant is available starting with Windows 8.1 and Windows Server 2012 R2.</note>
        /// </summary>
        ARM64 = 0xAA64,

        /// <summary>
        /// CEE
        /// </summary>
        CEE = 0xC0EE,
    }

    /// <summary>
    /// The subsystem of the executable.
    /// </summary>
    public enum ExecutableType : ushort
    {
        /// <summary>
        /// Unknown
        /// </summary>
        Unknown = IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_UNKNOWN,

        /// <summary>
        /// Native
        /// </summary>
        Native = IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_NATIVE,

        /// <summary>
        /// Windows GUI
        /// </summary>
        GUI = IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_WINDOWS_GUI,

        /// <summary>
        /// Windows CUI
        /// </summary>
        Console = IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_WINDOWS_CUI,

        /// <summary>
        /// OS/2 CUI
        /// </summary>
        OS2 = IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_OS2_CUI,

        /// <summary>
        /// POSIX CUI
        /// </summary>
        POSIX = IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_POSIX_CUI,

        /// <summary>
        /// Windows CE GUI
        /// </summary>
        WindowsCE = IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_WINDOWS_CE_GUI,

        /// <summary>
        /// EFI Application
        /// </summary>
        EFIApplication = IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_EFI_APPLICATION,

        /// <summary>
        /// EFI Boot Service Driver
        /// </summary>
        EFIBootServiceDriver = IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_EFI_BOOT_SERVICE_DRIVER,

        /// <summary>
        /// EFI Runtime Driver
        /// </summary>
        EFIRuntimeDriver = IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_EFI_RUNTIME_DRIVER,

        /// <summary>
        /// EFI ROM
        /// </summary>
        EFIROM = IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_EFI_ROM,

        /// <summary>
        /// Xbox
        /// </summary>
        Xbox = IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_XBOX,

        /// <summary>
        /// Windows boot application
        /// </summary>
        WindowsBootApplication = IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_WINDOWS_BOOT_APPLICATION,
    }

    /// <summary>
    /// Valid value types for ValueTypeConverter.
    /// </summary>
    public enum ValueTypes
    {
        /// <summary>
        /// A signed byte.
        /// </summary>
	    SByte,

        /// <summary>
        /// An unsigned byte.
        /// </summary>
	    Byte,

        /// <summary>
        /// A signed 16-bit integer.
        /// </summary>
	    Short,

        /// <summary>
        /// An unsigned 16-bit integer.
        /// </summary>
	    Int16,

        /// <summary>
        /// A signed 32-bit integer.
        /// </summary>
	    UShort,

        /// <summary>
        /// An unsigned 32-bit integer.
        /// </summary>
	    UInt16,

        /// <summary>
        /// A signed 32-bit integer.
        /// </summary>
	    Int,

        /// <summary>
        /// An unsigned 32-bit integer.
        /// </summary>
	    Int32,

        /// <summary>
        /// A signed 64-bit integer.
        /// </summary>
	    UInt,

        /// <summary>
        /// An unsigned 64-bit integer.
        /// </summary>
	    UInt32,

        /// <summary>
        /// A signed 64-bit integer.
        /// </summary>
	    Long,

        /// <summary>
        /// An unsigned 64-bit integer.
        /// </summary>
	    Int64,

        /// <summary>
        /// A signed 64-bit integer.
        /// </summary>
	    ULong,

        /// <summary>
        /// An unsigned 64-bit integer.
        /// </summary>
	    UInt64,
    }
}
