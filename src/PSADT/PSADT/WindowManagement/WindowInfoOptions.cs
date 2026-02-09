using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;

namespace PSADT.WindowManagement
{
    /// <summary>
    /// Represents filtering options for retrieving window information.
    /// </summary>
    /// <remarks>This record provides criteria for filtering windows based on their titles, handles, or parent
    /// processes. Any of the filters can be null, indicating that the corresponding criterion should not be
    /// applied.</remarks>
    [DataContract]
    public sealed record WindowInfoOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WindowInfoOptions"/> class with optional filters for window
        /// titles, handles, and parent processes.
        /// </summary>
        /// <param name="windowTitleFilter">An optional array of strings specifying window titles to filter. If <see langword="null"/>, no filtering is
        /// applied based on window titles.</param>
        /// <param name="windowHandleFilter">An optional array of native window handles (<see cref="nint"/>) to filter. If <see langword="null"/>, no
        /// filtering is applied based on window handles.</param>
        /// <param name="parentProcessFilter">An optional array of strings specifying parent process names to filter. If <see langword="null"/>, no
        /// filtering is applied based on parent processes.</param>
        /// <param name="parentProcessIdFilter">A list of parent process IDs to include in the filter. Only windows whose parent process ID matches any of
        /// these values will be considered. Can be null to disable parent process ID filtering.</param>
        /// <param name="parentProcessMainWindowHandleFilter">A list of main window handles for parent processes to include in the filter. Only windows whose parent
        /// process main window handle matches any of these values will be considered. Can be null to disable this
        /// filtering.</param>
        public WindowInfoOptions(IReadOnlyList<string>? windowTitleFilter, IReadOnlyList<nint>? windowHandleFilter, IReadOnlyList<string>? parentProcessFilter, IReadOnlyList<int> parentProcessIdFilter, IReadOnlyList<nint> parentProcessMainWindowHandleFilter)
        {
            // Ensure list inputs are not empty if they're not null.
            if (windowTitleFilter?.Count == 0)
            {
                throw new ArgumentException("The window title filter list cannot be empty.", nameof(windowTitleFilter));
            }
            if (windowHandleFilter?.Count == 0)
            {
                throw new ArgumentException("The window handle filter list cannot be empty.", nameof(windowHandleFilter));
            }
            if (parentProcessFilter?.Count == 0)
            {
                throw new ArgumentException("The parent process filter list cannot be empty.", nameof(parentProcessFilter));
            }
            if (parentProcessIdFilter?.Count == 0)
            {
                throw new ArgumentException("The parent process ID filter list cannot be empty.", nameof(parentProcessIdFilter));
            }
            if (parentProcessMainWindowHandleFilter?.Count == 0)
            {
                throw new ArgumentException("The parent process main window handle filter list cannot be empty.", nameof(parentProcessMainWindowHandleFilter));
            }

            // Assign read-only collections or null based on input.
            WindowTitleFilter = windowTitleFilter?.Count > 0 ? new ReadOnlyCollection<string>([.. windowTitleFilter]) : null;
            WindowHandleFilter = windowHandleFilter?.Count > 0 ? new ReadOnlyCollection<nint>([.. windowHandleFilter]) : null;
            ParentProcessFilter = parentProcessFilter?.Count > 0 ? new ReadOnlyCollection<string>([.. parentProcessFilter]) : null;
            ParentProcessIdFilter = parentProcessIdFilter?.Count > 0 ? new ReadOnlyCollection<int>([.. parentProcessIdFilter]) : null;
            ParentProcessMainWindowHandleFilter = parentProcessMainWindowHandleFilter?.Count > 0 ? new ReadOnlyCollection<nint>([.. parentProcessMainWindowHandleFilter]) : null;
        }

        /// <summary>
        /// Gets the filter criteria for window titles.
        /// </summary>
        [DataMember]
        public IReadOnlyList<string>? WindowTitleFilter { get; private set; }

        /// <summary>
        /// Represents a filter for window handles used to determine which windows are included in certain operations.
        /// </summary>
        /// <remarks>This array contains the native integer (nint) values of window handles to be
        /// filtered. If the array is <see langword="null"/>, no filtering is applied.</remarks>
        [DataMember]
        public IReadOnlyList<nint>? WindowHandleFilter { get; private set; }

        /// <summary>
        /// Represents a filter for parent process names used to determine specific conditions or behaviors.
        /// </summary>
        /// <remarks>This array contains the names of parent processes that are used as a filter. If the
        /// array is null or empty, no filtering is applied. This member is intended for internal use and should not be
        /// accessed directly.</remarks>
        [DataMember]
        public IReadOnlyList<string>? ParentProcessFilter { get; private set; }

        /// <summary>
        /// Gets the list of parent process IDs to use as a filter when selecting processes.
        /// </summary>
        /// <remarks>If the list is empty, no filtering by parent process ID is applied. This property is
        /// read-only.</remarks>
        [DataMember]
        public IReadOnlyList<int>? ParentProcessIdFilter { get; private set; }

        /// <summary>
        /// Gets the collection of main window handles used to filter parent processes.
        /// </summary>
        /// <remarks>This property provides a read-only list of native window handles (HWND) that are used
        /// to identify or filter parent processes based on their main window. The list may be empty if no filters are
        /// applied.</remarks>
        [DataMember]
        public IReadOnlyList<nint>? ParentProcessMainWindowHandleFilter { get; private set; }
    }
}
