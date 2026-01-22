using System;
using Microsoft.Win32.SafeHandles;
using Windows.Win32;
using Windows.Win32.System.Com.StructuredStorage;

namespace PSADT.LibraryInterfaces.SafeHandles
{
    /// <summary>
    /// Provides a safe handle for managing the lifetime of a PROPVARIANT structure used in interop scenarios.
    /// </summary>
    /// <remarks>This class is intended for advanced or interop scenarios where direct management of
    /// PROPVARIANT handles is required. It ensures that the underlying PROPVARIANT is released safely to prevent
    /// resource leaks. Instances of this class should be disposed when no longer needed to release native resources
    /// promptly.</remarks>
    internal sealed class SafePropVariantHandle : SafeHandleMinusOneIsInvalid
    {
        /// <summary>
        /// Initializes a new instance of the SafePropVariantHandle class using the specified PROPVARIANT value and
        /// ownership flag.
        /// </summary>
        /// <param name="pv">The PROPVARIANT value to associate with the handle.</param>
        /// <param name="ownsHandle">true to indicate that the handle is owned and should be released when the SafePropVariantHandle is disposed;
        /// otherwise, false.</param>
        internal SafePropVariantHandle(in PROPVARIANT pv, bool ownsHandle) : base(ownsHandle)
        {
            SetHandle(IntPtr.Zero);
            Value = pv;
        }

        /// <summary>
        /// Returns the underlying PROPVARIANT handle for advanced scenarios.
        /// </summary>
        /// <remarks>This method exposes the raw PROPVARIANT handle and should be used with caution.
        /// Modifying or using the handle incorrectly can lead to resource leaks or undefined behavior. Intended for
        /// advanced or interop scenarios where direct access to the handle is required.</remarks>
        /// <returns>The PROPVARIANT value representing the internal handle.</returns>
        internal new ref PROPVARIANT DangerousGetHandle()
        {
            return ref Value;
        }

        /// <summary>
        /// Releases the handle.
        /// </summary>
        /// <returns></returns>
        protected override bool ReleaseHandle()
        {
            if (handle == new IntPtr(-1))
            {
                return true;
            }
            _ = PInvoke.PropVariantClear(ref Value).ThrowOnFailure();
            handle = new IntPtr(-1);
            return true;
        }

        /// <summary>
        /// Represents the underlying PROPVARIANT value associated with this instance.
        /// </summary>
        /// <remarks>PROPVARIANT is a Windows structure used to store a wide variety of data types. This
        /// field provides direct access to the raw PROPVARIANT value, which may be useful for advanced interop
        /// scenarios.</remarks>
        private PROPVARIANT Value;
    }
}
