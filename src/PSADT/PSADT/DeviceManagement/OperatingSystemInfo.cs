using System;
using System.ComponentModel;
using System.Globalization;
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

            NtDll.RtlGetVersion(out OSVERSIONINFOEXW osVersion);
            SUITE_MASK suiteMask = (SUITE_MASK)osVersion.wSuiteMask;
            PRODUCT_TYPE productType = (PRODUCT_TYPE)osVersion.wProductType;
            string? editionId = null;
            string? productName = null;
            int ubr = 0;

            ulong windowsOS = (((ulong)osVersion.dwMajorVersion) << 48) | (((ulong)osVersion.dwMinorVersion) << 32) | (((ulong)osVersion.dwBuildNumber) << 16); WindowsOS operatingSystem = WindowsOS.Unknown;
            if (Enum.IsDefined(typeof(WindowsOS), windowsOS))
            {
                operatingSystem = (WindowsOS)windowsOS;
            }

            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion")!)
            {
                if (key.GetValue("UBR") is int ubrValue)
                {
                    ubr = ubrValue;
                }
                if (key.GetValue("ReleaseId") is string relId && !string.IsNullOrWhiteSpace(relId))
                {
                    ReleaseId = relId;
                }
                if (key.GetValue("DisplayVersion") is string relIdVer && !string.IsNullOrWhiteSpace(relIdVer))
                {
                    ReleaseIdName = relIdVer;
                }
                if (key.GetValue("EditionID") is string editionIdValue && !string.IsNullOrWhiteSpace(editionIdValue))
                {
                    editionId = editionIdValue;
                }
                if (key.GetValue("ProductName") is string productNameValue && !string.IsNullOrWhiteSpace(productNameValue))
                {
                    productName = productNameValue;
                }
            }

            Kernel32.GetProductInfo(osVersion.dwMajorVersion, osVersion.dwMinorVersion, osVersion.wServicePackMajor, osVersion.wServicePackMinor, out OS_PRODUCT_TYPE edition);
            Name = string.Format(CultureInfo.InvariantCulture, ((DescriptionAttribute[])typeof(WindowsOS).GetField(operatingSystem.ToString())!.GetCustomAttributes(typeof(DescriptionAttribute), false))[0].Description, editionId);
            Version = new((int)osVersion.dwMajorVersion, (int)osVersion.dwMinorVersion, (int)osVersion.dwBuildNumber, ubr);
            Edition = edition.ToString();
            Architecture = RuntimeInformation.OSArchitecture;
            Is64BitOperatingSystem = Environment.Is64BitOperatingSystem;
            IsTerminalServer = ((suiteMask & SUITE_MASK.VER_SUITE_TERMINAL) == SUITE_MASK.VER_SUITE_TERMINAL) && !((suiteMask & SUITE_MASK.VER_SUITE_SINGLEUSERTS) == SUITE_MASK.VER_SUITE_SINGLEUSERTS);
            IsWorkstationEnterpriseMultiSessionOS = IsOperatingSystemEnterpriseMultiSessionOS(edition, editionId, productName);
            IsWorkstation = productType == PRODUCT_TYPE.VER_NT_WORKSTATION;
            IsServer = !IsWorkstation;
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
        /// Release Id of the operating system.
        /// </summary>
        public string? ReleaseId { get; }

        /// <summary>
        /// Release Id name of the operating system.
        /// </summary>
        public string? ReleaseIdName { get; }

        /// <summary>
        /// Architecture of the operating system.
        /// </summary>
        public Architecture Architecture { get; }

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
