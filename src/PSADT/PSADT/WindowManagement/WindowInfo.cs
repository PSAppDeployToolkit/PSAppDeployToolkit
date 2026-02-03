using System;
using System.Text.Json.Serialization;

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
        /// <param name="parentProcessId">The ID of the parent process.</param>
        /// <param name="parentProcessMainWindowHandle">The handle to the main window of the parent process.</param>
        [JsonConstructor]
        internal WindowInfo(string windowTitle, IntPtr windowHandle, string parentProcess, int parentProcessId, IntPtr parentProcessMainWindowHandle)
        {
            WindowTitle = !string.IsNullOrWhiteSpace(windowTitle) ? windowTitle : throw new ArgumentNullException("Window title cannot be null or empty.", (Exception?)null);
            WindowHandle = windowHandle != IntPtr.Zero ? windowHandle : throw new ArgumentNullException("Window handle cannot be null.", (Exception?)null);
            ParentProcess = !string.IsNullOrWhiteSpace(parentProcess) ? parentProcess : throw new ArgumentNullException("Parent process name cannot be null or empty.", (Exception?)null);
            ParentProcessId = parentProcessId > 0 ? parentProcessId : throw new ArgumentNullException("Parent process ID cannot be null or empty.", (Exception?)null);
            ParentProcessMainWindowHandle = parentProcessMainWindowHandle != IntPtr.Zero ? parentProcessMainWindowHandle : throw new ArgumentNullException("Parent process main window handle cannot be null.", (Exception?)null);
        }

        /// <summary>
        /// Gets the title of the window.
        /// </summary>
        public string WindowTitle { get; }

        /// <summary>
        /// Gets the handle to the window.
        /// </summary>
        public IntPtr WindowHandle { get; }

        /// <summary>
        /// Gets the name of the parent process that owns the window.
        /// </summary>
        public string ParentProcess { get; }

        /// <summary>
        /// Gets the ID of the parent process.
        /// </summary>
        public int ParentProcessId { get; }

        /// <summary>
        /// Gets the handle to the main window of the parent process.
        /// </summary>
        public IntPtr ParentProcessMainWindowHandle { get; }
    }
}
