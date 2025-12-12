using PSADT.LibraryInterfaces;

namespace PSADT.DeviceManagement
{
    /// <summary>
    /// Represents the domain or workgroup join status of a computer, including the associated domain or workgroup name
    /// if applicable.
    /// </summary>
    /// <remarks>Use this type to determine whether a computer is joined to a domain, a workgroup, or is
    /// unjoined, and to retrieve the corresponding domain or workgroup name when available. This record is
    /// immutable.</remarks>
    public sealed record DomainStatus
    {
        /// <summary>
        /// Initializes a new instance of the DomainStatus class with the specified join status and domain or workgroup
        /// name.
        /// </summary>
        /// <param name="joinStatus">The status indicating whether the computer is joined to a domain, a workgroup, or is unjoined.</param>
        /// <param name="domainOrWorkgroupName">The name of the domain or workgroup associated with the current join status, or null if not applicable.</param>
        public DomainStatus(NETSETUP_JOIN_STATUS joinStatus, string? domainOrWorkgroupName)
        {
            JoinStatus = joinStatus;
            DomainOrWorkgroupName = domainOrWorkgroupName;
        }

        /// <summary>
        /// Gets the status of the computer's domain or workgroup join operation.
        /// </summary>
        public NETSETUP_JOIN_STATUS JoinStatus { get; }

        /// <summary>
        /// Gets the name of the domain or workgroup to which the computer belongs.
        /// </summary>
        public string? DomainOrWorkgroupName { get; }
    }
}
