namespace PSADT.Invoke.LibraryInterfaces
{
    /// <summary>
    /// Processor architecture types supported by Windows.
    /// </summary>
    internal enum ProcessorArchitecture : ushort
    {
        PROCESSOR_ARCHITECTURE_INTEL = 0,
        PROCESSOR_ARCHITECTURE_MIPS = 1,
        PROCESSOR_ARCHITECTURE_ALPHA = 2,
        PROCESSOR_ARCHITECTURE_PPC = 3,
        PROCESSOR_ARCHITECTURE_SHX = 4,
        PROCESSOR_ARCHITECTURE_ARM = 5,
        PROCESSOR_ARCHITECTURE_IA64 = 6,
        PROCESSOR_ARCHITECTURE_ALPHA64 = 7,
        PROCESSOR_ARCHITECTURE_MSIL = 8,
        PROCESSOR_ARCHITECTURE_AMD64 = 9,
        PROCESSOR_ARCHITECTURE_IA32_ON_WIN64 = 10,
        PROCESSOR_ARCHITECTURE_NEUTRAL = 11,
        PROCESSOR_ARCHITECTURE_ARM64 = 12,
        PROCESSOR_ARCHITECTURE_ARM32_ON_WIN64 = 13,
        PROCESSOR_ARCHITECTURE_UNKNOWN = 0xFFFF
    }

    /// <summary>
    /// Process information classes for querying and setting process information.
    /// </summary>
    internal enum PROCESSINFOCLASS : int
    {
        /// <summary>
        /// Retrieves the process basic information.
        /// </summary>
        ProcessBasicInformation = 0,
    }
}
