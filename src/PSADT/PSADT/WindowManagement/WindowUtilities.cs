using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using PSADT.LibraryInterfaces;

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
        /// <returns>A read-only list of <see cref="WindowInfo"/> objects containing details about the visible windows that match
        /// the specified filters. If no filters are provided, all visible windows are included.</returns>
        internal static ReadOnlyCollection<WindowInfo> GetProcessWindowInfo(IReadOnlyList<string>? windowTitleFilter = null, IReadOnlyList<nint>? windowHandleFilter = null, IReadOnlyList<string>? parentProcessFilter = null)
        {
            // Get the list of processes based on the provided filters.
            var processes = windowHandleFilter is not null && parentProcessFilter is not null ? Process.GetProcesses().Where(p => windowHandleFilter.Contains(p.MainWindowHandle) && parentProcessFilter.Contains(p.ProcessName)) :
                            windowHandleFilter is not null ? Process.GetProcesses().Where(p => windowHandleFilter.Contains(p.MainWindowHandle)) :
                            parentProcessFilter is not null ? Process.GetProcesses().Where(p => parentProcessFilter.Contains(p.ProcessName)) :
                            Process.GetProcesses();

            // Create a list to hold the window information.
            Regex? windowTitleRegex = windowTitleFilter is not null ? new(string.Join("|", windowTitleFilter.Select(static t => Regex.Escape(t))), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled) : null;
            List<WindowInfo> windowInfos = [];
            foreach (var window in WindowTools.EnumWindows())
            {
                // Continue if window isn't visible.
                if (!User32.IsWindowVisible(window))
                {
                    continue;
                }

                // Continue if the window doesn't have any text.
                if (WindowTools.GetWindowText(window) is not string windowText || string.IsNullOrWhiteSpace(windowText))
                {
                    continue;
                }

                // Continue if the visible window title doesn't match our filter.
                if (windowTitleRegex is not null && !windowTitleRegex.IsMatch(windowText))
                {
                    continue;
                }

                // Continue if the window doesn't have an associated process.
                var process = processes.FirstOrDefault(p => p.Id == WindowTools.GetWindowThreadProcessId(window));
                if (process is null)
                {
                    continue;
                }

                // Add the window information to the list.
                windowInfos.Add(new(windowText!, window, process.ProcessName, process.MainWindowHandle, process.Id));
            }

            // Return the list of window information.
            return windowInfos.AsReadOnly();
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
            return GetProcessWindowInfo(options.WindowTitleFilter, options.WindowHandleFilter, options.ParentProcessFilter);
        }
    }
}
