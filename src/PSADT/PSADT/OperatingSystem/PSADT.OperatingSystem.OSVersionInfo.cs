using System;
using System.Linq;
using System.ComponentModel;
using PSADT.PInvokes;
using PSADT.Shared;

namespace PSADT.OperatingSystem
{
    public class OSVersionInfo
    {
        public WindowsOS OperatingSystem { get; }
        public string? Name { get; }
        public PRODUCT_SKU Edition { get; }
        public Version? Version { get; }
        public string? ReleaseId { get; }
        public string? ReleaseIdName { get; }
        public string? ServicePackName { get; }
        public Version? ServicePackVersion { get; }
        public SystemArchitecture Architecture { get; }
        public bool Is64BitOperatingSystem { get; }
        public bool IsTerminalServer { get; }
        public bool IsWorkstationEnterpriseMultiSessionOS { get; }
        public bool IsWorkstation { get; }
        public bool IsServer { get; }
        public bool IsDomainController { get; }

        private OSVersionInfo()
        {
            OSHelper.GetRtlVersion(out OSVERSIONINFOEX OSVersionInfoEx);

            string OSVersion;
            int? Revision = OSHelper.GetOsRevision();
            if (Revision.HasValue)
            {
                OSVersion = $"{OSVersionInfoEx.MajorVersion}.{OSVersionInfoEx.MinorVersion}.{OSVersionInfoEx.BuildNumber}.{Revision}";
            }
            else
            {
                OSVersion = $"{OSVersionInfoEx.MajorVersion}.{OSVersionInfoEx.MinorVersion}.{OSVersionInfoEx.BuildNumber}";
            }

            NativeMethods.GetProductInfo(
                (ushort)OSVersionInfoEx.MajorVersion,
                (ushort)OSVersionInfoEx.MinorVersion,
                (ushort)OSVersionInfoEx.ServicePackMajor,
                (ushort)OSVersionInfoEx.ServicePackMinor,
                out PRODUCT_SKU OSEdition);

            var isTerminal = (OSVersionInfoEx.SuiteMask & SuiteMask.VER_SUITE_TERMINAL) == SuiteMask.VER_SUITE_TERMINAL;
            var isSingleUserTs = (OSVersionInfoEx.SuiteMask & SuiteMask.VER_SUITE_SINGLEUSERTS) == SuiteMask.VER_SUITE_SINGLEUSERTS;
            var isTerminalServer = isTerminal && !isSingleUserTs;

            var isWorkstationEnterpriseMultiSessionOS = isTerminalServer && OSEdition == PRODUCT_SKU.PRODUCT_SERVERRDSH && OSHelper.IsWorkstationEnterpriseMultiSessionOS();
            var isProductSKUServer = OSEdition.ToString().Contains("SERVER");

            Version = Version.Parse(OSVersion);
            IsTerminalServer = isTerminalServer;
            IsWorkstationEnterpriseMultiSessionOS = isWorkstationEnterpriseMultiSessionOS;
            IsWorkstation = OSVersionInfoEx.ProductType == ProductType.Workstation || isWorkstationEnterpriseMultiSessionOS || !isProductSKUServer;
            IsServer = isProductSKUServer || ((OSVersionInfoEx.ProductType == ProductType.Server || OSVersionInfoEx.ProductType == ProductType.DomainController) && !isWorkstationEnterpriseMultiSessionOS);
            IsDomainController = OSVersionInfoEx.ProductType == ProductType.DomainController;
            Is64BitOperatingSystem = Environment.Is64BitOperatingSystem;
            ReleaseId = OSHelper.GetOsReleaseId();
            ReleaseIdName = OSHelper.GetOsReleaseIdName();
            ServicePackName = OSVersionInfoEx.CSDVersion.Trim('\0');
            if (OSVersionInfoEx.ServicePackMajor > 0)
            {
                ServicePackVersion = Version.Parse($"{OSVersionInfoEx.ServicePackMajor}.{OSVersionInfoEx.ServicePackMinor}");
            }
            OperatingSystem = OSHelper.GetOperatingSystem(
                OSVersionInfoEx.MajorVersion,
                OSVersionInfoEx.MinorVersion,
                OSVersionInfoEx.BuildNumber,
                OSVersionInfoEx.ServicePackMajor,
                IsWorkstation,
                IsServer,
                Is64BitOperatingSystem,
                OSVersionInfoEx.SuiteMask);

            Name = ((DescriptionAttribute[])OperatingSystem.GetType().GetField(OperatingSystem.ToString())!.GetCustomAttributes(typeof(DescriptionAttribute), false)).First().Description;
            Edition = OSEdition;
            Architecture = OSHelper.GetArchitecture();
        }

        public static readonly OSVersionInfo Current = new OSVersionInfo();
    }
}
