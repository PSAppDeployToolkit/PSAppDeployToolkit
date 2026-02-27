using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text.RegularExpressions;
using System.Xml;
using PSADT.Extensions;
using PSADT.Interop;
using PSADT.Interop.Extensions;
using PSADT.Utilities;
using Windows.Win32;
using Windows.Win32.System.ApplicationInstallationAndServicing;
using Windows.Win32.System.LibraryLoader;
using Windows.Win32.System.Variant;

namespace PSADT.WindowsInstaller
{
    /// <summary>
    /// Public P/Invokes from the msi.dll library.
    /// </summary>
    public static class MsiUtilities
    {
        /// <summary>
        /// Retrieves the message string associated with an MSI exit code from the msimsg.dll resource.
        /// </summary>
        /// <param name="exitCode">The MSI exit code.</param>
        /// <returns>The message string associated with the given MSI exit code.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the library cannot be loaded or the message cannot be retrieved.</exception>
        public static string? GetMessageFromMsiExitCode(uint exitCode)
        {
            using FreeLibrarySafeHandle hInstance = NativeMethods.LoadLibraryEx("msimsg.dll", LOAD_LIBRARY_FLAGS.LOAD_LIBRARY_SEARCH_SYSTEM32 | LOAD_LIBRARY_FLAGS.LOAD_LIBRARY_AS_DATAFILE);
            string? lpBuffer;
            try
            {
                _ = NativeMethods.LoadString(hInstance, exitCode, out lpBuffer);
            }
            catch
            {
                return null;
                throw;
            }
            return !string.IsNullOrWhiteSpace(lpBuffer) ? Regex.Replace(lpBuffer, @"\s{2,}", " ") : null;
        }

        /// <summary>
        /// Retrieves a read-only dictionary of key-value pairs from a specified table in an MSI database, using the
        /// provided column indices for keys and values.
        /// </summary>
        /// <remarks>The method applies any provided transform files before querying the database. If the
        /// table or columns specified do not exist, an exception is thrown. The returned dictionary is immutable and
        /// will be null if the table contains no matching rows.</remarks>
        /// <param name="szDatabasePath">The path to the MSI database file to query.</param>
        /// <param name="table">The name of the table within the MSI database from which to retrieve properties.</param>
        /// <param name="keyColumn">The zero-based index of the column to use as the key in the resulting dictionary.</param>
        /// <param name="valueColumn">The zero-based index of the column to use as the value in the resulting dictionary.</param>
        /// <param name="szTransformFiles">An optional collection of transform files to apply when opening the database. May be null if no transforms
        /// are required.</param>
        /// <returns>A read-only dictionary containing the key-value pairs from the specified table and columns, or null if no
        /// properties are found.</returns>
        /// <exception cref="InvalidDataException">Thrown if the specified table or column indices are not found in the database.</exception>
        public static IReadOnlyDictionary<string, string>? GetMsiTableDictionary(string szDatabasePath, string table, int keyColumn, int valueColumn, params IReadOnlyList<string>? szTransformFiles)
        {
            // Open the database, factoring in any transforms provided, then confirm the caller input is valid.
            using MsiCloseHandleSafeHandle hDatabase = OpenDatabase(szDatabasePath, szTransformFiles);
            if (ResolveTableName(hDatabase, table) is not string resolvedTableName)
            {
                throw new InvalidDataException($"The specified table '{table}' was not found in the database.");
            }
            if (ResolveColumnName(hDatabase, resolvedTableName, keyColumn) is not string keyColumnName)
            {
                throw new InvalidDataException($"The specified key column number '{keyColumn}' was not found in the table '{resolvedTableName}'.");
            }
            if (ResolveColumnName(hDatabase, resolvedTableName, valueColumn) is not string valueColumnName)
            {
                throw new InvalidDataException($"The specified value column number '{valueColumn}' was not found in the table '{resolvedTableName}'.");
            }

            // Query the database for the specified table and columns, then build a dictionary from the results.
            _ = NativeMethods.MsiDatabaseOpenView(hDatabase, $"SELECT `{keyColumnName}`, `{valueColumnName}` FROM `{resolvedTableName}`", out MsiCloseHandleSafeHandle hView);
            using (hView)
            {
                _ = NativeMethods.MsiViewExecute(hView);
                Dictionary<string, string> result = [];
                while (true)
                {
                    using MsiCloseHandleSafeHandle? hRecord = ViewFetch(hView);
                    if (hRecord is null)
                    {
                        break;
                    }
                    if (GetRecordString(hRecord, 1) is string key && GetRecordString(hRecord, 2) is string value)
                    {
                        result.Add(key, value);
                    }
                }
                return result.Count > 0 ? new ReadOnlyDictionary<string, string>(result) : null;
            }
        }

        /// <summary>
        /// Retrieves the values from a specified column in a given table within an MSI database file.
        /// </summary>
        /// <param name="szDatabasePath">The path to the MSI database file from which to retrieve column values. Cannot be null or empty.</param>
        /// <param name="table">The name of the table to query for column values. Must exist in the database.</param>
        /// <param name="column">The zero-based index of the column whose values are to be retrieved. Must refer to a valid column in the
        /// specified table.</param>
        /// <param name="szTransformFiles">An optional collection of transform files to apply when opening the database. If provided, transforms are
        /// applied in the order specified.</param>
        /// <returns>A read-only list of strings containing the values from the specified column in the specified table. The list
        /// will be empty if the table contains no rows.</returns>
        /// <exception cref="InvalidDataException">Thrown if the specified table or column does not exist in the database.</exception>
        public static IReadOnlyList<string> GetMsiTableColumnValues(string szDatabasePath, string table, int column, params IReadOnlyList<string>? szTransformFiles)
        {
            // Open the database, factoring in any transforms provided, then confirm the caller input is valid.
            using MsiCloseHandleSafeHandle hDatabase = OpenDatabase(szDatabasePath, szTransformFiles);
            if (ResolveTableName(hDatabase, table) is not string resolvedTableName)
            {
                throw new InvalidDataException($"The specified table '{table}' was not found in the database.");
            }
            if (ResolveColumnName(hDatabase, resolvedTableName, column) is not string columnName)
            {
                throw new InvalidDataException($"The specified column number '{column}' was not found in the table '{resolvedTableName}'.");
            }

            // Query the database for the specified table and columns, then build a dictionary from the results.
            _ = NativeMethods.MsiDatabaseOpenView(hDatabase, $"SELECT `{columnName}` FROM `{resolvedTableName}`", out MsiCloseHandleSafeHandle hView);
            using (hView)
            {
                _ = NativeMethods.MsiViewExecute(hView);
                List<string> result = [];
                while (true)
                {
                    using MsiCloseHandleSafeHandle? hRecord = ViewFetch(hView);
                    if (hRecord is null)
                    {
                        break;
                    }
                    if (GetRecordString(hRecord, 1) is string value)
                    {
                        result.Add(value);
                    }
                }
                return new ReadOnlyCollection<string>(result);
            }
        }

        /// <summary>
        /// Creates a new Windows Installer transform (MST) file that updates specified properties in an MSI package.
        /// Optionally applies an existing transform before generating the new one.
        /// </summary>
        /// <remarks>This method creates a temporary copy of the original MSI, applies property changes,
        /// and generates a transform file representing those changes. If an existing transform is specified, it is
        /// applied before property modifications. The method ensures that all file paths are absolute and that no
        /// existing files are overwritten unintentionally.</remarks>
        /// <param name="msiPath">The path to the original MSI file that serves as the baseline for the transform. This path must be valid and
        /// the file must exist.</param>
        /// <param name="newTransformPath">The absolute path where the new transform file will be created. The file must not already exist.</param>
        /// <param name="transformProperties">A read-only dictionary containing the property names and their new values to be included in the transform.
        /// At least one property must be specified.</param>
        /// <param name="applyTransformPath">An optional path to an existing transform file to apply to the temporary MSI before creating the new
        /// transform. If specified, the file must exist.</param>
        /// <param name="tempMsiPath">An optional path for a temporary MSI file used during transform creation. If not specified, a temporary file
        /// is created automatically. The file must not already exist.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="msiPath"/>, <paramref name="newTransformPath"/>, or <paramref
        /// name="transformProperties"/> is null.</exception>
        /// <exception cref="FileNotFoundException">Thrown if the file specified by <paramref name="msiPath"/> or <paramref name="applyTransformPath"/> does not
        /// exist.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="newTransformPath"/> is not an absolute path, if <paramref
        /// name="transformProperties"/> is empty or contains null or empty keys, or if <paramref name="tempMsiPath"/>
        /// already exists.</exception>
        /// <exception cref="IOException">Thrown if the transform file could not be generated at the specified <paramref name="newTransformPath"/>.</exception>
        public static void CreatePropertyTransformFile(string msiPath, string newTransformPath, IReadOnlyDictionary<string, string> transformProperties, string? applyTransformPath = null, string? tempMsiPath = null)
        {
            // Validate input parameters.
            if (!File.Exists(msiPath = Path.GetFullPath(msiPath.ThrowIfNullOrWhiteSpace())))
            {
                throw new FileNotFoundException("MSI file not found.", msiPath);
            }
            if (!Path.IsPathRooted(newTransformPath = Path.GetFullPath(newTransformPath.ThrowIfNullOrWhiteSpace())))
            {
                throw new ArgumentException("The new transform path must be an absolute path.", nameof(newTransformPath));
            }
            if (transformProperties is null)
            {
                throw new ArgumentNullException(nameof(transformProperties));
            }
            if (transformProperties.Count == 0)
            {
                throw new ArgumentException("TransformProperties cannot be empty. At least one property must be specified to create a transform.", nameof(transformProperties));
            }
            if (!string.IsNullOrWhiteSpace(applyTransformPath) && !File.Exists(applyTransformPath = Path.GetFullPath(applyTransformPath)))
            {
                throw new FileNotFoundException("The transform file specified in ApplyTransformPath was not found.", applyTransformPath);
            }

            // Set up the temp MSI path.
            bool deleteTempMsi; if (!string.IsNullOrWhiteSpace(tempMsiPath))
            {
                if (File.Exists(tempMsiPath = Path.GetFullPath(tempMsiPath)))
                {
                    throw new InvalidOperationException("The specified temp MSI path already exists. Please provide a non-existing path or allow the method to create a temp file automatically.");
                }
                _ = Directory.CreateDirectory(Path.GetDirectoryName(tempMsiPath)!);
                deleteTempMsi = false;
            }
            else
            {
                tempMsiPath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(msiPath) + "_" + Guid.NewGuid().ToString("N") + ".msi"));
                deleteTempMsi = true;
            }

            // Copy MSI to temp.
            File.Copy(msiPath, tempMsiPath, overwrite: true);
            try
            {
                // Open original read-only.
                using MsiCloseHandleSafeHandle hDatabaseOrig = OpenDatabase(msiPath, MSI_PERSISTENCE_MODE.MSIDBOPEN_READONLY);

                // Open temp in transact mode so we can modify + commit.
                using (MsiCloseHandleSafeHandle hDatabaseTemp = !string.IsNullOrWhiteSpace(applyTransformPath) ? OpenDatabase(tempMsiPath, MSI_PERSISTENCE_MODE.MSIDBOPEN_TRANSACT, applyTransformPath!) : OpenDatabase(tempMsiPath, MSI_PERSISTENCE_MODE.MSIDBOPEN_TRANSACT))
                {
                    // One view, executed once, then assign repeatedly. No string interpolation of keys/values; we use records.
                    _ = NativeMethods.MsiDatabaseOpenView(hDatabaseTemp, "SELECT `Property`,`Value` FROM `Property`", out MsiCloseHandleSafeHandle hView);
                    using (hView)
                    {
                        // Reuse a single record handle for speed.
                        _ = NativeMethods.MsiViewExecute(hView);
                        using MsiCloseHandleSafeHandle hRecord = NativeMethods.MsiCreateRecord(2);
                        foreach (KeyValuePair<string, string> kvp in transformProperties)
                        {
                            // Don't allow a null/empty key.
                            if (string.IsNullOrWhiteSpace(kvp.Key))
                            {
                                throw new InvalidOperationException("TransformProperties cannot contain null or empty keys.");
                            }

                            // Field indices are 1-based in MSI records.
                            _ = NativeMethods.MsiRecordSetString(hRecord, 1, kvp.Key);
                            _ = NativeMethods.MsiRecordSetString(hRecord, 2, kvp.Value);
                            _ = NativeMethods.MsiViewModify(hView, MSIMODIFY.MSIMODIFY_ASSIGN, hRecord);
                        }
                    }

                    // Commit changes to the temp DB.
                    _ = NativeMethods.MsiDatabaseCommit(hDatabaseTemp);
                }

                // Reopen temp read-only for transform generation.
                using MsiCloseHandleSafeHandle hDatabaseTempRo = OpenDatabase(tempMsiPath, MSI_PERSISTENCE_MODE.MSIDBOPEN_READONLY);

                // Remove existing MST if present.
                if (!File.Exists(newTransformPath))
                {
                    _ = Directory.CreateDirectory(Path.GetDirectoryName(newTransformPath)!);
                }
                else
                {
                    File.Delete(newTransformPath);
                }

                // Generate transform: temp (modified) vs original (baseline)
                _ = NativeMethods.MsiDatabaseGenerateTransform(hDatabaseTempRo, hDatabaseOrig, newTransformPath);

                // Create summary info for the transform (equivalent to CreateTransformSummaryInfo)
                MSITRANSFORM_VALIDATE validateFlags = (MSITRANSFORM_VALIDATE)((Enum.GetValues(typeof(MSITRANSFORM_VALIDATE)).Cast<MSITRANSFORM_VALIDATE>().Max(static v => Convert.ToUInt32(v, CultureInfo.InvariantCulture)) << 1) - 1);
                _ = NativeMethods.MsiCreateTransformSummaryInfo(hDatabaseTempRo, hDatabaseOrig, newTransformPath, MSITRANSFORM_ERROR.MSITRANSFORM_ERROR_NONE, validateFlags);
            }
            finally
            {
                // Remove temp MSI if we created it.
                if (deleteTempMsi && File.Exists(tempMsiPath))
                {
                    File.Delete(tempMsiPath);
                }
            }
        }

        /// <summary>
        /// Retrieves the list of product codes supported by the specified Windows Installer patch package (MSP file).
        /// </summary>
        /// <remarks>The returned product codes indicate which products the patch can be applied to. This
        /// method does not validate the existence or format of the specified file; callers should ensure that the path
        /// points to a valid MSP file.</remarks>
        /// <param name="szDatabasePath">The file path to the patch package (MSP) database. Cannot be null or empty.</param>
        /// <returns>A string containing the supported product codes, separated by semicolons. Returns an empty string if no
        /// product codes are found.</returns>
        public static IReadOnlyList<Guid> GetMspSupportedProductCodes(string szDatabasePath)
        {
            // Get the summary information from the patch database, then determine the size of the buffer we need.
            using MsiCloseHandleSafeHandle hSummaryInfo = GetSummaryInformation(szDatabasePath);
            return GetSummaryInfoStringProperty(hSummaryInfo, MSI_PROPERTY_ID.PID_TEMPLATE) is not string template || string.IsNullOrWhiteSpace(template)
                ? throw new InvalidOperationException("The patch database did not contain a valid PID_TEMPLATE property with supported product codes.")
                : (IReadOnlyList<Guid>)new ReadOnlyCollection<Guid>([.. template.Split(';').Select(static g => new Guid(g))]);
        }

        /// <summary>
        /// Extracts and loads the XML data embedded in a Windows Installer patch file into an XmlDocument.
        /// </summary>
        /// <remarks>This method reads the XML data stored within a Windows Installer patch file using the
        /// MsiExtractPatchXMLData API. The returned XmlDocument represents the patch's internal XML metadata, which may
        /// include information about the patch contents, target products, and other installer details. The caller is
        /// responsible for handling any exceptions that may occur if the file is invalid or does not contain XML
        /// data.</remarks>
        /// <param name="szPatchPath">The full path to the patch file (.msp) from which to extract XML data. Must not be null or empty.</param>
        /// <returns>An XmlDocument containing the XML data extracted from the specified patch file.</returns>
        public static XmlDocument ExtractPatchXmlData(string szPatchPath)
        {
            _ = NativeMethods.MsiExtractPatchXMLData(szPatchPath, null, out uint requiredLength);
            Span<char> bufSpan = stackalloc char[(int)requiredLength + 1];
            _ = NativeMethods.MsiExtractPatchXMLData(szPatchPath, bufSpan, out _);
            return XmlUtilities.SafeLoadFromText(bufSpan.Slice(0, (int)requiredLength).ToString());
        }

        /// <summary>
        /// Retrieves the installation state of a product identified by its unique product code.
        /// </summary>
        /// <remarks>This method calls the Windows Installer to determine the installation state. Ensure
        /// that the provided product code is a valid GUID; otherwise, the returned state may not accurately reflect the
        /// product's status.</remarks>
        /// <param name="productCode">The unique identifier (GUID) of the product whose installation state is to be queried.</param>
        /// <returns>An INSTALLSTATE value that indicates the current installation state of the specified product.</returns>
        public static Interop.INSTALLSTATE QueryProductState(Guid productCode)
        {
            return (Interop.INSTALLSTATE)NativeMethods.MsiQueryProductState(productCode);
        }

        /// <summary>
        /// Converts a 32-bit integer representing an MSI version into a corresponding <see cref="Version"/> object.
        /// </summary>
        /// <remarks>This method is useful for interpreting version information stored in the DWORD format
        /// commonly used by Windows Installer (MSI) packages. The resulting <see cref="Version"/> object can be used
        /// for version comparisons and display within .NET applications.</remarks>
        /// <param name="v">A 32-bit integer in which the major version is stored in the highest byte, the minor version in the next
        /// highest byte, and the build number in the lowest two bytes, as used by Windows Installer (MSI) versioning.</param>
        /// <returns>A <see cref="Version"/> object containing the major, minor, and build numbers extracted from the specified
        /// MSI version integer.</returns>
        public static Version ParseVersionDWord(int v)
        {
            return new((v >> 24) & 0xFF, (v >> 16) & 0xFF, v & 0xFFFF);
        }

        /// <summary>
        /// Converts a GUID to its compressed hexadecimal string representation in MSI packed form.
        /// </summary>
        /// <remarks>The compressed format arranges each byte of the GUID with the low nibble first,
        /// followed by the high nibble, as required by MSI packed form. This method is useful for scenarios where GUIDs
        /// must be represented in a compact, MSI-compatible string format.</remarks>
        /// <param name="unpacked">The GUID to be converted to a compressed hexadecimal string.</param>
        /// <returns>A string containing the compressed hexadecimal format of the provided GUID.</returns>
        public static string CompressGuid(Guid unpacked)
        {
            // Internal helper method to convert nibble to char.
            static char ToHexUpper(int nibble)
            {
                return (char)(nibble < 10 ? ('0' + nibble) : ('A' + (nibble - 10)));
            }

            // Create a 1-element span over the Guid, then view it as bytes (no allocations).
            byte[] bytes = unpacked.ToByteArray(); int o = 0;
            Span<char> destination = stackalloc char[32];
            for (int i = 0; i < 16; i++)
            {
                // MSI packed form wants low nibble first, then high nibble.
                byte b = bytes[i];
                destination[o++] = ToHexUpper(b & 0x0F);
                destination[o++] = ToHexUpper(b >> 4);
            }
            return destination.ToString();
        }

        /// <summary>
        /// Converts a packed 32-character string representation of a GUID, as used by Windows Installer (MSI), into a
        /// Guid object.
        /// </summary>
        /// <remarks>This method is intended for use with GUIDs stored in the packed format commonly found
        /// in MSI databases. The input string must be exactly 32 characters in length and formatted according to MSI
        /// conventions.</remarks>
        /// <param name="packed32">The packed 32-character string representing a GUID in MSI format. Cannot be null.</param>
        /// <returns>A Guid object that corresponds to the specified packed MSI GUID string.</returns>
        public static Guid DecompressPackedGuid(string packed32)
        {
            return DecompressPackedGuid(packed32.AsSpan());
        }

        /// <summary>
        /// Converts a 32-character hexadecimal string representation of a GUID in MSI packed format to a Guid
        /// structure.
        /// </summary>
        /// <remarks>The input string must be in the MSI packed GUID format, where the first 8 characters
        /// represent the most significant bits in reversed nibble order, the next 4 characters represent the next 16
        /// bits in reversed nibble order, the following 4 characters represent the next 16 bits in reversed nibble
        /// order, and the final 16 characters represent the remaining 8 bytes with each byte's nibbles swapped. This
        /// format is commonly used in Windows Installer databases.</remarks>
        /// <param name="packed32">A read-only span of 32 characters containing the packed hexadecimal representation of a GUID. Each character
        /// must be a valid hexadecimal digit (0-9, A-F, a-f).</param>
        /// <returns>A Guid structure that represents the GUID decoded from the specified packed hexadecimal string.</returns>
        /// <exception cref="ArgumentException">Thrown if packed32 does not contain exactly 32 characters or contains non-hexadecimal characters.</exception>
        internal static Guid DecompressPackedGuid(ReadOnlySpan<char> packed32)
        {
            // Validate provided input.
            if (packed32.Length != 32)
            {
                throw new ArgumentException("Expected 32 hex characters.", nameof(packed32));
            }
            for (int idx = 0; idx < 32; idx++)
            {
                if (packed32[idx] is not ((>= '0' and <= '9') or (>= 'A' and <= 'F') or (>= 'a' and <= 'f')))
                {
                    throw new ArgumentException("Input contained non-hex characters.", nameof(packed32));
                }
            }

            // Internal helper methods extract characters and reverse ordering.
            static int HexNibble(char c)
            {
                return c is >= '0' and <= '9' ? c - '0' : c is >= 'A' and <= 'F' ? c - 'A' + 10 : c - 'a' + 10;
            }
            static int ReadInt32FromReversedChars(ReadOnlySpan<char> s8)
            {
                // Build from reversed char order, 1 nibble at a time.
                uint v = 0; for (int i = 0; i < 8; i++)
                {
                    v = (v << 4) | (uint)HexNibble(s8[7 - i]);
                }
                return unchecked((int)v);
            }
            static short ReadInt16FromReversedChars(ReadOnlySpan<char> s4)
            {
                // Build from reversed char order, 1 nibble at a time.
                uint v = 0; for (int i = 0; i < 4; i++)
                {
                    v = (v << 4) | (uint)HexNibble(s4[3 - i]);
                }
                return unchecked((short)v);
            }
            static byte ReadByteFromSwappedPair(ReadOnlySpan<char> s32, int p)
            {
                // Packed tail swaps each 2-char pair: "AB" -> "BA".
                // So at position p, byte is hex of chars [p+1][p].
                return (byte)((HexNibble(s32[p + 1]) << 4) | HexNibble(s32[p]));
            }

            // Packed form needs:
            // - first 8 chars reversed (by nibble) -> Data1 (uint)
            // - next 4 reversed -> Data2 (ushort)
            // - next 4 reversed -> Data3 (ushort)
            // - last 16: swap each byte pair -> Data4[8]
            return new(
                ReadInt32FromReversedChars(packed32.Slice(0, 8)),
                ReadInt16FromReversedChars(packed32.Slice(8, 4)),
                ReadInt16FromReversedChars(packed32.Slice(12, 4)),
                ReadByteFromSwappedPair(packed32, 16),
                ReadByteFromSwappedPair(packed32, 18),
                ReadByteFromSwappedPair(packed32, 20),
                ReadByteFromSwappedPair(packed32, 22),
                ReadByteFromSwappedPair(packed32, 24),
                ReadByteFromSwappedPair(packed32, 26),
                ReadByteFromSwappedPair(packed32, 28),
                ReadByteFromSwappedPair(packed32, 30));
        }

        /// <summary>
        /// Opens a Windows Installer database from the specified file path, optionally applying transformation files.
        /// </summary>
        /// <remarks>If the specified database is a patch file, transformations cannot be applied. Ensure
        /// that the database path is valid and accessible.</remarks>
        /// <param name="szDatabasePath">The path to the database file to be opened. This must be a valid file path to an MSI or MSP file.</param>
        /// <param name="szPersist">An optional persistence mode for opening the database. If null, the method will determine the appropriate mode based on the file type.</param>
        /// <param name="szTransformFiles">An optional collection of transformation file paths to apply to the database. Transforms cannot be applied
        /// to patch files.</param>
        /// <returns>A handle to the opened database. This handle must be disposed of when no longer needed.</returns>
        internal static MsiCloseHandleSafeHandle OpenDatabase(string szDatabasePath, MSI_PERSISTENCE_MODE? szPersist = null, params IReadOnlyList<string>? szTransformFiles)
        {
            // Open the msi/msp as a database.
            bool isPatchFile = Path.GetExtension(szDatabasePath).Equals(".msp", StringComparison.OrdinalIgnoreCase);
            if (szPersist is null)
            {
                szPersist = MSI_PERSISTENCE_MODE.MSIDBOPEN_READONLY;
                if (isPatchFile)
                {
                    szPersist += MSI_PERSISTENCE_MODE.MSIDBOPEN_PATCHFILE;
                }
            }
            _ = NativeMethods.MsiOpenDatabase(szDatabasePath, szPersist, out MsiCloseHandleSafeHandle hDatabase);
            try
            {
                // Apply any transformations to the database.
                if (szTransformFiles is not null)
                {
                    if (isPatchFile)
                    {
                        throw new InvalidOperationException("Cannot apply transforms to patch files.");
                    }
                    if (szTransformFiles.Count == 0)
                    {
                        throw new ArgumentException("Transform files collection cannot be empty if provided.", nameof(szTransformFiles));
                    }
                    foreach (string szTransformFile in szTransformFiles)
                    {
                        _ = NativeMethods.MsiDatabaseApplyTransform(hDatabase, szTransformFile);
                    }
                }
                return hDatabase;
            }
            catch
            {
                hDatabase.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Opens a Windows Installer database from the specified file path and applies optional transformation files.
        /// </summary>
        /// <param name="szDatabasePath">The full path to the database file to open. This parameter cannot be null or empty.</param>
        /// <param name="szTransformFiles">An optional array of transformation file paths to apply to the database. Each transformation file must be
        /// valid and accessible. May be null.</param>
        /// <returns>A safe handle representing the opened database. The caller is responsible for closing the handle to release
        /// resources.</returns>
        internal static MsiCloseHandleSafeHandle OpenDatabase(string szDatabasePath, params IReadOnlyList<string>? szTransformFiles)
        {
            return OpenDatabase(szDatabasePath, null, szTransformFiles);
        }

        /// <summary>
        /// Retrieves the summary information stream from a Windows Installer database file.
        /// </summary>
        /// <remarks>This method opens the specified database and applies any provided transforms before
        /// retrieving the summary information using the Windows Installer API. Ensure that the database file exists and
        /// is accessible. The returned handle must be closed to release unmanaged resources.</remarks>
        /// <param name="szDatabasePath">The full path to the Windows Installer database file from which to retrieve summary information. This
        /// parameter cannot be null or empty.</param>
        /// <param name="szTransformFiles">An optional read-only collection of transform file paths to apply to the database before retrieving summary
        /// information. If null, no transforms are applied.</param>
        /// <returns>A safe handle to the summary information stream of the specified database. The caller is responsible for
        /// disposing of the handle when it is no longer needed.</returns>
        internal static MsiCloseHandleSafeHandle GetSummaryInformation(string szDatabasePath, params IReadOnlyList<string>? szTransformFiles)
        {
            using MsiCloseHandleSafeHandle hDatabase = OpenDatabase(szDatabasePath, szTransformFiles);
            _ = NativeMethods.MsiGetSummaryInformation(hDatabase, 0, out MsiCloseHandleSafeHandle hSummaryInfo);
            return hSummaryInfo;
        }

        /// <summary>
        /// Retrieves the string value of a specified property from the summary information handle.
        /// </summary>
        /// <remarks>The caller is responsible for ensuring that the summary information handle is
        /// properly closed after use to avoid resource leaks.</remarks>
        /// <param name="hSummaryInfo">A handle to the summary information from which the property value is retrieved. The handle must be valid and
        /// open.</param>
        /// <param name="propertyId">The identifier of the summary property to retrieve. This should correspond to a string property in the
        /// summary information.</param>
        /// <returns>A string containing the value of the specified summary property. Returns an empty string if the property is
        /// not set.</returns>
        internal static string? GetSummaryInfoStringProperty(MsiCloseHandleSafeHandle hSummaryInfo, MSI_PROPERTY_ID propertyId)
        {
            _ = NativeMethods.MsiSummaryInfoGetProperty(hSummaryInfo, propertyId, out _, out _, out _, null, out uint requiredSize);
            if (requiredSize == 0)
            {
                return null;
            }
            Span<char> bufSpan = stackalloc char[(int)requiredSize + 1];
            _ = NativeMethods.MsiSummaryInfoGetProperty(hSummaryInfo, propertyId, out _, out _, out _, bufSpan, out _);
            ReadOnlySpan<char> resSpan = bufSpan.Slice(0, (int)requiredSize).Trim();
            return !resSpan.IsEmpty ? resSpan.ToString() : null;
        }

        /// <summary>
        /// Retrieves the value of an integer property from the summary information of a Windows Installer package.
        /// </summary>
        /// <remarks>This method returns a value only if the specified property is stored as a 16-bit or
        /// 32-bit integer. If the property is of a different type, the method returns null.</remarks>
        /// <param name="hSummaryInfo">A handle to the summary information structure obtained from a Windows Installer package. This handle must be
        /// valid and open for reading.</param>
        /// <param name="propertyId">The identifier of the summary property to retrieve, specified as an MSI_PROPERTY_ID enumeration value.</param>
        /// <returns>An integer value representing the property if it is of type VT_I2 or VT_I4; otherwise, null.</returns>
        internal static int? GetSummaryInfoIntProperty(MsiCloseHandleSafeHandle hSummaryInfo, MSI_PROPERTY_ID propertyId)
        {
            _ = NativeMethods.MsiSummaryInfoGetProperty(hSummaryInfo, propertyId, out VARENUM puiDataType, out int piValue, out _, null, out _);
            return puiDataType is VARENUM.VT_I2 or VARENUM.VT_I4 ? piValue : null;
        }

        /// <summary>
        /// Retrieves the date property value from the summary information of a specified Windows Installer (MSI)
        /// handle.
        /// </summary>
        /// <remarks>If the property value is not set, the method returns null. This method is intended
        /// for use with MSI summary information properties that store date and time values.</remarks>
        /// <param name="hSummaryInfo">The handle to the summary information structure from which to retrieve the date property.</param>
        /// <param name="propertyId">The identifier of the summary property to retrieve, specified as an MSI_PROPERTY_ID value.</param>
        /// <returns>A nullable DateTime representing the value of the requested date property if it is set; otherwise, null.</returns>
        internal static DateTime? GetSummaryInfoDateProperty(MsiCloseHandleSafeHandle hSummaryInfo, MSI_PROPERTY_ID propertyId)
        {
            _ = NativeMethods.MsiSummaryInfoGetProperty(hSummaryInfo, propertyId, out _, out _, out FILETIME pftValue, null, out _);
            return !pftValue.IsZero() ? pftValue.ToDateTime() : null;
        }

        /// <summary>
        /// Retrieves the name of the specified table from the database if it exists.
        /// </summary>
        /// <remarks>This method executes a query against the database to check for the existence of the
        /// specified table. It is important to ensure that the database handle is properly managed and disposed of
        /// after use.</remarks>
        /// <param name="hDatabase">The handle to the database from which to retrieve the table name. This handle must be valid and opened prior
        /// to calling this method.</param>
        /// <param name="table">The name of the table to resolve. This parameter cannot be null or empty.</param>
        /// <returns>The name of the table if it exists; otherwise, null.</returns>
        private static string? ResolveTableName(MsiCloseHandleSafeHandle hDatabase, string table)
        {
            _ = NativeMethods.MsiDatabaseOpenView(hDatabase, "SELECT `Name` FROM `_Tables` WHERE `Name` = ?", out MsiCloseHandleSafeHandle hView);
            using (hView)
            {
                using MsiCloseHandleSafeHandle hRecord = NativeMethods.MsiCreateRecord(1);
                _ = NativeMethods.MsiRecordSetString(hRecord, 1, table);
                _ = NativeMethods.MsiViewExecute(hView, hRecord);
                _ = NativeMethods.MsiViewFetch(hView, out MsiCloseHandleSafeHandle phRecord);
                using (phRecord)
                {
                    return GetRecordString(phRecord, 1);
                }
            }
        }

        /// <summary>
        /// Retrieves the name of the specified column from a given table in the Windows Installer database.
        /// </summary>
        /// <remarks>This method queries the Windows Installer database to obtain the column name based on
        /// the provided table and column index. Ensure that the table name and column number are valid to avoid
        /// unexpected results.</remarks>
        /// <param name="hDatabase">A handle to the Windows Installer database from which the column name is to be resolved.</param>
        /// <param name="table">The name of the table containing the column whose name is to be retrieved. Cannot be null or empty.</param>
        /// <param name="columnNumber">The zero-based index of the column within the specified table. Must be a valid column index.</param>
        /// <returns>The name of the column as a string if found; otherwise, null.</returns>
        private static string? ResolveColumnName(MsiCloseHandleSafeHandle hDatabase, string table, int columnNumber)
        {
            _ = NativeMethods.MsiDatabaseOpenView(hDatabase, "SELECT `Name` FROM `_Columns` WHERE `Table` = ? AND `Number` = ?", out MsiCloseHandleSafeHandle hView);
            using (hView)
            {
                using MsiCloseHandleSafeHandle hRecord = NativeMethods.MsiCreateRecord(2);
                _ = NativeMethods.MsiRecordSetString(hRecord, 1, table);
                _ = NativeMethods.MsiRecordSetInteger(hRecord, 2, columnNumber);
                _ = NativeMethods.MsiViewExecute(hView, hRecord);
                _ = NativeMethods.MsiViewFetch(hView, out MsiCloseHandleSafeHandle phRecord);
                using (phRecord)
                {
                    return GetRecordString(phRecord, 1);
                }
            }
        }

        /// <summary>
        /// Fetches the next record from the specified Windows Installer view.
        /// </summary>
        /// <remarks>If the fetch operation fails, the method returns null without throwing an exception.
        /// Ensure that the view handle is properly initialized before calling this method.</remarks>
        /// <param name="hView">The handle to the view from which to fetch the record. This handle must be valid and opened with the
        /// appropriate permissions.</param>
        /// <returns>A handle to the fetched record, or null if no more records are available or an error occurs during the fetch
        /// operation.</returns>
        private static MsiCloseHandleSafeHandle? ViewFetch(MsiCloseHandleSafeHandle hView)
        {
            try
            {
                _ = NativeMethods.MsiViewFetch(hView, out MsiCloseHandleSafeHandle hRecord);
                return hRecord;
            }
            catch
            {
                return null;
                throw;
            }
        }

        /// <summary>
        /// Retrieves the string value of the specified field from a Windows Installer record handle.
        /// </summary>
        /// <remarks>If the required size for the string is zero, the method returns null. The returned
        /// string is trimmed of any leading or trailing whitespace.</remarks>
        /// <param name="hRecord">The handle to the record from which to obtain the string value. This handle must be valid and not closed.</param>
        /// <param name="field">The zero-based index of the field within the record whose string value is to be retrieved.</param>
        /// <returns>The string value of the specified field if it exists; otherwise, null if the field is empty or not found.</returns>
        private static string? GetRecordString(MsiCloseHandleSafeHandle hRecord, uint field)
        {
            _ = NativeMethods.MsiRecordGetString(hRecord, field, null, out uint requiredSize);
            if (requiredSize == 0)
            {
                return null;
            }
            Span<char> bufSpan = stackalloc char[(int)requiredSize + 1];
            _ = NativeMethods.MsiRecordGetString(hRecord, field, bufSpan, out _);
            ReadOnlySpan<char> resSpan = bufSpan.Slice(0, (int)requiredSize).Trim();
            return !resSpan.IsEmpty ? resSpan.ToString() : null;
        }
    }
}
