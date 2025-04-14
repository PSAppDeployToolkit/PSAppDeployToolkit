using System;
using System.Runtime.InteropServices;
using PSADT.LibraryInterfaces;
using PSADT.SafeHandles;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;

namespace PSADT.Utilities
{
    /// <summary>
    /// Provides methods for interacting with the Windows Explorer.
    /// </summary>
    public static class ShellUtilities
    {
        /// <summary>
        /// Refreshes the desktop icons and updates the environment variables in the system.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the operation fails.</exception>
        public static void RefreshDesktopAndEnvironmentVariables()
        {
            // Update desktop icons using SHChangeNotify, then notify all top-level windows that the environment variables have changed.
            Shell32.SHChangeNotify(SHCNE_ID.SHCNE_ASSOCCHANGED, SHCNF_FLAGS.SHCNF_FLUSHNOWAIT, IntPtr.Zero, IntPtr.Zero);
            User32.SendMessageTimeout(HWND.HWND_BROADCAST, PInvoke.WM_SETTINGCHANGE, new WPARAM(0), SafeMemoryHandle.Null, SEND_MESSAGE_TIMEOUT_FLAGS.SMTO_ABORTIFHUNG, 100, out _);
            using (var lpString = SafeHGlobalHandle.StringToUni("Environment"))
            {
                User32.SendMessageTimeout(HWND.HWND_BROADCAST, PInvoke.WM_SETTINGCHANGE, new WPARAM(0), lpString, SEND_MESSAGE_TIMEOUT_FLAGS.SMTO_ABORTIFHUNG, 100, out _);
            }
        }

        /// <summary>
        /// Gets the user notification state.
        /// </summary>
        /// <returns>The user notification state.</returns>
        public static LibraryInterfaces.QUERY_USER_NOTIFICATION_STATE GetUserNotificationState()
        {
            Shell32.SHQueryUserNotificationState(out Windows.Win32.UI.Shell.QUERY_USER_NOTIFICATION_STATE state);
            return (LibraryInterfaces.QUERY_USER_NOTIFICATION_STATE)state;
        }
    }
}
