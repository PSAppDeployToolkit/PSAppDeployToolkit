using System;
using System.ComponentModel;
using Windows.Win32;

namespace PSADT.OperatingSystem
{
    /// <summary>
    /// Windows operating systems in order of OS family and then chronological release date starting with Windows 2000.
    /// Note: There were a few XP family releases that came out after Windows Vista was released. However, the XP family releases are logically grouped together.
    /// </summary>
    public enum WindowsOS : ulong
    {
        /// <summary>
        /// Windows 2000, relased December 15, 1999.
        /// </summary>
        [Description("Windows 2000 {0}")]
        Windows2000RTM = (5UL << 48) | (0UL << 32) | (2195UL << 16) | (1UL << 0),

        /// <summary>
        /// Windows 2000 SP1, released July 31, 2000.
        /// </summary>
        [Description("Windows 2000 {0} SP1")]
        Windows2000SP1 = (5UL << 48) | (0UL << 32) | (2195UL << 16) | (1620UL << 0),

        /// <summary>
        /// Windows 2000 SP2, released May 16, 2001.
        /// </summary>
        [Description("Windows 2000 {0} SP2")]
        Windows2000SP2 = (5UL << 48) | (0UL << 32) | (2195UL << 16) | (2951UL << 0),

        /// <summary>
        /// Windows 2000 SP3, released August 29, 2002.
        /// </summary>
        [Description("Windows 2000 {0} SP3")]
        Windows2000SP3 = (5UL << 48) | (0UL << 32) | (2195UL << 16) | (5438UL << 0),

        /// <summary>
        /// Windows 2000 SP4, released June 26, 2003.
        /// </summary>
        [Description("Windows 2000 {0} SP4")]
        Windows2000SP4 = (5UL << 48) | (0UL << 32) | (2195UL << 16) | (6717UL << 0),

        /// <summary>
        /// Windows 2000 SP4 with Update Rollup 1, released June 28, 2005.
        /// </summary>
        [Description("Windows 2000 {0} SP4 with Update Rollup 1")]
        Windows2000SP4UR1 = (5UL << 48) | (0UL << 32) | (2195UL << 16) | (7045UL << 0),

        /// <summary>
        /// Windows XP, released August 24, 2001.
        /// </summary>
        [Description("Windows XP {0}")]
        WindowsXPRTM = (5UL << 48) | (1UL << 32) | (2600UL << 16) | (0UL << 0),

        /// <summary>
        /// Windows XP SP1, released September 9, 2002.
        /// </summary>
        [Description("Windows XP {0} SP1")]
        WindowsXPSP1 = (5UL << 48) | (1UL << 32) | (2600UL << 16) | (1106UL << 0),

        /// <summary>
        /// Windows XP SP2, released August 25, 2004.
        /// </summary>
        [Description("Windows XP {0} SP2")]
        WindowsXPSP2 = (5UL << 48) | (1UL << 32) | (2600UL << 16) | (2180UL << 0),

        /// <summary>
        /// Windows XP SP3, released April 21, 2008.
        /// </summary>
        [Description("Windows XP {0} SP3")]
        WindowsXPSP3 = (5UL << 48) | (1UL << 32) | (2600UL << 16) | (5512UL << 0),

        /// <summary>
        /// Windows Server 2003, released April 24, 2003.
        /// </summary>
        [Description("Windows Server 2003 {0}")]
        WindowsServer2003RTM = (5UL << 48) | (2UL << 32) | (3790UL << 16) | (0UL << 0),

        /// <summary>
        /// Windows Server 2003 SP1, released March 30, 2005.
        /// </summary>
        [Description("Windows Server 2003 {0} SP1")]
        WindowsServer2003SP1 = (5UL << 48) | (2UL << 32) | (3790UL << 16) | (1830UL << 0),

        /// <summary>
        /// Windows Server 2003 R2, released December 6, 2005.
        /// </summary>
        [Description("Windows Server 2003 R2 {0}")]
        WindowsServer2003R2 = (5UL << 48) | (2UL << 32) | (3790UL << 16) | (2075UL << 0),

        /// <summary>
        /// Windows Server 2003 R2 SP2, released March 13, 2007.
        /// </summary>
        [Description("Windows Server 2003 {0} SP2")]
        WindowsServer2003SP2 = (5UL << 48) | (2UL << 32) | (3790UL << 16) | (3959UL << 0),

        /// <summary>
        /// Windows XP x64 Edition, released April 25, 2005.
        /// </summary>
        [Description("Windows XP {0} x64 Edition")]
        WindowsXPProfessionalx64EditionRTM = WindowsServer2003SP1,

        /// <summary>
        /// Windows XP x64 Edition SP2, released March 13, 2007.
        /// </summary>
        [Description("Windows XP {0} x64 Edition SP2")]
        WindowsXPProfessionalx64EditionSP2 = WindowsServer2003SP2,

        /// <summary>
        /// Windows Vista, released November 8, 2006.
        /// </summary>
        [Description("Windows Vista {0}")]
        WindowsVistaRTM = (6UL << 48) | (0UL << 32) | (6000UL << 16) | (16386UL << 0),

        /// <summary>
        /// Windows Vista SP1, released February 4, 2008.
        /// </summary>
        [Description("Windows Vista {0} SP1")]
        WindowsVistaSP1 = (6UL << 48) | (0UL << 32) | (6001UL << 16) | (18000UL << 0),

        /// <summary>
        /// Windows Vista SP2, released April 28, 2009.
        /// </summary>
        [Description("Windows Vista {0} SP2")]
        WindowsVistaSP2 = (6UL << 48) | (0UL << 32) | (6002UL << 16) | (18005UL << 0),

        /// <summary>
        /// Windows Vista SP2 with Lifecycle Servicing Update, released March 20, 2019.
        /// </summary>
        [Description("Windows Vista {0} SP2 with Lifecycle Servicing Update")]
        WindowsVistaSP2LSU = (6UL << 48) | (0UL << 32) | (6003UL << 16) | (20489UL << 0),

        /// <summary>
        /// Windows Server 2008, released February 27, 2008.
        /// </summary>
        [Description("Windows Server 2008 {0}")]
        WindowsServer2008RTM = WindowsVistaSP1,

        /// <summary>
        /// Windows Server 2008 SP2, released May 26, 2009.
        /// </summary>
        [Description("Windows Server 2008 {0} SP2")]
        WindowsServer2008SP2 = WindowsVistaSP2,

        /// <summary>
        /// Windows Server 2008 SP2 with Lifecycle Servicing Update, released March 20, 2019.
        /// </summary>
        [Description("Windows Server 2008 {0} SP2 with Lifecycle Servicing Update")]
        WindowsServer2008SP2LSU = WindowsVistaSP2LSU,

        /// <summary>
        /// Windows 7, released July 22, 2009.
        /// </summary>
        [Description("Windows 7 {0}")]
        Windows7RTM = (6UL << 48) | (1UL << 32) | (7600UL << 16) | (16385UL << 0),

        /// <summary>
        /// Windows 7 SP1, released February 9, 2011.
        /// </summary>
        [Description("Windows 7 {0} SP1")]
        Windows7SP1 = (6UL << 48) | (1UL << 32) | (7601UL << 16) | (17514UL << 0),

        /// <summary>
        /// Windows Server 2008 R2, released October 22, 2009.
        /// </summary>
        [Description("Windows Server 2008 R2 {0}")]
        WindowsServer2008R2RTM = Windows7RTM,

        /// <summary>
        /// Windows Server 2008 R2 SP1, released February 9, 2011.
        /// </summary>
        [Description("Windows Server 2008 R2 {0} SP1")]
        WindowsServer2008R2SP1 = Windows7SP1,

        /// <summary>
        /// Windows 8, released October 26, 2012.
        /// </summary>
        [Description("Windows 8 {0}")]
        Windows8 = (6UL << 48) | (2UL << 32) | (9200UL << 16) | (16384UL << 0),

        /// <summary>
        /// Windows 8.1, released August 27, 2013.
        /// </summary>
        [Description("Windows Server 2012 {0}")]
        WindowsServer2012 = Windows8,

        /// <summary>
        /// Windows 8.1, released August 27, 2013.
        /// </summary>
        [Description("Windows 8.1 {0}")]
        Windows81RTM = (6UL << 48) | (3UL << 32) | (9600UL << 16) | (16384UL << 0),

        /// <summary>
        /// Windows 8.1 with Update, released April 8, 2014.
        /// </summary>
        [Description("Windows 8.1 {0} with IR2 Update")]
        Windows81IRL = (6UL << 48) | (3UL << 32) | (9600UL << 16) | (16422UL << 0),

        /// <summary>
        /// Windows 8.1 with Update 1, released April 2, 2014.
        /// </summary>
        [Description("Windows 8.1 {0} with Update 1")]
        Windows81Update1 = (6UL << 48) | (3UL << 32) | (9600UL << 16) | (17031UL << 0),

        /// <summary>
        /// Windows 8.1 with Update 2, released August 12, 2014.
        /// </summary>
        [Description("Windows 8.1 {0} with Update 2")]
        Windows81Update2 = (6UL << 48) | (3UL << 32) | (9600UL << 16) | (17238UL << 0),

        /// <summary>
        /// Windows 8.1 with Update 3, released November 17, 2014.
        /// </summary>
        [Description("Windows 8.1 {0} with Update 3")]
        Windows81Update3 = (6UL << 48) | (3UL << 32) | (9600UL << 16) | (17415UL << 0),

        /// <summary>
        /// Windows Server 2012, released September 4, 2012.
        /// </summary>
        [Description("Windows Server 2012 R2 {0}")]
        WindowsServer2012R2RTM = Windows81RTM,

        /// <summary>
        /// Windows Server 2012 R2 with Update 1, released April 2, 2014.
        /// </summary>
        [Description("Windows Server 2012 R2 {0} with Update 1")]
        WindowsServer2012R2Update1 = Windows81Update1,

        /// <summary>
        /// Windows Server 2012 R2 with Update 2, released August 12, 2014.
        /// </summary>
        [Description("Windows Server 2012 R2 {0} with Update 2")]
        WindowsServer2012R2Update2 = Windows81Update2,

        /// <summary>
        /// Windows Server 2012 R2 with Update 3, released November 17, 2014.
        /// </summary>
        [Description("Windows Server 2012 R2 {0} with Update 3")]
        WindowsServer2012R2Update3 = Windows81Update3,

        /// <summary>
        /// Windows 10 1507, released July 29, 2015.
        /// </summary>
        [Description("Windows 10 {0} 1507 (Build 10240)")]
        Windows10Version1507 = (10UL << 48) | (0UL << 32) | (10240UL << 16) | (0UL << 0),

        /// <summary>
        /// Windows 10 1511, released November 10, 2015.
        /// </summary>
        [Description("Windows 10 {0} 1511 (Build 10586)")]
        Windows10Version1511 = (10UL << 48) | (0UL << 32) | (10586UL << 16) | (0UL << 0),

        /// <summary>
        /// Windows 10 1607, released August 2, 2016.
        /// </summary>
        [Description("Windows 10 {0} 1607 (Build 14393)")]
        Windows10Version1607 = (10UL << 48) | (0UL << 32) | (14393UL << 16) | (0UL << 0),

        /// <summary>
        /// Windows 10 1703, released April 15, 2017.
        /// </summary>
        [Description("Windows 10 {0} 1703 (Build 15063)")]
        Windows10Version1703 = (10UL << 48) | (0UL << 32) | (15063UL << 16) | (0UL << 0),

        /// <summary>
        /// Windows 10 1709, released October 17, 2017.
        /// </summary>
        [Description("Windows 10 {0} 1709 (Build 16299)")]
        Windows10Version1709 = (10UL << 48) | (0UL << 32) | (16299UL << 16) | (0UL << 0),

        /// <summary>
        /// Windows 10 1803, released April 30, 2018.
        /// </summary>
        [Description("Windows 10 {0} 1803 (Build 17134)")]
        Windows10Version1803 = (10UL << 48) | (0UL << 32) | (17134UL << 16) | (0UL << 0),

        /// <summary>
        /// Windows 10 1809, released November 13, 2018.
        /// </summary>
        [Description("Windows 10 {0} 1809 (Build 17763)")]
        Windows10Version1809 = (10UL << 48) | (0UL << 32) | (17763UL << 16) | (0UL << 0),

        /// <summary>
        /// Windows 10 1903, released May 21, 2019.
        /// </summary>
        [Description("Windows 10 {0} 1903 (Build 18362)")]
        Windows10Version1903 = (10UL << 48) | (0UL << 32) | (18362UL << 16) | (0UL << 0),

        /// <summary>
        /// Windows 10 1909, released November 12, 2019.
        /// </summary>
        [Description("Windows 10 {0} 1909 (Build 18363)")]
        Windows10Version1909 = (10UL << 48) | (0UL << 32) | (18363UL << 16) | (0UL << 0),

        /// <summary>
        /// Windows 10 2004, released May 27, 2020.
        /// </summary>
        [Description("Windows 10 {0} 2004 (Build 19041)")]
        Windows10Version2004 = (10UL << 48) | (0UL << 32) | (19041UL << 16) | (0UL << 0),

        /// <summary>
        /// Windows 10 20H2, released October 20, 2020.
        /// </summary>
        [Description("Windows 10 {0} 20H2 (Build 19042)")]
        Windows10Version20H2 = (10UL << 48) | (0UL << 32) | (19042UL << 16) | (0UL << 0),

        /// <summary>
        /// Windows 10 21H1, released May 18, 2021.
        /// </summary>
        [Description("Windows 10 {0} 21H1 (Build 19043)")]
        Windows10Version21H1 = (10UL << 48) | (0UL << 32) | (19043UL << 16) | (0UL << 0),

        /// <summary>
        /// Windows 10 21H2, released November 16, 2021.
        /// </summary>
        [Description("Windows 10 {0} 21H2 (Build 19044)")]
        Windows10Version21H2 = (10UL << 48) | (0UL << 32) | (19044UL << 16) | (0UL << 0),

        /// <summary>
        /// Windows 10 22H2, released October 18, 2022.
        /// </summary>
        [Description("Windows 10 {0} 22H2 (Build 19045)")]
        Windows10Version22H2 = (10UL << 48) | (0UL << 32) | (19045UL << 16) | (0UL << 0),

        /// <summary>
        /// Windows Server 2016, released October 12, 2016.
        /// </summary>
        [Description("Windows Server 2016 {0}")]
        WindowsServer2016 = Windows10Version1607,

        /// <summary>
        /// Windows Server, version 1709, released October 17, 2017.
        /// </summary>
        [Description("Windows Server {0}, version 1709")]
        WindowsServerVersion1709 = Windows10Version1709,

        /// <summary>
        /// Windows Server, version 1803, released April 30, 2018.
        /// </summary>
        [Description("Windows Server {0}, version 1803")]
        WindowsServerVersion1803 = Windows10Version1803,

        /// <summary>
        /// Windows Server 2019, released November 13, 2018.
        /// </summary>
        [Description("Windows Server 2019 {0}")]
        WindowsServer2019 = Windows10Version1809,

        /// <summary>
        /// Windows Server, version 1903, released May 21, 2019.
        /// </summary>
        [Description("Windows Server {0}, version 1903")]
        WindowsServerVersion1903 = Windows10Version1903,

        /// <summary>
        /// Windows Server, version 1909, released November 12, 2019.
        /// </summary>
        [Description("Windows Server {0}, version 1909")]
        WindowsServerVersion1909 = Windows10Version1909,

        /// <summary>
        /// Windows Server, version 2004, released May 27, 2020.
        /// </summary>
        [Description("Windows Server {0}, version 2004")]
        WindowsServerVersion2004 = Windows10Version2004,

        /// <summary>
        /// Windows Server, version 20H2, released October 20, 2020.
        /// </summary>
        [Description("Windows Server {0}, version 20H2")]
        WindowsServerVersion20H2 = Windows10Version20H2,

        /// <summary>
        /// Windows Server 2022, released August 18, 2021.
        /// </summary>
        [Description("Windows Server 2022 {0}")]
        WindowsServer2022 = (10UL << 48) | (0UL << 32) | (20348UL << 16) | (0UL << 0),

        /// <summary>
        /// Windows 11 21H2, released October 5, 2021.
        /// </summary>
        [Description("Windows 11 {0} 21H2 (Build 22000)")]
        Windows11Version21H2 = (10UL << 48) | (0UL << 32) | (22000UL << 16) | (0UL << 0),

        /// <summary>
        /// Windows 11 22H2, released September 20, 2022.
        /// </summary>
        [Description("Windows 11 {0} 22H2 (Build 22621)")]
        Windows11Version22H2 = (10UL << 48) | (0UL << 32) | (22621UL << 16) | (0UL << 0),

        /// <summary>
        /// Windows 11 23H2, released October 31, 2023.
        /// </summary>
        [Description("Windows 11 {0} 23H2 (Build 22631)")]
        Windows11Version23H2 = (10UL << 48) | (0UL << 32) | (22631UL << 16) | (0UL << 0),

        /// <summary>
        /// Windows 11 24H2, released June 15, 2024.
        /// </summary>
        [Description("Windows 11 {0} 24H2 (Build 26100)")]
        Windows11Version24H2 = (10UL << 48) | (0UL << 32) | (26100UL << 16) | (0UL << 0),

        /// <summary>
        /// Windows Server, version 23H2, released October 24, 2023.
        /// </summary>
        [Description("Windows Server {0}, version 23H2")]
        WindowsServerVersion23H2 = (10UL << 48) | (0UL << 32) | (25398UL << 16) | (0UL << 0),

        /// <summary>
        /// Windows Server 2025, released November 1, 2024.
        /// </summary>
        [Description("Windows Server 2025 {0}")]
        WindowsServer2025 = Windows11Version24H2,

        /// <summary>
        /// Unknown Windows edition.
        /// </summary>
        [Description("Unknown Windows {0} Edition")]
        Unknown = ulong.MaxValue,
    }

    /// <summary>
    /// Flags for determining a product's paricular SKU features.
    /// </summary>
    [Flags]
    public enum SUITE_MASK : ushort
    {
        /// <summary>
        /// Microsoft BackOffice components are installed. 
        /// </summary>
        VER_SUITE_BACKOFFICE = (ushort)PInvoke.VER_SUITE_BACKOFFICE,

        /// <summary>
        /// Windows Server 2003, Web Edition is installed
        /// </summary>
        VER_SUITE_BLADE = (ushort)PInvoke.VER_SUITE_BLADE,

        /// <summary>
        /// Windows Server 2003, Compute Cluster Edition is installed.
        /// </summary>
        VER_SUITE_COMPUTE_SERVER = (ushort)PInvoke.VER_SUITE_COMPUTE_SERVER,

        /// <summary>
        /// Windows Server 2008 Datacenter, Windows Server 2003, Datacenter Edition, or Windows 2000 Datacenter Server is installed. 
        /// </summary>
        VER_SUITE_DATACENTER = (ushort)PInvoke.VER_SUITE_DATACENTER,

        /// <summary>
        /// Windows Server 2008 Enterprise, Windows Server 2003, Enterprise Edition, or Windows 2000 Advanced Server is installed.
        /// Refer to the Remarks section for more information about this bit flag. 
        /// </summary>
        VER_SUITE_ENTERPRISE = (ushort)PInvoke.VER_SUITE_ENTERPRISE,

        /// <summary>
        /// Windows XP Embedded is installed. 
        /// </summary>
        VER_SUITE_EMBEDDEDNT = (ushort)PInvoke.VER_SUITE_EMBEDDEDNT,

        /// <summary>
        /// Windows Vista Home Premium, Windows Vista Home Basic, or Windows XP Home Edition is installed. 
        /// </summary>
        VER_SUITE_PERSONAL = (ushort)PInvoke.VER_SUITE_PERSONAL,

        /// <summary>
        /// Remote Desktop is supported, but only one interactive session is supported. This value is set unless the system is running in application server mode. 
        /// </summary>
        VER_SUITE_SINGLEUSERTS = (ushort)PInvoke.VER_SUITE_SINGLEUSERTS,

        /// <summary>
        /// Microsoft Small Business Server was once installed on the system, but may have been upgraded to another version of Windows.
        /// Refer to the Remarks section for more information about this bit flag. 
        /// </summary>
        VER_SUITE_SMALLBUSINESS = (ushort)PInvoke.VER_SUITE_SMALLBUSINESS,

        /// <summary>
        /// Microsoft Small Business Server is installed with the restrictive client license in force. Refer to the Remarks section for more information about this bit flag. 
        /// </summary>
        VER_SUITE_SMALLBUSINESS_RESTRICTED = (ushort)PInvoke.VER_SUITE_SMALLBUSINESS_RESTRICTED,

        /// <summary>
        /// Windows Storage Server 2003 R2 or Windows Storage Server 2003 is installed. 
        /// </summary>
        VER_SUITE_STORAGE_SERVER = (ushort)PInvoke.VER_SUITE_STORAGE_SERVER,

        /// <summary>
        /// Terminal Services is installed. This value is always set.
        /// If VER_SUITE_TERMINAL is set but VER_SUITE_SINGLEUSERTS is not set, the system is running in application server mode.
        /// </summary>
        VER_SUITE_TERMINAL = (ushort)PInvoke.VER_SUITE_TERMINAL,

        /// <summary>
        /// Windows Home Server is installed. 
        /// </summary>
        VER_SUITE_WH_SERVER = (ushort)PInvoke.VER_SUITE_WH_SERVER
    }

    /// <summary>
    /// Values for determining a product's type.
    /// </summary>
    public enum PRODUCT_TYPE : byte
    {
        /// <summary>
        /// The operating system is Windows 10, Windows 8, Windows 7,...
        /// </summary>
        VER_NT_WORKSTATION = (byte)PInvoke.VER_NT_WORKSTATION,

        /// <summary>
        /// The system is a domain controller and the operating system is Windows Server.
        /// </summary>
        VER_NT_DOMAIN_CONTROLLER = (byte)PInvoke.VER_NT_DOMAIN_CONTROLLER,

        /// <summary>
        /// The operating system is Windows Server. Note that a server that is also a domain controller
        /// is reported as VER_NT_DOMAIN_CONTROLLER, not VER_NT_SERVER.
        /// </summary>
        VER_NT_SERVER = (byte)PInvoke.VER_NT_SERVER,
    }
}
