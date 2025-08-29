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
using PSADT.SafeHandles;
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
        public static ProcessVersionInfo GetVersionInfo(Process process) => new(process, null, null);

        /// <summary>
        /// Retrieves version information for the specified process.
        /// </summary>
        /// <remarks>This method provides a convenient way to access version information for a process, 
        /// utilizing a lookup table to resolve NT paths.</remarks>
        /// <param name="process">The process for which to obtain version information. This parameter cannot be null.</param>
        /// <param name="ntPathLookupTable">A read-only dictionary that maps NT paths to their corresponding user-friendly paths. This is used to
        /// resolve paths within the process's version information.</param>
        /// <returns>A <see cref="ProcessVersionInfo"/> object containing the version details of the specified process.</returns>
        internal static ProcessVersionInfo GetVersionInfo(Process process, ReadOnlyDictionary<string, string> ntPathLookupTable) => new(process, null, ntPathLookupTable);

        /// <summary>
        /// Retrieves version information for a specified process and file path.
        /// </summary>
        /// <param name="process">The process for which to obtain version information.</param>
        /// <param name="filePath">The file path associated with the process, used to locate version details.</param>
        /// <returns>A <see cref="ProcessVersionInfo"/> object containing the version information of the specified process and
        /// file path.</returns>
        internal static ProcessVersionInfo GetVersionInfo(Process process, string filePath) => new(process, filePath, null);

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessVersionInfo"/> class for the specified process.
        /// </summary>
        /// <remarks>This constructor retrieves and parses version information from the specified
        /// process's main module. It requires the SeDebugPrivilege to read the process memory. If the privilege is not
        /// enabled, it will attempt to enable it.</remarks>
        /// <param name="process">The process from which to retrieve version information. Cannot be <see langword="null"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="process"/> is <see langword="null"/>.</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown if the current process does not have the required SeDebugPrivilege to read the target process memory.</exception>
        private ProcessVersionInfo(Process process, string? filePath, ReadOnlyDictionary<string, string>? ntPathLookupTable)
        {
            // Validate the input process.
            if (null == process)
            {
                throw new ArgumentNullException(nameof(process));
            }

            // Confirm we've got the privilege to read the process memory.
            if (!PrivilegeManager.HasPrivilege(SE_PRIVILEGE.SeDebugPrivilege))
            {
                throw new UnauthorizedAccessException("The current process does not have the required SeDebugPrivilege to read the target process memory.");
            }
            PrivilegeManager.EnablePrivilegeIfDisabled(SE_PRIVILEGE.SeDebugPrivilege);

            // Set initial values. This is the minimum required to create a valid ProcessVersionInfo object.
            FileName = !string.IsNullOrWhiteSpace(filePath) ? filePath! : ProcessUtilities.GetProcessImageName(process.Id, ntPathLookupTable ?? FileSystemUtilities.GetNtPathLookupTable());
            Process = process;

            // Get the main module base address and read the version resource from memory.
            SafeHGlobalHandle versionResource;
            using (var processHandle = Kernel32.OpenProcess(PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_INFORMATION | PROCESS_ACCESS_RIGHTS.PROCESS_VM_READ, false, (uint)process.Id))
            {
                try
                {
                    versionResource = ReadVersionResource(processHandle, GetMainModuleInfo(processHandle));
                }
                catch
                {
                    return;
                }
            }

            // If we got a valid version resource, parse it.
            using (versionResource)
            {
                // Read the version information from the resource.
                Version32.VerQueryValue(versionResource, @"\", out var fixedInfoPtr, out _);
                FixedFileInfo = Marshal.PtrToStructure<VS_FIXEDFILEINFO>(fixedInfoPtr);
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
                var codepageTable = GetTranslationTableCombinations(versionResource).ToList();
                Language = GetFileVersionLanguage(versionResource, codepageTable[0]); bool success = false;
                foreach (var codepage in codepageTable)
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
        }

        /// <summary>
        /// Gets information about the main module of the specified process.
        /// </summary>
        private static MODULEINFO GetMainModuleInfo(SafeFileHandle processHandle)
        {
            // Get all process modules, then return the first one (main module).
            PsApi.EnumProcessModules(processHandle, SafeMemoryHandle.Null, out var bytesNeeded);
            using var moduleBuffer = SafeHGlobalHandle.Alloc((int)bytesNeeded);
            PsApi.EnumProcessModules(processHandle, moduleBuffer, out bytesNeeded);
            PsApi.GetModuleInformation(processHandle, moduleBuffer.ToStructure<HMODULE>(), out var moduleInfo);
            return moduleInfo;
        }

        /// <summary>
        /// Reads the version resource from the process memory.
        /// </summary>
        private static SafeHGlobalHandle ReadVersionResource(SafeFileHandle processHandle, in MODULEINFO moduleInfo)
        {
            // Read the DOS header to make sure we have a valid PE header.
            IntPtr baseAddress; unsafe { baseAddress = (IntPtr)moduleInfo.lpBaseOfDll; }
            var dosHeader = ReadProcessMemory<IMAGE_DOS_HEADER>(processHandle, baseAddress);
            if (dosHeader.e_magic != PInvoke.IMAGE_DOS_SIGNATURE)
            {
                throw new InvalidOperationException("The specified process does not have a valid PE header.");
            }

            // Read the NT headers to check the magic number.
            var ntHeadersAddress = baseAddress + dosHeader.e_lfanew;
            var ntHeaders64 = ReadProcessMemory<IMAGE_NT_HEADERS64>(processHandle, ntHeadersAddress);
            if (ntHeaders64.Signature != PInvoke.IMAGE_NT_SIGNATURE)
            {
                throw new InvalidOperationException("The specified process does not have a valid NT header signature.");
            }

            // Determine the resource directory RVA and size based on the optional header magic number.
            uint resourceRva, resourceSize;
            if (ntHeaders64.OptionalHeader.Magic == IMAGE_OPTIONAL_HEADER_MAGIC.IMAGE_NT_OPTIONAL_HDR32_MAGIC) // PE32
            {
                var ntHeaders32 = ReadProcessMemory<IMAGE_NT_HEADERS32>(processHandle, ntHeadersAddress);
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
            if (resourceSize == 0)
            {
                throw new InvalidOperationException("The specified process does not have a valid resource directory size.");
            }
            return FindVersionResource(processHandle, baseAddress + (int)resourceRva, baseAddress);
        }

        /// <summary>
        /// Navigates the resource directory structure to find the version resource.
        /// </summary>
        private static SafeHGlobalHandle FindVersionResource(SafeFileHandle processHandle, IntPtr resourceDirectoryAddress, IntPtr baseAddress)
        {
            // Read the resource directory
            var resourceDir = ReadProcessMemory<IMAGE_RESOURCE_DIRECTORY>(processHandle, resourceDirectoryAddress);
            var totalEntries = resourceDir.NumberOfNamedEntries + resourceDir.NumberOfIdEntries;
            var entriesAddress = resourceDirectoryAddress + Marshal.SizeOf<IMAGE_RESOURCE_DIRECTORY>();

            // Look for RT_VERSION resource (type 16) and throw if not found.
            for (int i = 0; i < totalEntries; i++)
            {
                var entryAddress = entriesAddress + (i * Marshal.SizeOf<IMAGE_RESOURCE_DIRECTORY_ENTRY>());
                var entry = ReadProcessMemory<IMAGE_RESOURCE_DIRECTORY_ENTRY>(processHandle, entryAddress);
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
        private static SafeHGlobalHandle ReadVersionResourceData(SafeFileHandle processHandle, IntPtr resourceDirectoryAddress, IntPtr baseAddress, uint offsetToData)
        {
            // Navigate through the directory levels using a do/while loop.
            var currentOffsetToData = offsetToData;
            IMAGE_RESOURCE_DIRECTORY_ENTRY currentEntry;
            do
            {
                var currentAddress = resourceDirectoryAddress + (int)(currentOffsetToData & IMAGE_RESOURCE_RVA_MASK);
                var currentEntryAddress = currentAddress + Marshal.SizeOf<IMAGE_RESOURCE_DIRECTORY>();
                currentEntry = ReadProcessMemory<IMAGE_RESOURCE_DIRECTORY_ENTRY>(processHandle, currentEntryAddress);
                currentOffsetToData = currentEntry.Anonymous2.OffsetToData;
            }
            while ((currentOffsetToData & PInvoke.IMAGE_RESOURCE_DATA_IS_DIRECTORY) != 0);

            // At this point, currentEntry points to a data entry, not a directory.
            var dataEntryAddress = resourceDirectoryAddress + (int)currentEntry.Anonymous2.OffsetToData;
            var dataEntry = ReadProcessMemory<IMAGE_RESOURCE_DATA_ENTRY>(processHandle, dataEntryAddress);
            if (dataEntry.Size > 0)
            {
                var buffer = SafeHGlobalHandle.Alloc((int)dataEntry.Size);
                Kernel32.ReadProcessMemory(processHandle, baseAddress + (int)dataEntry.OffsetToData, buffer, out _);
                return buffer;
            }
            throw new InvalidOperationException($"Invalid data entry size: {dataEntry.Size} at address 0x{dataEntryAddress.ToInt64():X}");
        }

        /// <summary>
        /// Gets language/codepage combinations from the Translation table.
        /// </summary>
        private static IEnumerable<string> GetTranslationTableCombinations(SafeHGlobalHandle versionResource)
        {
            // Return any translation pairs found in the version resource.
            Version32.VerQueryValue(versionResource, @"\VarFileInfo\Translation", out var translationPtr, out var translationLength);
            var langAndCodepageSize = Marshal.SizeOf<Version32.LANGANDCODEPAGE>();
            for (int i = 0; i < translationLength / langAndCodepageSize; i++)
            {
                yield return Marshal.PtrToStructure<Version32.LANGANDCODEPAGE>(IntPtr.Add(translationPtr, i * langAndCodepageSize)).ToTranslationTableString();
            }

            // Add some common fallback combinations that are known to work in many cases.
            // These are based on common language/codepage pairs used in version resources.
            yield return "040904B0";
            yield return "040904E4";
            yield return "04090000";
        }

        /// <summary>
        /// Retrieves the language name associated with a specified codepage from a version resource.
        /// </summary>
        /// <remarks>The method uses the high-order word of the codepage to determine the language
        /// name.</remarks>
        /// <param name="versionResource">A handle to the version resource containing the language information.</param>
        /// <param name="codepage">A string representing the codepage, which is used to identify the language.</param>
        /// <returns>A string containing the language name if the codepage is valid and the language can be determined;
        /// otherwise, <see langword="null"/>.</returns>
        private static string? GetFileVersionLanguage(SafeHGlobalHandle versionResource, string codepage)
        {
            Span<char> szLang = stackalloc char[(int)PInvoke.MAX_PATH];
            var len = Kernel32.VerLanguageName(PInvoke.HIWORD(uint.Parse(codepage, NumberStyles.HexNumber)), szLang);
            string result = szLang.Slice(0, (int)len).ToString().TrimRemoveNull();
            if (!string.IsNullOrWhiteSpace(result))
            {
                return result;
            }
            return null;
        }

        /// <summary>
        /// Retrieves the version string from a specified version resource.
        /// </summary>
        /// <remarks>This method queries the specified version resource for the version information
        /// associated with the given name. If the information is found and is not empty or whitespace, it returns the
        /// version string; otherwise, it returns <see langword="null"/>.</remarks>
        /// <param name="versionResource">A handle to the version resource from which to retrieve the version string.</param>
        /// <param name="name">The name of the version information to query.</param>
        /// <returns>A string containing the version information if found and not empty; otherwise, <see langword="null"/>.</returns>
        private static string? GetFileVersionString(SafeHGlobalHandle versionResource, string codepage, string name, ref bool success)
        {
            // Attempt to query the version resource for the specified name.
            try
            {
                if (Version32.VerQueryValue(versionResource, string.Format(CultureInfo.InvariantCulture, @"\StringFileInfo\{0}\{1}", codepage, name), out var lplpBuffer, out var _) && lplpBuffer != IntPtr.Zero)
                {
                    string? result = Marshal.PtrToStringUni(lplpBuffer)?.TrimRemoveNull();
                    if (!string.IsNullOrWhiteSpace(result))
                    {
                        success = true;
                        return result;
                    }
                }
            }
            catch
            {
                return null;
            }
            return null;
        }

        /// <summary>
        /// Reads a structure from process memory using stack-allocated buffer.
        /// </summary>
        private static T ReadProcessMemory<T>(SafeFileHandle processHandle, IntPtr address) where T : struct
        {
            using var buffer = SafeHGlobalHandle.Alloc(Marshal.SizeOf<T>());
            Kernel32.ReadProcessMemory(processHandle, address, buffer, out _);
            return buffer.ToStructure<T>();
        }

        /// <summary>
        /// Returns a partial list of properties in the System.Diagnostics.ProcessVersionInfo and their values.
        /// </summary>
        /// <remarks>The formatting of this methood 1:1 matches System.Diagnostics.FileVersionInfo.</remarks>
        public override string ToString()
        {
            StringBuilder stringBuilder = new(128);
            string value = "\r\n";
            stringBuilder.Append("File:             ");
            stringBuilder.Append(FileName);
            stringBuilder.Append(value);
            stringBuilder.Append("InternalName:     ");
            stringBuilder.Append(InternalName);
            stringBuilder.Append(value);
            stringBuilder.Append("OriginalFilename: ");
            stringBuilder.Append(OriginalFilename);
            stringBuilder.Append(value);
            stringBuilder.Append("FileVersion:      ");
            stringBuilder.Append(FileVersion);
            stringBuilder.Append(value);
            stringBuilder.Append("FileDescription:  ");
            stringBuilder.Append(FileDescription);
            stringBuilder.Append(value);
            stringBuilder.Append("Product:          ");
            stringBuilder.Append(ProductName);
            stringBuilder.Append(value);
            stringBuilder.Append("ProductVersion:   ");
            stringBuilder.Append(ProductVersion);
            stringBuilder.Append(value);
            stringBuilder.Append("Debug:            ");
            stringBuilder.Append(IsDebug);
            stringBuilder.Append(value);
            stringBuilder.Append("Patched:          ");
            stringBuilder.Append(IsPatched);
            stringBuilder.Append(value);
            stringBuilder.Append("PreRelease:       ");
            stringBuilder.Append(IsPreRelease);
            stringBuilder.Append(value);
            stringBuilder.Append("PrivateBuild:     ");
            stringBuilder.Append(IsPrivateBuild);
            stringBuilder.Append(value);
            stringBuilder.Append("SpecialBuild:     ");
            stringBuilder.Append(IsSpecialBuild);
            stringBuilder.Append(value);
            stringBuilder.Append("Language:         ");
            stringBuilder.Append(Language);
            stringBuilder.Append(value);
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Gets the raw version information of the file.
        /// </summary>
        public readonly Version FileVersionRaw = new(0, 0, 0, 0);

        /// <summary>
        /// Gets the raw version information of the product.
        /// </summary>
        public readonly Version ProductVersionRaw = new(0, 0, 0, 0);

        /// <summary>
        /// Gets the comments associated with the file.
        /// </summary>
        public readonly string? Comments;

        /// <summary>
        /// Gets the name of the company that produced the file.
        /// </summary>
        public readonly string? CompanyName;

        /// <summary>
        /// Gets the build number of the file.
        /// </summary>
        public readonly int FileBuildPart;

        /// <summary>
        /// Gets the description of the file.
        /// </summary>
        public readonly string? FileDescription;

        /// <summary>
        /// Gets the major part of the version number.
        /// </summary>
        public readonly int FileMajorPart;

        /// <summary>
        /// Gets the minor part of the version number of the file.
        /// </summary>
        public readonly int FileMinorPart;

        /// <summary>
        /// Gets the name of the file that this object instance describes.
        /// </summary>
        public readonly string FileName;

        /// <summary>
        /// Gets the file private part number.
        /// </summary>
        public readonly int FilePrivatePart;

        /// <summary>
        /// Gets the file version number.
        /// </summary>
        public readonly string? FileVersion;

        /// <summary>
        /// Gets the internal name of the file, if one exists.
        /// </summary>
        public readonly string? InternalName;

        /// <summary>
        /// Gets a value that specifies whether the file contains debugging information or is compiled with debugging features enabled.
        /// </summary>
        public readonly bool IsDebug;

        /// <summary>
        /// Gets a value that specifies whether the file has been modified and is not identical to the original shipping file of the same version number.
        /// </summary>
        public readonly bool IsPatched;

        /// <summary>
        /// Gets a value that specifies whether the file was built using standard release procedures.
        /// </summary>
        public readonly bool IsPrivateBuild;

        /// <summary>
        /// Gets a value that specifies whether the file is a development version, rather than a commercially released product.
        /// </summary>
        public readonly bool IsPreRelease;

        /// <summary>
        /// Gets a value that specifies whether the file is a special build.
        /// </summary>
        public readonly bool IsSpecialBuild;

        /// <summary>
        /// Gets the default language string for the version info block.
        /// </summary>
        public readonly string? Language;

        /// <summary>
        /// Gets all copyright notices that apply to the specified file.
        /// </summary>
        public readonly string? LegalCopyright;

        /// <summary>
        /// Gets the trademarks and registered trademarks that apply to the file.
        /// </summary>
        public readonly string? LegalTrademarks;

        /// <summary>
        /// Gets the name the file was created with.
        /// </summary>
        public readonly string? OriginalFilename;

        /// <summary>
        /// Gets information about a private version of the file.
        /// </summary>
        public readonly string? PrivateBuild;

        /// <summary>
        /// Represents the process associated with the current operation.
        /// </summary>
        /// <remarks>This field provides access to the underlying process object, allowing inspection and
        /// control of the process's execution. It is read-only and should be used to retrieve information about the
        /// process or to perform operations such as starting, stopping, or monitoring the process.</remarks>
        public readonly Process Process;

        /// <summary>
        /// Gets the build number of the product this file is associated with.
        /// </summary>
        public readonly int ProductBuildPart;

        /// <summary>
        /// Gets the major part of the version number for the product this file is associated with.
        /// </summary>
        public readonly int ProductMajorPart;

        /// <summary>
        /// Gets the minor part of the version number for the product the file is associated with.
        /// </summary>
        public readonly int ProductMinorPart;

        /// <summary>
        /// Gets the name of the product this file is distributed with.
        /// </summary>
        public readonly string? ProductName;

        /// <summary>
        /// Gets the private part number of the product this file is associated with.
        /// </summary>
        public readonly int ProductPrivatePart;

        /// <summary>
        /// Gets the version of the product this file is distributed with.
        /// </summary>
        public readonly string? ProductVersion;

        /// <summary>
        /// Gets the special build information for the file.
        /// </summary>
        public readonly string? SpecialBuild;

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
        /// cref="PInvoke.IMAGE_RESOURCE_DATA_IS_DIRECTORY"/>  to isolate the RVA portion of an image resource entry. It
        /// is typically used in scenarios where the directory flag needs to be cleared to obtain the actual
        /// RVA.</remarks>
        private const uint IMAGE_RESOURCE_RVA_MASK = ~PInvoke.IMAGE_RESOURCE_DATA_IS_DIRECTORY;
    }
}
