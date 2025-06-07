using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using PSADT.Extensions;
using PSADT.LibraryInterfaces;
using PSADT.Types;
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
                    if (hWnd != HWND.Null)
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
        public static void BringWindowToFront(IntPtr hWnd)
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
        public static uint GetWindowThreadProcessId(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero)
            {
                throw new ArgumentNullException(nameof(hWnd), "Window handle cannot be zero.");
            }
            User32.GetWindowThreadProcessId((HWND)hWnd, out uint processId);
            return processId;
        }

        /// <summary>
        /// Retrieves information about visible windows associated with processes, filtered by optional criteria.
        /// </summary>
        /// <remarks>This method enumerates all visible windows and filters them based on the provided
        /// criteria. If multiple filters are specified, windows must satisfy all filters to be included in the
        /// results. The method returns an empty list if no windows match the specified filters.</remarks>
        /// <param name="windowTitleFilter">An optional array of strings representing window title patterns to filter the results. Only windows with
        /// titles matching one or more of the specified patterns will be included.</param>
        /// <param name="windowHandleFilter">An optional array of window handles (<see cref="IntPtr"/>) to filter the results. Only windows with handles
        /// matching one or more of the specified handles will be included.</param>
        /// <param name="parentProcessFilter">An optional array of process names to filter the results. Only windows associated with processes whose
        /// names match one or more of the specified names will be included.</param>
        /// <returns>A read-only list of <see cref="WindowInfo"/> objects containing details about the visible windows that match
        /// the specified filters. If no filters are provided, all visible windows are included.</returns>
        public static IReadOnlyList<WindowInfo> GetProcessWindowInfo(string[]? windowTitleFilter = null, IntPtr[]? windowHandleFilter = null, string[]? parentProcessFilter = null)
        {
            // Get the list of processes based on the provided filters.
            var processes = (null != windowHandleFilter) && (null != parentProcessFilter) ? Process.GetProcesses().Where(p => windowHandleFilter.Contains(p.MainWindowHandle) && parentProcessFilter.Contains(p.ProcessName)) :
                            (null != windowHandleFilter) ? Process.GetProcesses().Where(p => windowHandleFilter.Contains(p.MainWindowHandle)) :
                            (null != parentProcessFilter) ? Process.GetProcesses().Where(p => parentProcessFilter.Contains(p.ProcessName)) :
                            Process.GetProcesses();

            // Create a list to hold the window information.
            Regex? windowTitleRegex = null != windowTitleFilter ? new(string.Join("|", windowTitleFilter), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled) : null;
            List<WindowInfo> windowInfos = new List<WindowInfo>();
            foreach (var window in EnumWindows())
            {
                // Continue if window isn't visible.
                if (!User32.IsWindowVisible((HWND)window))
                {
                    continue;
                }

                // Continue if the window doesn't have any text.
                string? windowText = GetWindowText(window);
                if (string.IsNullOrWhiteSpace(windowText))
                {
                    continue;
                }

                // Continue if the visible window title doesn't match our filter.
                if ((null != windowTitleRegex) && !windowTitleRegex.IsMatch(windowText))
                {
                    continue;
                }

                // Continue if the window doesn't have an associated process.
                var process = processes.FirstOrDefault(p => p.Id == GetWindowThreadProcessId(window));
                if (null == process)
                {
                    continue;
                }

                // Add the window information to the list.
                windowInfos.Add(new(windowText!, window, process.ProcessName, process.MainWindowHandle, process.Id));
            }

            // Return the list of window information.
            return windowInfos.AsReadOnly();
        }
    }
}
