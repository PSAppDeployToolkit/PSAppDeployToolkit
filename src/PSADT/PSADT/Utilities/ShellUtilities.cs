using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using PSADT.Extensions;
using PSADT.LibraryInterfaces;
using PSADT.ProcessManagement;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using Windows.Win32.UI.Shell;

namespace PSADT.Utilities
{
    /// <summary>
    /// Provides methods for interacting with the Windows Explorer.
    /// </summary>
    public static class ShellUtilities
    {
        /// <summary>
        /// Notifies the system that file associations have changed and refreshes the desktop environment.
        /// </summary>
        /// <remarks>Call this method after making changes to file associations or related system settings
        /// to ensure that the desktop and shell reflect the updates. This method triggers a system-wide notification,
        /// which may cause open Explorer windows and desktop icons to refresh.</remarks>
        internal static void RefreshDesktop()
        {
            // Update desktop icons using SHChangeNotify. This covers the bulk of things.
            Shell32.SHChangeNotify(SHCNE_ID.SHCNE_ASSOCCHANGED, SHCNF_FLAGS.SHCNF_FLUSH, IntPtr.Zero, IntPtr.Zero);

            // Refresh the taskbar. See https://stackoverflow.com/questions/70260518/how-can-i-refresh-the-taskbar-programatically-in-windows-10-and-higher for details.
            _ = User32.SendNotifyMessage(HWND.HWND_BROADCAST, WINDOW_MESSAGE.WM_SETTINGCHANGE, null, "TraySettings");

            // Terminate the StartMenuExperienceHost to refresh the start menu. Windows restarts this process instantly.
            foreach (RunningProcessInfo runningProcessInfo in RunningProcessInfo.Get(new ProcessDefinition("StartMenuExperienceHost")))
            {
                using Process process = runningProcessInfo.Process;
                process.Kill();
            }
        }

        /// <summary>
        /// Refreshes the desktop icons and updates the environment variables in the system.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the operation fails.</exception>
        internal static void RefreshDesktopAndEnvironmentVariables()
        {
            // Update desktop icons using SHChangeNotify.
            RefreshDesktop();

            // Notify all top-level windows that the environment variables have changed.
            _ = User32.SendNotifyMessage(HWND.HWND_BROADCAST, WINDOW_MESSAGE.WM_SETTINGCHANGE, UIntPtr.Zero, IntPtr.Zero);
            _ = User32.SendNotifyMessage(HWND.HWND_BROADCAST, WINDOW_MESSAGE.WM_SETTINGCHANGE, null, "Environment");
        }

        /// <summary>
        /// Gets the user notification state.
        /// </summary>
        /// <returns>The user notification state.</returns>
        internal static LibraryInterfaces.QUERY_USER_NOTIFICATION_STATE GetUserNotificationState()
        {
            _ = Shell32.SHQueryUserNotificationState(out Windows.Win32.UI.Shell.QUERY_USER_NOTIFICATION_STATE state);
            return (LibraryInterfaces.QUERY_USER_NOTIFICATION_STATE)state;
        }

        /// <summary>
        /// Minimizes all open windows on the desktop.
        /// </summary>
        /// <remarks>This method sends a command to the system shell to minimize all currently open windows. It is equivalent to the "Show Desktop" functionality in Windows.</remarks>
        internal static void MinimizeAllWindows()
        {
            _ = User32.SendMessage(GetTrayWindowHandle(), WINDOW_MESSAGE.WM_COMMAND, User32.MIN_ALL, IntPtr.Zero);
        }

        /// <summary>
        /// Restores all minimized windows on the desktop to their previous state.
        /// </summary>
        /// <remarks>This method sends a system command to undo the "Minimize All Windows" action, effectively restoring all previously minimized windows. It has no effect if no windows are currently minimized.</remarks>
        internal static void RestoreAllWindows()
        {
            _ = User32.SendMessage(GetTrayWindowHandle(), WINDOW_MESSAGE.WM_COMMAND, User32.MIN_ALL_UNDO, IntPtr.Zero);
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
            _ = User32.GetWindowThreadProcessId(User32.GetShellWindow(), out uint pid);
            return pid;
        }

        /// <summary>
        /// Retrieves the process ID of the application associated with the current foreground window.
        /// </summary>
        /// <remarks>This method uses the Windows API to determine the process ID of the foreground
        /// window. It is intended for internal use and may require appropriate permissions to access window
        /// information.</remarks>
        /// <returns>The process ID of the application owning the foreground window. Returns 0 if no foreground window is found.</returns>
        internal static uint GetForegroundWindowProcessId()
        {
            _ = User32.GetWindowThreadProcessId(User32.GetForegroundWindow(), out uint pid);
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
        internal static string GetApplicationUserModelId(SafeHandle hProcess)
        {
            if (hProcess is null)
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
            Span<char> appUserModelId = stackalloc char[(int)APPX_IDENTITY.APPLICATION_USER_MODEL_ID_MAX_LENGTH]; uint length = (uint)appUserModelId.Length;
            _ = Kernel32.GetApplicationUserModelId(hProcess, ref length, appUserModelId);
            return appUserModelId.Slice(0, (int)length).ToString().TrimRemoveNull();
        }

        /// <summary>
        /// Retrieves the Application User Model ID (AUMID) for the specified process.
        /// </summary>
        /// <param name="process">The process for which to obtain the AUMID. Must not be null.</param>
        /// <returns>The Application User Model ID associated with the specified process.</returns>
        public static string GetApplicationUserModelId(Process process)
        {
            // We don't own the process, so don't dispose of its SafeHande as .NET caches it...
            return process is null
                ? throw new ArgumentNullException(nameof(process), "Process cannot be null.")
                : GetApplicationUserModelId(process.SafeHandle);
        }

        /// <summary>
        /// Retrieves the Application User Model ID (AUMID) for a specified process.
        /// </summary>
        /// <param name="processId">The ID of the process for which to retrieve the AUMID.</param>
        /// <returns>The Application User Model ID associated with the specified process.</returns>
        public static string GetApplicationUserModelId(uint processId)
        {
            using Process process = Process.GetProcessById((int)processId);
            return GetApplicationUserModelId(process);
        }

        /// <summary>
        /// Retrieves the time elapsed since the last user input event.
        /// </summary>
        /// <remarks>This method uses system-level APIs to determine the time of the last user input, such
        /// as keyboard or mouse activity. The returned value may be useful for detecting user inactivity or
        /// implementing idle timeouts.</remarks>
        /// <returns>A <see cref="TimeSpan"/> representing the duration since the last user input event. The value is calculated
        /// based on the system's tick count.</returns>
        internal static TimeSpan GetLastInputTime()
        {
            // Get the last input information using User32 API.
            _ = User32.GetLastInputInfo(out LASTINPUTINFO lastInputInfo);
            ulong now64 = PInvoke.GetTickCount64();
            ulong last32 = lastInputInfo.dwTime;

            // Project 32-bit last-input onto the 64-bit timeline.
            ulong base64 = now64 & ~0xFFFF_FFFFUL;
            ulong last64 = base64 | last32;
            if (last64 > now64)
            {
                last64 -= 1UL << 32;
            }
            return TimeSpan.FromMilliseconds(now64 - last64);
        }

        /// <summary>
        /// Retrieves the window handle for the Windows taskbar (system tray).
        /// </summary>
        /// <remarks>This method is intended for internal use when interacting with the Windows shell. The
        /// returned handle can be used with other Windows API functions that require a reference to the taskbar
        /// window.</remarks>
        /// <returns>A handle to the taskbar window, or <see cref="HWND.Null"/> if the taskbar is not found.</returns>
        private static HWND GetTrayWindowHandle()
        {
            return User32.FindWindow("Shell_TrayWnd", null);
        }
    }
}
