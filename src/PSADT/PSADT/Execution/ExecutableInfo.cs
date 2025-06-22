using System.IO;

namespace PSADT.Execution
{
    /// <summary>
    /// Provides information about a PE file.
    /// </summary>
    public sealed record ExecutableInfo
    {
        /// <summary>
        /// Creates a new instance of the ExecutableInfo class.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="isDotNetExecutable"></param>
        /// <param name="machine"></param>
        /// <param name="subsystem"></param>
        /// <param name="entryPoint"></param>
        /// <param name="imageBase"></param>
        public ExecutableInfo(
            string filePath,
            LibraryInterfaces.IMAGE_FILE_MACHINE machine,
            LibraryInterfaces.IMAGE_SUBSYSTEM subsystem,
            bool isDotNetExecutable,
            uint? entryPoint,
            ulong? imageBase)
        {
            FileInfo = new FileInfo(filePath);
            Machine = machine;
            Subsystem = subsystem;
            IsDotNetExecutable = isDotNetExecutable;
            EntryPoint = entryPoint;
            ImageBase = imageBase;
        }

        /// <summary>
        /// The FileInfo object for the executable.
        /// </summary>
        public readonly FileInfo FileInfo;

        /// <summary>
        /// The machine type of the executable.
        /// </summary>
        public readonly LibraryInterfaces.IMAGE_FILE_MACHINE Machine;

        /// <summary>
        /// The subsystem of the executable.
        /// </summary>
        public readonly LibraryInterfaces.IMAGE_SUBSYSTEM Subsystem;

        /// <summary>
        /// Whether the file is a .NET executable.
        /// </summary>
        public readonly bool IsDotNetExecutable;

        /// <summary>
        /// The entry point of the executable.
        /// </summary>
        public readonly uint? EntryPoint;

        /// <summary>
        /// The image base of the executable.
        /// </summary>
        public readonly ulong? ImageBase;
    }
}
