using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using PSADT.LibraryInterfaces;
using PSADT.LibraryInterfaces.Extensions;
using PSADT.LibraryInterfaces.SafeHandles;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.DirectWrite;

namespace PSADT.Utilities
{
    /// <summary>
    /// Provides methods for managing font resources.
    /// </summary>
    public static class FontUtilities
    {
        /// <summary>
        /// Installs a font resource from the specified file.
        /// </summary>
        /// <param name="fontFilePath">The full path to the font file.</param>
        /// <returns>The number of fonts added if successful; otherwise, 0.</returns>
        /// <exception cref="FileNotFoundException">Thrown when the font file does not exist.</exception>
        public static int AddFont(string fontFilePath)
        {
            if (!File.Exists(fontFilePath))
            {
                throw new FileNotFoundException("Font file not found.", fontFilePath);
            }
            int result = Gdi32.AddFontResource(fontFilePath);
            _ = User32.SendNotifyMessage(HWND.HWND_BROADCAST, WINDOW_MESSAGE.WM_FONTCHANGE);
            return result;
        }

        /// <summary>
        /// Removes a font resource from the specified file.
        /// </summary>
        /// <param name="fontFilePaths">The full path to the font file.</param>
        /// <returns>True if the font was removed successfully; otherwise, false.</returns>
        /// ///
        public static IReadOnlyDictionary<string, int> AddFonts(IReadOnlyList<string> fontFilePaths)
        {
            if (fontFilePaths is null)
            {
                throw new ArgumentNullException(nameof(fontFilePaths));
            }
            Dictionary<string, int> fontResults = [];
            foreach (string fontFilePath in fontFilePaths)
            {
                if (!File.Exists(fontFilePath))
                {
                    throw new FileNotFoundException("Font file not found.", fontFilePath);
                }
                fontResults.Add(fontFilePath, Gdi32.AddFontResource(fontFilePath));
            }
            _ = User32.SendNotifyMessage(HWND.HWND_BROADCAST, WINDOW_MESSAGE.WM_FONTCHANGE);
            return new ReadOnlyDictionary<string, int>(fontResults);
        }

        /// <summary>
        /// Removes a font resource from the specified file.
        /// </summary>
        /// <param name="fontFilePath">The full path to the font file.</param>
        /// <returns>True if the font was removed successfully; otherwise, false.</returns>
        /// ///
        public static void RemoveFont(string fontFilePath)
        {
            // Remove the font resource. We don't check for file existence because the input is just value that names a font resource file.
            // See https://learn.microsoft.com/en-us/windows/win32/api/wingdi/nf-wingdi-removefontresourcew#parameters for more details.
            _ = Gdi32.RemoveFontResource(fontFilePath);
            _ = User32.SendNotifyMessage(HWND.HWND_BROADCAST, WINDOW_MESSAGE.WM_FONTCHANGE);
        }

        /// <summary>
        /// Removes the specified font resources from the system font table.
        /// </summary>
        /// <remarks>After removing the fonts, a system-wide font change notification is broadcast. The
        /// specified file paths are passed directly to the underlying system API and are not validated for existence.
        /// Removing a font does not delete the font file from disk; it only unregisters the font from the
        /// system.</remarks>
        /// <param name="fontFilePaths">A read-only list of file paths that identify the font resource files to remove from the system.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="fontFilePaths"/> is <see langword="null"/>.</exception>
        public static void RemoveFonts(IReadOnlyList<string> fontFilePaths)
        {
            if (fontFilePaths is null)
            {
                throw new ArgumentNullException(nameof(fontFilePaths));
            }
            foreach (string fontFilePath in fontFilePaths)
            {
                // Remove the font resource. We don't check for file existence because the input is just value that names a font resource file.
                // See https://learn.microsoft.com/en-us/windows/win32/api/wingdi/nf-wingdi-removefontresourcew#parameters for more details.
                _ = Gdi32.RemoveFontResource(fontFilePath);
            }
            _ = User32.SendNotifyMessage(HWND.HWND_BROADCAST, WINDOW_MESSAGE.WM_FONTCHANGE);
        }

        /// <summary>
        /// Retrieves the title of a font from the specified font file path.
        /// </summary>
        /// <remarks>This method supports font collections such as TrueType Collection (.ttc) and OpenType
        /// Collection (.otc) files by examining each face in the collection. If the font's title cannot be read from
        /// the name table, the method falls back to returning the file name without its extension.</remarks>
        /// <param name="fontPath">The path to the font file. The path must refer to an existing file and can be absolute or relative. Leading
        /// and trailing whitespace and quotes are ignored.</param>
        /// <returns>A string containing the font's title as specified in the font's name table. If the title cannot be
        /// determined, returns the file name without its extension.</returns>
        /// <exception cref="ArgumentNullException">Thrown if fontPath is null, empty, or consists only of whitespace.</exception>
        /// <exception cref="FileNotFoundException">Thrown if the file specified by fontPath does not exist.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the font file format is not supported or if the font file contains no font faces.</exception>
        public static string GetFontTitle(string fontPath)
        {
            // Ensure the specified path is valid.
            if (string.IsNullOrWhiteSpace(fontPath))
            {
                throw new ArgumentNullException(nameof(fontPath), "Path cannot be null/empty.");
            }
            if (!File.Exists(fontPath = Path.GetFullPath(fontPath.Trim().Trim('"'))))
            {
                throw new FileNotFoundException("Font file not found.", fontPath);
            }

            // Create factory and font file reference.
            _ = DWrite.DWriteCreateFactory(DWRITE_FACTORY_TYPE.DWRITE_FACTORY_TYPE_SHARED, out IDWriteFactory factory);
            factory.CreateFontFileReference(fontPath, null, out IDWriteFontFile fontFile);
            fontFile.Analyze(out BOOL supported, out _, out DWRITE_FONT_FACE_TYPE fontFaceType, out uint faceCount);
            if (!supported)
            {
                throw new InvalidOperationException("Font file format is not supported.");
            }
            if (faceCount == 0)
            {
                throw new InvalidOperationException("Font file contains no faces.");
            }

            // Iterate faces (handles .ttc/.otc) to find the font title.
            IDWriteFontFile[] files = [fontFile];
            for (uint faceIndex = 0; faceIndex < faceCount; faceIndex++)
            {
                factory.CreateFontFace(fontFaceType, 1, files, faceIndex, DWRITE_FONT_SIMULATIONS.DWRITE_FONT_SIMULATIONS_NONE, out IDWriteFontFace fontFace);
                fontFace.TryGetFontTable(TAG_NAME, out IntPtr tableData, out uint tableSize, out SafeFontTableHandle tableContext, out BOOL exists);
                using (tableContext)
                {
                    string? fontTitle = exists && tableData != IntPtr.Zero && tableSize >= 6 ? GetBestFontTitleFromNameTable(tableData.AsReadOnlySpan<byte>((int)tableSize)) : null;
                    if (!string.IsNullOrWhiteSpace(fontTitle))
                    {
                        return fontTitle!;
                    }
                }
            }
            throw new InvalidOperationException("Unable to determine font title from name table.");
        }

        /// <summary>
        /// Parses a font name table to determine the most appropriate display title for the font.
        /// </summary>
        /// <remarks>This method prioritizes the font's full name, followed by typographic family and
        /// subfamily, and finally the regular family and subfamily names. The returned title is suitable for display in
        /// user interfaces or font selection dialogs.</remarks>
        /// <param name="nameTable">A read-only span of bytes representing the font's name table data, typically from an OpenType or TrueType
        /// font file.</param>
        /// <returns>A string containing the best available title for the font, or null if no suitable name is found.</returns>
        private static string? GetBestFontTitleFromNameTable(ReadOnlySpan<byte> nameTable)
        {
            // Local function to combine family and style.
            static string NormalizeFamilyStyle(string family, string? style)
            {
                return string.IsNullOrWhiteSpace(style) || style!.Equals("Regular", StringComparison.OrdinalIgnoreCase) ? family : $"{family} {style}";
            }

            // Use the font's full name if available.
            ushort count = BinaryPrimitives.ReadUInt16BigEndian(nameTable.Slice(2));
            ushort stringOffset = BinaryPrimitives.ReadUInt16BigEndian(nameTable.Slice(4));
            string? full = GetFontTitleByNameId(nameTable, count, stringOffset, NAME_ID.NAME_ID_FULL_NAME);
            if (!string.IsNullOrWhiteSpace(full))
            {
                return full;
            }

            // Otherwise, use typographic family/subfamily if available.
            string? tf = GetFontTitleByNameId(nameTable, count, stringOffset, NAME_ID.NAME_ID_TYPOGRAPHIC_FAMILY);
            string? ts = GetFontTitleByNameId(nameTable, count, stringOffset, NAME_ID.NAME_ID_TYPOGRAPHIC_SUBFAMILY);
            if (!string.IsNullOrWhiteSpace(tf))
            {
                return NormalizeFamilyStyle(tf!, ts);
            }

            // Otherwise, use regular family/subfamily.
            string? fam = GetFontTitleByNameId(nameTable, count, stringOffset, NAME_ID.NAME_ID_FONT_FAMILY);
            string? sub = GetFontTitleByNameId(nameTable, count, stringOffset, NAME_ID.NAME_ID_FONT_SUBFAMILY);
            return !string.IsNullOrWhiteSpace(fam) ? NormalizeFamilyStyle(fam!, sub) : null;
        }

        /// <summary>
        /// Searches the name table for the best matching string corresponding to the specified name ID, using a
        /// deterministic preference order for platform and language.
        /// </summary>
        /// <remarks>The search prioritizes Windows Unicode records matching the current culture, then the
        /// current UI culture, followed by en-US, any Windows Unicode language, any Windows encoding or language, and
        /// finally Unicode and Macintosh platforms. The method ensures that the same input will always yield the same
        /// result, making the selection order deterministic.</remarks>
        /// <param name="nameTable">A span of bytes representing the font name table data to search.</param>
        /// <param name="count">The number of name records present in the name table.</param>
        /// <param name="stringOffset">The offset, in bytes, from the start of the name table to the beginning of the string storage area.</param>
        /// <param name="desiredNameId">The name ID to locate within the name table records.</param>
        /// <returns>A string containing the best matching name for the specified name ID, or null if no suitable match is found.</returns>
        private static string? GetFontTitleByNameId(ReadOnlySpan<byte> nameTable, ushort count, ushort stringOffset, NAME_ID desiredNameId)
        {
            // Pass 1: Windows Unicode, current culture.
            if (TryFindNameIdRecord(nameTable, count, stringOffset, desiredNameId, platformId: 3, requireWindowsUnicode: true, languageId: (ushort)(System.Globalization.CultureInfo.CurrentCulture.LCID & 0xFFFF), out string? s))
            {
                return s;
            }

            // Pass 2: Windows Unicode, current UI culture.
            if (TryFindNameIdRecord(nameTable, count, stringOffset, desiredNameId, platformId: 3, requireWindowsUnicode: true, languageId: (ushort)(System.Globalization.CultureInfo.CurrentUICulture.LCID & 0xFFFF), out s))
            {
                return s;
            }

            // Pass 3: Windows Unicode, en-US.
            if (TryFindNameIdRecord(nameTable, count, stringOffset, desiredNameId, platformId: 3, requireWindowsUnicode: true, languageId: 0x0409, out s))
            {
                return s;
            }

            // Pass 4: Windows Unicode, any language.
            if (TryFindNameIdRecord(nameTable, count, stringOffset, desiredNameId, platformId: 3, requireWindowsUnicode: true, languageId: null, out s))
            {
                return s;
            }

            // Pass 5: Windows, any encoding/language.
            if (TryFindNameIdRecord(nameTable, count, stringOffset, desiredNameId, platformId: 3, requireWindowsUnicode: false, languageId: null, out s))
            {
                return s;
            }

            // Pass 6: Unicode platform, any.
            if (TryFindNameIdRecord(nameTable, count, stringOffset, desiredNameId, platformId: 0, requireWindowsUnicode: false, languageId: null, out s))
            {
                return s;
            }

            // Pass 7: Macintosh platform, any.
            if (TryFindNameIdRecord(nameTable, count, stringOffset, desiredNameId, platformId: 1, requireWindowsUnicode: false, languageId: null, out s))
            {
                return s;
            }

            // No suitable record found.
            return null;
        }

        /// <summary>
        /// Attempts to locate and decode a name record from a font name table that matches the specified criteria.
        /// </summary>
        /// <remarks>This method searches the provided name table for a record that matches the specified
        /// name ID, platform ID, and, if provided, language ID. If requireWindowsUnicode is set to true, only name
        /// records with a Windows Unicode encoding are considered. The method decodes the name string using the
        /// appropriate encoding for the platform. The result is trimmed of leading and trailing whitespace if
        /// found.</remarks>
        /// <param name="nameTable">A read-only span of bytes representing the font's name table data to search.</param>
        /// <param name="count">The number of name records present in the name table.</param>
        /// <param name="stringOffset">The offset, in bytes, from the start of the name table to the beginning of the string storage area.</param>
        /// <param name="desiredNameId">The name identifier to search for within the name records.</param>
        /// <param name="platformId">The platform identifier to match when searching for a name record.</param>
        /// <param name="requireWindowsUnicode">true to require that the name record uses a Windows Unicode encoding; otherwise, false.</param>
        /// <param name="languageId">The language identifier to match, or null to ignore the language when searching.</param>
        /// <param name="result">When this method returns, contains the decoded name string if a matching record is found and successfully
        /// decoded; otherwise, null. This parameter is passed uninitialized.</param>
        /// <returns>true if a matching name record is found and successfully decoded; otherwise, false.</returns>
        private static bool TryFindNameIdRecord(ReadOnlySpan<byte> nameTable, ushort count, ushort stringOffset, NAME_ID desiredNameId, ushort platformId, bool requireWindowsUnicode, ushort? languageId, out string? result)
        {
            // Attempt to decode the name string.
            const int RecordStart = 6; const int RecordSize = 12;
            for (int i = 0; i < count; i++)
            {
                // Ensure we don't read past the end of the table.
                int rec = RecordStart + (i * RecordSize);
                if (rec + RecordSize > nameTable.Length)
                {
                    break;
                }

                // Read the name record fields.
                ushort p = BinaryPrimitives.ReadUInt16BigEndian(nameTable.Slice(rec + 0, 2));
                ushort e = BinaryPrimitives.ReadUInt16BigEndian(nameTable.Slice(rec + 2, 2));
                ushort l = BinaryPrimitives.ReadUInt16BigEndian(nameTable.Slice(rec + 4, 2));
                ushort n = BinaryPrimitives.ReadUInt16BigEndian(nameTable.Slice(rec + 6, 2));
                ushort len = BinaryPrimitives.ReadUInt16BigEndian(nameTable.Slice(rec + 8, 2));
                ushort off = BinaryPrimitives.ReadUInt16BigEndian(nameTable.Slice(rec + 10, 2));

                // Check for matching criteria.
                if (n != (ushort)desiredNameId)
                {
                    continue;
                }
                if (p != platformId)
                {
                    continue;
                }
                if (requireWindowsUnicode && p == 3 && !(e is 1 or 10))
                {
                    continue;
                }
                if (languageId.HasValue && l != languageId.Value)
                {
                    continue;
                }

                // Check string bounds and decode.
                int strPos = stringOffset + off;
                if (strPos < 0 || strPos + len > nameTable.Length)
                {
                    continue;
                }
                try
                {
                    unsafe
                    {
                        fixed (byte* pBytes = nameTable.Slice(strPos, len))
                        {
                            result = (TT_PLATFORM_ID)p == TT_PLATFORM_ID.TT_PLATFORM_MACINTOSH
                                ? Encoding.GetEncoding(10000).GetString(pBytes, len)
                                : Encoding.BigEndianUnicode.GetString(pBytes, len);
                        }
                    }
                    return true;
                }
                catch (Exception ex) when (ex.Message is not null)
                {
                    continue;
                }
            }
            result = null;
            return false;
        }

        /// <summary>
        /// Represents the OpenType 'name' table tag identifier used to access font naming information.
        /// </summary>
        /// <remarks>The 'name' table in OpenType fonts contains human-readable strings such as the font
        /// family name, style, and other metadata. This tag can be used when working with APIs that require specifying
        /// the 'name' table by its four-character identifier.</remarks>
        private static readonly uint TAG_NAME = DWrite.DWRITE_MAKE_OPENTYPE_TAG('n', 'a', 'm', 'e');

        /// <summary>
        /// OpenType/TrueType 'name' table Name IDs (nameID field in each NameRecord).
        /// </summary>
        /// <remarks> These identifiers specify the semantic meaning of a string stored in the font's 'name' table.
        /// The exact set of Name IDs present can vary between fonts.For more information about these values, see
        /// https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-ids</remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1712:Do not prefix enum values with type name", Justification = "These are how they're named in the specification.")]
        private enum NAME_ID : ushort
        {
            /// <summary>
            /// 0 - Copyright notice.
            /// </summary>
            NAME_ID_COPYRIGHT = 0,

            /// <summary>
            /// 1 - Font Family name.
            /// </summary>
            NAME_ID_FONT_FAMILY = 1,

            /// <summary>
            /// 2 - Font Subfamily name.
            /// </summary>
            NAME_ID_FONT_SUBFAMILY = 2,

            /// <summary>
            /// 3 - Unique font identifier.
            /// </summary>
            NAME_ID_UNIQUE_ID = 3,

            /// <summary>
            /// 4 - Full font name (typically reflects the complete family and subfamily descriptors).
            /// </summary>
            NAME_ID_FULL_NAME = 4,

            /// <summary>
            /// 5 - Version string.
            /// </summary>
            NAME_ID_VERSION_STRING = 5,

            /// <summary>
            /// 6 - PostScript name.
            /// </summary>
            NAME_ID_POSTSCRIPT_NAME = 6,

            /// <summary>
            /// 7 - Trademark.
            /// </summary>
            NAME_ID_TRADEMARK = 7,

            /// <summary>
            /// 8 - Manufacturer name.
            /// </summary>
            NAME_ID_MANUFACTURER = 8,

            /// <summary>
            /// 9 - Designer.
            /// </summary>
            NAME_ID_DESIGNER = 9,

            /// <summary>
            /// 10 - Description.
            /// </summary>
            NAME_ID_DESCRIPTION = 10,

            /// <summary>
            /// 11 - URL of the font vendor.
            /// </summary>
            NAME_ID_VENDOR_URL = 11,

            /// <summary>
            /// 12 - URL of the font designer.
            /// </summary>
            NAME_ID_DESIGNER_URL = 12,

            /// <summary>
            /// 13 - License description.
            /// </summary>
            NAME_ID_LICENSE = 13,

            /// <summary>
            /// 14 - License information URL.
            /// </summary>
            NAME_ID_LICENSE_URL = 14,

            /// <summary>
            /// 15 - Reserved.
            /// </summary>
            NAME_ID_RESERVED = 15,

            /// <summary>
            /// 16 - Typographic Family name.
            /// </summary>
            NAME_ID_TYPOGRAPHIC_FAMILY = 16,

            /// <summary>
            /// 17 - Typographic Subfamily name.
            /// </summary>
            NAME_ID_TYPOGRAPHIC_SUBFAMILY = 17,

            /// <summary>
            /// 18 - Compatible Full (Macintosh only).
            /// </summary>
            NAME_ID_MAC_FULL_NAME = 18,

            /// <summary>
            /// 19 - Sample text.
            /// </summary>
            NAME_ID_SAMPLE_TEXT = 19,

            /// <summary>
            /// 20 - PostScript CID findfont name.
            /// </summary>
            NAME_ID_CID_FINDFONT_NAME = 20,

            /// <summary>
            /// 21 - WWS Family name (Weight/Width/Slope family).
            /// </summary>
            NAME_ID_WWS_FAMILY = 21,

            /// <summary>
            /// 22 - WWS Subfamily name (Weight/Width/Slope subfamily).
            /// </summary>
            NAME_ID_WWS_SUBFAMILY = 22,

            /// <summary>
            /// 23 - Light background palette.
            /// </summary>
            NAME_ID_LIGHT_BACKGROUND = 23,

            /// <summary>
            /// 24 - Dark background palette.
            /// </summary>
            NAME_ID_DARK_BACKGROUND = 24,

            /// <summary>
            /// 25 - Variations PostScript name prefix.
            /// </summary>
            NAME_ID_VARIATIONS_PS_PREFIX = 25,
        }

        /// <summary>
        /// Specifies platform identifiers used in TrueType font tables to indicate the character encoding scheme.
        /// </summary>
        /// <remarks>These values correspond to the 'platformID' field in the TrueType 'name' table and
        /// other font structures. Each identifier represents a different platform or encoding standard, which may
        /// affect how character data is interpreted. This enumeration is primarily used when parsing or generating font
        /// files that conform to the TrueType specification.</remarks>
        private enum TT_PLATFORM_ID : ushort
        {
            /// <summary>
            /// 0 - Unicode (a.k.a. Apple Unicode).
            /// </summary>
            TT_PLATFORM_APPLE_UNICODE = 0,

            /// <summary>
            /// 1 - Macintosh.
            /// </summary>
            TT_PLATFORM_MACINTOSH = 1,

            /// <summary>
            /// 2 - ISO (deprecated/rare).
            /// </summary>
            TT_PLATFORM_ISO = 2,

            /// <summary>
            /// 3 - Microsoft (Windows).
            /// </summary>
            TT_PLATFORM_MICROSOFT = 3,

            /// <summary>
            /// 4 - Custom (rare).
            /// </summary>
            TT_PLATFORM_CUSTOM = 4,
        }

        /// <summary>
        /// Specifies the Microsoft platform-specific character set identifiers used in TrueType and OpenType font
        /// tables.
        /// </summary>
        /// <remarks>These identifiers are used to indicate the encoding scheme for character mapping
        /// tables in font files. The values correspond to different character sets, such as Unicode, Symbol, and
        /// various East Asian encodings, as defined by the OpenType specification.</remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1712:Do not prefix enum values with type name", Justification = "These are how they're named in the specification.")]
        private enum TT_MS_ID : ushort
        {
            /// <summary>
            /// 0 - Symbol encoding.
            /// </summary>
            TT_MS_ID_SYMBOL_CS = 0,

            /// <summary>
            /// 1 - Unicode BMP (UCS-2).
            /// </summary>
            TT_MS_ID_UNICODE_CS = 1,

            /// <summary>
            /// 2 - ShiftJIS.
            /// </summary>
            TT_MS_ID_SJIS = 2,

            /// <summary>
            /// 3 - PRC (GBK / GB2312).
            /// </summary>
            TT_MS_ID_PRC = 3,

            /// <summary>
            /// 4 - Big5.
            /// </summary>
            TT_MS_ID_BIG_5 = 4,

            /// <summary>
            /// 5 - Wansung.
            /// </summary>
            TT_MS_ID_WANSUNG = 5,

            /// <summary>
            /// 6 - Johab.
            /// </summary>
            TT_MS_ID_JOHAB = 6,

            /// <summary>
            /// 10 - Unicode full repertoire (UCS-4 / UTF-32 semantics).
            /// </summary>
            TT_MS_ID_UCS_4 = 10,
        }

        /// <summary>
        /// Specifies the Mac encoding IDs used in TrueType and OpenType font tables to identify character sets for
        /// different languages and scripts on Macintosh platforms.
        /// </summary>
        /// <remarks>These encoding IDs are used in font naming and character mapping tables to indicate
        /// the specific Mac script or language encoding. The values correspond to the platform-specific identifiers
        /// defined by Apple for font files. This enumeration is typically used when parsing or generating font metadata
        /// that targets the Macintosh platform.</remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1712:Do not prefix enum values with type name", Justification = "These are how they're named in the specification.")]
        private enum TT_MAC_ID : ushort
        {
            /// <summary>
            /// 0 - Roman (MacRoman).
            /// </summary>
            TT_MAC_ID_ROMAN = 0,

            /// <summary>
            /// 1 - Japanese.
            /// </summary>
            TT_MAC_ID_JAPANESE = 1,

            /// <summary>
            /// 2 - Traditional Chinese.
            /// </summary>
            TT_MAC_ID_TRADITIONAL_CHINESE = 2,

            /// <summary>
            /// 3 - Korean.
            /// </summary>
            TT_MAC_ID_KOREAN = 3,

            /// <summary>
            /// 4 - Arabic.
            /// </summary>
            TT_MAC_ID_ARABIC = 4,

            /// <summary>
            /// 5 - Hebrew.
            /// </summary>
            TT_MAC_ID_HEBREW = 5,

            /// <summary>
            /// 6 - Greek.
            /// </summary>
            TT_MAC_ID_GREEK = 6,

            /// <summary>
            /// 7 - Russian.
            /// </summary>
            TT_MAC_ID_RUSSIAN = 7,

            /// <summary>
            /// 8 - RSymbol.
            /// </summary>
            TT_MAC_ID_RSYMBOL = 8,

            /// <summary>
            /// 9 - Devanagari.
            /// </summary>
            TT_MAC_ID_DEVANAGARI = 9,

            /// <summary>
            /// 10 - Gurmukhi.
            /// </summary>
            TT_MAC_ID_GURMUKHI = 10,

            /// <summary>
            /// 11 - Gujarati.
            /// </summary>
            TT_MAC_ID_GUJARATI = 11,

            /// <summary>
            /// 12 - Oriya.
            /// </summary>
            TT_MAC_ID_ORIYA = 12,

            /// <summary>
            /// 13 - Bengali.
            /// </summary>
            TT_MAC_ID_BENGALI = 13,

            /// <summary>
            /// 14 - Tamil.
            /// </summary>
            TT_MAC_ID_TAMIL = 14,

            /// <summary>
            /// 15 - Telugu.
            /// </summary>
            TT_MAC_ID_TELUGU = 15,

            /// <summary>
            /// 16 - Kannada.
            /// </summary>
            TT_MAC_ID_KANNADA = 16,

            /// <summary>
            /// 17 - Malayalam.
            /// </summary>
            TT_MAC_ID_MALAYALAM = 17,

            /// <summary>
            /// 18 - Sinhalese.
            /// </summary>
            TT_MAC_ID_SINHALESE = 18,

            /// <summary>
            /// 19 - Burmese.
            /// </summary>
            TT_MAC_ID_BURMESE = 19,

            /// <summary>
            /// 20 - Khmer.
            /// </summary>
            TT_MAC_ID_KHMER = 20,

            /// <summary>
            /// 21 - Thai.
            /// </summary>
            TT_MAC_ID_THAI = 21,

            /// <summary>
            /// 22 - Laotian.
            /// </summary>
            TT_MAC_ID_LAOTIAN = 22,

            /// <summary>
            /// 23 - Georgian.
            /// </summary>
            TT_MAC_ID_GEORGIAN = 23,

            /// <summary>
            /// 24 - Armenian.
            /// </summary>
            TT_MAC_ID_ARMENIAN = 24,

            /// <summary>
            /// 25 - Simplified Chinese.
            /// </summary>
            TT_MAC_ID_SIMPLIFIED_CHINESE = 25,

            /// <summary>
            /// 26 - Tibetan.
            /// </summary>
            TT_MAC_ID_TIBETAN = 26,

            /// <summary>
            /// 27 - Mongolian.
            /// </summary>
            TT_MAC_ID_MONGOLIAN = 27,

            /// <summary>
            /// 28 - Geez.
            /// </summary>
            TT_MAC_ID_GEEZ = 28,

            /// <summary>
            /// 29 - Slavic.
            /// </summary>
            TT_MAC_ID_SLAVIC = 29,

            /// <summary>
            /// 30 - Vietnamese.
            /// </summary>
            TT_MAC_ID_VIETNAMESE = 30,

            /// <summary>
            /// 31 - Sindhi.
            /// </summary>
            TT_MAC_ID_SINDHI = 31,

            /// <summary>
            /// 32 - Uninterpreted.
            /// </summary>
            TT_MAC_ID_UNINTERPRETED = 32,
        }

        /// <summary>
        /// Unicode platform encoding identifiers for OpenType/TrueType 'name' table records
        /// (encodingID field when <see cref="TT_PLATFORM_ID.TT_PLATFORM_APPLE_UNICODE"/> is the platformID).
        /// </summary>
        /// <remarks>
        /// These values are defined by the OpenType specification for the Unicode (platformID = 0) platform.
        /// In modern fonts, Unicode-platform name records are typically encoded as UTF-16BE in practice.
        /// </remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1712:Do not prefix enum values with type name", Justification = "These are how they're named in the specification.")]
        private enum TT_UNICODE_ID : ushort
        {
            /// <summary>
            /// 0 - Unicode 1.0 semantics (default).
            /// </summary>
            TT_UNICODE_ID_DEFAULT = 0,

            /// <summary>
            /// 1 - Unicode 1.1 semantics.
            /// </summary>
            TT_UNICODE_ID_UNICODE_1_1 = 1,

            /// <summary>
            /// 2 - ISO/IEC 10646 semantics (deprecated / historical).
            /// </summary>
            TT_UNICODE_ID_ISO_10646 = 2,

            /// <summary>
            /// 3 - Unicode 2.0 and later semantics (BMP only).
            /// </summary>
            TT_UNICODE_ID_UNICODE_2_0_BMP = 3,

            /// <summary>
            /// 4 - Unicode 2.0 and later semantics (full repertoire).
            /// </summary>
            TT_UNICODE_ID_UNICODE_2_0_FULL = 4,
        }
    }
}
