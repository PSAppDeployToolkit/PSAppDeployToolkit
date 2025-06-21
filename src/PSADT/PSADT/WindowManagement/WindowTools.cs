using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using PSADT.Extensions;
using PSADT.LibraryInterfaces;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace PSADT.WindowManagement
{
    internal static class WindowTools
    {
        /// <summary>
        /// Enumerates all top-level windows on the screen.
        /// </summary>
        /// <returns>A list of window handles.</returns>
        internal static ReadOnlyCollection<HWND> EnumWindows()
        {
            List<HWND> windows = [];
            GCHandle hItems = GCHandle.Alloc(windows);
            try
            {
                nint lItems = GCHandle.ToIntPtr(hItems);
                User32.EnumWindows((hWnd, lParam) =>
                {
                    if (hWnd != HWND.Null)
                    {
                        GCHandle hItems = GCHandle.FromIntPtr(lItems);
                        if (hItems.Target is List<nint> items)
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
            return windows.AsReadOnly();
        }

        /// <summary>
        /// Retrieves the text of the specified window.
        /// </summary>
        /// <param name="hWnd">A handle to the window.</param>
        /// <returns>The window text.</returns>
        internal static string? GetWindowText(nint hWnd)
        {
            if (hWnd == IntPtr.Zero)
            {
                throw new ArgumentNullException(nameof(hWnd), "Window handle cannot be zero.");
            }
            var hwnd = (HWND)hWnd;
            int textLength = User32.GetWindowTextLength(hwnd);
            if (textLength > 0)
            {
                Span<char> buffer = stackalloc char[textLength + 1];
                User32.GetWindowText(hwnd, buffer);
                var text = buffer.ToString().TrimRemoveNull();
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
        /// <param name="hWnd">A handle to the window.</param>
        /// <returns>True if the window was brought to the foreground; otherwise, false.</returns>
        internal static void BringWindowToFront(nint hWnd)
        {
            // Throw if we have a null or zero handle.
            if (hWnd == IntPtr.Zero)
            {
                throw new ArgumentNullException(nameof(hWnd), "Window handle cannot be zero.");
            }
            var hwnd = (HWND)hWnd;

            // Restore the window if it's minimized.
            if (PInvoke.IsIconic(hwnd))
            {
                PInvoke.ShowWindow(hwnd, SHOW_WINDOW_CMD.SW_RESTORE);
            }

            // Bring the window to the foreground.
            uint currentThreadId = PInvoke.GetCurrentThreadId();
            uint windowThreadId = User32.GetWindowThreadProcessId(hwnd, out _);
            User32.AttachThreadInput(currentThreadId, windowThreadId, true);
            try
            {
                User32.BringWindowToTop(hwnd);
                User32.SetForegroundWindow(hwnd);
                User32.SetActiveWindow(hwnd);
                User32.SetFocus(hwnd);
            }
            finally
            {
                User32.AttachThreadInput(currentThreadId, windowThreadId, false);
            }
        }

        /// <summary>
        /// Gets the process ID of the specified window.
        /// </summary>
        /// <param name="hWnd">A handle to the window.</param>
        /// <returns>The process ID.</returns>
        internal static uint GetWindowThreadProcessId(nint hWnd)
        {
            if (hWnd == IntPtr.Zero)
            {
                throw new ArgumentNullException(nameof(hWnd), "Window handle cannot be zero.");
            }
            User32.GetWindowThreadProcessId((HWND)hWnd, out uint processId);
            return processId;
        }
    }
}
