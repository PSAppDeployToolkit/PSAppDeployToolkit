using System;
using System.Runtime.Serialization;
using PSADT.Serialization;

namespace PSADT.WindowManagement
{
    /// <summary>
    /// Represents information about a window in the system.
    /// </summary>
    [DataContract]
    public sealed record WindowInfo
    {
        /// <summary>
        /// Initializes the <see cref="WindowInfo"/> class and registers it as a serializable type.
        /// </summary>
        /// <remarks>This static constructor ensures that the <see cref="WindowInfo"/> type is added
        /// to the list of serializable types for data contract serialization. This allows instances of <see
        /// cref="ClientException"/> to be serialized and deserialized using data contract serializers.</remarks>
        static WindowInfo()
        {
            DataContractSerialization.AddSerializableType(typeof(WindowInfo));
        }

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
        [DataMember]
        public readonly string WindowTitle;

        /// <summary>
        /// Gets the handle to the window.
        /// </summary>
        [DataMember]
        public readonly IntPtr WindowHandle;

        /// <summary>
        /// Gets the name of the parent process that owns the window.
        /// </summary>
        [DataMember]
        public readonly string? ParentProcess;

        /// <summary>
        /// Gets the handle to the main window of the parent process.
        /// </summary>
        [DataMember]
        public readonly IntPtr ParentProcessMainWindowHandle;

        /// <summary>
        /// Gets the ID of the parent process.
        /// </summary>
        [DataMember]
        public readonly int ParentProcessId;
    }
}
