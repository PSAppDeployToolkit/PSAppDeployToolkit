using System;
using System.Runtime.Serialization;

namespace PSADT.WindowManagement
{
    /// <summary>
    /// Represents information about a window in the system.
    /// </summary>
    [DataContract]
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
        internal WindowInfo(string windowTitle, nint windowHandle, string parentProcess, int parentProcessId, nint parentProcessMainWindowHandle)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(windowTitle);
            ArgumentException.ThrowIfNullOrWhiteSpace(parentProcess);
            WindowTitle = windowTitle;
            WindowHandle = windowHandle;
            ParentProcess = parentProcess;
            ParentProcessId = parentProcessId;
            ParentProcessMainWindowHandle = parentProcessMainWindowHandle;
        }

        /// <summary>
        /// Gets the title of the window.
        /// </summary>
        [DataMember]
        public readonly string WindowTitle;

        /// <summary>
        /// Gets the handle to the window.
        /// </summary>
        [DataMember]
        public readonly nint WindowHandle;

        /// <summary>
        /// Gets the name of the parent process that owns the window.
        /// </summary>
        [DataMember]
        public readonly string ParentProcess;

        /// <summary>
        /// Gets the ID of the parent process.
        /// </summary>
        [DataMember]
        public readonly int ParentProcessId;

        /// <summary>
        /// Gets the handle to the main window of the parent process.
        /// </summary>
        [DataMember]
        public readonly nint ParentProcessMainWindowHandle;
    }
}
