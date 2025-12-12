using System;
using Newtonsoft.Json;

namespace PSADT.WindowManagement
{
    /// <summary>
    /// Represents information about a window in the system.
    /// </summary>
    public sealed record WindowInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WindowInfo"/> struct.
        /// </summary>
        /// <param name="windowTitle">The title of the window.</param>
        /// <param name="windowHandle">The handle to the window.</param>
        /// <param name="parentProcess">The name of the parent process that owns the window.</param>
        /// <param name="parentProcessMainWindowHandle">The handle to the main window of the parent process.</param>
        /// <param name="parentProcessId">The ID of the parent process.</param>
        [JsonConstructor]
        internal WindowInfo(string windowTitle, IntPtr windowHandle, string? parentProcess, IntPtr parentProcessMainWindowHandle, int parentProcessId)
        {
            WindowTitle = !string.IsNullOrWhiteSpace(windowTitle) ? windowTitle : throw new ArgumentNullException("Window title cannot be null or empty.", (Exception?)null);
            WindowHandle = windowHandle;
            ParentProcess = parentProcess;
            ParentProcessMainWindowHandle = parentProcessMainWindowHandle;
            ParentProcessId = parentProcessId >= 0 ? parentProcessId : throw new ArgumentOutOfRangeException("Process ID must be a non-negative number.", (Exception?)null);
        }

        /// <summary>
        /// Gets the title of the window.
        /// </summary>
        [JsonProperty]
        public string WindowTitle { get; }

        /// <summary>
        /// Gets the handle to the window.
        /// </summary>
        [JsonProperty]
        public IntPtr WindowHandle { get; }

        /// <summary>
        /// Gets the name of the parent process that owns the window.
        /// </summary>
        [JsonProperty]
        public string? ParentProcess { get; }

        /// <summary>
        /// Gets the handle to the main window of the parent process.
        /// </summary>
        [JsonProperty]
        public IntPtr ParentProcessMainWindowHandle { get; }

        /// <summary>
        /// Gets the ID of the parent process.
        /// </summary>
        [JsonProperty]
        public int ParentProcessId { get; }
    }
}
