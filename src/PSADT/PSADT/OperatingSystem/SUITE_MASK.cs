using System;
using Windows.Win32;

namespace PSADT.OperatingSystem
{
    /// <summary>
    /// Flags for determining a product's paricular SKU features.
    /// </summary>
    [Flags]
    internal enum SUITE_MASK : ushort
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
}
