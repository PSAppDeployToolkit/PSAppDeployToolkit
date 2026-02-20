namespace PSADT.LibraryInterfaces
{
    /// <summary>
    /// Specifies the status of a computer's membership in a workgroup or domain.
    /// </summary>
    /// <remarks>This enumeration is typically used to indicate whether a computer is joined to a domain, a
    /// workgroup, or is unjoined. The values correspond to the possible states returned by network management APIs when
    /// querying the join status of a system.</remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "This is named as per the Win32 API.")]
    public enum NETSETUP_JOIN_STATUS
    {
        /// <summary>
        /// Domain join status of the machine is unknown.
        /// </summary>
        NetSetupUnknownStatus = Windows.Win32.NetworkManagement.NetManagement.NETSETUP_JOIN_STATUS.NetSetupUnknownStatus,

        /// <summary>
        /// Machine is not joined to a domain or to a workgroup.
        /// </summary>
        NetSetupUnjoined = Windows.Win32.NetworkManagement.NetManagement.NETSETUP_JOIN_STATUS.NetSetupUnjoined,

        /// <summary>
        /// Machine is joined to a workgroup.
        /// </summary>
        NetSetupWorkgroupName = Windows.Win32.NetworkManagement.NetManagement.NETSETUP_JOIN_STATUS.NetSetupWorkgroupName,

        /// <summary>
        /// Machine is joined to a domain.
        /// </summary>
        NetSetupDomainName = Windows.Win32.NetworkManagement.NetManagement.NETSETUP_JOIN_STATUS.NetSetupDomainName,
    }
}
