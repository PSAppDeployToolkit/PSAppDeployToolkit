using System;
using System.IO;
using System.Runtime.InteropServices;
using PSADT.Interop.Extensions;
using Windows.Win32;
using Windows.Win32.System.Diagnostics.Debug;
using Windows.Win32.System.SystemInformation;
using Windows.Win32.System.SystemServices;

namespace PSADT.FileSystem
{
    /// <summary>
    /// Provides information about a PE file.
    /// </summary>
    public sealed record ExecutableInfo
    {
        /// <summary>
        /// Retrieves information about a Portable Executable (PE) file at the specified path, including its
        /// architecture, subsystem, entry point, and image base.
        /// </summary>
        /// <remarks>This method supports both 32-bit and 64-bit PE files. It validates the file structure
        /// before extracting information. Use this method to inspect executables for compatibility or metadata
        /// purposes.</remarks>
        /// <param name="filePath">The path to the executable file to analyze. This parameter must not be null or empty.</param>
        /// <returns>An ExecutableInfo instance containing details about the executable, such as machine type, subsystem,
        /// presence of a CLR header, entry point address, and image base.</returns>
        /// <exception cref="InvalidDataException">Thrown if the specified file does not have a valid PE header, PE signature, or optional header magic number.</exception>
        public static ExecutableInfo Get(string filePath)
        {
            // Internal helper methods to read structures and check for CLR header.
            static bool HasCLRHeader(__IMAGE_DATA_DIRECTORY_16 dataDirectory)
            {
                if (dataDirectory.Length <= 14)
                {
                    return false;
                }
                IMAGE_DATA_DIRECTORY comDir = dataDirectory._14;
                return comDir.VirtualAddress != 0 && comDir.Size != 0;
            }
            static ref readonly T ReadStruct<T>(BinaryReader reader) where T : unmanaged
            {
                return ref reader.ReadBytes(Marshal.SizeOf<T>()).AsSpan().AsReadOnlyStructure<T>();
            }

            // Read the DOS header and check for the PE signature.
            using FileStream fs = new(filePath, FileMode.Open, FileAccess.Read);
            using BinaryReader reader = new(fs);
            ref readonly IMAGE_DOS_HEADER dosHeader = ref ReadStruct<IMAGE_DOS_HEADER>(reader); _ = fs.Seek(dosHeader.e_lfanew, SeekOrigin.Begin);
            if (dosHeader.e_magic != PInvoke.IMAGE_DOS_SIGNATURE)
            {
                throw new InvalidDataException("The specified file does not have a valid PE header.");
            }
            if (reader.ReadUInt32() != PInvoke.IMAGE_NT_SIGNATURE)
            {
                throw new InvalidDataException("The specified file does not have a valid PE signature.");
            }

            // Read the file header and optional header, returning the ExecutableInfo.
            ref readonly IMAGE_FILE_MACHINE machine = ref ReadStruct<IMAGE_FILE_HEADER>(reader).Machine;
            IMAGE_OPTIONAL_HEADER_MAGIC magic = (IMAGE_OPTIONAL_HEADER_MAGIC)reader.ReadUInt16(); _ = fs.Seek(-2, SeekOrigin.Current);
            if (magic == IMAGE_OPTIONAL_HEADER_MAGIC.IMAGE_NT_OPTIONAL_HDR32_MAGIC)
            {
                ref readonly IMAGE_OPTIONAL_HEADER32 opt32 = ref ReadStruct<IMAGE_OPTIONAL_HEADER32>(reader);
                return new(filePath, machine, opt32.Subsystem, HasCLRHeader(opt32.DataDirectory), opt32.AddressOfEntryPoint, opt32.ImageBase);
            }
            else if (magic == IMAGE_OPTIONAL_HEADER_MAGIC.IMAGE_NT_OPTIONAL_HDR64_MAGIC)
            {
                ref readonly IMAGE_OPTIONAL_HEADER64 opt64 = ref ReadStruct<IMAGE_OPTIONAL_HEADER64>(reader);
                return new(filePath, machine, opt64.Subsystem, HasCLRHeader(opt64.DataDirectory), opt64.AddressOfEntryPoint, opt64.ImageBase);
            }
            else
            {
                throw new InvalidDataException("The specified file does not have a valid optional header magic number.");
            }
        }

        /// <summary>
        /// Initializes a new instance of the ExecutableInfo class with the specified executable file path, machine
        /// type, subsystem, .NET status, entry point address, and image base address.
        /// </summary>
        /// <remarks>This constructor validates the file path before creating an instance. Use this
        /// constructor to create an ExecutableInfo object with all required metadata for the executable file.</remarks>
        /// <param name="filePath">The path to the executable file. This parameter cannot be null or empty.</param>
        /// <param name="machine">The machine type of the executable, specified as a value from the IMAGE_FILE_MACHINE enumeration.</param>
        /// <param name="subsystem">The subsystem type of the executable, specified as a value from the IMAGE_SUBSYSTEM enumeration.</param>
        /// <param name="isDotNetExecutable">true if the executable is a .NET assembly; otherwise, false.</param>
        /// <param name="entryPoint">The address of the entry point of the executable.</param>
        /// <param name="imageBase">The base address of the image in memory.</param>
        /// <exception cref="ArgumentNullException">Thrown when the filePath parameter is null or empty.</exception>
        private ExecutableInfo(string filePath, IMAGE_FILE_MACHINE machine, IMAGE_SUBSYSTEM subsystem, bool isDotNetExecutable, uint entryPoint, ulong imageBase)
        {
            FileInfo = !string.IsNullOrWhiteSpace(filePath) ? new(filePath) : throw new ArgumentNullException("File path cannot be null or empty.", (Exception?)null);
            Machine = (Interop.IMAGE_FILE_MACHINE)machine;
            Subsystem = (Interop.IMAGE_SUBSYSTEM)subsystem;
            IsDotNetExecutable = isDotNetExecutable;
            EntryPoint = entryPoint;
            ImageBase = imageBase;
        }

        /// <summary>
        /// The FileInfo object for the executable.
        /// </summary>
        public FileInfo FileInfo { get; }

        /// <summary>
        /// The machine type of the executable.
        /// </summary>
        public Interop.IMAGE_FILE_MACHINE Machine { get; }

        /// <summary>
        /// The subsystem of the executable.
        /// </summary>
        public Interop.IMAGE_SUBSYSTEM Subsystem { get; }

        /// <summary>
        /// Whether the file is a .NET executable.
        /// </summary>
        public bool IsDotNetExecutable { get; }

        /// <summary>
        /// The entry point of the executable.
        /// </summary>
        public uint EntryPoint { get; }

        /// <summary>
        /// The image base of the executable.
        /// </summary>
        public ulong ImageBase { get; }
    }
}
