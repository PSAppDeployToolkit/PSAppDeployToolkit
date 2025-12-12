namespace PSADT.Interop
{
    /// <summary>
    /// Values for determining a product's type.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1008:Enums should have zero value", Justification = "There is no zero value in the Win32 API for PRODUCT_TYPE.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1028:Enum Storage should be Int32", Justification = "These values are represented as 8-bit values in the Win32 API.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "These are named as per the Win32 API.")]
    public enum PRODUCT_TYPE : byte
    {
        /// <summary>
        /// The operating system is Windows 10, Windows 8, Windows 7,...
        /// </summary>
        VER_NT_WORKSTATION = (byte)Windows.Win32.PInvoke.VER_NT_WORKSTATION,

        /// <summary>
        /// The system is a domain controller and the operating system is Windows Server.
        /// </summary>
        VER_NT_DOMAIN_CONTROLLER = (byte)Windows.Win32.PInvoke.VER_NT_DOMAIN_CONTROLLER,

        /// <summary>
        /// The operating system is Windows Server. Note that a server that is also a domain controller
        /// is reported as VER_NT_DOMAIN_CONTROLLER, not VER_NT_SERVER.
        /// </summary>
        VER_NT_SERVER = (byte)Windows.Win32.PInvoke.VER_NT_SERVER,
    }
}
