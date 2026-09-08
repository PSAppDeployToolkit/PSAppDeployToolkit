using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using PSADT.Collections;

namespace PSADT.WindowManagement
{
    /// <summary>
    /// Represents filtering options for retrieving window information.
    /// </summary>
    /// <remarks>This record provides criteria for filtering windows based on their titles, handles, or parent
    /// processes. Any of the filters can be empty, indicating that the corresponding criterion should not be
    /// applied. <para> A filter that was not supplied is held and handed back as an empty list rather than as
    /// nothing, since a caller piping one in PowerShell gets an iteration out of nothing but none out of an empty
    /// list. Supplying one that is empty is still refused below: it would have meant matching nothing, and is far
    /// more likely to be a filter that lost its contents on the way in. </para></remarks>
    [DataContract]
    public sealed record class WindowInfoOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WindowInfoOptions"/> class with optional filters for window
        /// titles, handles, and parent processes.
        /// </summary>
        /// <param name="windowTitleRegex">An optional regular expression string to filter window titles. If <see langword="null"/>, no filtering is applied based on window titles.</param>
        /// <param name="windowHandleFilter">An optional array of native window handles (<see cref="nint"/>) to filter. If <see langword="null"/>, no
        /// filtering is applied based on window handles.</param>
        /// <param name="parentProcessFilter">An optional array of strings specifying parent process names to filter. If <see langword="null"/>, no
        /// filtering is applied based on parent processes.</param>
        /// <param name="parentProcessIdFilter">A list of parent process IDs to include in the filter. Only windows whose parent process ID matches any of
        /// these values will be considered. Can be null to disable parent process ID filtering.</param>
        /// <param name="parentProcessMainWindowHandleFilter">A list of main window handles for parent processes to include in the filter. Only windows whose parent
        /// process main window handle matches any of these values will be considered. Can be null to disable this
        /// filtering.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S3236:Caller information arguments should not be provided explicitly", Justification = "This is intentional as we're testing a parameter member.")]
        public WindowInfoOptions(string? windowTitleRegex, ReadOnlyCollection<nint>? windowHandleFilter, ReadOnlyCollection<string>? parentProcessFilter, ReadOnlyCollection<uint>? parentProcessIdFilter, ReadOnlyCollection<nint>? parentProcessMainWindowHandleFilter)
        {
            // Ensure list inputs are not empty if they're not null.
            if (windowTitleRegex is not null)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(windowTitleRegex);
            }
            if (windowHandleFilter is not null)
            {
                ArgumentOutOfRangeException.ThrowIfZero(windowHandleFilter.Count, nameof(windowHandleFilter));
            }
            if (parentProcessFilter is not null)
            {
                ArgumentOutOfRangeException.ThrowIfZero(parentProcessFilter.Count, nameof(parentProcessFilter));
            }
            if (parentProcessIdFilter is not null)
            {
                ArgumentOutOfRangeException.ThrowIfZero(parentProcessIdFilter.Count, nameof(parentProcessIdFilter));
            }
            if (parentProcessMainWindowHandleFilter is not null)
            {
                ArgumentOutOfRangeException.ThrowIfZero(parentProcessMainWindowHandleFilter.Count, nameof(parentProcessMainWindowHandleFilter));
            }

            // Assign read-only collections, empty where nothing was supplied.
            WindowTitleRegex = windowTitleRegex;
            WindowHandleFilterValues = windowHandleFilter?.Select(static h => (long)h) is { } windowHandleFilterList ? new(windowHandleFilterList) : EquatableList<long>.Empty;
            ParentProcessFilterValues = parentProcessFilter is { } parentProcessFilterList ? new(parentProcessFilterList) : EquatableList<string>.Empty;
            ParentProcessIdFilterValues = parentProcessIdFilter is { } parentProcessIdFilterList ? new(parentProcessIdFilterList) : EquatableList<uint>.Empty;
            ParentProcessMainWindowHandleFilterValues = parentProcessMainWindowHandleFilter?.Select(static h => (long)h) is { } parentProcessMainWindowHandleFilterList ? new(parentProcessMainWindowHandleFilterList) : EquatableList<long>.Empty;
        }

        /// <summary>
        /// Gets the filter criteria for window titles.
        /// </summary>
        [DataMember]
        public readonly string? WindowTitleRegex;

        /// <summary>
        /// Represents a filter for window handles used to determine which windows are included in certain operations.
        /// </summary>
        /// <remarks>This array contains the native integer (nint) values of window handles to be
        /// filtered. If it is empty, no filtering is applied.</remarks>
        [IgnoreDataMember]
        public IReadOnlyList<nint> WindowHandleFilter => new ReadOnlyCollection<nint>([.. WindowHandleFilterValues.Select(static v => (nint)v)]);

        /// <summary>
        /// Represents a filter for parent process names used to determine specific conditions or behaviors.
        /// </summary>
        /// <remarks>This array contains the names of parent processes that are used as a filter. If it is
        /// empty, no filtering is applied.</remarks>
        /// <remarks>Held as a <see cref="EquatableList{T}"/> so that this record compares by the list's contents. Every
        /// collection the framework offers compares by reference, so holding one directly would make two of these
        /// unequal however alike they were.</remarks>
        [IgnoreDataMember]
        public IReadOnlyList<string> ParentProcessFilter => ParentProcessFilterValues;

        /// <summary>
        /// Gets the list of parent process IDs to use as a filter when selecting processes.
        /// </summary>
        /// <remarks>If the list is empty, no filtering by parent process ID is applied. This property is
        /// read-only.</remarks>
        [IgnoreDataMember]
        public IReadOnlyList<uint> ParentProcessIdFilter => ParentProcessIdFilterValues;

        /// <summary>
        /// Gets the collection of main window handles used to filter parent processes.
        /// </summary>
        /// <remarks>This property provides a read-only list of native window handles (HWND) that are used
        /// to identify or filter parent processes based on their main window. The list may be empty if no filters are
        /// applied.</remarks>
        [IgnoreDataMember]
        public IReadOnlyList<nint> ParentProcessMainWindowHandleFilter => new ReadOnlyCollection<nint>([.. ParentProcessMainWindowHandleFilterValues.Select(static v => (nint)v)]);

        /// <summary>
        /// Gets the window handle filter values for serialization.
        /// </summary>
        [DataMember]
        private readonly EquatableList<long> WindowHandleFilterValues;

        /// <summary>
        /// Gets the parent process main window handle filter values for serialization.
        /// </summary>
        [DataMember]
        private readonly EquatableList<long> ParentProcessMainWindowHandleFilterValues;

        /// <summary>
        /// The list recorded for <see cref="ParentProcessFilter"/>.
        /// </summary>
        [DataMember]
        private readonly EquatableList<string> ParentProcessFilterValues;

        /// <summary>
        /// The list recorded for <see cref="ParentProcessIdFilter"/>.
        /// </summary>
        [DataMember]
        private readonly EquatableList<uint> ParentProcessIdFilterValues;
    }
}
