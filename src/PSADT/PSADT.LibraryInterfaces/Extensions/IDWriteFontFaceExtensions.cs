using System;
using PSADT.LibraryInterfaces.SafeHandles;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.DirectWrite;

namespace PSADT.LibraryInterfaces.Extensions
{
    /// <summary>
    /// Provides extension methods for working with IDWriteFontFace instances.
    /// </summary>
    internal static class IDWriteFontFaceExtensions
    {
        /// <summary>
        /// Attempts to retrieve a pointer to the specified OpenType font table from the given font face.
        /// </summary>
        /// <remarks>The returned table data and context are valid only if <paramref name="exists"/> is
        /// set to <see langword="true"/>. The caller is responsible for releasing the font table using the context
        /// pointer, according to the requirements of the underlying font API.</remarks>
        /// <param name="fontFace">The font face from which to retrieve the font table. Cannot be null.</param>
        /// <param name="openTypeTableTag">The four-character OpenType table tag identifying the font table to retrieve.</param>
        /// <param name="tableData">When this method returns, contains a pointer to the font table data if the table exists; otherwise, the
        /// value is undefined.</param>
        /// <param name="tableSize">When this method returns, contains the size, in bytes, of the font table data if the table exists;
        /// otherwise, the value is undefined.</param>
        /// <param name="tableContext">When this method returns, contains a context pointer used for releasing the font table. This value is valid
        /// only if the table exists.</param>
        /// <param name="exists">When this method returns, set to <see langword="true"/> if the specified font table exists; otherwise, <see
        /// langword="false"/>.</param>
        internal static void TryGetFontTable(this IDWriteFontFace fontFace, uint openTypeTableTag, out IntPtr tableData, out uint tableSize, out SafeFontTableHandle tableContext, out BOOL exists)
        {
            unsafe
            {
                fontFace.TryGetFontTable(openTypeTableTag, out void* tableDataLocal, out tableSize, out void* tableContextLocal, out exists);
                tableContext = new(fontFace, (IntPtr)tableContextLocal, true);
                tableData = (IntPtr)tableDataLocal;
            }
        }
    }
}
