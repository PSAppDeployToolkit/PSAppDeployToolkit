using System.Runtime.InteropServices;

namespace PSADT.Interop
{
    /// <summary>
    /// Represents a combination of a language identifier and a code page identifier.
    /// </summary>
    /// <remarks>This structure is used to specify a language and code page pair, typically in
    /// scenarios involving localization or encoding. The language identifier corresponds to a specific language or
    /// locale, while the code page identifier specifies the character encoding.</remarks>
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct LANGANDCODEPAGE
    {
        /// <summary>
        /// Represents the language identifier associated with the resource.
        /// </summary>
        /// <remarks>The language identifier is a 16-bit unsigned integer that specifies the
        /// language and sublanguage. This value is typically used in resource files or localization
        /// scenarios.</remarks>
        internal readonly ushort wLanguage;

        /// <summary>
        /// Represents the code page identifier used for character encoding.
        /// </summary>
        /// <remarks>The code page identifier specifies the character encoding used for text
        /// processing. This value is typically associated with a specific encoding standard, such as UTF-8 or
        /// Windows-1252.</remarks>
        internal readonly ushort wCodePage;

        /// <summary>
        /// Converts the language and code page identifiers into a hexadecimal string representation.
        /// </summary>
        /// <returns>A string containing the hexadecimal representation of the language and code page identifiers, formatted
        /// as "LLLLCCCC", where "LLLL" is the language identifier and "CCCC" is the code page identifier.</returns>
        internal readonly string ToTranslationTableString()
        {
            return $"{wLanguage:X4}{wCodePage:X4}";
        }
    }
}
