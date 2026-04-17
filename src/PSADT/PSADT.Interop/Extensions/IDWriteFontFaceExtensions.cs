using System;
using System.Runtime.ExceptionServices;
using PSADT.Interop.SafeHandles;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.DirectWrite;

namespace PSADT.Interop.Extensions
{
    /// <summary>
    /// Provides extension methods for working with IDWriteFontFace instances.
    /// </summary>
    internal static class IDWriteFontFaceExtensions
    {
        /// <summary>
        /// Attempts to retrieve a pointer to the specified OpenType font table from the given font face.
        /// </summary>
        /// <remarks>If the specified font table does not exist, the method releases any allocated
        /// resources and sets the output parameter to null. It is important to handle the output parameter correctly to
        /// avoid resource leaks.</remarks>
        /// <param name="fontFace">The font face from which to retrieve the font table. This parameter cannot be null.</param>
        /// <param name="openTypeTableTag">The OpenType table tag that identifies the specific font table to retrieve.</param>
        /// <param name="tableContext">An output parameter that, if the font table is successfully retrieved, contains a handle to the font table
        /// context; otherwise, it is set to null.</param>
        internal static void TryGetFontTable(this IDWriteFontFace fontFace, uint openTypeTableTag, out SafeFontTableHandle? tableContext)
        {
            unsafe
            {
                fontFace.TryGetFontTable(openTypeTableTag, out void* tableData, out uint tableSize, out void* tableContextLocal, out BOOL exists);
                if (!exists)
                {
                    fontFace.ReleaseFontTable(tableContextLocal);
                    tableContext = null;
                    return;
                }
                try
                {
                    InvalidOperationException.ThrowIfInvalid((nint)tableContextLocal, "Failed to retrieve font table context.");
                    InvalidOperationException.ThrowIfZeroOrInvalid((nint)tableData, "Failed to retrieve font table data pointer.");
                    InvalidOperationException.ThrowIfZero(tableSize, "Retrieved font table size is zero, which is invalid.");
                    tableContext = new(fontFace, (nint)tableContextLocal, (nint)tableData, tableSize, true);
                }
                catch (Exception ex) when (ex.Message is not null)
                {
                    fontFace.ReleaseFontTable(tableContextLocal);
                    ExceptionDispatchInfo.Capture(ex).Throw();
                    throw;
                }
            }
        }
    }
}
