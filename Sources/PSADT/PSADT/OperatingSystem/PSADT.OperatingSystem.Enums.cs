namespace PSADT.OperatingSystem
{
    /// <summary>
    /// Windows operating systems in order of OS family and then chronological release date starting with Windows 2000.
    /// Note: There were a few XP family releases that came out after Windows Vista was released. However, the XP family releases are logically grouped together.
    /// </summary>
    public enum WindowsOS
	{
		Unknown,
		Windows2000 = 1000,                     // December 15, 1999
		WindowsXP = 2000,                       // August 24, 2001
		WindowsXPSP1,                           // September 9, 2002
		WindowsServer2003,                      // April 24, 2003. Derived from Windows XP.
		WindowsXPSP2,                           // August 25, 2004
		WindowsServer2003SP1,                   // March 30, 2005. Derived from Windows XP SP2.
		WindowsXPProfessionalx64Edition,        // April 25, 2005. Shares code base with Windows Server 2003 SP1.
		WindowsServer2003R2,                    // December 6, 2005. Matches Windows Server 2003 SP1 as it was essentially 2003 SP1 with some additional optional features.
		WindowsServer2003SP2,                   // March 13, 2007
		WindowsXPProfessionalx64EditionSP2,     // March 13, 2007. Shares code base with Windows Server 2003 SP2.
		WindowsXPSP3,                           // April 21, 2008
		WindowsVista = 3000,                    // November 8, 2006
		WindowsVistaSP1,                        // February 4, 2008
		WindowsServer2008,                      // February 27, 2008. Shares code base with Vista SP1.
		WindowsVistaSP2,                        // April 28, 2009
		WindowsServer2008SP2,                   // May 26, 2009. Shares code base with Windows Vista SP2.
		Windows7 = 4000,                        // July 22, 2009
		WindowsServer2008R2,                    // October 22, 2009. Shares code base with Windows 7.
		Windows7SP1,                            // February 9, 2011
		WindowsServer2008R2SP1,                 // February 9, 2011. Shares code base with Windows 7 SP1.
		Windows8 = 5000,                        // August 1, 2012
		WindowsServer2012,                      // September 4, 2012. Shares code base with Windows 8.
		Windows8Point1,                         // August 27, 2013
		WindowsServer2012R2,                    // October 17, 2013. Shares code base with Windows 8.1.
		Windows10Version1507 = 6000,            // July 15, 2015
		Windows10Version1511,                   // November 10, 2015
		Windows10Version1607,                   // August 2, 2016
		WindowsServer2016,                      // October 12, 2016. Shares code base with Windows 10 Version 1607.
		Windows10Version1703,                   // April 15, 2017
		Windows10Version1709,                   // October 17, 2017
		Windows10Version1803,                   // April 30, 2018
		Windows10Version1809,                   // November 13, 2018
		WindowsServer2019,                      // November 13, 2018. Shares code base with Windows 10 Version 1809.
		Windows10Version1903,                   // May 21, 2019
		WindowsServerVersion1903,               // May 21, 2019. Shares code base with Windows 10 Version 1903.
		Windows10Version1909,                   // November 12, 2019
		WindowsServerVersion1909,               // November 12, 2019. Shares code base with Windows 10 Version 1909.
		Windows10Version2004,                   // May 27, 2020
		WindowsServerVersion2004,               // May 27, 2020. Shares code base with Windows 10 Version 2004
		Windows10Version20H2,                   // October 20, 2020
		WindowsServerVersion20H2,               // October 20, 2020. Shares code base with Windows 10 Version 20H2.
		Windows10Version21H1,                   // May 18, 2021
		WindowsServer2022,                      // August 18, 2021. Shares code base with Windows 10 Version 21H2.
		Windows10Version21H2,                   // November 16, 2021
		Windows10Version22H2,                   // October 18, 2022
		Windows10NewVersionUnknown = 6990,
		Windows11Version21H2 = 7000,            // October 05, 2021
		Windows11Version22H2,                   // September 20, 2022
		Windows11Version23H2,                   // October 31, 2023
		WindowsServerVersion23H2,               // October 14, 2023. Shares code base with Windows 11 Version 23H2.
		Windows11Version24H2,                   // June 15, 2024
		Windows11NewVersionUnknown = 7990,
		WindowsServerNewVersionUnknown = 9991
	}

    /// <summary>
    /// Indicates the processor architecture.
    /// </summary>
    public enum OSArchitecture
    {
        /// <summary>
        /// An Intel-based 32-bit processor architecture.
        /// </summary>
        X86,
        /// <summary>
        /// An Intel-based 64-bit processor architecture.
        /// </summary>
        X64,
        /// <summary>
        /// A 32-bit ARM processor architecture.
        /// </summary>
        /// <remarks>
        /// This value indicates ARMv7 base instructions, VFPv3 floating point support and registers, and Thumb2 compact instruction set.
        /// </remarks>
        Arm,
        /// <summary>
        /// A 64-bit ARM processor architecture.
        /// </summary>
        Arm64,
        /// <summary>
        /// The WebAssembly platform.
        /// </summary>
        Wasm,
        /// <summary>
        /// A S390x platform architecture.
        /// </summary>
        S390x,
        /// <summary>
        /// A LoongArch64 processor architecture.
        /// </summary>
        LoongArch64,
        /// <summary>
        /// A 32-bit ARMv6 processor architecture.
        /// </summary>
        /// <remarks>
        /// This value indicates ARMv6 base instructions, VFPv2 floating point support and registers, hard-float ABI, and no compact instruction set.
        /// </remarks>
        Armv6,
        /// <summary>
        /// A PowerPC 64-bit (little-endian) processor architecture.
        /// </summary>
        Ppc64le,
        /// <summary>
        /// A RiscV 64-bit processor architecture.
        /// </summary>
        /// <remarks>
        /// This value indicates RV64GC set of extensions.
        /// </remarks>
        RiscV64,
    }
}
