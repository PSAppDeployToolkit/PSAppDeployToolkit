using static PSADT.LibraryInterfaces.NtDll;

namespace PSADT.FileSystem
{
    /// <summary>
    /// Represents information about a file handle.
    /// </summary>
    public sealed class FileHandleInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileHandleInfo"/> class with the specified handle information and file path.
        /// </summary>
        /// <param name="handleInfo"></param>
        /// <param name="filePath"></param>
        public FileHandleInfo(SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX handleInfo, string filePath, string ntPath, string handleType)
        {
            FilePath = filePath;
            NtPath = ntPath;
            HandleType = handleType;
            HandleInfo = handleInfo;
        }

        /// <summary>
        /// The file path associated with the handle.
        /// </summary>
        public readonly string FilePath;

        /// <summary>
        /// The NT path associated with the handle.
        /// </summary>
        public readonly string NtPath;

        /// <summary>
        /// The type of the handle (e.g., "File", "Directory", etc.).
        /// </summary>
        public readonly string HandleType;

        /// <summary>
        /// Information about the open handle.
        /// </summary>
        public readonly SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX HandleInfo;
    }
}
