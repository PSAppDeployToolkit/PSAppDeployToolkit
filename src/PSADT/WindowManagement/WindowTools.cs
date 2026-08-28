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
            _ = NativeMethods.EnumWindows((hWnd, _) =>
            {
                if (hWnd != HWND.Null)
                {
                    windows.Add(hWnd);
                }
                return true;
            });
            return windows.AsReadOnly();
        }

        /// <summary>
        /// Determines whether the specified handle still identifies an existing window.
        /// </summary>
        /// <remarks>A window can be destroyed at any moment, including between being enumerated and being
        /// asked about, so a handle obtained a moment ago is not necessarily one any more. Unlike the other members
        /// here this one does not refuse a handle of nothing: not being a window is precisely the answer it exists to
        /// give, and a caller checking a handle before using it has no reason to check it for null first.</remarks>
        /// <param name="hWnd">A handle to test.</param>
        /// <returns><see langword="true"/> if the handle identifies an existing window; otherwise, <see langword="false"/>.</returns>
        internal static bool IsWindow(HWND hWnd)
        {
            return PInvoke.IsWindow(hWnd);
        }

        /// <summary>
        /// Retrieves the text of the specified window.
        /// </summary>
        /// <param name="hWnd">A handle to the window.</param>
        /// <returns>The window text.</returns>
        internal static string? GetWindowText(HWND hWnd)
        {
            int textLength = NativeMethods.GetWindowTextLength(hWnd);
            if (textLength > 0)
            {
                Span<char> buffer = stackalloc char[textLength + 1];
                string text = buffer[..NativeMethods.GetWindowText(hWnd, buffer)].Trim().ToString();
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

            // AttachThreadInput fails with ERROR_INVALID_PARAMETER when the two threads are the
            // same (a window we own, e.g. a dialog raising itself from its own Loaded handler),
            // so only attach across threads while bringing the window handle to the foreground.
            uint currentThreadId = PInvoke.GetCurrentThreadId();
            uint windowThreadId = NativeMethods.GetWindowThreadProcessId(hWnd, out _);
            bool attachInput = windowThreadId != 0 && windowThreadId != currentThreadId;
            if (attachInput)
            {
                _ = NativeMethods.AttachThreadInput(currentThreadId, windowThreadId, fAttach: true);
            }
            try
            {
                _ = NativeMethods.BringWindowToTop(hWnd);
                _ = NativeMethods.SetForegroundWindow(hWnd, noThrowOnFailure: true);
                _ = NativeMethods.SetActiveWindow(hWnd);
                _ = NativeMethods.SetFocus(hWnd);
            }
            finally
            {
                if (attachInput)
                {
                    _ = NativeMethods.AttachThreadInput(currentThreadId, windowThreadId, fAttach: false);
                }
            }
        }

        /// <summary>
        /// Gets the process ID of the specified window.
        /// </summary>
        /// <param name="hWnd">A handle to the window.</param>
        /// <returns>The process ID.</returns>
        internal static uint GetWindowThreadProcessId(HWND hWnd)
        {
            _ = NativeMethods.GetWindowThreadProcessId(hWnd, out uint processId);
            return processId;
        }
    }
}
