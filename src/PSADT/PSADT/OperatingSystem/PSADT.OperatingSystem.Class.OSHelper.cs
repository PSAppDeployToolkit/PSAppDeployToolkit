using System;
using PSADT.LibraryInterfaces;
using PSADT.Types;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.LibraryLoader;
using Windows.Win32.System.SystemInformation;

namespace PSADT.OperatingSystem
{
    /// <summary>
    /// Operating System utility methods.
    /// </summary>
    public static class OSHelper
    {
        /// <summary>
        /// Returns the OS architecture of the current system.
        /// </summary>
        public static SystemArchitecture GetSystemArchitecture()
        {
            // Attempt to get the OS architecture via isWow64Process2() if we can (only available on Windows 10 1709 or higher).
            // The reason why this is important is that GetNativeSystemInfo() will always report x64 if in an x64 process on a non-x64 operating system.
            var hKernel32Dll = Kernel32.LoadLibraryEx("kernel32.dll", default, LOAD_LIBRARY_FLAGS.LOAD_LIBRARY_SEARCH_SYSTEM32);
            try
            {
                var hKernel32EntryPoint = Kernel32.GetProcAddress(hKernel32Dll, "IsWow64Process2");
                Kernel32.IsWow64Process2(PInvoke.GetCurrentProcess(), out Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE pProcessMachine, out Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE pNativeMachine);
                return (SystemArchitecture)pNativeMachine;
            }
            catch
            {
                // Just fall through here.
            }
            finally
            {
                Kernel32.FreeLibrary(ref hKernel32Dll);
            }

            // If we're here, we're older than 1709 or isWow64Process2 failed.
            PInvoke.GetNativeSystemInfo(out SYSTEM_INFO systemInfo);
            switch (systemInfo.Anonymous.Anonymous.wProcessorArchitecture)
            {
                case PROCESSOR_ARCHITECTURE.PROCESSOR_ARCHITECTURE_ARM64:
                    return SystemArchitecture.ARM64;
                case PROCESSOR_ARCHITECTURE.PROCESSOR_ARCHITECTURE_ARM:
                    return SystemArchitecture.ARM;
                case PROCESSOR_ARCHITECTURE.PROCESSOR_ARCHITECTURE_AMD64:
                    return SystemArchitecture.AMD64;
                case PROCESSOR_ARCHITECTURE.PROCESSOR_ARCHITECTURE_INTEL:
                    return SystemArchitecture.i386;
                default:
                    throw new Exception("An unsupported operating system architecture was detected.");
            }
        }

        /// <summary>
        /// Private helper method for determining whether the operating system is a workstation capable of multiple sessions (AVD, etc).
        /// </summary>
        /// <param name="productType"></param>
        /// <param name="editionId"></param>
        /// <param name="productName"></param>
        /// <returns></returns>
        internal static bool IsOperatingSystemEnterpriseMultiSessionOS(OS_PRODUCT_TYPE productType, string? editionId, string? productName)
        {
            // If the ProductType is 3 (Server), perform additional checks.
            if (productType == OS_PRODUCT_TYPE.PRODUCT_DATACENTER_SERVER)
            {
                if ("EnterpriseMultiSession".Equals(editionId, StringComparison.InvariantCultureIgnoreCase) || "ServerRdsh".Equals(editionId, StringComparison.InvariantCultureIgnoreCase))
                {
                    if (!string.IsNullOrWhiteSpace(productName) && (productName!.IndexOf("Virtual Desktops", StringComparison.OrdinalIgnoreCase) >= 0 || productName!.IndexOf("Multi-Session", StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
