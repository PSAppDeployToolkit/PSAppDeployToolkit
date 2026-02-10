using System;
using PSADT.LibraryInterfaces.Utilities;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Memory;

namespace PSADT.LibraryInterfaces.SafeHandles
{
    /// <summary>
    /// Represents a wrapper for a virtual memory allocation handle that ensures the handle is properly released.
    /// </summary>
    internal sealed class SafeVirtualAllocHandle : SafeMemoryHandle<SafeVirtualAllocHandle>
    {
        /// <summary>
        /// Allocates a block of memory of the specified size and returns a <see cref="SafeVirtualAllocHandle"/> that wraps the allocated memory.
        /// This is fully implemented here as static virtual members don't come until C# 11 (.NET 7).
        /// </summary>
        /// <param name="length"></param>
        /// <param name="allocationType"></param>
        /// <param name="protect"></param>
        /// <returns></returns>
        /// <exception cref="OutOfMemoryException"></exception>
        internal static SafeVirtualAllocHandle Alloc(int length, VIRTUAL_ALLOCATION_TYPE allocationType, PAGE_PROTECTION_FLAGS protect)
        {
            unsafe
            {
                void* handle = PInvoke.VirtualAlloc(null, (nuint)length, allocationType, protect);
                if (handle is null)
                {
                    throw new InvalidOperationException("Failed to allocate memory.");
                }
                return new((IntPtr)handle, length, true);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SafeVirtualAllocHandle"/> class with the specified handle and size.
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="length"></param>
        /// <param name="ownsHandle"></param>
        private SafeVirtualAllocHandle(IntPtr handle, int length, bool ownsHandle) : base(handle, length, ownsHandle)
        {
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
