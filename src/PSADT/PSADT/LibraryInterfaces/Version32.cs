using System;
using System.Runtime.InteropServices;
using PSADT.SafeHandles;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace PSADT.LibraryInterfaces
{
    /// <summary>
    /// P/Invoke wrappers for the version.dll library.
    /// </summary>
    internal static class Version32
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

        /// <summary>
        /// Queries version information from the specified version-information resource.
        /// </summary>
        /// <param name="pBlock">A handle to the memory block containing the version-information resource. This handle must be valid and not
        /// null.</param>
        /// <param name="lpSubBlock">The version-information value to be retrieved. This string must specify a valid sub-block within the
        /// version-information resource.</param>
        /// <param name="lplpBuffer">When this method returns, contains a pointer to the requested version-information value. This parameter is
        /// passed uninitialized.</param>
        /// <param name="puLen">When this method returns, contains the length, in bytes, of the data pointed to by <paramref
        /// name="lplpBuffer"/>. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if the specified version-information value is successfully retrieved; otherwise, <see
        /// langword="false"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the version-information value cannot be queried.</exception>
        internal unsafe static BOOL VerQueryValue(SafeHGlobalHandle pBlock, string lpSubBlock, out IntPtr lplpBuffer, out uint puLen)
        {
            bool pBlockAddRef = false;
            try
            {
                pBlock.DangerousAddRef(ref pBlockAddRef);
                var res = PInvoke.VerQueryValue(pBlock.DangerousGetHandle().ToPointer(), lpSubBlock, out var lplpBufferLocal, out puLen);
                if (!res)
                {
                    throw new InvalidOperationException($"Failed to query [{lpSubBlock}] version value.");
                }
                lplpBuffer = (IntPtr)lplpBufferLocal;
                return res;
            }
            finally
            {
                if (pBlockAddRef)
                {
                    pBlock.DangerousRelease();
                }
            }
        }
    }
}
