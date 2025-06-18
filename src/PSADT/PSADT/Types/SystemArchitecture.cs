using PSADT.LibraryInterfaces;

namespace PSADT.Types
{
    /// <summary>
    /// Windows PE architecture values. This is a public clone of IMAGE_FILE_MACHINE from CsWin32.
    /// </summary>
    public enum SystemArchitecture : ushort
    {
        /// <summary>
        /// Unknown
        /// </summary>
        Unknown = IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_UNKNOWN,

        /// <summary>
        /// Interacts with the host and not a WOW64 guest. <note>This constant is available starting with Windows 10, version 1607 and
        /// Windows Server 2016.</note>
        /// </summary>
        TargetHost = IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_TARGET_HOST,

        /// <summary>
        /// Intel 386
        /// </summary>
        i386 = IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_I386,

        /// <summary>
        /// MIPS little-endian, 0x160 big-endian
        /// </summary>
        R3000 = IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_R3000,

        /// <summary>
        /// MIPS little-endian
        /// </summary>
        R4000 = IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_R4000,

        /// <summary>
        /// MIPS little-endian
        /// </summary>
        R10000 = IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_R10000,

        /// <summary>
        /// MIPS little-endian WCE v2
        /// </summary>
        WCEMIPSV2 = IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_WCEMIPSV2,

        /// <summary>
        /// Alpha_AXP
        /// </summary>
        Alpha = IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_ALPHA,

        /// <summary>
        /// SH3 little-endian
        /// </summary>
        SH3 = IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_SH3,

        /// <summary>
        /// SH3DSP
        /// </summary>
        SH3DSP = IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_SH3DSP,

        /// <summary>
        /// SH3E little-endian
        /// </summary>
        SH3E = IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_SH3E,

        /// <summary>
        /// SH4 little-endian
        /// </summary>
        SH4 = IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_SH4,

        /// <summary>
        /// SH5
        /// </summary>
        SH5 = IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_SH5,

        /// <summary>
        /// ARM Little-Endian
        /// </summary>
        ARM = IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_ARM,

        /// <summary>
        /// ARM Thumb/Thumb-2 Little-Endian
        /// </summary>
        THUMB = IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_THUMB,

        /// <summary>
        /// ARM Thumb-2 Little-Endian <note>This constant is available starting with Windows 7 and Windows Server 2008 R2.</note>
        /// </summary>
        ARMNT = IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_ARMNT,

        /// <summary>
        /// TAM33BD
        /// </summary>
        AM33 = IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_AM33,

        /// <summary>
        /// IBM PowerPC Little-Endian
        /// </summary>
        PowerPC = IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_POWERPC,

        /// <summary>
        /// POWERPCFP
        /// </summary>
        PowerPCFP = IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_POWERPCFP,

        /// <summary>
        /// Intel 64
        /// </summary>
        IA64 = IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_IA64,

        /// <summary>
        /// MIPS
        /// </summary>
        MIPS16 = IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_MIPS16,

        /// <summary>
        /// ALPHA64
        /// </summary>
        Alpha64 = IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_ALPHA64,

        /// <summary>
        /// MIPS
        /// </summary>
        MIPSFPU = IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_MIPSFPU,

        /// <summary>
        /// MIPS
        /// </summary>
        MIPSFPU16 = IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_MIPSFPU16,

        /// <summary>
        /// AXP64
        /// </summary>
        AXP64 = IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_AXP64,

        /// <summary>
        /// Infineon
        /// </summary>
        TriCore = IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_TRICORE,

        /// <summary>
        /// CEF
        /// </summary>
        CEF = IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_CEF,

        /// <summary>
        /// EFI Byte Code
        /// </summary>
        EBC = IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_EBC,

        /// <summary>
        /// AMD64 (K8)
        /// </summary>
        AMD64 = IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_AMD64,

        /// <summary>
        /// M32R little-endian
        /// </summary>
        M32R = IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_M32R,

        /// <summary>
        /// ARM64 Little-Endian <note>This constant is available starting with Windows 8.1 and Windows Server 2012 R2.</note>
        /// </summary>
        ARM64 = IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_ARM64,

        /// <summary>
        /// CEE
        /// </summary>
        CEE = IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_CEE,
    }
}
