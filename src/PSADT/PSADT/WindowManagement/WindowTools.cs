using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using PSADT.Interop;
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
            _ = NativeMethods.EnumWindows((hWnd, lParam) =>
            {
                if (hWnd != HWND.Null)
                {
                    windows.Add(hWnd);
                }
                return true;
            }, null);
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
            int textLength = NativeMethods.GetWindowTextLength(hWnd);
            if (textLength > 0)
            {
                Span<char> buffer = stackalloc char[textLength + 1];
                string text = buffer.Slice(0, NativeMethods.GetWindowText(hWnd, buffer)).Trim().ToString();
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

            // Minimise the window first to ensure it comes to the front.
            if (!PInvoke.IsIconic(hWnd))
            {
                _ = PInvoke.ShowWindow(hWnd, Windows.Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD.SW_MINIMIZE);
            }

            // Restore the window if it's minimized.
            if (PInvoke.IsIconic(hWnd))
            {
                _ = PInvoke.ShowWindow(hWnd, Windows.Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD.SW_RESTORE);
            }

            // Bring the window to the foreground.
            uint currentThreadId = PInvoke.GetCurrentThreadId();
            uint windowThreadId = NativeMethods.GetWindowThreadProcessId(hWnd, out _);
            _ = NativeMethods.AttachThreadInput(currentThreadId, windowThreadId, true);
            try
            {
                _ = NativeMethods.BringWindowToTop(hWnd);
                _ = NativeMethods.SetForegroundWindow(hWnd, true);
                _ = NativeMethods.SetActiveWindow(hWnd);
                _ = NativeMethods.SetFocus(hWnd);
            }
            finally
            {
                _ = NativeMethods.AttachThreadInput(currentThreadId, windowThreadId, false);
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
            _ = NativeMethods.GetWindowThreadProcessId(hWnd, out uint processId);
            return processId;
        }
    }
}
