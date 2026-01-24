using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;
using PSADT.Extensions;
using PSADT.FileSystem;
using PSADT.LibraryInterfaces;
using PSADT.LibraryInterfaces.Extensions;
using PSADT.Security;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Storage.FileSystem;
using Windows.Win32.System.Diagnostics.Debug;
using Windows.Win32.System.ProcessStatus;
using Windows.Win32.System.SystemServices;
using Windows.Win32.System.Threading;

namespace PSADT.ProcessManagement
{
    /// <summary>
    /// Provides detailed version information for a specified process.
    /// </summary>
    /// <remarks>The <see cref="ProcessVersionInfo"/> class encapsulates version details of a process,
    /// including file and product version numbers, company name, and other metadata. It is designed to retrieve and
    /// represent version information from the main module of a process, allowing users to access various attributes
    /// such as the file description, internal name, and whether the file is a debug or special build. This class is
    /// particularly useful for applications that need to inspect or display version information of running
    /// processes.</remarks>
    public sealed record ProcessVersionInfo
    {
        /// <summary>
        /// Retrieves version information for the specified process.
        /// </summary>
        /// <param name="process">The process for which to obtain version information. Cannot be null.</param>
        /// <returns>A <see cref="ProcessVersionInfo"/> object containing the version details of the specified process.</returns>
        public static ProcessVersionInfo GetVersionInfo(Process process)
        {
            return new(process, null, null);
        }

        /// <summary>
        /// Retrieves version information for the process with the specified process identifier.
        /// </summary>
        /// <param name="processId">The unique identifier of the process for which to retrieve version information. Must correspond to a running
        /// process.</param>
        /// <returns>A <see cref="ProcessVersionInfo"/> object containing version information for the specified process.</returns>
        public static ProcessVersionInfo GetVersionInfo(int processId)
        {
            using Process process = Process.GetProcessById(processId);
            return new(process, null, null);
        }

        /// <summary>
        /// Retrieves version information for the specified process.
        /// </summary>
        /// <remarks>This method provides a convenient way to access version information for a process, 
        /// utilizing a lookup table to resolve NT paths.</remarks>
        /// <param name="process">The process for which to obtain version information. This parameter cannot be null.</param>
        /// <param name="ntPathLookupTable">A read-only dictionary that maps NT paths to their corresponding user-friendly paths. This is used to
        /// resolve paths within the process's version information.</param>
        /// <returns>A <see cref="ProcessVersionInfo"/> object containing the version details of the specified process.</returns>
        internal static ProcessVersionInfo GetVersionInfo(Process process, ReadOnlyDictionary<string, string> ntPathLookupTable)
        {
            return new(process, null, ntPathLookupTable);
        }

        /// <summary>
        /// Retrieves version information for a specified process and file path.
        /// </summary>
        /// <param name="process">The process for which to obtain version information.</param>
        /// <param name="filePath">The file path associated with the process, used to locate version details.</param>
        /// <returns>A <see cref="ProcessVersionInfo"/> object containing the version information of the specified process and
        /// file path.</returns>
        internal static ProcessVersionInfo GetVersionInfo(Process process, string filePath)
        {
            return new(process, filePath, null);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessVersionInfo"/> class for the specified process.
        /// </summary>
        /// <remarks>This constructor retrieves and parses version information from the specified
        /// process's main module. It requires the SeDebugPrivilege to read the process memory. If the privilege is not
        /// enabled, it will attempt to enable it.</remarks>
        /// <param name="process">The process from which to retrieve version information. Cannot be <see langword="null"/>.</param>
        /// <param name="filePath">The file path associated with the process. If <see langword="null"/> or whitespace, the image name of the process will be used.</param>
        /// <param name="ntPathLookupTable">A read-only dictionary for resolving NT paths to user-friendly paths. If <see langword="null"/>, a default lookup table will be used.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="process"/> is <see langword="null"/>.</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown if the current process does not have the required SeDebugPrivilege to read the target process memory.</exception>
        private ProcessVersionInfo(Process process, string? filePath, ReadOnlyDictionary<string, string>? ntPathLookupTable)
        {
            // Validate the input process.
            if (process is null)
            {
                throw new ArgumentNullException(nameof(process));
            }

            // Confirm we've got the privilege to read the process memory.
            if (!PrivilegeManager.HasPrivilege(SE_PRIVILEGE.SeDebugPrivilege))
            {
                throw new UnauthorizedAccessException("The current process does not have the required SeDebugPrivilege to read the target process memory.");
            }
            PrivilegeManager.EnablePrivilegeIfDisabled(SE_PRIVILEGE.SeDebugPrivilege);

            // Get the main module base address and read the version resource from memory.
            FileName = !string.IsNullOrWhiteSpace(filePath) ? filePath! : process.GetFilePath(ntPathLookupTable ?? FileSystemUtilities.GetNtPathLookupTable()); ReadOnlySpan<byte> versionResource;
            using (SafeFileHandle processHandle = Kernel32.OpenProcess(PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION | PROCESS_ACCESS_RIGHTS.PROCESS_VM_READ, false, (uint)process.Id))
            {
                try
                {
                    MODULEINFO moduleInfo = GetMainModuleInfo(processHandle);
                    versionResource = ReadVersionResource(processHandle, in moduleInfo);
                }
                catch
                {
                    return;
                    throw;
                }
            }

            // If we got a valid version resource, parse it.
            // Read the version information from the resource.
            _ = Version32.VerQueryValue(versionResource, @"\", out IntPtr fixedInfoPtr, out _);
            FixedFileInfo = fixedInfoPtr.AsReadOnlyStructure<VS_FIXEDFILEINFO>();
            FileMajorPart = PInvoke.HIWORD(FixedFileInfo.dwFileVersionMS);
            FileMinorPart = PInvoke.LOWORD(FixedFileInfo.dwFileVersionMS);
            FileBuildPart = PInvoke.HIWORD(FixedFileInfo.dwFileVersionLS);
            FilePrivatePart = PInvoke.LOWORD(FixedFileInfo.dwFileVersionLS);
            ProductMajorPart = PInvoke.HIWORD(FixedFileInfo.dwProductVersionMS);
            ProductMinorPart = PInvoke.LOWORD(FixedFileInfo.dwProductVersionMS);
            ProductBuildPart = PInvoke.HIWORD(FixedFileInfo.dwProductVersionLS);
            ProductPrivatePart = PInvoke.LOWORD(FixedFileInfo.dwProductVersionLS);
            FileVersionRaw = new(FileMajorPart, FileMinorPart, FileBuildPart, FilePrivatePart);
            ProductVersionRaw = new(ProductMajorPart, ProductMinorPart, ProductBuildPart, ProductPrivatePart);

            // Set the flags based on the fixed file info.
            IsDebug = (FixedFileInfo.dwFileFlags & VS_FIXEDFILEINFO_FILE_FLAGS.VS_FF_DEBUG) != 0;
            IsPatched = (FixedFileInfo.dwFileFlags & VS_FIXEDFILEINFO_FILE_FLAGS.VS_FF_PATCHED) != 0;
            IsPrivateBuild = (FixedFileInfo.dwFileFlags & VS_FIXEDFILEINFO_FILE_FLAGS.VS_FF_PRIVATEBUILD) != 0;
            IsPreRelease = (FixedFileInfo.dwFileFlags & VS_FIXEDFILEINFO_FILE_FLAGS.VS_FF_PRERELEASE) != 0;
            IsSpecialBuild = (FixedFileInfo.dwFileFlags & VS_FIXEDFILEINFO_FILE_FLAGS.VS_FF_SPECIALBUILD) != 0;

            // Read the version resource strings.
            ReadOnlyCollection<string> codepageTable = GetTranslationTableCombinations(versionResource);
            Language = GetFileVersionLanguage(codepageTable[0]); bool success = false;
            foreach (string codepage in codepageTable)
            {
                // Exit loop if we successfully retrieved at least one string.
                Comments = GetFileVersionString(versionResource, codepage, "Comments", ref success);
                CompanyName = GetFileVersionString(versionResource, codepage, "CompanyName", ref success);
                FileDescription = GetFileVersionString(versionResource, codepage, "FileDescription", ref success);
                FileVersion = GetFileVersionString(versionResource, codepage, "FileVersion", ref success);
                InternalName = GetFileVersionString(versionResource, codepage, "InternalName", ref success);
                LegalCopyright = GetFileVersionString(versionResource, codepage, "LegalCopyright", ref success);
                LegalTrademarks = GetFileVersionString(versionResource, codepage, "LegalTrademarks", ref success);
                OriginalFilename = GetFileVersionString(versionResource, codepage, "OriginalFilename", ref success);
                PrivateBuild = GetFileVersionString(versionResource, codepage, "PrivateBuild", ref success);
                ProductName = GetFileVersionString(versionResource, codepage, "ProductName", ref success);
                ProductVersion = GetFileVersionString(versionResource, codepage, "ProductVersion", ref success);
                SpecialBuild = GetFileVersionString(versionResource, codepage, "SpecialBuild", ref success);
                if (success)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Gets information about the main module of the specified process.
        /// </summary>
        private static MODULEINFO GetMainModuleInfo(SafeFileHandle processHandle)
        {
            // Get all process modules, then return the first one (main module).
            _ = PsApi.EnumProcessModules(processHandle, null, out uint bytesNeeded); Span<byte> moduleBuffer = stackalloc byte[(int)bytesNeeded];
            _ = PsApi.EnumProcessModules(processHandle, moduleBuffer, out bytesNeeded);
            ref readonly HMODULE hModule = ref moduleBuffer.AsReadOnlyStructure<HMODULE>();
            _ = PsApi.GetModuleInformation(processHandle, in hModule, out MODULEINFO moduleInfo);
            return moduleInfo;
        }

        /// <summary>
        /// Reads the version resource from the process memory.
        /// </summary>
        private static byte[] ReadVersionResource(SafeFileHandle processHandle, in MODULEINFO moduleInfo)
        {
            // Read the DOS header to make sure we have a valid PE header.
            IntPtr baseAddress; unsafe { baseAddress = (IntPtr)moduleInfo.lpBaseOfDll; }
            IMAGE_DOS_HEADER dosHeader = ReadProcessMemory<IMAGE_DOS_HEADER>(processHandle, baseAddress);
            if (dosHeader.e_magic != PInvoke.IMAGE_DOS_SIGNATURE)
            {
                throw new InvalidOperationException("The specified process does not have a valid PE header.");
            }

            // Read the NT headers to check the magic number.
            IntPtr ntHeadersAddress = unchecked(baseAddress + dosHeader.e_lfanew);
            IMAGE_NT_HEADERS64 ntHeaders64 = ReadProcessMemory<IMAGE_NT_HEADERS64>(processHandle, ntHeadersAddress);
            if (ntHeaders64.Signature != PInvoke.IMAGE_NT_SIGNATURE)
            {
                throw new InvalidOperationException("The specified process does not have a valid NT header signature.");
            }

            // Determine the resource directory RVA and size based on the optional header magic number.
            uint resourceRva, resourceSize;
            if (ntHeaders64.OptionalHeader.Magic == IMAGE_OPTIONAL_HEADER_MAGIC.IMAGE_NT_OPTIONAL_HDR32_MAGIC) // PE32
            {
                IMAGE_NT_HEADERS32 ntHeaders32 = ReadProcessMemory<IMAGE_NT_HEADERS32>(processHandle, ntHeadersAddress);
                if (ntHeaders32.OptionalHeader.NumberOfRvaAndSizes <= 2)
                {
                    throw new InvalidOperationException("The specified process does not have enough data directories to contain a resource directory.");
                }
                resourceRva = ntHeaders32.OptionalHeader.DataDirectory._2.VirtualAddress;  // INDEX_RESOURCE = 2
                resourceSize = ntHeaders32.OptionalHeader.DataDirectory._2.Size;
            }
            else if (ntHeaders64.OptionalHeader.Magic == IMAGE_OPTIONAL_HEADER_MAGIC.IMAGE_NT_OPTIONAL_HDR64_MAGIC) // PE32+
            {
                if (ntHeaders64.OptionalHeader.NumberOfRvaAndSizes <= 2)
                {
                    throw new InvalidOperationException("The specified process does not have enough data directories to contain a resource directory.");
                }
                resourceRva = ntHeaders64.OptionalHeader.DataDirectory._2.VirtualAddress;  // INDEX_RESOURCE = 2
                resourceSize = ntHeaders64.OptionalHeader.DataDirectory._2.Size;
            }
            else
            {
                throw new InvalidOperationException("The specified process does not have a valid optional header magic number.");
            }

            // Validate the resource directory size.
            return resourceSize == 0
                ? throw new InvalidOperationException("The specified process does not have a valid resource directory size.")
                : FindVersionResource(processHandle, unchecked(baseAddress + (int)resourceRva), baseAddress);
        }

        /// <summary>
        /// Navigates the resource directory structure to find the version resource.
        /// </summary>
        private static byte[] FindVersionResource(SafeFileHandle processHandle, IntPtr resourceDirectoryAddress, IntPtr baseAddress)
        {
            // Read the resource directory
            IMAGE_RESOURCE_DIRECTORY resourceDir = ReadProcessMemory<IMAGE_RESOURCE_DIRECTORY>(processHandle, resourceDirectoryAddress);
            int totalEntries = resourceDir.NumberOfNamedEntries + resourceDir.NumberOfIdEntries;
            IntPtr entriesAddress = unchecked(resourceDirectoryAddress + Marshal.SizeOf<IMAGE_RESOURCE_DIRECTORY>());

            // Look for RT_VERSION resource (type 16) and throw if not found.
            for (int i = 0; i < totalEntries; i++)
            {
                IntPtr entryAddress = unchecked(entriesAddress + (i * Marshal.SizeOf<IMAGE_RESOURCE_DIRECTORY_ENTRY>()));
                IMAGE_RESOURCE_DIRECTORY_ENTRY entry = ReadProcessMemory<IMAGE_RESOURCE_DIRECTORY_ENTRY>(processHandle, entryAddress);
                if (entry.Anonymous1.Name == RESOURCE_TYPE.RT_VERSION)
                {
                    return ReadVersionResourceData(processHandle, resourceDirectoryAddress, baseAddress, entry.Anonymous2.OffsetToData);
                }
            }
            throw new InvalidOperationException("The specified process does not contain an RT_VERSION resource in its Level 1 data.");
        }

        /// <summary>
        /// Reads the actual version resource data.
        /// </summary>
        private static byte[] ReadVersionResourceData(SafeFileHandle processHandle, IntPtr resourceDirectoryAddress, IntPtr baseAddress, uint offsetToData)
        {
            // Navigate through the directory levels using a do/while loop.
            uint currentOffsetToData = offsetToData;
            IMAGE_RESOURCE_DIRECTORY_ENTRY currentEntry;
            do
            {
                IntPtr currentAddress = unchecked(resourceDirectoryAddress + (int)(currentOffsetToData & IMAGE_RESOURCE_RVA_MASK));
                IntPtr currentEntryAddress = unchecked(currentAddress + Marshal.SizeOf<IMAGE_RESOURCE_DIRECTORY>());
                currentEntry = ReadProcessMemory<IMAGE_RESOURCE_DIRECTORY_ENTRY>(processHandle, currentEntryAddress);
                currentOffsetToData = currentEntry.Anonymous2.OffsetToData;
            }
            while ((currentOffsetToData & PInvoke.IMAGE_RESOURCE_DATA_IS_DIRECTORY) != 0);

            // At this point, currentEntry points to a data entry, not a directory.
            IntPtr dataEntryAddress = unchecked(resourceDirectoryAddress + (int)currentEntry.Anonymous2.OffsetToData);
            IMAGE_RESOURCE_DATA_ENTRY dataEntry = ReadProcessMemory<IMAGE_RESOURCE_DATA_ENTRY>(processHandle, dataEntryAddress);
            if (dataEntry.Size > 0)
            {
                byte[] buffer = new byte[(int)dataEntry.Size];
                _ = Kernel32.ReadProcessMemory(processHandle, unchecked(baseAddress + (int)dataEntry.OffsetToData), buffer, out _);
                return buffer;
            }
            throw new InvalidOperationException($"Invalid data entry size: {dataEntry.Size} at address 0x{dataEntryAddress.ToInt64():X}");
        }

        /// <summary>
        /// Gets language/codepage combinations from the Translation table.
        /// </summary>
        private static ReadOnlyCollection<string> GetTranslationTableCombinations(ReadOnlySpan<byte> versionResource)
        {
            // Return any translation pairs found in the version resource.
            List<string> translationCombinations = [];
            _ = Version32.VerQueryValue(versionResource, @"\VarFileInfo\Translation", out IntPtr translationPtr, out uint translationLength);
            int langAndCodepageSize = Marshal.SizeOf<Version32.LANGANDCODEPAGE>();
            for (int i = 0; i < translationLength / langAndCodepageSize; i++)
            {
                ref readonly Version32.LANGANDCODEPAGE langAndCodePage = ref (translationPtr + (i * langAndCodepageSize)).AsReadOnlyStructure<Version32.LANGANDCODEPAGE>();
                translationCombinations.Add(langAndCodePage.ToTranslationTableString());
            }

            // Add some common fallback combinations that are known to work in many cases.
            // These are based on common language/codepage pairs used in version resources.
            translationCombinations.Add("040904B0");
            translationCombinations.Add("040904E4");
            translationCombinations.Add("04090000");
            return new([.. translationCombinations.Distinct()]);
        }

        /// <summary>
        /// Retrieves the language name associated with a specified codepage from a version resource.
        /// </summary>
        /// <remarks>The method uses the high-order word of the codepage to determine the language
        /// name.</remarks>
        /// <param name="codepage">A string representing the codepage, which is used to identify the language.</param>
        /// <returns>A string containing the language name if the codepage is valid and the language can be determined;
        /// otherwise, <see langword="null"/>.</returns>
        private static string? GetFileVersionLanguage(string codepage)
        {
            Span<char> szLang = stackalloc char[(int)PInvoke.MAX_PATH];
            uint len = Kernel32.VerLanguageName(PInvoke.HIWORD(uint.Parse(codepage, NumberStyles.HexNumber, CultureInfo.InvariantCulture)), szLang);
            string result = szLang.Slice(0, (int)len).ToString().TrimRemoveNull();
            return !string.IsNullOrWhiteSpace(result) ? result : null;
        }

        /// <summary>
        /// Retrieves the version string from a specified version resource.
        /// </summary>
        /// <remarks>This method queries the specified version resource for the version information
        /// associated with the given name. If the information is found and is not empty or whitespace, it returns the
        /// version string; otherwise, it returns <see langword="null"/>.</remarks>
        /// <param name="versionResource">A handle to the version resource from which to retrieve the version string.</param>
        /// <param name="codepage">The codepage string used to locate the version information.</param>
        /// <param name="name">The name of the version information to query.</param>
        /// <param name="success">A reference boolean that indicates whether the retrieval was successful.</param>
        /// <returns>A string containing the version information if found and not empty; otherwise, <see langword="null"/>.</returns>
        private static string? GetFileVersionString(ReadOnlySpan<byte> versionResource, string codepage, string name, ref bool success)
        {
            // Attempt to query the version resource for the specified name.
            try
            {
                _ = Version32.VerQueryValue(versionResource, string.Format(CultureInfo.InvariantCulture, @"\StringFileInfo\{0}\{1}", codepage, name), out IntPtr lplpBuffer, out _);
                string? result = Marshal.PtrToStringUni(lplpBuffer)?.TrimRemoveNull();
                if (!string.IsNullOrWhiteSpace(result))
                {
                    success = true;
                    return result;
                }
            }
            catch
            {
                return null;
                throw;
            }
            return null;
        }

        /// <summary>
        /// Reads a structure from process memory using stack-allocated buffer.
        /// </summary>
        private static T ReadProcessMemory<T>(SafeFileHandle processHandle, IntPtr address) where T : struct
        {
            Span<byte> buffer = stackalloc byte[Marshal.SizeOf<T>()];
            _ = Kernel32.ReadProcessMemory(processHandle, address, buffer, out _);
            return MemoryMarshal.Read<T>(buffer);
        }

        /// <summary>
        /// Returns a partial list of properties in the System.Diagnostics.ProcessVersionInfo and their values.
        /// </summary>
        /// <remarks>The formatting of this methood 1:1 matches System.Diagnostics.FileVersionInfo.</remarks>
        public override string ToString()
        {
            StringBuilder stringBuilder = new(128);
            string value = "\r\n";
            _ = stringBuilder.Append("File:             ");
            _ = stringBuilder.Append(FileName);
            _ = stringBuilder.Append(value);
            _ = stringBuilder.Append("InternalName:     ");
            _ = stringBuilder.Append(InternalName);
            _ = stringBuilder.Append(value);
            _ = stringBuilder.Append("OriginalFilename: ");
            _ = stringBuilder.Append(OriginalFilename);
            _ = stringBuilder.Append(value);
            _ = stringBuilder.Append("FileVersion:      ");
            _ = stringBuilder.Append(FileVersion);
            _ = stringBuilder.Append(value);
            _ = stringBuilder.Append("FileDescription:  ");
            _ = stringBuilder.Append(FileDescription);
            _ = stringBuilder.Append(value);
            _ = stringBuilder.Append("Product:          ");
            _ = stringBuilder.Append(ProductName);
            _ = stringBuilder.Append(value);
            _ = stringBuilder.Append("ProductVersion:   ");
            _ = stringBuilder.Append(ProductVersion);
            _ = stringBuilder.Append(value);
            _ = stringBuilder.Append("Debug:            ");
            _ = stringBuilder.Append(IsDebug);
            _ = stringBuilder.Append(value);
            _ = stringBuilder.Append("Patched:          ");
            _ = stringBuilder.Append(IsPatched);
            _ = stringBuilder.Append(value);
            _ = stringBuilder.Append("PreRelease:       ");
            _ = stringBuilder.Append(IsPreRelease);
            _ = stringBuilder.Append(value);
            _ = stringBuilder.Append("PrivateBuild:     ");
            _ = stringBuilder.Append(IsPrivateBuild);
            _ = stringBuilder.Append(value);
            _ = stringBuilder.Append("SpecialBuild:     ");
            _ = stringBuilder.Append(IsSpecialBuild);
            _ = stringBuilder.Append(value);
            _ = stringBuilder.Append("Language:         ");
            _ = stringBuilder.Append(Language);
            _ = stringBuilder.Append(value);
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Gets the raw version information of the file.
        /// </summary>
        public Version FileVersionRaw { get; } = new(0, 0, 0, 0);

        /// <summary>
        /// Gets the raw version information of the product.
        /// </summary>
        public Version ProductVersionRaw { get; } = new(0, 0, 0, 0);

        /// <summary>
        /// Gets the comments associated with the file.
        /// </summary>
        public string? Comments { get; }

        /// <summary>
        /// Gets the name of the company that produced the file.
        /// </summary>
        public string? CompanyName { get; }

        /// <summary>
        /// Gets the build number of the file.
        /// </summary>
        public int FileBuildPart { get; }

        /// <summary>
        /// Gets the description of the file.
        /// </summary>
        public string? FileDescription { get; }

        /// <summary>
        /// Gets the major part of the version number.
        /// </summary>
        public int FileMajorPart { get; }

        /// <summary>
        /// Gets the minor part of the version number of the file.
        /// </summary>
        public int FileMinorPart { get; }

        /// <summary>
        /// Gets the name of the file that this object instance describes.
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// Gets the file private part number.
        /// </summary>
        public int FilePrivatePart { get; }

        /// <summary>
        /// Gets the file version number.
        /// </summary>
        public string? FileVersion { get; }

        /// <summary>
        /// Gets the internal name of the file, if one exists.
        /// </summary>
        public string? InternalName { get; }

        /// <summary>
        /// Gets a value that specifies whether the file contains debugging information or is compiled with debugging features enabled.
        /// </summary>
        public bool IsDebug { get; }

        /// <summary>
        /// Gets a value that specifies whether the file has been modified and is not identical to the original shipping file of the same version number.
        /// </summary>
        public bool IsPatched { get; }

        /// <summary>
        /// Gets a value that specifies whether the file was built using standard release procedures.
        /// </summary>
        public bool IsPrivateBuild { get; }

        /// <summary>
        /// Gets a value that specifies whether the file is a development version, rather than a commercially released product.
        /// </summary>
        public bool IsPreRelease { get; }

        /// <summary>
        /// Gets a value that specifies whether the file is a special build.
        /// </summary>
        public bool IsSpecialBuild { get; }

        /// <summary>
        /// Gets the default language string for the version info block.
        /// </summary>
        public string? Language { get; }

        /// <summary>
        /// Gets all copyright notices that apply to the specified file.
        /// </summary>
        public string? LegalCopyright { get; }

        /// <summary>
        /// Gets the trademarks and registered trademarks that apply to the file.
        /// </summary>
        public string? LegalTrademarks { get; }

        /// <summary>
        /// Gets the name the file was created with.
        /// </summary>
        public string? OriginalFilename { get; }

        /// <summary>
        /// Gets information about a private version of the file.
        /// </summary>
        public string? PrivateBuild { get; }

        /// <summary>
        /// Gets the build number of the product this file is associated with.
        /// </summary>
        public int ProductBuildPart { get; }

        /// <summary>
        /// Gets the major part of the version number for the product this file is associated with.
        /// </summary>
        public int ProductMajorPart { get; }

        /// <summary>
        /// Gets the minor part of the version number for the product the file is associated with.
        /// </summary>
        public int ProductMinorPart { get; }

        /// <summary>
        /// Gets the name of the product this file is distributed with.
        /// </summary>
        public string? ProductName { get; }

        /// <summary>
        /// Gets the private part number of the product this file is associated with.
        /// </summary>
        public int ProductPrivatePart { get; }

        /// <summary>
        /// Gets the version of the product this file is distributed with.
        /// </summary>
        public string? ProductVersion { get; }

        /// <summary>
        /// Gets the special build information for the file.
        /// </summary>
        public string? SpecialBuild { get; }

        /// <summary>
        /// Represents the fixed file information of a version resource.
        /// </summary>
        /// <remarks>This field provides version information that is fixed for a specific file, such as
        /// the file version number, product version number, and other attributes. It is typically used in version
        /// management and file identification.</remarks>
        private readonly VS_FIXEDFILEINFO FixedFileInfo;

        /// <summary>
        /// Represents a mask used to extract the relative virtual address (RVA) from an image resource.
        /// </summary>
        /// <remarks>This constant is used in conjunction with the <see
        /// cref="PInvoke.IMAGE_RESOURCE_DATA_IS_DIRECTORY"/> to isolate the RVA portion of an image resource entry. It
        /// is typically used in scenarios where the directory flag needs to be cleared to obtain the actual
        /// RVA.</remarks>
        private const uint IMAGE_RESOURCE_RVA_MASK = ~PInvoke.IMAGE_RESOURCE_DATA_IS_DIRECTORY;
    }
}
