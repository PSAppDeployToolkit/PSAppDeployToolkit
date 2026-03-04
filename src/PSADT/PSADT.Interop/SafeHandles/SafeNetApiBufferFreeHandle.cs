using System;
using PSADT.Interop.Extensions;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace PSADT.Interop.SafeHandles
{
    /// <summary>
    /// Provides a safe handle for memory buffers allocated by the Net API, ensuring that resources are properly
    /// released when no longer needed.
    /// </summary>
    /// <remarks>This class inherits from SafeMemoryHandle and is designed to manage the lifetime of unmanaged
    /// memory buffers allocated by the Net API. When the handle is released, the associated memory is freed using
    /// NetApiBufferFree to prevent resource leaks.</remarks>
    internal sealed class SafeNetApiBufferFreeHandle : SafeMemoryHandle<SafeNetApiBufferFreeHandle>
    {
        /// <summary>
        /// Initializes a new instance of the SafeNetApiBufferFreeHandle class using the specified unmanaged resource
        /// handle, buffer length, and ownership flag.
        /// </summary>
        /// <remarks>This constructor is intended for internal use and ensures proper management of the
        /// handle's lifecycle, including resource cleanup when ownership is specified.</remarks>
        /// <param name="handle">The handle to the unmanaged resource to be wrapped by this instance.</param>
        /// <param name="length">The length, in bytes, of the buffer associated with the handle.</param>
        /// <param name="ownsHandle">true to release the handle when this instance is disposed; otherwise, false.</param>
        internal SafeNetApiBufferFreeHandle(IntPtr handle, int length, bool ownsHandle) : base(handle, length, ownsHandle)
        {
        }

        /// <summary>
        /// Initializes a new instance of the SafeNetApiBufferFreeHandle class to manage a specified native string
        /// buffer handle.
        /// </summary>
        /// <remarks>Use this constructor when you need to wrap an existing PWSTR handle for safe resource
        /// management. The handle will be released automatically if ownsHandle is set to true.</remarks>
        /// <param name="handle">The native string buffer handle to be managed. Must be a valid, non-null PWSTR.</param>
        /// <param name="ownsHandle">true to indicate that this instance owns the handle and will release it when disposed; otherwise, false.</param>
        internal SafeNetApiBufferFreeHandle(PWSTR handle, bool ownsHandle) : base(handle.ToIntPtr(), handle.Length * sizeof(char), ownsHandle)
        {
        }

        /// <summary>
        /// Releases the handle.
        /// </summary>
        /// <returns></returns>
        protected override bool ReleaseHandle()
        {
            if (default == handle)
            {
                return true;
            }
            try
            {
                unsafe
                {
                    _ = ((WIN32_ERROR)PInvoke.NetApiBufferFree((void*)handle)).ThrowOnFailure();
                }
            }
            finally
            {
                handle = default;
            }
            return true;
        }
    }
}
