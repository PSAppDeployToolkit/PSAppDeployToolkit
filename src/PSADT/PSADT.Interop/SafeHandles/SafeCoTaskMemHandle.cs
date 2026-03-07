using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using PSADT.Interop.Extensions;
using Windows.Win32.Foundation;

namespace PSADT.Interop.SafeHandles
{
    /// <summary>
    /// Provides a safe handle for unmanaged memory allocated with COM task memory allocation, ensuring proper memory
    /// management and disposal.
    /// </summary>
    /// <remarks>This class is designed to manage the lifecycle of unmanaged memory, automatically releasing
    /// it when the handle is disposed. It is essential for preventing memory leaks in applications that interact with
    /// unmanaged resources.</remarks>
    internal sealed class SafeCoTaskMemHandle : SafeMemoryHandle<SafeCoTaskMemHandle>
    {
        /// <summary>
        /// Allocates a block of unmanaged memory of the specified size and returns a handle to the allocated memory.
        /// </summary>
        /// <remarks>The returned handle owns the allocated memory and will release it automatically when
        /// disposed. Allocating unmanaged memory requires careful management to avoid memory leaks.</remarks>
        /// <param name="byteCount">The number of bytes to allocate. Must be a positive integer.</param>
        /// <returns>A SafeCoTaskMemHandle that represents the allocated memory block. The caller is responsible for releasing
        /// the handle when it is no longer needed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static SafeCoTaskMemHandle Alloc(int byteCount)
        {
            return new(Marshal.AllocCoTaskMem(byteCount), byteCount, ownsHandle: true);
        }

        /// <summary>
        /// Initializes a new instance of the SafeCoTaskMemHandle class using the specified native memory handle, memory
        /// length, and ownership flag.
        /// </summary>
        /// <remarks>This constructor is intended for advanced scenarios where manual control over native
        /// memory management is required. The caller must ensure that the provided handle and length are valid and that
        /// ownership is set appropriately to avoid memory leaks or premature release.</remarks>
        /// <param name="handle">A pointer to the native memory block to be wrapped by the handle.</param>
        /// <param name="length">The size, in bytes, of the memory block referenced by the handle.</param>
        /// <param name="ownsHandle">A value indicating whether the SafeCoTaskMemHandle instance is responsible for releasing the native memory
        /// when disposed.</param>
        internal SafeCoTaskMemHandle(nint handle, int length, bool ownsHandle) : base(handle, length, ownsHandle)
        {
        }

        /// <summary>
        /// Initializes a new instance of the SafeCoTaskMemHandle class using the specified memory handle and ownership
        /// flag.
        /// </summary>
        /// <remarks>This constructor is intended for scenarios where explicit control over memory
        /// ownership is required. The caller is responsible for ensuring that the handle is valid and that ownership is
        /// correctly specified to prevent memory leaks or premature release.</remarks>
        /// <param name="handle">A PWSTR representing the memory block to be managed by the handle. The pointer is converted to an IntPtr for
        /// internal use.</param>
        /// <param name="ownsHandle">true to indicate that the SafeCoTaskMemHandle instance should release the memory when disposed; otherwise,
        /// false.</param>
        internal SafeCoTaskMemHandle(PWSTR handle, bool ownsHandle) : base(handle.ToIntPtr(), handle.Length * sizeof(char), ownsHandle)
        {
        }

        /// <summary>
        /// Releases the handle and frees the associated unmanaged memory.
        /// </summary>
        /// <remarks>Overrides the base class implementation to ensure that memory allocated with COM task
        /// memory allocation is properly freed. If the handle is already set to its default value, no action is
        /// taken.</remarks>
        /// <returns>Always returns <see langword="true"/>, indicating that the handle has been released.</returns>
        protected override bool ReleaseHandle()
        {
            if (default == handle)
            {
                return true;
            }
            try
            {
                Marshal.FreeCoTaskMem(handle);
            }
            finally
            {
                handle = default;
            }
            return true;
        }
    }
}
