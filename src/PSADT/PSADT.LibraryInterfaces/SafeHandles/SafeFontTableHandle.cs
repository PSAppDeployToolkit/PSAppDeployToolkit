using System;
using Microsoft.Win32.SafeHandles;
using Windows.Win32.Graphics.DirectWrite;

namespace PSADT.LibraryInterfaces.SafeHandles
{
    /// <summary>
    /// Provides a safe handle for managing the lifetime of a native font table resource associated with a DirectWrite
    /// font face.
    /// </summary>
    /// <remarks>This class ensures that the native font table resource is released appropriately when the
    /// handle is disposed or finalized. It is intended for internal use when working with DirectWrite font tables and
    /// should not be used directly by application code.</remarks>
    internal sealed class SafeFontTableHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        /// <summary>
        /// Initializes a new instance of the SafeFontTableHandle class for managing a font table handle associated with
        /// the specified font face.
        /// </summary>
        /// <param name="fontFace">The font face associated with the font table handle. Cannot be null.</param>
        /// <param name="context">A pointer to the native font table context to be managed by the handle.</param>
        /// <param name="ownsHandle">true to indicate that the handle is responsible for releasing the native resource; otherwise, false.</param>
        /// <exception cref="ArgumentNullException">Thrown if fontFace is null.</exception>
        internal SafeFontTableHandle(IDWriteFontFace fontFace, IntPtr context, bool ownsHandle) : base(ownsHandle)
        {
            FontFace = fontFace ?? throw new ArgumentNullException(nameof(fontFace));
            SetHandle(context);
        }

        /// <summary>
        /// Releases the handle.
        /// </summary>
        /// <returns></returns>
        protected override bool ReleaseHandle()
        {
            if (handle == default || IntPtr.Zero == handle)
            {
                return true;
            }
            unsafe
            {
                FontFace.ReleaseFontTable((void*)handle);
            }
            handle = default;
            return true;
        }

        /// <summary>
        /// Represents the underlying DirectWrite font face associated with this instance.
        /// </summary>
        /// <remarks>This field provides access to font face functionality from the DirectWrite API. It is
        /// intended for internal use and should not be accessed directly by consumers of this class.</remarks>
        private readonly IDWriteFontFace FontFace;
    }
}
