using System;
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
    internal static class ShellUtilities
    {
        /// <summary>
        /// Refreshes the desktop icons and updates the environment variables in the system.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the operation fails.</exception>
        internal static void RefreshDesktopAndEnvironmentVariables()
        {
            // Update desktop icons using SHChangeNotify, then notify all top-level windows that the environment variables have changed.
            Shell32.SHChangeNotify(SHCNE_ID.SHCNE_ASSOCCHANGED, SHCNF_FLAGS.SHCNF_FLUSHNOWAIT, IntPtr.Zero, IntPtr.Zero);
            User32.SendMessageTimeout(HWND.HWND_BROADCAST, PInvoke.WM_SETTINGCHANGE, UIntPtr.Zero, SafeMemoryHandle.Null, SEND_MESSAGE_TIMEOUT_FLAGS.SMTO_ABORTIFHUNG, 100, out _);
            using (var lpString = SafeHGlobalHandle.StringToUni("Environment"))
            {
                User32.SendMessageTimeout(HWND.HWND_BROADCAST, PInvoke.WM_SETTINGCHANGE, UIntPtr.Zero, lpString, SEND_MESSAGE_TIMEOUT_FLAGS.SMTO_ABORTIFHUNG, 100, out _);
            }
        }

        /// <summary>
        /// Gets the user notification state.
        /// </summary>
        /// <returns>The user notification state.</returns>
        internal static LibraryInterfaces.QUERY_USER_NOTIFICATION_STATE GetUserNotificationState()
        {
            Shell32.SHQueryUserNotificationState(out Windows.Win32.UI.Shell.QUERY_USER_NOTIFICATION_STATE state);
            return (LibraryInterfaces.QUERY_USER_NOTIFICATION_STATE)state;
        }

        /// <summary>
        /// Minimizes all open windows on the desktop.
        /// </summary>
        /// <remarks>This method sends a command to the system shell to minimize all currently open windows. It is equivalent to the "Show Desktop" functionality in Windows.</remarks>
        internal static void MinimizeAllWindows()
        {
            User32.SendMessage(User32.FindWindow("Shell_TrayWnd", null), PInvoke.WM_COMMAND, User32.MIN_ALL, IntPtr.Zero);
        }

        /// <summary>
        /// Restores all minimized windows on the desktop to their previous state.
        /// </summary>
        /// <remarks>This method sends a system command to undo the "Minimize All Windows" action, effectively restoring all previously minimized windows. It has no effect if no  windows are currently minimized.</remarks>
        internal static void RestoreAllWindows()
        {
            User32.SendMessage(User32.FindWindow("Shell_TrayWnd", null), PInvoke.WM_COMMAND, User32.MIN_ALL_UNDO, IntPtr.Zero);
        }

        /// <summary>
        /// Retrieves the process ID of the Windows Explorer shell process.
        /// </summary>
        /// <remarks>This method uses the Windows API to obtain the process ID associated with the shell
        /// window. It is intended for internal use and may not be suitable for general-purpose process
        /// management.</remarks>
        /// <returns>The process ID of the Windows Explorer shell process as an unsigned integer.</returns>
        internal static uint GetExplorerProcessId()
        {
            User32.GetWindowThreadProcessId(User32.GetShellWindow(), out var pid);
            return pid;
        }
    }
}
