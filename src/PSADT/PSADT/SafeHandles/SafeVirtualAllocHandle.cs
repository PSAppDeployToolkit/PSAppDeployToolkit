using System;
using System.Runtime.InteropServices;
using PSADT.LibraryInterfaces;
using Windows.Win32.System.Memory;

namespace PSADT.SafeHandles
{
    /// <summary>
    /// Represents a wrapper for a virtual memory allocation handle that ensures the handle is properly released.
    /// </summary>
    internal sealed class SafeVirtualAllocHandle(IntPtr handle, bool ownsHandle) : SafeBaseHandle(handle, ownsHandle)
    {
        /// <summary>
        /// Writes the provided data to the allocated memory.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="startIndex"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        internal void Write(byte[] data, int startIndex = 0)
        {
            if (null == data)
            {
                throw new ArgumentNullException(nameof(data));
            }
            if (data.Length == 0)
            {
                throw new ArgumentException("Code length cannot be zero.", nameof(data));
            }
            Marshal.Copy(data, startIndex, handle, data.Length);
        }

        /// <summary>
        /// Releases the handle.
        /// </summary>
        /// <returns></returns>
        protected override bool ReleaseHandle() => Kernel32.VirtualFree(handle, UIntPtr.Zero, VIRTUAL_FREE_TYPE.MEM_RELEASE);
    }
}
