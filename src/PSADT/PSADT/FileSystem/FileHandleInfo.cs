using System;
using System.Diagnostics;
using PSADT.LibraryInterfaces;

namespace PSADT.FileSystem
{
    /// <summary>
    /// Represents information about a file handle.
    /// </summary>
    public sealed record FileHandleInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileHandleInfo"/> class with the specified handle information and file path.
        /// </summary>
        /// <param name="handleInfo"></param>
        /// <param name="filePath"></param>
        /// <param name="ntPath"></param>
        /// <param name="handleType"></param>
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
