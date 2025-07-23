using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using PSADT.Extensions;
using PSADT.LibraryInterfaces;
using PSADT.SafeHandles;
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
        internal static void RefreshDesktopAndEnvironmentVariables()
        {
            // Update desktop icons using SHChangeNotify, then notify all top-level windows that the environment variables have changed.
            Shell32.SHChangeNotify(SHCNE_ID.SHCNE_ASSOCCHANGED, SHCNF_FLAGS.SHCNF_FLUSHNOWAIT, IntPtr.Zero, IntPtr.Zero);
            User32.SendMessageTimeout(HWND.HWND_BROADCAST, WINDOW_MESSAGE.WM_SETTINGCHANGE, UIntPtr.Zero, SafeMemoryHandle.Null, SEND_MESSAGE_TIMEOUT_FLAGS.SMTO_ABORTIFHUNG, 100, out _);
            using (var lpString = SafeHGlobalHandle.StringToUni("Environment"))
            {
                User32.SendMessageTimeout(HWND.HWND_BROADCAST, WINDOW_MESSAGE.WM_SETTINGCHANGE, UIntPtr.Zero, lpString, SEND_MESSAGE_TIMEOUT_FLAGS.SMTO_ABORTIFHUNG, 100, out _);
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
        internal static void MinimizeAllWindows() => User32.SendMessage(User32.FindWindow(Shell_TrayWnd, null), WINDOW_MESSAGE.WM_COMMAND, User32.MIN_ALL, IntPtr.Zero);

        /// <summary>
        /// Restores all minimized windows on the desktop to their previous state.
        /// </summary>
        /// <remarks>This method sends a system command to undo the "Minimize All Windows" action, effectively restoring all previously minimized windows. It has no effect if no windows are currently minimized.</remarks>
        internal static void RestoreAllWindows() => User32.SendMessage(User32.FindWindow(Shell_TrayWnd, null), WINDOW_MESSAGE.WM_COMMAND, User32.MIN_ALL_UNDO, IntPtr.Zero);

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

        /// <summary>
        /// Retrieves the process ID of the application associated with the current foreground window.
        /// </summary>
        /// <remarks>This method uses the Windows API to determine the process ID of the foreground
        /// window.  It is intended for internal use and may require appropriate permissions to access window
        /// information.</remarks>
        /// <returns>The process ID of the application owning the foreground window. Returns 0 if no foreground window is found.</returns>
        internal static uint GetForegroundWindowProcessId()
        {
            User32.GetWindowThreadProcessId(User32.GetForegroundWindow(), out var pid);
            return pid;
        }

        /// <summary>
        /// Retrieves the Application User Model ID (AUMID) for a specified process.
        /// </summary>
        /// <remarks>The Application User Model ID is used to uniquely identify an application in the
        /// Windows operating system.</remarks>
        /// <param name="hProcess">A handle to the process for which the Application User Model ID is retrieved. The handle must be valid and
        /// not closed.</param>
        /// <returns>The Application User Model ID of the specified process as a string.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="hProcess"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if <paramref name="hProcess"/> is closed or invalid.</exception>
        public static string GetApplicationUserModelId(SafeHandle hProcess)
        {
            if (hProcess == null)
            {
                throw new ArgumentNullException(nameof(hProcess), "Process handle cannot be null.");
            }
            if (hProcess.IsClosed)
            {
                throw new InvalidOperationException("Process handle is closed.");
            }
            if (hProcess.IsInvalid)
            {
                throw new InvalidOperationException("Process handle is invalid.");
            }
            Span<char> appUserModelId = stackalloc char[(int)APPX_IDENTITY.APPLICATION_USER_MODEL_ID_MAX_LENGTH]; var length = (uint)appUserModelId.Length;
            Kernel32.GetApplicationUserModelId(hProcess, ref length, appUserModelId);
            return appUserModelId.Slice(0, (int)length).ToString().TrimRemoveNull();
        }

        /// <summary>
        /// Retrieves the Application User Model ID (AUMID) for the specified process.
        /// </summary>
        /// <param name="process">The process for which to obtain the AUMID. Must not be null.</param>
        /// <returns>The Application User Model ID associated with the specified process.</returns>
        public static string GetApplicationUserModelId(Process process)
        {
            using (process.SafeHandle)
            {
                return GetApplicationUserModelId(process.SafeHandle);
            }
        }

        /// <summary>
        /// Retrieves the Application User Model ID (AUMID) for a specified process.
        /// </summary>
        /// <param name="processId">The ID of the process for which to retrieve the AUMID.</param>
        /// <returns>The Application User Model ID associated with the specified process.</returns>
        public static string GetApplicationUserModelId(uint processId)
        {
            using (var process = Process.GetProcessById((int)processId))
            {
                return GetApplicationUserModelId(process);
            }
        }

        /// <summary>
        /// Represents the class name of the Windows taskbar.
        /// </summary>
        /// <remarks>This constant is used to identify the taskbar window in Windows operating
        /// systems.</remarks>
        private const string Shell_TrayWnd = "Shell_TrayWnd";
    }
}
