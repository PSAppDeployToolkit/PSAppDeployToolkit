using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Newtonsoft.Json;

namespace PSADT.WindowManagement
{
    /// <summary>
    /// Represents filtering options for retrieving window information.
    /// </summary>
    /// <remarks>This record provides criteria for filtering windows based on their titles, handles, or parent
    /// processes. Any of the filters can be null, indicating that the corresponding criterion should not be
    /// applied.</remarks>
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
        [JsonConstructor]
        public WindowInfoOptions(IReadOnlyList<string>? windowTitleFilter = null, IReadOnlyList<nint>? windowHandleFilter = null, IReadOnlyList<string>? parentProcessFilter = null)
        {
            WindowTitleFilter = windowTitleFilter?.Count > 0 ? new ReadOnlyCollection<string>(windowTitleFilter.ToArray()) : null;
            WindowHandleFilter = windowHandleFilter?.Count > 0 ? new ReadOnlyCollection<nint>(windowHandleFilter.ToArray()) : null;
            ParentProcessFilter = parentProcessFilter?.Count > 0 ? new ReadOnlyCollection<string>(parentProcessFilter.ToArray()) : null;
        }

        /// <summary>
        /// Gets the filter criteria for window titles.
        /// </summary>
        [JsonProperty]
        public IReadOnlyList<string>? WindowTitleFilter { get; }

        /// <summary>
        /// Represents a filter for window handles used to determine which windows are included in certain operations.
        /// </summary>
        /// <remarks>This array contains the native integer (nint) values of window handles to be
        /// filtered. If the array is <see langword="null"/>, no filtering is applied.</remarks>
        [JsonProperty]
        public IReadOnlyList<nint>? WindowHandleFilter { get; }

        /// <summary>
        /// Represents a filter for parent process names used to determine specific conditions or behaviors.
        /// </summary>
        /// <remarks>This array contains the names of parent processes that are used as a filter. If the
        /// array is null or empty,  no filtering is applied. This member is intended for internal use and should not be
        /// accessed directly.</remarks>
        [JsonProperty]
        public IReadOnlyList<string>? ParentProcessFilter { get; }
    }
}
