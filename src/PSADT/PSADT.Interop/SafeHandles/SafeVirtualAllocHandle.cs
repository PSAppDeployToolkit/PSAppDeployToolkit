using System;
using PSADT.Interop.Utilities;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Memory;

namespace PSADT.Interop.SafeHandles
{
    /// <summary>
    /// Represents a wrapper for a virtual memory allocation handle that ensures the handle is properly released.
    /// </summary>
    internal sealed class SafeVirtualAllocHandle : SafeMemoryHandle<SafeVirtualAllocHandle>
    {
        /// <summary>
        /// Allocates a block of virtual memory of the specified size, allocation type, and protection flags.
        /// </summary>
        /// <remarks>This method is intended for internal use. Improper use may result in memory leaks or
        /// access violations. The caller must ensure that the allocated memory is released appropriately.</remarks>
        /// <param name="length">The size, in bytes, of the memory block to allocate. Must be a positive integer.</param>
        /// <param name="allocationType">The type of memory allocation to perform. Determines how the memory is reserved or committed.</param>
        /// <param name="protect">The memory protection to apply to the allocated region. Specifies the allowed access rights for the memory
        /// block.</param>
        /// <returns>A SafeVirtualAllocHandle that represents the allocated memory block. The caller is responsible for releasing
        /// the handle when it is no longer needed.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the memory allocation fails.</exception>
        internal static SafeVirtualAllocHandle Alloc(int length, VIRTUAL_ALLOCATION_TYPE allocationType, PAGE_PROTECTION_FLAGS protect)
        {
            unsafe
            {
                void* handle = PInvoke.VirtualAlloc(null, (nuint)length, allocationType, protect);
                if (handle is null)
                {
                    throw new InvalidOperationException("Failed to allocate memory.");
                }
                return new((nint)handle, length, true);
            }
        }

        /// <summary>
        /// Initializes a new instance of the SafeVirtualAllocHandle class with the specified memory handle, length, and
        /// ownership flag.
        /// </summary>
        /// <remarks>This constructor is intended for internal use only and should not be called directly
        /// from user code.</remarks>
        /// <param name="handle">A handle to the memory block that the SafeVirtualAllocHandle instance will manage.</param>
        /// <param name="length">The size, in bytes, of the memory block associated with the handle.</param>
        /// <param name="ownsHandle">true to indicate that the SafeVirtualAllocHandle instance is responsible for releasing the handle;
        /// otherwise, false.</param>
        private SafeVirtualAllocHandle(nint handle, int length, bool ownsHandle) : base(handle, length, ownsHandle)
        {
        }

        /// <summary>
        /// Releases the handle associated with the allocated virtual memory resource.
        /// </summary>
        /// <remarks>This method is typically called during resource cleanup to free unmanaged memory. If
        /// the handle is already set to its default value, no action is taken and the method returns true. If the
        /// release operation fails, an exception is thrown to indicate the error.</remarks>
        /// <returns>true if the handle was successfully released or was already set to its default value; otherwise, an
        /// exception is thrown.</returns>
        protected override bool ReleaseHandle()
        {
            if (default == handle)
            {
                return true;
            }
            BOOL res;
            unsafe
            {
                res = PInvoke.VirtualFree((void*)handle, default, VIRTUAL_FREE_TYPE.MEM_RELEASE);
            }
            if (!res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            handle = default;
            return res;
        }
    }
}
