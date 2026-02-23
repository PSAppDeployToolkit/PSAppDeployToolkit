using System.Runtime.InteropServices;
using PSADT.Interop.SafeHandles;

namespace PSADT.SafeHandles
{
    /// <summary>
    /// Represents a wrapper for a <see cref="GCHandle"/> that pins an object in memory.
    /// </summary>
    internal sealed class SafePinnedGCHandle : SafeMemoryHandle<SafePinnedGCHandle>
    {
        /// <summary>
        /// Allocates a pinned handle for the specified array, preventing the garbage collector from moving it during a
        /// pinning operation.
        /// </summary>
        /// <remarks>This method is useful when you need to pass a managed array to unmanaged code without
        /// the risk of the garbage collector moving it. The allocated handle should be released when no longer needed
        /// to avoid memory leaks.</remarks>
        /// <typeparam name="T">The type of the elements in the array being pinned.</typeparam>
        /// <param name="value">The array of type T to be pinned in memory. This array cannot be null.</param>
        /// <returns>A SafePinnedGCHandle that represents the pinned handle for the specified array.</returns>
        internal static SafePinnedGCHandle Alloc<T>(T[] value)
        {
            return new(GCHandle.Alloc(value, GCHandleType.Pinned), Marshal.SizeOf<T>() * value.Length, true);
        }

        /// <summary>
        /// Initializes a new instance of the SafePinnedGCHandle class using the specified pinned GCHandle, memory
        /// length, and ownership flag.
        /// </summary>
        /// <remarks>This constructor is intended for internal use and should not be called directly from
        /// user code.</remarks>
        /// <param name="handle">The GCHandle representing the object to be pinned in memory. This handle must be valid and must not have
        /// been previously freed.</param>
        /// <param name="length">The length, in bytes, of the memory region to be pinned. Must be greater than zero.</param>
        /// <param name="ownsHandle">A value indicating whether the SafePinnedGCHandle instance is responsible for releasing the handle. Specify
        /// <see langword="true"/> to release the handle when the instance is disposed; otherwise, <see
        /// langword="false"/>.</param>
        private SafePinnedGCHandle(GCHandle handle, int length, bool ownsHandle) : base(handle.AddrOfPinnedObject(), length, ownsHandle)
        {
            pinnedHandle = handle;
        }

        /// <summary>
        /// Releases the handle and frees any resources associated with the pinned object.
        /// </summary>
        /// <remarks>This method is called by the runtime or by user code to release the underlying handle
        /// when it is no longer needed. It ensures that any pinned resources are properly freed to prevent memory
        /// leaks.</remarks>
        /// <returns>Always returns <see langword="true"/>, indicating that the handle was released successfully.</returns>
        protected override bool ReleaseHandle()
        {
            pinnedHandle.Free();
            return true;
        }

        /// <summary>
        /// The underlying <see cref="GCHandle"/> that holds the pinned object.
        /// </summary>
        private readonly GCHandle pinnedHandle;
    }
}
