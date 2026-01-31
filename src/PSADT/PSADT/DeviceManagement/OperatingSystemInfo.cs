using System;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using PSADT.LibraryInterfaces;
using Windows.Win32.System.SystemInformation;

namespace PSADT.DeviceManagement
{
    /// <summary>
    /// Utility singleton class for getting OS version info.
    /// </summary>
    public sealed record OperatingSystemInfo
    {
        /// <summary>
        /// This operating system's version information.
        /// </summary>
        public static readonly OperatingSystemInfo Current = new();

        /// <summary>
        /// Constructor for the singleton.
        /// </summary>
        private OperatingSystemInfo()
        {
            // Helper function to determine if the OS is an Enterprise Multi-Session OS.
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Enforcing this rule just makes a mess.")]
            static bool IsOperatingSystemEnterpriseMultiSessionOS(OS_PRODUCT_TYPE productType, string? editionId, string? productName)
            {
                if (productType != OS_PRODUCT_TYPE.PRODUCT_DATACENTER_SERVER)
                {
                    return false;
                }
                if (!"EnterpriseMultiSession".Equals(editionId, StringComparison.OrdinalIgnoreCase) && !"ServerRdsh".Equals(editionId, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
                if (string.IsNullOrWhiteSpace(productName) || (productName!.IndexOf("Virtual Desktops", StringComparison.OrdinalIgnoreCase) < 0 && productName!.IndexOf("Multi-Session", StringComparison.OrdinalIgnoreCase) < 0))
                {
                    return false;
                }
                return true;
            }

            // Get OS version information.
            _ = NtDll.RtlGetVersion(out OSVERSIONINFOEXW osVersion);
            SUITE_MASK suiteMask = (SUITE_MASK)osVersion.wSuiteMask;
            PRODUCT_TYPE productType = (PRODUCT_TYPE)osVersion.wProductType;

            // Read additional OS information from the registry.
            string? editionId = null; string? productName = null; int ubr = 0;
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion")!)
            {
                if (key.GetValue("UBR") is int ubrValue)
                {
                    ubr = ubrValue;
                }
                DisplayVersion = (string?)key.GetValue("DisplayVersion");
                productName = (string)key.GetValue("ProductName")!;
                editionId = (string?)key.GetValue("EditionID");
            }

            // Build out the properties for this instance.
            _ = Kernel32.GetProductInfo(osVersion.dwMajorVersion, osVersion.dwMinorVersion, osVersion.wServicePackMajor, osVersion.wServicePackMinor, out OS_PRODUCT_TYPE edition);
            Name = productType == PRODUCT_TYPE.VER_NT_WORKSTATION && productName.Contains("10") && osVersion.dwBuildNumber >= 22000 ? productName.Replace("10", "11") : productName;
            Version = new((int)osVersion.dwMajorVersion, (int)osVersion.dwMinorVersion, (int)osVersion.dwBuildNumber, ubr);
            Edition = edition.ToString();
            Architecture = RuntimeInformation.OSArchitecture;
            ProductType = productType;
            Is64BitOperatingSystem = Environment.Is64BitOperatingSystem;
            IsTerminalServer = ((suiteMask & SUITE_MASK.VER_SUITE_TERMINAL) == SUITE_MASK.VER_SUITE_TERMINAL) && !((suiteMask & SUITE_MASK.VER_SUITE_SINGLEUSERTS) == SUITE_MASK.VER_SUITE_SINGLEUSERTS);
            IsWorkstationEnterpriseMultiSessionOS = IsOperatingSystemEnterpriseMultiSessionOS(edition, editionId, productName);
            IsWorkstation = productType == PRODUCT_TYPE.VER_NT_WORKSTATION;
            IsServer = productType == PRODUCT_TYPE.VER_NT_SERVER;
            IsDomainController = productType == PRODUCT_TYPE.VER_NT_DOMAIN_CONTROLLER;
        }

        /// <summary>
        /// Display name of the operating system.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Edition of the operating system.
        /// </summary>
        public string Edition { get; }

        /// <summary>
        /// Version of the operating system.
        /// </summary>
        public Version Version { get; }

        /// <summary>
        /// Represents the display-friendly version string for the associated object.
        /// </summary>
        public string? DisplayVersion { get; }

        /// <summary>
        /// Architecture of the operating system.
        /// </summary>
        public Architecture Architecture { get; }

        /// <summary>
        /// Gets the type of product represented by this instance.
        /// </summary>
        public PRODUCT_TYPE ProductType { get; }

        /// <summary>
        /// Whether the operating system is 64-bit.
        /// </summary>
        public bool Is64BitOperatingSystem { get; }

        /// <summary>
        /// Whether the operating system is a terminal server.
        /// </summary>
        public bool IsTerminalServer { get; }

        /// <summary>
        /// Whether the operating system is a workstation capable of multiple sessions (AVD, etc).
        /// </summary>
        public bool IsWorkstationEnterpriseMultiSessionOS { get; }

        /// <summary>
        /// Whether the operating system is a workstation.
        /// </summary>
        public bool IsWorkstation { get; }

        /// <summary>
        /// Whether the operating system is a server.
        /// </summary>
        public bool IsServer { get; }

        /// <summary>
        /// Whether the operating system is a domain controller.
        /// </summary>
        public bool IsDomainController { get; }
    }
}
