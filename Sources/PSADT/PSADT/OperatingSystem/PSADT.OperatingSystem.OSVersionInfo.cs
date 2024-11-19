using System;
using PSADT.PInvoke;
using PSADT.Shared;

namespace PSADT.OperatingSystem
{
    public class OSVersionInfo
    {
        public WindowsOS OperatingSystem { get; set; }
        public string? Name { get; set; }
        public PRODUCT_SKU Edition { get; set; }
        public Version? Version { get; set; }
        public string? ReleaseId { get; set; }
        public string? ReleaseIdName { get; set; }
        public string? ServicePackName { get; set; }
        public Version? ServicePackVersion { get; set; }
        public SystemArchitecture Architecture { get; set; }
        public bool Is64BitOperatingSystem { get; set; }
        public bool IsTerminalServer { get; set; }
        public bool IsWorkstationEnterpriseMultiSessionOS { get; set; }
        public bool IsWorkstation { get; set; }
        public bool IsServer { get; set; }
        public bool IsDomainController { get; set; }
    }
}
