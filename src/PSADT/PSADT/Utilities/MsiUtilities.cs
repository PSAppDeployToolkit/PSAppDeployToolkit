using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using PSADT.LibraryInterfaces;
using PSADT.LibraryInterfaces.Extensions;
using Windows.Win32;
using Windows.Win32.System.LibraryLoader;

namespace PSADT.Utilities
{
    /// <summary>
    /// Public P/Invokes from the msi.dll library.
    /// </summary>
    public static class MsiUtilities
    {
        /// <summary>
        /// Retrieves the message string associated with an MSI exit code from the msimsg.dll resource.
        /// </summary>
        /// <param name="msiExitCode">The MSI exit code.</param>
        /// <returns>The message string associated with the given MSI exit code.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the library cannot be loaded or the message cannot be retrieved.</exception>
        public static string? GetMessageFromMsiExitCode(uint msiExitCode)
        {
            using FreeLibrarySafeHandle hMsiMsgDll = Kernel32.LoadLibraryEx("msimsg.dll", LOAD_LIBRARY_FLAGS.LOAD_LIBRARY_AS_DATAFILE);
            _ = User32.LoadString(hMsiMsgDll, msiExitCode, out string? msiMsgString);
            return !string.IsNullOrWhiteSpace(msiMsgString) ? Regex.Replace(msiMsgString, @"\s{2,}", " ") : null;
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
            // Open the patch file as a database.
            _ = Msi.MsiOpenDatabase(szDatabasePath, MSI_PERSISTENCE_MODE.MSIDBOPEN_READONLY + MSI_PERSISTENCE_MODE.MSIDBOPEN_PATCHFILE, out MsiCloseHandleSafeHandle hDatabase);
            using (hDatabase)
            {
                // Get the summary information from the database.
                _ = Msi.MsiGetSummaryInformation(hDatabase, 0, out MsiCloseHandleSafeHandle hSummaryInfo);
                using (hSummaryInfo)
                {
                    // Determine the size of the buffer we need.
                    _ = Msi.MsiSummaryInfoGetProperty(hSummaryInfo, MSI_PROPERTY_ID.PID_TEMPLATE, out _, out _, out _, null, out uint requiredSize);
                    Span<char> bufSpan = stackalloc char[(int)requiredSize];

                    // Grab the supported product codes and return them to the caller.
                    _ = Msi.MsiSummaryInfoGetProperty(hSummaryInfo, MSI_PROPERTY_ID.PID_TEMPLATE, out _, out _, out _, bufSpan, out _);
                    return new ReadOnlyCollection<Guid>([.. bufSpan.ToString().TrimRemoveNull().Split([';'], StringSplitOptions.RemoveEmptyEntries).Select(static g => new Guid(g))]);
                }
            }
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
            _ = Msi.MsiExtractPatchXMLData(szPatchPath, null, out uint requiredLength);
            Span<char> bufSpan = stackalloc char[(int)requiredLength];
            _ = Msi.MsiExtractPatchXMLData(szPatchPath, bufSpan, out _);
            return XmlUtilities.SafeLoadFromText(bufSpan.ToString().TrimRemoveNull());
        }

        /// <summary>
        /// Retrieves the installation state of a product identified by its unique product code.
        /// </summary>
        /// <remarks>This method calls the Windows Installer to determine the installation state. Ensure
        /// that the provided product code is a valid GUID; otherwise, the returned state may not accurately reflect the
        /// product's status.</remarks>
        /// <param name="productCode">The unique identifier (GUID) of the product whose installation state is to be queried.</param>
        /// <returns>An INSTALLSTATE value that indicates the current installation state of the specified product.</returns>
        public static INSTALLSTATE QueryProductState(Guid productCode)
        {
            return (INSTALLSTATE)Msi.MsiQueryProductState(productCode);
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
    }
}
