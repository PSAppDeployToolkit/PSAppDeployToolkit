using System.IO;
using System.Runtime.InteropServices;
using PSADT.Types;
using Windows.Win32;
using Windows.Win32.System.SystemServices;
using Windows.Win32.System.Diagnostics.Debug;

namespace PSADT.Execution
{
    /// <summary>
    /// Provides utility methods for working with executables.
    /// </summary>
    public static class ExecutableUtilities
    {
        /// <summary>
        /// Parses the specified PE file and returns information about it.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static ExecutableInfo GetExecutableInfo(string filePath)
        {
            bool HasCLRHeader(__IMAGE_DATA_DIRECTORY_16 dataDirectory)
            {
                if (dataDirectory.Length > 14)
                {
                    var comDir = dataDirectory._14;
                    return comDir.VirtualAddress != 0 && comDir.Size != 0;
                }
                return false;
            }

            T ReadStruct<T>(BinaryReader reader) where T : struct
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

            using FileStream fs = new(filePath, FileMode.Open, FileAccess.Read);
            using BinaryReader reader = new(fs);

            LibraryInterfaces.IMAGE_SUBSYSTEM subsystem = LibraryInterfaces.IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_UNKNOWN;
            uint? entryPoint = null;
            ulong? imageBase = null;
            bool isDotNet = false;

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

            return new ExecutableInfo(
                filePath,
                machine,
                subsystem,
                (SystemArchitecture)machine,
                (ExecutableType)subsystem,
                isDotNet,
                entryPoint,
                imageBase
            );
        }
    }
}
