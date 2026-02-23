using System;
using System.Diagnostics;
using PSADT.Interop;

namespace PSADT.FileSystem
{
    /// <summary>
    /// Represents information about a file handle.
    /// </summary>
    public sealed record FileHandleInfo
    {
        /// <summary>
        /// Initializes a new instance of the FileHandleInfo class using the specified handle information and file
        /// paths.
        /// </summary>
        /// <param name="handleInfo">The handle information associated with the file, represented as a SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX
        /// structure.</param>
        /// <param name="filePath">The full path to the file. This value cannot be null or empty.</param>
        /// <param name="ntPath">The NT path of the file. This value cannot be null or empty.</param>
        /// <param name="handleType">The type of the handle. This value cannot be null or empty.</param>
        /// <exception cref="ArgumentNullException">Thrown if the filePath, ntPath, or handleType parameter is null or empty.</exception>
        internal FileHandleInfo(in SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX handleInfo, string filePath, string ntPath, string handleType)
        {
            ProcessName = Process.GetProcessById((int)handleInfo.UniqueProcessId).ProcessName;
            FilePath = !string.IsNullOrWhiteSpace(filePath) ? filePath : throw new ArgumentNullException("File path cannot be null or empty.", (Exception?)null);
            NtPath = !string.IsNullOrWhiteSpace(ntPath) ? ntPath : throw new ArgumentNullException("NT path cannot be null or empty.", (Exception?)null);
            HandleType = !string.IsNullOrWhiteSpace(handleType) ? handleType : throw new ArgumentNullException("Handle type cannot be null or empty.", (Exception?)null);
            HandleInfo = handleInfo;
        }

        /// <summary>
        /// The name of the process that owns the handle.
        /// </summary>
        public string ProcessName { get; }

        /// <summary>
        /// The file path associated with the handle.
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// The NT path associated with the handle.
        /// </summary>
        public string NtPath { get; }

        /// <summary>
        /// The type of the handle (e.g., "File", "Directory", etc.).
        /// </summary>
        public string HandleType { get; }

        /// <summary>
        /// Information about the open handle.
        /// </summary>
        public SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX HandleInfo { get; }
    }
}
