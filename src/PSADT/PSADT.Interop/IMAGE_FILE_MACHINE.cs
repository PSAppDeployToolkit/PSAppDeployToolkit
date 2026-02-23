namespace PSADT.Interop
{
    /// <summary>
    /// The architecture of the executable.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1028:Enum Storage should be Int32", Justification = "The type is correct for the underlying Win32 API.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1069:Enums values should not be duplicated", Justification = "These values are precisely as they're defined in the Win32 API.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "These values are precisely as they're defined in the Win32 API.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1712:Do not prefix enum values with type name", Justification = "These values are precisely as they're defined in the Win32 API.")]
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
}
