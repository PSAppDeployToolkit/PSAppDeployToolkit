using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using PSADT.LibraryInterfaces;
using PSADT.OperatingSystem;
using Windows.Win32;
using Windows.Win32.UI.HiDpi;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;
using Windows.Win32.Foundation;

namespace PSADT.GUI
{
    /// <summary>
    /// Provides methods for interacting with UI automation on Windows.
    /// </summary>
    public static class UiAutomation
    {
        /// <summary>
        /// Enumerates all top-level windows on the screen.
        /// </summary>
        /// <returns>A list of window handles.</returns>
        public static List<IntPtr> EnumWindows()
        {
            List<IntPtr> windows = new List<IntPtr>();
            GCHandle hItems = GCHandle.Alloc(windows);

            try
            {
                IntPtr lItems = GCHandle.ToIntPtr(hItems);
                User32.EnumWindows((hWnd, lParam) =>
                {
                    if (hWnd != IntPtr.Zero)
                    {
                        GCHandle hItems = GCHandle.FromIntPtr(lItems);
                        if (hItems.Target is List<IntPtr> items)
                        {
                            items.Add(hWnd);
                            return true;
                        }
                    }
                    return false;
                }, lItems);
            }
            finally
            {
                if (hItems.IsAllocated)
                {
                    hItems.Free();
                }
            }
            return windows;
        }

        /// <summary>
        /// Retrieves the text of the specified window.
        /// </summary>
        /// <param name="hWnd">A handle to the window.</param>
        /// <returns>The window text.</returns>
        public static string? GetWindowText(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero)
            {
                throw new ArgumentNullException(nameof(hWnd), "Window handle cannot be zero.");
            }
            var hwnd = new HWND(hWnd);
            int textLength = User32.GetWindowTextLength(hwnd);
            if (textLength > 0)
            {
                var buffer = new char[textLength + 1];
                User32.GetWindowText(hwnd, buffer);
                var text = new string(buffer).TrimEnd('\0').Trim();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return text;
                }
            }
            return null;
        }

        /// <summary>
        /// Brings the specified window to the foreground.
        /// </summary>
        /// <param name="windowHandle">A handle to the window.</param>
        /// <returns>True if the window was brought to the foreground; otherwise, false.</returns>
        public static bool BringWindowToFront(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero)
            {
                throw new ArgumentNullException(nameof(hWnd), "Window handle cannot be zero.");
            }

            var hwnd = new HWND(hWnd);
            if (PInvoke.IsIconic(hwnd))
            {
                PInvoke.ShowWindow(hwnd, SHOW_WINDOW_CMD.SW_RESTORE);
            }

            uint currentThreadId = PInvoke.GetCurrentThreadId();
            uint windowThreadId = User32.GetWindowThreadProcessId((HWND)User32.GetForegroundWindow(), out _);
            User32.AttachThreadInput(currentThreadId, windowThreadId, true);
            try
            {
                User32.BringWindowToTop(hwnd);
                if (!PInvoke.SetForegroundWindow(hwnd))
                {
                    throw new InvalidOperationException($"Failed to set the window as foreground.");
                }
                User32.SetActiveWindow(hwnd);
                User32.SetFocus(hwnd);
                return true;
            }
            finally
            {
                User32.AttachThreadInput(currentThreadId, windowThreadId, false);
            }
        }

        /// <summary>
        /// Gets the process ID of the specified window.
        /// </summary>
        /// <param name="windowHandle">A handle to the window.</param>
        /// <returns>The process ID.</returns>
        public static uint GetWindowThreadProcessId(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero)
            {
                throw new ArgumentNullException(nameof(hWnd), "Window handle cannot be zero.");
            }
            User32.GetWindowThreadProcessId(new HWND(hWnd), out uint processId);
            return processId;
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
