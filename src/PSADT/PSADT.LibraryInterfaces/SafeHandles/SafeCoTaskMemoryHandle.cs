using System;
using System.Runtime.InteropServices;

namespace PSADT.LibraryInterfaces.SafeHandles
{
    /// <summary>
    /// Provides a safe handle for managing unmanaged memory allocated with COM task memory allocation functions
    /// (CoTaskMem).
    /// </summary>
    /// <remarks>SafeCoTaskMemoryHandle is intended for advanced scenarios where direct management of
    /// unmanaged memory is required, such as interoperability with native code or COM components. The handle ensures
    /// that the associated memory is released appropriately, reducing the risk of memory leaks. Ownership semantics
    /// must be specified correctly when creating an instance to avoid invalid memory access or resource leaks. This
    /// type is not intended for general-purpose memory management and should be used with care.</remarks>
    internal sealed class SafeCoTaskMemoryHandle : SafeMemoryHandle<SafeCoTaskMemoryHandle>
    {
        /// <summary>
        /// Initializes a new instance of the SafeCoTaskMemoryHandle class with the specified memory handle, length, and
        /// ownership flag.
        /// </summary>
        /// <param name="handle">A pointer to the unmanaged memory block to be managed by the handle.</param>
        /// <param name="length">The length, in bytes, of the memory block referenced by handle. Must be non-negative.</param>
        /// <param name="ownsHandle">true to indicate that the SafeCoTaskMemoryHandle instance is responsible for releasing the memory;
        /// otherwise, false.</param>
        internal SafeCoTaskMemoryHandle(IntPtr handle, int length, bool ownsHandle) : base(handle, length, ownsHandle)
        {
        }

        /// <summary>
        /// Initializes a new instance of the SafeCoTaskMemoryHandle class using the specified memory pointer, length,
        /// and ownership flag.
        /// </summary>
        /// <remarks>This constructor is intended for advanced scenarios where an existing unmanaged
        /// memory block, typically allocated with COM task memory allocation functions, needs to be managed by a
        /// SafeHandle. The caller is responsible for ensuring that the pointer and length are valid and that ownership
        /// semantics are correctly specified to avoid memory leaks or invalid memory access.</remarks>
        /// <param name="handle">A pointer to the unmanaged memory block to be wrapped by the handle.</param>
        /// <param name="length">The length, in bytes, of the memory block referenced by handle. Must be non-negative.</param>
        /// <param name="ownsHandle">true to indicate that the SafeCoTaskMemoryHandle instance is responsible for releasing the memory;
        /// otherwise, false.</param>
        internal unsafe SafeCoTaskMemoryHandle(void* handle, int length, bool ownsHandle) : base((IntPtr)handle, length, ownsHandle)
        {
        }

        /// <summary>
        /// Releases the handle by freeing the associated unmanaged memory.
        /// </summary>
        /// <remarks>This method is called by the runtime to release the unmanaged memory allocated for
        /// the handle. It should not be called directly from user code.</remarks>
        /// <returns>true if the handle is released successfully; otherwise, false.</returns>
        protected override bool ReleaseHandle()
        {
            if (handle == default || IntPtr.Zero == handle)
            {
                return true;
            }
            Marshal.FreeCoTaskMem(handle);
            handle = default;
            return true;
        }
    }
}
