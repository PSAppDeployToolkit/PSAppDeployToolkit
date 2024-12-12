using System.ComponentModel;

namespace PSADT.OperatingSystem
{
    /// <summary>
    /// Windows operating systems in order of OS family and then chronological release date starting with Windows 2000.
    /// Note: There were a few XP family releases that came out after Windows Vista was released. However, the XP family releases are logically grouped together.
    /// </summary>
    public enum WindowsOS
	{
		[Description("Unknown Windows Edition")]
		Unknown,

		[Description("Windows 2000")]
		Windows2000 = 1000,                     // December 15, 1999

		[Description("Windows XP")]
		WindowsXP = 2000,                       // August 24, 2001

		[Description("Windows XP SP1")]
		WindowsXPSP1,                           // September 9, 2002

		[Description("Windows Server 2003")]
		WindowsServer2003,                      // April 24, 2003. Derived from Windows XP.

		[Description("Windows XP SP2")]
		WindowsXPSP2,                           // August 25, 2004

		[Description("Windows Server 2003 SP1")]
		WindowsServer2003SP1,                   // March 30, 2005. Derived from Windows XP SP2.

		[Description("Windows XP Professional x64 Edition")]
		WindowsXPProfessionalx64Edition,        // April 25, 2005. Shares code base with Windows Server 2003 SP1.

		[Description("Windows Server 2003 R2")]
		WindowsServer2003R2,                    // December 6, 2005. Matches Windows Server 2003 SP1 as it was essentially 2003 SP1 with some additional optional features.

		[Description("Windows Server 2003 SP2")]
		WindowsServer2003SP2,                   // March 13, 2007

		[Description("Windows XP Professional x64 Edition SP2")]
		WindowsXPProfessionalx64EditionSP2,     // March 13, 2007. Shares code base with Windows Server 2003 SP2.

		[Description("Windows XP SP3")]
		WindowsXPSP3,                           // April 21, 2008

		[Description("Windows Vista")]
		WindowsVista = 3000,                    // November 8, 2006

		[Description("Windows Vista SP1")]
		WindowsVistaSP1,                        // February 4, 2008

		[Description("Windows Server 2008")]
		WindowsServer2008,                      // February 27, 2008. Shares code base with Vista SP1.

		[Description("Windows Vista SP2")]
		WindowsVistaSP2,                        // April 28, 2009

		[Description("Windows Server 2008 SP2")]
		WindowsServer2008SP2,                   // May 26, 2009. Shares code base with Windows Vista SP2.

		[Description("Windows 7")]
		Windows7 = 4000,                        // July 22, 2009

		[Description("Windows Server 2008 R2")]
		WindowsServer2008R2,                    // October 22, 2009. Shares code base with Windows 7.

		[Description("Windows 7 SP1")]
		Windows7SP1,                            // February 9, 2011

		[Description("Windows Server 2008 R2 SP1")]
		WindowsServer2008R2SP1,                 // February 9, 2011. Shares code base with Windows 7 SP1.

		[Description("Windows 8")]
		Windows8 = 5000,                        // August 1, 2012

		[Description("Windows Server 2012")]
		WindowsServer2012,                      // September 4, 2012. Shares code base with Windows 8.

		[Description("Windows 8.1")]
		Windows8Point1,                         // August 27, 2013

		[Description("Windows Server 2012 R2")]
		WindowsServer2012R2,                    // October 17, 2013. Shares code base with Windows 8.1.

		[Description("Windows 10 1507 (10240)")]
		Windows10Version1507 = 6000,            // July 15, 2015

		[Description("Windows 10 1511 (10586) (November Update)")]
		Windows10Version1511,                   // November 10, 2015

		[Description("Windows 10 1607 (14393) (Anniversary Update)")]
		Windows10Version1607,                   // August 2, 2016

		[Description("Windows Server 2016")]
		WindowsServer2016,                      // October 12, 2016. Shares code base with Windows 10 Version 1607.

		[Description("Windows 10 1703 (15063) (Creators Update)")]
		Windows10Version1703,                   // April 15, 2017

		[Description("Windows 10 1709 (16299) (Fall Creators Update)")]
		Windows10Version1709,                   // October 17, 2017

		[Description("Windows Server, version 1709")]
		WindowsServerVersion1709,               // October 17, 2017. Shares code base with Windows 10 Version 1709.

		[Description("Windows 10 1803 (17134) (April 2018 Update)")]
		Windows10Version1803,                   // April 30, 2018

		[Description("Windows Server, version 1803")]
		WindowsServerVersion1803,               // April 30, 2018. Shares code base with Windows 10 Version 1803.

		[Description("Windows 10 1809 (17763) (October 2018 Update)")]
		Windows10Version1809,                   // November 13, 2018

		[Description("Windows Server 2019")]
		WindowsServer2019,                      // November 13, 2018. Shares code base with Windows 10 Version 1809.

		[Description("Windows 10 1903 (18362) (19H1)")]
		Windows10Version1903,                   // May 21, 2019

		[Description("Windows Server, version 1903")]
		WindowsServerVersion1903,               // May 21, 2019. Shares code base with Windows 10 Version 1903.

		[Description("Windows 10 1909 (18363) (19H2)")]
		Windows10Version1909,                   // November 12, 2019

		[Description("Windows Server, version 1909")]
		WindowsServerVersion1909,               // November 12, 2019. Shares code base with Windows 10 Version 1909.

		[Description("Windows 10 2004 (19041) (20H1)")]
		Windows10Version2004,                   // May 27, 2020

		[Description("Windows Server, version 2004")]
		WindowsServerVersion2004,               // May 27, 2020. Shares code base with Windows 10 Version 2004

		[Description("Windows 10 20H2 (19042)")]
		Windows10Version20H2,                   // October 20, 2020

		[Description("Windows Server, version 20H2")]
		WindowsServerVersion20H2,               // October 20, 2020. Shares code base with Windows 10 Version 20H2.

		[Description("Windows 10 21H1 (19043)")]
		Windows10Version21H1,                   // May 18, 2021

		[Description("Windows Server 2022")]
		WindowsServer2022,                      // August 18, 2021. Shares code base with Windows 10 Version 21H2.

		[Description("Windows 10 21H2 (19044)")]
		Windows10Version21H2,                   // November 16, 2021

		[Description("Windows 10 22H2 (19045)")]
		Windows10Version22H2,                   // October 18, 2022

		[Description("Windows 10 New Unknown Version")]
		Windows10NewVersionUnknown = 6990,

		[Description("Windows 11 21H2 (22000)")]
		Windows11Version21H2 = 7000,            // October 05, 2021

		[Description("Windows 11 22H2 (22621)")]
		Windows11Version22H2,                   // September 20, 2022

		[Description("Windows 11 23H2 (22632)")]
		Windows11Version23H2,                   // October 31, 2023

		[Description("Windows Server, version 23H2")]
		WindowsServerVersion23H2,               // October 14, 2023. Shares code base with Windows 11 Version 23H2.

		[Description("Windows 11 24H2 (26100)")]
		Windows11Version24H2,                   // June 15, 2024

		[Description("Windows 11 New Unknown Version")]
		Windows11NewVersionUnknown = 7990,

		[Description("Windows Server New Unknown Version")]
		WindowsServerNewVersionUnknown = 9991
	}
}
