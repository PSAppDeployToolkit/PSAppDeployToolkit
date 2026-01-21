using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using PSADT.Extensions;
using PSADT.LibraryInterfaces;
using Windows.Win32;
using Windows.Win32.Foundation;

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
            _ = User32.EnumWindows((hWnd, lParam) =>
            {
                if (hWnd != HWND.Null)
                {
                    windows.Add(hWnd);
                }
                return true;
            }, IntPtr.Zero);
            return windows.AsReadOnly();
        }

        /// <summary>
        /// Retrieves the text of the specified window.
        /// </summary>
        /// <param name="hWnd">A handle to the window.</param>
        /// <returns>The window text.</returns>
        internal static string? GetWindowText(HWND hWnd)
        {
            if (hWnd.IsNull)
            {
                throw new ArgumentNullException(nameof(hWnd), "Window handle cannot be zero.");
            }
            int textLength = User32.GetWindowTextLength(hWnd);
            if (textLength > 0)
            {
                Span<char> buffer = stackalloc char[textLength + 1];
                int len = User32.GetWindowText(hWnd, buffer);
                string text = buffer.Slice(0, len).ToString().TrimRemoveNull();
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
        internal static void BringWindowToFront(HWND hWnd)
        {
            // Throw if we have a null or zero handle.
            if (hWnd.IsNull)
            {
                throw new ArgumentNullException(nameof(hWnd), "Window handle cannot be zero.");
            }

            // Restore the window if it's minimized.
            if (PInvoke.IsIconic(hWnd))
            {
                _ = PInvoke.ShowWindow(hWnd, Windows.Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD.SW_RESTORE);
            }

            // Bring the window to the foreground.
            uint currentThreadId = PInvoke.GetCurrentThreadId();
            uint windowThreadId = User32.GetWindowThreadProcessId(hWnd, out _);
            _ = User32.AttachThreadInput(currentThreadId, windowThreadId, true);
            try
            {
                _ = User32.BringWindowToTop(hWnd);
                _ = User32.SetForegroundWindow(hWnd);
                _ = User32.SetActiveWindow(hWnd);
                _ = User32.SetFocus(hWnd);
            }
            finally
            {
                _ = User32.AttachThreadInput(currentThreadId, windowThreadId, false);
            }
        }

        /// <summary>
        /// Gets the process ID of the specified window.
        /// </summary>
        /// <param name="hWnd">A handle to the window.</param>
        /// <returns>The process ID.</returns>
        internal static uint GetWindowThreadProcessId(HWND hWnd)
        {
            if (hWnd.IsNull)
            {
                throw new ArgumentNullException(nameof(hWnd), "Window handle cannot be zero.");
            }
            _ = User32.GetWindowThreadProcessId(hWnd, out uint processId);
            return processId;
        }
    }
}
