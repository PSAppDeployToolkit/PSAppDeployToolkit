using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using PSADT.PInvoke;
using PSADT.Shared;

namespace PSADT.OperatingSystem
{
    public static partial class OSHelper
    {
        private static string? GetValue(string keyName, string valueName)
        {
            using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(keyName))
            {
                return key?.GetValue(valueName)?.ToString();
            }
        }

        private static bool TestValue(string keyName, string valueName)
        {
            using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(keyName))
            {
                return key?.GetValue(valueName) != null;
            }
        }

        private static bool GetRtlVersion(out OSVERSIONINFOEX OSVersionInfo)
        {
            OSVersionInfo = new OSVERSIONINFOEX { OSVersionInfoSize = Marshal.SizeOf(typeof(OSVERSIONINFOEX)) };

            if (NativeMethods.RtlGetVersion(out OSVersionInfo) != NTSTATUS.STATUS_SUCCESS)
            {
                return false;
            }

            return true;
        }

        public static OSVersionInfo GetOsVersionInfo()
        {
            GetRtlVersion(out OSVERSIONINFOEX OSVersionInfoEx);

            string OSVersion;
            int? Revision = GetOsRevision();
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
                out PRODUCT_TYPE OSEdition);

            var isTerminal = (OSVersionInfoEx.SuiteMask & SuiteMask.VER_SUITE_TERMINAL) == SuiteMask.VER_SUITE_TERMINAL;
            var isSingleUserTs = (OSVersionInfoEx.SuiteMask & SuiteMask.VER_SUITE_SINGLEUSERTS) == SuiteMask.VER_SUITE_SINGLEUSERTS;
            var isTerminalServer = isTerminal && !isSingleUserTs;

            var isWorkstationEnterpriseMultiSessionOS = isTerminalServer && OSEdition == PRODUCT_TYPE.PRODUCT_SERVERRDSH && IsWorkstationEnterpriseMultiSessionOS();

            var OSVersionInfo = new OSVersionInfo();
            OSVersionInfo.Version = Version.Parse(OSVersion);
            OSVersionInfo.IsTerminalServer = isTerminalServer;
            OSVersionInfo.IsWorkstationEnterpriseMultiSessionOS = isWorkstationEnterpriseMultiSessionOS;
            OSVersionInfo.IsWorkstation = OSVersionInfoEx.ProductType == ProductType.Workstation || isWorkstationEnterpriseMultiSessionOS;
            OSVersionInfo.IsServer = (OSVersionInfoEx.ProductType == ProductType.Server && !isWorkstationEnterpriseMultiSessionOS) || (OSVersionInfoEx.ProductType == ProductType.DomainController);
            OSVersionInfo.IsDomainController = OSVersionInfoEx.ProductType == ProductType.DomainController;
            OSVersionInfo.Is64BitOperatingSystem = Environment.Is64BitOperatingSystem;
            OSVersionInfo.ReleaseId = GetOsReleaseId();
            OSVersionInfo.ReleaseIdName = GetOsReleaseIdName();
            OSVersionInfo.ServicePackName = OSVersionInfoEx.CSDVersion;
            OSVersionInfo.ServicePackVersion = Version.Parse($"{OSVersionInfoEx.ServicePackMajor}.{OSVersionInfoEx.ServicePackMinor}");
            OSVersionInfo.OperatingSystem = GetOperatingSystem(
                OSVersionInfoEx.MajorVersion,
                OSVersionInfoEx.MinorVersion,
                OSVersionInfoEx.BuildNumber,
                OSVersionInfoEx.ServicePackMajor,
                OSVersionInfo.ReleaseId,
                OSVersionInfo.IsWorkstation,
                OSVersionInfo.IsServer,
                OSVersionInfo.Is64BitOperatingSystem,
                OSVersionInfoEx.SuiteMask);

            OSVersionInfo.Edition = OSEdition;

            if (Environment.Is64BitOperatingSystem)
            {
                OSVersionInfo.Architecture = "64-bit";
            }
            else
            {
                OSVersionInfo.Architecture = "32-bit";
            }

            return OSVersionInfo;
        }

        public static int? GetOsRevision()
        {
            string? OSVersionRevision = String.Empty;
            
            if (TestValue(@"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "UBR"))
            {
                OSVersionRevision = GetValue(@"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "UBR");
            }
            else if (TestValue(@"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "BuildLabEx"))
            {
                string? BuildLabEx = GetValue(@"HKLM\SOFTWARE\\Microsoft\Windows NT\CurrentVersion", "BuildLabEx");
                if (!String.IsNullOrWhiteSpace(BuildLabEx))
                {
                    OSVersionRevision = BuildLabEx?.Split(".".ToCharArray())[1];
                }
            }

            if (OSVersionRevision != null && !Regex.IsMatch(OSVersionRevision, @"^[\\d\\.]+$"))
            {
                OSVersionRevision = String.Empty;
            }

            if (!String.IsNullOrWhiteSpace(OSVersionRevision))
            {
                if (Int32.TryParse(OSVersionRevision, out int OSVersionRevisionAsInt))
                {
                    return OSVersionRevisionAsInt;
                }

                return null;
            }

            return null;
        }

        public static string? GetOsReleaseId()
        {
            string? OSReleaseId = String.Empty;

            if (TestValue(@"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ReleaseId"))
            {
                OSReleaseId = GetValue(@"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ReleaseId");
            }

            return OSReleaseId;
        }

        public static string? GetOsReleaseIdName()
        {
            string? OSReleaseIdName = String.Empty;

            if (TestValue(@"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "DisplayVersion"))
            {
                OSReleaseIdName = GetValue(@"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "DisplayVersion");
            }
            else
            {
                OSReleaseIdName = GetOsReleaseId();
            }

            return OSReleaseIdName;
        }
        
        public static bool GetIsWindowsIoTCore(PRODUCT_TYPE ProductType)
        {
            if ((ProductType == PRODUCT_TYPE.PRODUCT_IOTENTERPRISE) || (ProductType == PRODUCT_TYPE.PRODUCT_IOTUAP))
            {
                return true;
            }

            return false;
        }

        public static bool GetIsWindowsHomeEdition(PRODUCT_TYPE ProductType)
        {
            switch (ProductType)
            {
                case PRODUCT_TYPE.PRODUCT_CORE:
                case PRODUCT_TYPE.PRODUCT_CORE_COUNTRYSPECIFIC:
                case PRODUCT_TYPE.PRODUCT_CORE_N:
                case PRODUCT_TYPE.PRODUCT_CORE_SINGLELANGUAGE:
                case PRODUCT_TYPE.PRODUCT_HOME_BASIC:
                case PRODUCT_TYPE.PRODUCT_HOME_BASIC_N:
                case PRODUCT_TYPE.PRODUCT_HOME_PREMIUM:
                case PRODUCT_TYPE.PRODUCT_HOME_PREMIUM_N:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsWorkstationEnterpriseMultiSessionOS()
        {
            // Get OS version information
            GetRtlVersion(out OSVERSIONINFOEX osVersionInfo);

            // Retrieve the product type using GetProductInfo
            bool isProductInfoRetrieved = NativeMethods.GetProductInfo(
                (uint)osVersionInfo.MajorVersion,
                (uint)osVersionInfo.MinorVersion,
                (uint)osVersionInfo.ServicePackMajor,
                (uint)osVersionInfo.ServicePackMinor,
                out PRODUCT_TYPE productType);

            // If the ProductType is 3 (Server), perform additional checks
            if (isProductInfoRetrieved && productType == PRODUCT_TYPE.PRODUCT_DATACENTER_SERVER)
            {
                // Check the EditionID registry key to differentiate between Server and Multi-Session Workstation
                string? editionId = GetValue(@"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "EditionID");

                string? productName = GetValue(@"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ProductName");
                


                // Typical EditionID values for multi-session workstations
                string[] multiSessionEditionIds = { "EnterpriseMultiSession", "ServerRdsh" };

                if (!string.IsNullOrEmpty(editionId) && multiSessionEditionIds.Any(id => id.Equals(editionId, StringComparison.OrdinalIgnoreCase)))
                {
                    if (!string.IsNullOrEmpty(productName) &&
                        (productName!.IndexOf("Virtual Desktops", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         productName.IndexOf("Multi-Session", StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static WindowsOS GetOperatingSystem(int MajorVersion, int MinorVersion, int BuildNumber, ushort ServicePackMajor, string? ReleaseId, bool IsWorkstation, bool IsServer, bool Is64BitOperatingSystem, SuiteMask suiteMask)
        {
            switch (MajorVersion)
            {
                case 5:
                    switch (MinorVersion)
                    {
                        case 0:
                            return WindowsOS.Windows2000;
                        case 1:
                            switch (ServicePackMajor)
                            {
                                case 3:
                                    return WindowsOS.WindowsXPSP3;
                                case 2:
                                    return WindowsOS.WindowsXPSP2;
                                case 1:
                                    return WindowsOS.WindowsXPSP1;
                                default:
                                    return WindowsOS.WindowsXP;
                            }
                        case 2:
                            if (IsWorkstation && Is64BitOperatingSystem)
                            {
                                switch (ServicePackMajor)
                                {
                                    case 2:
                                        return WindowsOS.WindowsXPProfessionalx64EditionSP2;
                                    default:
                                        return WindowsOS.WindowsXPProfessionalx64Edition;
                                }
                            }

                            if (IsServer &&
                                NativeMethods.GetSystemMetrics(SystemMetric.SM_SERVERR2) == 0 &&
                                (suiteMask & SuiteMask.VER_SUITE_WH_SERVER) != SuiteMask.VER_SUITE_WH_SERVER)
                            {
                                switch (ServicePackMajor)
                                {
                                    case 2:
                                        return WindowsOS.WindowsServer2003SP2;
                                    case 1:
                                        return WindowsOS.WindowsServer2003SP1;
                                    default:
                                        return WindowsOS.WindowsServer2003;
                                }
                            }

                            if (IsServer && NativeMethods.GetSystemMetrics(SystemMetric.SM_SERVERR2) != 0)
                            {
                                return WindowsOS.WindowsServer2003R2;
                            }

                            break;
                        default:
                            return WindowsOS.Unknown;
                    }
                    return WindowsOS.Unknown;
                case 6:
                    switch (MinorVersion)
                    {
                        case 0:
                            if (IsWorkstation)
                            {
                                switch (ServicePackMajor)
                                {
                                    case 2:
                                        return WindowsOS.WindowsVistaSP2;
                                    case 1:
                                        return WindowsOS.WindowsVistaSP1;
                                    default:
                                        return WindowsOS.WindowsVista;
                                }
                            }
                            if (IsServer)
                            {
                                switch (ServicePackMajor)
                                {
                                    case 2:
                                        return WindowsOS.WindowsServer2008SP2;
                                    default:
                                        return WindowsOS.WindowsServer2008;
                                }
                            }
                            break;
                        case 1:
                            if (IsWorkstation)
                            {
                                switch (ServicePackMajor)
                                {
                                    case 1:
                                        return WindowsOS.Windows7SP1;
                                    default:
                                        return WindowsOS.Windows7;
                                }
                            }
                            if (IsServer)
                            {
                                switch (ServicePackMajor)
                                {
                                    case 1:
                                        return WindowsOS.WindowsServer2008R2SP1;
                                    default:
                                        return WindowsOS.WindowsServer2008R2;
                                }
                            }
                            break;
                        case 2:
                            if (IsWorkstation) { return WindowsOS.Windows8; }
                            if (IsServer) { return WindowsOS.WindowsServer2012; }
                            break;
                        case 3:
                            if (IsWorkstation) { return WindowsOS.Windows8Point1; }
                            if (IsServer) { return WindowsOS.WindowsServer2012R2; }
                            break;
                        default:
                            return WindowsOS.Unknown;
                    }
                    return WindowsOS.Unknown;
                case 10:
                    switch (MinorVersion)
                    {
                        case 0:
                            if (BuildNumber < 22000)
                            {
                                switch (BuildNumber)
                                {
                                    case 10240:
                                        // Original RTM version of Windows 10 which was retroactively named "version 1507" by Microsoft per it's naming conventions.
                                        return WindowsOS.Windows10Version1507;
                                    case 10586:
                                        return WindowsOS.Windows10Version1511;
                                    case 14393:
                                        if (IsWorkstation) { return WindowsOS.Windows10Version1607; }
                                        if (IsServer) { return WindowsOS.WindowsServer2016; }
                                        break;
                                    case 15063:
                                        return WindowsOS.Windows10Version1703;
                                    case 16299:
                                        return WindowsOS.Windows10Version1709;
                                    case 17134:
                                        return WindowsOS.Windows10Version1803;
                                    case 17763:
                                        if (IsWorkstation) { return WindowsOS.Windows10Version1809; }
                                        if (IsServer) { return WindowsOS.WindowsServer2019; }
                                        break;
                                    case 18362:
                                        if (IsWorkstation) { return WindowsOS.Windows10Version1903; }
                                        if (IsServer) { return WindowsOS.WindowsServerVersion1903; }
                                        break;
                                    case 18363:
                                        if (IsWorkstation) { return WindowsOS.Windows10Version1909; }
                                        if (IsServer) { return WindowsOS.WindowsServerVersion1909; }
                                        break;
                                    case 19041:
                                        if (IsWorkstation) { return WindowsOS.Windows10Version2004; }
                                        if (IsServer) { return WindowsOS.WindowsServerVersion2004; }
                                        break;
                                    case 19042:
                                        if (IsWorkstation) { return WindowsOS.Windows10Version20H2; }
                                        if (IsServer) { return WindowsOS.WindowsServerVersion20H2; }
                                        break;
                                    case 19043:
                                        if (IsWorkstation) { return WindowsOS.Windows10Version21H1; }
                                        break;
                                    case 19044:
                                        if (IsWorkstation) { return WindowsOS.Windows10Version21H2; }
                                        break;
                                    case 20348:
                                        if (IsServer) { return WindowsOS.WindowsServer2022; }
                                        break;
                                    default:
                                        if (IsWorkstation)
                                        {
                                            if (BuildNumber > 19044)
                                            {
                                                return WindowsOS.Windows10NewVersionUnknown;
                                            }
                                        }
                                        if (IsServer)
                                        {
                                            if (BuildNumber > 20348)
                                            {
                                                return WindowsOS.WindowsServerNewVersionUnknown;
                                            }
                                        }
                                        return WindowsOS.Unknown;
                                }
                                return WindowsOS.Unknown;
                            }
                            else
                            {
                                switch (BuildNumber)
                                {
                                    case 22000:
                                        if (IsWorkstation) { return WindowsOS.Windows11Version21H2; }
                                        break;
                                    case 22621:
                                        if (IsWorkstation) { return WindowsOS.Windows11Version22H2; }
                                        break;
                                    case 22631:
                                        if (IsWorkstation) { return WindowsOS.Windows11Version23H2; }
                                        break;
                                    case 25398:
                                        if (IsServer) { return WindowsOS.WindowsServerVersion23H2; }
                                        break;
                                    case 26100:
                                        if (IsWorkstation) { return WindowsOS.Windows11Version24H2; }
                                        break;
                                    default:
                                        if (IsWorkstation)
                                        {
                                            if (BuildNumber > 22000)
                                            {
                                                return WindowsOS.Windows11NewVersionUnknown;
                                            }
                                        }
                                        if (IsServer)
                                        {
                                            if (BuildNumber > 22000)
                                            {
                                                return WindowsOS.WindowsServerNewVersionUnknown;
                                            }
                                        }
                                        return WindowsOS.Unknown;
                                }
                            }
                            return WindowsOS.Unknown;
                        default:
                            return WindowsOS.Unknown;
                    }
                default:
                    return WindowsOS.Unknown;
            }
        }

        /// <summary>
        /// Returns the OS architecture of the current system.
        /// </summary>
        public static SystemArchitecture GetArchitecture()
        {
            // Attempt to get the OS architecture via isWow64Process2() if we can (only available on Windows 10 1709 or higher).
            // The reason why this is important is that GetNativeSystemInfo() will always report x64 if in an x64 process on a non-x64 operating system.
            using SafeLibraryHandle hKernel32Dll = NativeMethods.LoadLibraryEx("kernel32.dll", SafeLibraryHandle.Null, LoadLibraryExFlags.LOAD_LIBRARY_SEARCH_SYSTEM32);
            if (!hKernel32Dll.IsInvalid && !hKernel32Dll.IsClosed && NativeMethods.GetProcAddress(hKernel32Dll, "IsWow64Process2") != IntPtr.Zero)
            {
                if (NativeMethods.IsWow64Process2(NativeMethods.GetCurrentProcess(), out IMAGE_FILE_MACHINE processMachine, out IMAGE_FILE_MACHINE nativeMachine) != false)
                {
                    return (SystemArchitecture)nativeMachine;
                }
            }

            // If we're here, we're older than 1709 or isWow64Process2 failed.
            NativeMethods.GetNativeSystemInfo(out SYSTEM_INFO systemInfo);
            switch (systemInfo.wProcessorArchitecture)
            {
                case ProcessorArchitecture.PROCESSOR_ARCHITECTURE_ARM64:
                    return SystemArchitecture.ARM64;
                case ProcessorArchitecture.PROCESSOR_ARCHITECTURE_ARM:
                    return SystemArchitecture.ARM;
                case ProcessorArchitecture.PROCESSOR_ARCHITECTURE_AMD64:
                    return SystemArchitecture.AMD64;
                case ProcessorArchitecture.PROCESSOR_ARCHITECTURE_INTEL:
                    return SystemArchitecture.i386;
                default:
                    throw new Exception("An unsupported operating system architecture was detected.");
            }
        }

        public static bool GetIsWindowsVersionXOrGreaterThanX(WindowsOS LowerBound, WindowsOS TargetOS)
        {
            var EnumOperatingSystemValues = Enum.GetValues(typeof(WindowsOS)).Cast<int>().OrderBy(x => x);
            return (((int)TargetOS != 0) && (int)TargetOS >= EnumOperatingSystemValues.SkipWhile<int>(enumValue => enumValue != (int)LowerBound).First()) && ((int)TargetOS <= EnumOperatingSystemValues.Last());
        }

        public static bool GetIsWindows2000OrGreater(WindowsOS operatingSystem)
        {
            return GetIsWindowsVersionXOrGreaterThanX(WindowsOS.Windows2000, operatingSystem);
        }

        public static bool GetIsWindowsXPOrGreater(WindowsOS operatingSystem)
        {
            return GetIsWindowsVersionXOrGreaterThanX(WindowsOS.WindowsXP, operatingSystem);
        }

        public static bool GetIsWindowsXPSP3OrGreater(WindowsOS operatingSystem)
        {
            return GetIsWindowsVersionXOrGreaterThanX(WindowsOS.WindowsXPSP3, operatingSystem);
        }

        public static bool GetIsWindowsWindowsXPProfessionalx64EditionOrGreater(WindowsOS operatingSystem)
        {
            return GetIsWindowsVersionXOrGreaterThanX(WindowsOS.WindowsXPProfessionalx64Edition, operatingSystem);
        }

        public static bool GetIsWindowsWindowsXPProfessionalx64EditionSP2OrGreater(WindowsOS operatingSystem)
        {
            return GetIsWindowsVersionXOrGreaterThanX(WindowsOS.WindowsXPProfessionalx64EditionSP2, operatingSystem);
        }

        public static bool GetIsWindowsVistaOrGreater(WindowsOS operatingSystem)
        {
            return GetIsWindowsVersionXOrGreaterThanX(WindowsOS.WindowsVista, operatingSystem);
        }

        public static bool GetIsWindowsVistaSP1OrGreater(WindowsOS operatingSystem)
        {
            return GetIsWindowsVersionXOrGreaterThanX(WindowsOS.WindowsVistaSP1, operatingSystem);
        }

        public static bool GetIsWindowsVistaSP2OrGreater(WindowsOS operatingSystem)
        {
            return GetIsWindowsVersionXOrGreaterThanX(WindowsOS.WindowsVistaSP2, operatingSystem);
        }

        public static bool GetIsWindowsServer2008OrGreater(WindowsOS operatingSystem)
        {
            return GetIsWindowsVersionXOrGreaterThanX(WindowsOS.WindowsServer2008, operatingSystem);
        }

        public static bool GetIsWindows7OrGreater(WindowsOS operatingSystem)
        {
            return GetIsWindowsVersionXOrGreaterThanX(WindowsOS.Windows7, operatingSystem);
        }

        public static bool GetIsWindows7SP1OrGreater(WindowsOS operatingSystem)
        {
            return GetIsWindowsVersionXOrGreaterThanX(WindowsOS.Windows7SP1, operatingSystem);
        }

        public static bool GetIsWindowsServer2008R2OrGreater(WindowsOS operatingSystem)
        {
            return GetIsWindowsVersionXOrGreaterThanX(WindowsOS.WindowsServer2008R2, operatingSystem);
        }

        public static bool GetIsWindows8OrGreater(WindowsOS operatingSystem)
        {
            return GetIsWindowsVersionXOrGreaterThanX(WindowsOS.Windows8, operatingSystem);
        }

        public static bool GetIsWindowsServer2012OrGreater(WindowsOS operatingSystem)
        {
            return GetIsWindowsVersionXOrGreaterThanX(WindowsOS.WindowsServer2012, operatingSystem);
        }

        public static bool GetIsWindows8Point1OrGreater(WindowsOS operatingSystem)
        {
            return GetIsWindowsVersionXOrGreaterThanX(WindowsOS.Windows8Point1, operatingSystem);
        }

        public static bool GetIsWindowsServer2012R2OrGreater(WindowsOS operatingSystem)
        {
            return GetIsWindowsVersionXOrGreaterThanX(WindowsOS.WindowsServer2012R2, operatingSystem);
        }

        public static bool GetIsWindows10OrGreater(WindowsOS operatingSystem)
        {
            return GetIsWindowsVersionXOrGreaterThanX(WindowsOS.Windows10Version1507, operatingSystem);
        }

        public static bool GetIsWindows10Version1507OrGreater(WindowsOS operatingSystem)
        {
            return GetIsWindowsVersionXOrGreaterThanX(WindowsOS.Windows10Version1507, operatingSystem);
        }

        public static bool GetIsWindows10Version1511OrGreater(WindowsOS operatingSystem)
        {
            return GetIsWindowsVersionXOrGreaterThanX(WindowsOS.Windows10Version1511, operatingSystem);
        }

        public static bool GetIsWindows10Version1607OrGreater(WindowsOS operatingSystem)
        {
            return GetIsWindowsVersionXOrGreaterThanX(WindowsOS.Windows10Version1607, operatingSystem);
        }

        public static bool GetIsWindowsServer2016OrGreater(WindowsOS operatingSystem)
        {
            return GetIsWindowsVersionXOrGreaterThanX(WindowsOS.WindowsServer2016, operatingSystem);
        }

        public static bool GetIsWindows10Version1703OrGreater(WindowsOS operatingSystem)
        {
            return GetIsWindowsVersionXOrGreaterThanX(WindowsOS.Windows10Version1703, operatingSystem);
        }

        public static bool GetIsWindows10Version1709OrGreater(WindowsOS operatingSystem)
        {
            return GetIsWindowsVersionXOrGreaterThanX(WindowsOS.Windows10Version1709, operatingSystem);
        }

        public static bool GetIsWindows10Version1803OrGreater(WindowsOS operatingSystem)
        {
            return GetIsWindowsVersionXOrGreaterThanX(WindowsOS.Windows10Version1803, operatingSystem);
        }

        public static bool GetIsWindows10Version1809OrGreater(WindowsOS operatingSystem)
        {
            return GetIsWindowsVersionXOrGreaterThanX(WindowsOS.Windows10Version1809, operatingSystem);
        }

        public static bool GetIsWindowsServer2019OrGreater(WindowsOS operatingSystem)
        {
            return GetIsWindowsVersionXOrGreaterThanX(WindowsOS.WindowsServer2019, operatingSystem);
        }

        public static bool GetIsWindows10Version1903OrGreater(WindowsOS operatingSystem)
        {
            return GetIsWindowsVersionXOrGreaterThanX(WindowsOS.Windows10Version1903, operatingSystem);
        }

        public static bool GetIsWindowsServerVersion1903OrGreater(WindowsOS operatingSystem)
        {
            return GetIsWindowsVersionXOrGreaterThanX(WindowsOS.WindowsServerVersion1903, operatingSystem);
        }

        public static bool GetIsWindows10Version1909OrGreater(WindowsOS operatingSystem)
        {
            return GetIsWindowsVersionXOrGreaterThanX(WindowsOS.Windows10Version1909, operatingSystem);
        }

        public static bool GetIsWindowsServerVersion1909OrGreater(WindowsOS operatingSystem)
        {
            return GetIsWindowsVersionXOrGreaterThanX(WindowsOS.WindowsServerVersion1909, operatingSystem);
        }

        public static bool GetIsWindows10Version2004OrGreater(WindowsOS operatingSystem)
        {
            return GetIsWindowsVersionXOrGreaterThanX(WindowsOS.Windows10Version2004, operatingSystem);
        }

        public static bool GetIsWindowsServerVersion2004OrGreater(WindowsOS operatingSystem)
        {
            return GetIsWindowsVersionXOrGreaterThanX(WindowsOS.WindowsServerVersion2004, operatingSystem);
        }

        public static bool GetIsWindows10Version20H2OrGreater(WindowsOS operatingSystem)
        {
            return GetIsWindowsVersionXOrGreaterThanX(WindowsOS.Windows10Version20H2, operatingSystem);
        }

        public static bool GetIsWindowsServerVersion20H2OrGreater(WindowsOS operatingSystem)
        {
            return GetIsWindowsVersionXOrGreaterThanX(WindowsOS.WindowsServerVersion20H2, operatingSystem);
        }

        public static bool GetIsWindows10Version21H1OrGreater(WindowsOS operatingSystem)
        {
            return GetIsWindowsVersionXOrGreaterThanX(WindowsOS.Windows10Version21H1, operatingSystem);
        }

        public static bool GetIsWindowsServer2022OrGreater(WindowsOS operatingSystem)
        {
            return GetIsWindowsVersionXOrGreaterThanX(WindowsOS.WindowsServer2022, operatingSystem);
        }

        public static bool GetIsWindows10Version21H2OrGreater(WindowsOS operatingSystem)
        {
            return GetIsWindowsVersionXOrGreaterThanX(WindowsOS.Windows10Version21H2, operatingSystem);
        }

        public static bool GetIsWindows11Version21H2OrGreater(WindowsOS operatingSystem)
        {
            return GetIsWindowsVersionXOrGreaterThanX(WindowsOS.Windows11Version21H2, operatingSystem);
        }

        public static bool GetIsWindows11Version22H2OrGreater(WindowsOS operatingSystem)
        {
            return GetIsWindowsVersionXOrGreaterThanX(WindowsOS.Windows11Version22H2, operatingSystem);
        }

        public static bool GetIsWindows11Version23H2OrGreater(WindowsOS operatingSystem)
        {
            return GetIsWindowsVersionXOrGreaterThanX(WindowsOS.Windows11Version23H2, operatingSystem);
        }

        public static bool GetIsWindowsServerVersion23H2OrGreater(WindowsOS operatingSystem)
        {
            return GetIsWindowsVersionXOrGreaterThanX(WindowsOS.WindowsServerVersion23H2, operatingSystem);
        }

        public static bool GetIsWindows11Version24H2OrGreater(WindowsOS operatingSystem)
        {
            return GetIsWindowsVersionXOrGreaterThanX(WindowsOS.Windows11Version24H2, operatingSystem);
        }
    }
}
