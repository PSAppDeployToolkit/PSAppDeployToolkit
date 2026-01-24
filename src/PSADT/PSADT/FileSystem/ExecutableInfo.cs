using System;
using System.IO;
using System.Runtime.InteropServices;
using PSADT.Extensions;
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
        /// Parses the specified PE file and returns information about it.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
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
        /// Creates a new instance of the ExecutableInfo class.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="isDotNetExecutable"></param>
        /// <param name="machine"></param>
        /// <param name="subsystem"></param>
        /// <param name="entryPoint"></param>
        /// <param name="imageBase"></param>
        private ExecutableInfo(string filePath, IMAGE_FILE_MACHINE machine, IMAGE_SUBSYSTEM subsystem, bool isDotNetExecutable, uint entryPoint, ulong imageBase)
        {
            FileInfo = !string.IsNullOrWhiteSpace(filePath) ? new(filePath) : throw new ArgumentNullException("File path cannot be null or empty.", (Exception?)null);
            Machine = (LibraryInterfaces.IMAGE_FILE_MACHINE)machine;
            Subsystem = (LibraryInterfaces.IMAGE_SUBSYSTEM)subsystem;
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
        public LibraryInterfaces.IMAGE_FILE_MACHINE Machine { get; }

        /// <summary>
        /// The subsystem of the executable.
        /// </summary>
        public LibraryInterfaces.IMAGE_SUBSYSTEM Subsystem { get; }

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
