using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using PSADT.LibraryInterfaces;
using Windows.Win32.Foundation;

namespace PSADT.WindowManagement
{
    /// <summary>
    /// Provides methods for interacting with UI automation on Windows.
    /// </summary>
    internal static class WindowUtilities
    {
        /// <summary>
        /// Retrieves information about visible windows associated with processes, filtered by optional criteria.
        /// </summary>
        /// <remarks>This method enumerates all visible windows and filters them based on the provided
        /// criteria. If multiple filters are specified, windows must satisfy all filters to be included in the
        /// results. The method returns an empty list if no windows match the specified filters.</remarks>
        /// <param name="windowTitleFilter">An optional array of strings representing window title patterns to filter the results. Only windows with
        /// titles matching one or more of the specified patterns will be included.</param>
        /// <param name="windowHandleFilter">An optional array of window handles (<see cref="nint"/>) to filter the results. Only windows with handles
        /// matching one or more of the specified handles will be included.</param>
        /// <param name="parentProcessFilter">An optional array of process names to filter the results. Only windows associated with processes whose
        /// names match one or more of the specified names will be included.</param>
        /// <param name="parentProcessIdFilter">A collection of process IDs to filter parent processes. Only windows belonging to processes with IDs in this
        /// collection are included. If empty, no filtering by process ID is applied.</param>
        /// <param name="parentProcessMainWindowHandleFilter">A collection of main window handles to filter parent processes. Only windows belonging to processes whose
        /// main window handle is in this collection are included. If empty, no filtering by main window handle is
        /// applied.</param>
        /// <returns>A read-only list of <see cref="WindowInfo"/> objects containing details about the visible windows that match
        /// the specified filters. If no filters are provided, all visible windows are included.</returns>
        internal static ReadOnlyCollection<WindowInfo> GetProcessWindowInfo(IReadOnlyList<string>? windowTitleFilter = null, IReadOnlyList<nint>? windowHandleFilter = null, IReadOnlyList<string>? parentProcessFilter = null, IReadOnlyList<int>? parentProcessIdFilter = null, IReadOnlyList<nint>? parentProcessMainWindowHandleFilter = null)
        {
            // Get the list of processes based on the provided filters and start finding applicable windows.
            Process[] processes = [.. Process.GetProcesses().Where(p => p.MainWindowHandle != IntPtr.Zero && parentProcessFilter?.Any(f => f.Equals(p.ProcessName, StringComparison.OrdinalIgnoreCase)) != false && parentProcessIdFilter?.Contains(p.Id) != false && parentProcessMainWindowHandleFilter?.Contains(p.MainWindowHandle) != false)];
            List<WindowInfo> windows = []; Regex? windowTitleRegex = windowTitleFilter is not null ? new(string.Join("|", windowTitleFilter.Select(static t => Regex.Escape(t))), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled) : null;
            foreach (HWND windowHandle in windowHandleFilter is not null ? WindowTools.EnumWindows().Where(w => windowHandleFilter.Contains(w) && User32.IsWindowVisible(w)) : WindowTools.EnumWindows().Where(static w => User32.IsWindowVisible(w)))
            {
                // Return early if we can't find a process for this window.
                uint parentProcessId = WindowTools.GetWindowThreadProcessId(windowHandle);
                if (processes.FirstOrDefault(p => p.Id == parentProcessId) is not Process parentProcess)
                {
                    continue;
                }

                // Continue if the window doesn't have any text.
                if (WindowTools.GetWindowText(windowHandle) is not string windowTitle)
                {
                    continue;
                }

                // Continue if the visible window title doesn't match our filter.
                if (windowTitleRegex?.IsMatch(windowTitle) == false)
                {
                    continue;
                }
                windows.Add(new(windowTitle, windowHandle, parentProcess.ProcessName, parentProcess.Id, parentProcess.MainWindowHandle));
            }
            return windows.AsReadOnly();
        }

        /// <summary>
        /// Retrieves information about windows associated with a process, filtered by the specified options.
        /// </summary>
        /// <remarks>This method allows filtering windows based on specific criteria provided in the
        /// <paramref name="options"/> parameter. Use this method to retrieve detailed information about windows
        /// associated with a process, such as their titles, handles, and parent processes.</remarks>
        /// <param name="options">An object containing filtering criteria for the windows to retrieve, including window title, handle, and
        /// parent process filters.</param>
        /// <returns>A read-only list of <see cref="WindowInfo"/> objects representing the windows that match the specified
        /// filters. The list will be empty if no windows match the criteria.</returns>
        internal static ReadOnlyCollection<WindowInfo> GetProcessWindowInfo(WindowInfoOptions options)
        {
            return GetProcessWindowInfo(options.WindowTitleFilter, options.WindowHandleFilter, options.ParentProcessFilter, options.ParentProcessIdFilter, options.ParentProcessMainWindowHandleFilter);
        }
    }
}
