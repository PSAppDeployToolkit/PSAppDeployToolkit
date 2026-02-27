using System;
using System.Runtime.Serialization;
using PSADT.Interop.Extensions;

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
            WindowTitle = windowTitle.ThrowIfNullOrWhiteSpace();
            WindowHandle = windowHandle != default ? windowHandle : throw new ArgumentNullException(nameof(windowHandle), "Window handle cannot be null.");
            ParentProcess = parentProcess.ThrowIfNullOrWhiteSpace();
            ParentProcessId = parentProcessId > 0 ? parentProcessId : throw new ArgumentOutOfRangeException(nameof(parentProcessId), "Parent process ID must be greater than zero.");
            ParentProcessMainWindowHandle = parentProcessMainWindowHandle != default ? parentProcessMainWindowHandle : throw new ArgumentNullException(nameof(parentProcessMainWindowHandle), "Parent process main window handle cannot be null.");
        }

        /// <summary>
        /// Gets the title of the window.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly string WindowTitle;

        /// <summary>
        /// Gets the handle to the window.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly nint WindowHandle;

        /// <summary>
        /// Gets the name of the parent process that owns the window.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly string ParentProcess;

        /// <summary>
        /// Gets the ID of the parent process.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly int ParentProcessId;

        /// <summary>
        /// Gets the handle to the main window of the parent process.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly nint ParentProcessMainWindowHandle;
    }
}
