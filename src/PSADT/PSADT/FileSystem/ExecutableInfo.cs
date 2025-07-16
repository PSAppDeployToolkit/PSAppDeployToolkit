using System;
using System.IO;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.System.SystemServices;
using Windows.Win32.System.Diagnostics.Debug;

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
            static bool HasCLRHeader(__IMAGE_DATA_DIRECTORY_16 dataDirectory)
            {
                if (dataDirectory.Length > 14)
                {
                    var comDir = dataDirectory._14;
                    return comDir.VirtualAddress != 0 && comDir.Size != 0;
                }
                return false;
            }

            static T ReadStruct<T>(BinaryReader reader) where T : struct
            {
                var handle = GCHandle.Alloc(reader.ReadBytes(Marshal.SizeOf<T>()), GCHandleType.Pinned);
                try
                {
                    return Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
                }
                finally
                {
                    handle.Free();
                }
            }

            LibraryInterfaces.IMAGE_SUBSYSTEM subsystem = LibraryInterfaces.IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_UNKNOWN;
            uint entryPoint;
            ulong imageBase;
            bool isDotNet;

            using FileStream fs = new(filePath, FileMode.Open, FileAccess.Read);
            using BinaryReader reader = new(fs);
            var dosHeader = ReadStruct<IMAGE_DOS_HEADER>(reader);
            if (dosHeader.e_magic != PInvoke.IMAGE_DOS_SIGNATURE)
            {
                throw new InvalidDataException("The specified file does not have a valid PE header.");
            }

            fs.Seek(dosHeader.e_lfanew, SeekOrigin.Begin);
            if (reader.ReadUInt32() != PInvoke.IMAGE_NT_SIGNATURE)
            {
                throw new InvalidDataException("The specified file does not have a valid PE signature.");
            }

            var machine = (LibraryInterfaces.IMAGE_FILE_MACHINE)ReadStruct<IMAGE_FILE_HEADER>(reader).Machine;
            var magic = (IMAGE_OPTIONAL_HEADER_MAGIC)reader.ReadUInt16();
            fs.Seek(-2, SeekOrigin.Current);
            if (magic == IMAGE_OPTIONAL_HEADER_MAGIC.IMAGE_NT_OPTIONAL_HDR32_MAGIC)
            {
                var opt32 = ReadStruct<IMAGE_OPTIONAL_HEADER32>(reader);
                subsystem = (LibraryInterfaces.IMAGE_SUBSYSTEM)opt32.Subsystem;
                entryPoint = opt32.AddressOfEntryPoint;
                imageBase = opt32.ImageBase;
                isDotNet = HasCLRHeader(opt32.DataDirectory);
            }
            else if (magic == IMAGE_OPTIONAL_HEADER_MAGIC.IMAGE_NT_OPTIONAL_HDR64_MAGIC)
            {
                var opt64 = ReadStruct<IMAGE_OPTIONAL_HEADER64>(reader);
                subsystem = (LibraryInterfaces.IMAGE_SUBSYSTEM)opt64.Subsystem;
                entryPoint = opt64.AddressOfEntryPoint;
                imageBase = opt64.ImageBase;
                isDotNet = HasCLRHeader(opt64.DataDirectory);
            }
            else
            {
                throw new InvalidDataException("The specified file does not have a valid optional header magic number.");
            }
            return new(filePath, machine, subsystem, isDotNet, entryPoint, imageBase);
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
        private ExecutableInfo(string filePath, LibraryInterfaces.IMAGE_FILE_MACHINE machine, LibraryInterfaces.IMAGE_SUBSYSTEM subsystem, bool isDotNetExecutable, uint entryPoint, ulong imageBase)
        {
            FileInfo = !string.IsNullOrWhiteSpace(filePath) ? new FileInfo(filePath) : throw new ArgumentNullException("File path cannot be null or empty.", (Exception?)null);
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
        public readonly uint EntryPoint;

        /// <summary>
        /// The image base of the executable.
        /// </summary>
        public readonly ulong ImageBase;
    }
}
