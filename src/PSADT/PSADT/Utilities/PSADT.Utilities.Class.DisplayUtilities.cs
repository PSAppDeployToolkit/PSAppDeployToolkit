using System;
using PSADT.LibraryInterfaces;
using PSADT.OperatingSystem;
using Windows.Win32.UI.HiDpi;

namespace PSADT.Utilities
{
    /// <summary>
    /// Utility methods for managing display settings and DPI awareness.
    /// </summary>
    public static class DisplayUtilities
    {
        /// <summary>
        /// Sets the appropriate DPI awareness for the current process based on the operating system version.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the appropriate DPI awareness setting could not be applied.</exception>
        /// <remarks>
        /// This method will check the operating system version and apply the most advanced DPI awareness setting supported by the system.
        /// It will attempt to use Per Monitor DPI Awareness v2 for Windows 10 (version 15063 and later), fallback to earlier versions for
        /// Windows 8.1 and above, and finally to older APIs for Windows 7 and Vista.
        /// </remarks>
        public static void SetProcessDpiAwarenessForOSVersion()
        {
            if (OSVersionInfo.Current.Version >= new Version(10, 0, 15063)) // Windows 10, Creators Update (Version 1703) and later
            {
                User32.SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
            }
            else if (OSVersionInfo.Current.Version >= new Version(10, 0, 14393)) // Windows 10, Anniversary Update (Version 1607)
            {
                User32.SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE);
            }
            else if (OSVersionInfo.Current.Version >= new Version(6, 3, 9600)) // Windows 8.1
            {
                SHCore.SetProcessDpiAwareness(PROCESS_DPI_AWARENESS.PROCESS_PER_MONITOR_DPI_AWARE);
            }
            else if (OSVersionInfo.Current.Version >= new Version(6, 0, 6000)) // Windows Vista or Windows 7
            {
                User32.SetProcessDPIAware();
            }
            else
            {
                throw new NotSupportedException("The current operating system version does not support any known DPI awareness APIs.");
            }
        }
    }
}
