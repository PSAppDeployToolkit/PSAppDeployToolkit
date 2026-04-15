using System;
using System.Runtime.CompilerServices;
using Microsoft.Win32.SafeHandles;
using PSADT.Interop.Extensions;
using Windows.Win32.Graphics.DirectWrite;

namespace PSADT.Interop.SafeHandles
{
    /// <summary>
    /// Provides a safe handle for managing the lifetime of a native font table resource associated with a DirectWrite
    /// font face.
    /// </summary>
    /// <remarks>This class ensures that the native font table resource is released appropriately when the
    /// handle is disposed or finalized. It is intended for internal use when working with DirectWrite font tables and
    /// should not be used directly by application code.</remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S4002:Disposable types should declare finalizers", Justification = "•\tSafeHandleMinusOneIsInvalid already provides finalization; this subtype correctly participates by overriding ReleaseHandle().")]
    internal sealed class SafeFontTableHandle : SafeHandleMinusOneIsInvalid
    {
        /// <summary>
        /// Initializes a new instance of the SafeFontTableHandle class for managing a font table handle associated with
        /// the specified font face.
        /// </summary>
        /// <param name="fontFace">The font face associated with the font table handle. Cannot be null.</param>
        /// <param name="context">A pointer to the native font table context to be managed by the handle.</param>
        /// <param name="data">A pointer to the font table data. This value is stored for informational purposes and is not used for resource management.</param>
        /// <param name="length">The length of the font table data, in bytes. This value is stored for informational purposes and is not used for resource management.</param>
        /// <param name="ownsHandle">true to indicate that the handle is responsible for releasing the native resource; otherwise, false.</param>
        /// <exception cref="ArgumentNullException">Thrown if fontFace is null.</exception>
        internal SafeFontTableHandle(IDWriteFontFace fontFace, nint context, nint data, uint length, bool ownsHandle) : base(ownsHandle)
        {
            ArgumentNullException.ThrowIfNull(fontFace);
            ArgumentOutOfRangeException.ThrowIfInvalid(context);
            ArgumentOutOfRangeException.ThrowIfZeroOrInvalid(data);
            ArgumentOutOfRangeException.ThrowIfZero(length);
            SetHandle(context);
            FontFace = fontFace;
            Length = (int)length;
            Data = data;
        }

        /// <summary>
        /// Gets a read-only span of bytes that represents the current font table data.
        /// </summary>
        /// <remarks>This method provides direct access to the underlying font table data without copying
        /// it, enabling efficient memory usage. The returned span reflects the data as it exists at the time of the
        /// call.</remarks>
        /// <returns>A read-only span containing the font table data. The length of the span is determined by the current state
        /// of the object.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ReadOnlySpan<byte> GetFontTableData()
        {
            return Data.AsReadOnlySpan<byte>(Length);
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
            try
            {
                unsafe
                {
                    FontFace.ReleaseFontTable((void*)handle);
                }
            }
            finally
            {
                handle = default;
            }
            return true;
        }

        /// <summary>
        /// Gets the length of the data, in bytes.
        /// </summary>
        internal readonly int Length;

        /// <summary>
        /// Gets the raw data value stored in the Data field.
        /// </summary>
        private readonly nint Data;

        /// <summary>
        /// Represents the underlying DirectWrite font face associated with this instance.
        /// </summary>
        /// <remarks>This field provides access to font face functionality from the DirectWrite API. It is
        /// intended for internal use and should not be accessed directly by consumers of this class.</remarks>
        private readonly IDWriteFontFace FontFace;
    }
}
