using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using PSADT.Interop;
using Windows.Win32.Foundation;

namespace PSADT.WindowManagement
{
    /// <summary>
    /// Provides methods for interacting with UI automation on Windows.
    /// </summary>
    internal static class WindowUtilities
    {
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ReadOnlyCollection<WindowInfo> GetProcessWindowInfo(WindowInfoOptions options)
        {
            return GetProcessWindowInfo(parentProcessFilter: options.ParentProcessFilter, parentProcessIdFilter: options.ParentProcessIdFilter, parentProcessMainWindowHandleFilter: options.ParentProcessMainWindowHandleFilter, windowTitleRegex: options.WindowTitleRegex, windowHandleFilter: options.WindowHandleFilter);
        }

        /// <summary>
        /// Retrieves information about visible windows associated with processes, filtered by optional criteria.
        /// </summary>
        /// <remarks>This method enumerates all visible windows and filters them based on the provided
        /// criteria. If multiple filters are specified, windows must satisfy all filters to be included in the
        /// results. The method returns an empty list if no windows match the specified filters.</remarks>
        /// <param name="parentProcesses">An optional array of <see cref="Process"/> objects to limit the search to specific parent processes.</param>
        /// <param name="parentProcessFilter">An optional array of process names to filter the results. Only windows associated with processes whose
        /// names match one or more of the specified names will be included.</param>
        /// <param name="parentProcessIdFilter">A collection of process IDs to filter parent processes. Only windows belonging to processes with IDs in this
        /// collection are included. If empty, no filtering by process ID is applied.</param>
        /// <param name="parentProcessMainWindowHandleFilter">A collection of main window handles to filter parent processes. Only windows belonging to processes whose
        /// main window handle is in this collection are included. If empty, no filtering by main window handle is
        /// applied.</param>
        /// <param name="windowTitleRegex">An optional regular expression pattern to filter windows by their titles. Only windows with titles that match the
        /// specified pattern will be included.</param>
        /// <param name="windowHandleFilter">An optional array of window handles (<see cref="nint"/>) to filter the results. Only windows with handles
        /// matching one or more of the specified handles will be included.</param>
        /// <returns>A read-only list of <see cref="WindowInfo"/> objects containing details about the visible windows that match
        /// the specified filters. If no filters are provided, all visible windows are included.</returns>
        internal static ReadOnlyCollection<WindowInfo> GetProcessWindowInfo(IReadOnlyList<Process>? parentProcesses = null, IReadOnlyList<string>? parentProcessFilter = null, IReadOnlyList<int>? parentProcessIdFilter = null, IReadOnlyList<nint>? parentProcessMainWindowHandleFilter = null, string? windowTitleRegex = null, IReadOnlyList<nint>? windowHandleFilter = null)
        {
            // Ensure list inputs are not empty if they're not null.
            if (parentProcesses is not null)
            {
                ArgumentOutOfRangeException.ThrowIfZero(parentProcesses.Count);
            }
            if (parentProcessFilter is not null)
            {
                ArgumentOutOfRangeException.ThrowIfZero(parentProcessFilter.Count);
            }
            if (parentProcessIdFilter is not null)
            {
                ArgumentOutOfRangeException.ThrowIfZero(parentProcessIdFilter.Count);
            }
            if (parentProcessMainWindowHandleFilter is not null)
            {
                ArgumentOutOfRangeException.ThrowIfZero(parentProcessMainWindowHandleFilter.Count);
            }
            if (windowTitleRegex is not null)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(windowTitleRegex);
            }
            if (windowHandleFilter is not null)
            {
                ArgumentOutOfRangeException.ThrowIfZero(windowHandleFilter.Count);
            }

            // Get the list of processes based on the provided filters and start finding applicable windows.
            IReadOnlyList<Process> processes = parentProcesses ?? [.. Process.GetProcesses().Where(p => p.MainWindowHandle != default && parentProcessFilter?.Any(f => f.Equals(p.ProcessName, StringComparison.OrdinalIgnoreCase)) != false && parentProcessIdFilter?.Contains(p.Id) != false && parentProcessMainWindowHandleFilter?.Contains(p.MainWindowHandle) != false)];
            List<WindowInfo> windows = []; Regex? windowTitleRegexObject = windowTitleRegex is not null ? new(windowTitleRegex, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled) : null;
            foreach (HWND windowHandle in windowHandleFilter is not null ? WindowTools.EnumWindows().Where(w => windowHandleFilter.Contains(w) && NativeMethods.IsWindowVisible(w)) : WindowTools.EnumWindows().Where(static w => NativeMethods.IsWindowVisible(w)))
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
                if (windowTitleRegexObject?.IsMatch(windowTitle) == false)
                {
                    continue;
                }
                windows.Add(new(windowTitle, windowHandle, parentProcess.ProcessName, parentProcess.Id, parentProcess.MainWindowHandle));
            }
            return windows.AsReadOnly();
        }
    }
}
