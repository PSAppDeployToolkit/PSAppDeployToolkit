using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using PSADT.LibraryInterfaces;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace PSADT.Utilities
{
    /// <summary>
    /// Provides methods for interacting with UI automation on Windows.
    /// </summary>
    public static class WindowUtilities
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
                var text = new string(buffer).Replace("\0", string.Empty).Trim();
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
    }
}
