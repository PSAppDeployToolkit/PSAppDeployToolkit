using System;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.System.Threading;

namespace PSADT.SafeHandles
{
    /// <summary>
    /// Represents a wrapper for an environment block handle that ensures the handle is properly released.
    /// </summary>
    internal sealed class SafeProcThreadAttributeListHandle : SafeBaseHandle
    {
        /// <summary>
        /// Creates a new instance of <see cref="SafeProcThreadAttributeListHandle"/> with the specified number of
        /// attributes.
        /// </summary>
        /// <param name="count">The number of attributes to include in the list. Must be greater than zero.</param>
        /// <returns>A <see cref="SafeProcThreadAttributeListHandle"/> initialized with the specified number of attributes.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="count"/> is zero.</exception>
        internal static SafeProcThreadAttributeListHandle Create(uint count)
        {
            if (count == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Count must be greater than zero.");
            }
            nuint lpSize = UIntPtr.Zero;
            PInvoke.InitializeProcThreadAttributeList(default, count, ref lpSize);
            var handle = Marshal.AllocHGlobal((int)lpSize);
            PInvoke.InitializeProcThreadAttributeList((LPPROC_THREAD_ATTRIBUTE_LIST)handle, count, ref lpSize);
            return new(handle, true);
        }

        /// <summary>
        /// Represents a safe handle for a process thread attribute list.
        /// </summary>
        /// <remarks>This class is used to manage the lifetime of a handle to a process thread attribute
        /// list, ensuring that the handle is released properly when no longer needed. It inherits from a base safe
        /// handle class, which provides the necessary functionality to handle resource cleanup.</remarks>
        /// <param name="handle">The initial handle to the process thread attribute list.</param>
        /// <param name="ownsHandle">A value indicating whether the handle should be released when the safe handle is disposed. true if the
        /// handle should be released; otherwise, false.</param>
        private SafeProcThreadAttributeListHandle(IntPtr handle, bool ownsHandle) : base(handle, ownsHandle)
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
            PInvoke.DeleteProcThreadAttributeList((LPPROC_THREAD_ATTRIBUTE_LIST)handle);
            Marshal.FreeHGlobal(handle);
            handle = default;
            return true;
        }
    }
}
