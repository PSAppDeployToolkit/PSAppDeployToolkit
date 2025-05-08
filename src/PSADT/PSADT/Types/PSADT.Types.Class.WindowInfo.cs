using System;

namespace PSADT.Types
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
        public WindowInfo(string windowTitle, IntPtr windowHandle, string? parentProcess, IntPtr parentProcessMainWindowHandle, int parentProcessId)
        {
            WindowTitle = windowTitle ?? throw new ArgumentNullException(nameof(windowTitle));
            WindowHandle = windowHandle;
            ParentProcess = parentProcess;
            ParentProcessMainWindowHandle = parentProcessMainWindowHandle;
            ParentProcessId = parentProcessId >= 0 ? parentProcessId : throw new ArgumentOutOfRangeException(nameof(parentProcessId), "Process ID must be a non-negative number.");
        }

        /// <summary>
        /// Gets the title of the window.
        /// </summary>
        public readonly string WindowTitle;

        /// <summary>
        /// Gets the handle to the window.
        /// </summary>
        public readonly IntPtr WindowHandle;

        /// <summary>
        /// Gets the name of the parent process that owns the window.
        /// </summary>
        public readonly string? ParentProcess;

        /// <summary>
        /// Gets the handle to the main window of the parent process.
        /// </summary>
        public readonly IntPtr ParentProcessMainWindowHandle;

        /// <summary>
        /// Gets the ID of the parent process.
        /// </summary>
        public readonly int ParentProcessId;

        /// <summary>
        /// Provides a string representation of the <see cref="WindowInfo"/> struct.
        /// </summary>
        /// <returns>A string representing the window information.</returns>
        public override string ToString()
        {
            return $@"Window Title: {WindowTitle}, 
                      Window Handle: {WindowHandle}, 
                      Parent Process Name: {ParentProcess ?? "[Unknown]"}, 
                      Parent Process Handle: {ParentProcessMainWindowHandle}, 
                      Parent Process ID: {ParentProcessId}";
        }
    }
}
