using Windows.Win32;

namespace PSADT.OperatingSystem
{
    /// <summary>
    /// Values for determining a product's type.
    /// </summary>
    internal enum PRODUCT_TYPE : byte
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
