using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using PSADT.LibraryInterfaces;
using PSADT.Utilities;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Threading;

namespace PSADT.SafeHandles
{
    /// <summary>
    /// Represents a safe handle for a process thread attribute list used with Windows process and thread creation APIs.
    /// </summary>
    /// <remarks>This class manages the lifetime of a process thread attribute list handle, ensuring that
    /// unmanaged resources are released reliably. It is intended for use with native interop scenarios where process or
    /// thread attributes must be specified. The handle is released automatically when the object is disposed or
    /// finalized.</remarks>
    internal sealed class SafeProcThreadAttributeListHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        /// <summary>
        /// Creates a new instance of <see cref="SafeProcThreadAttributeListHandle"/> with the specified number of
        /// attributes.
        /// </summary>
        /// <param name="count">The number of attributes to include in the list. Must be greater than zero.</param>
        /// <returns>A <see cref="SafeProcThreadAttributeListHandle"/> initialized with the specified number of attributes.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="count"/> is zero.</exception>
        internal static SafeProcThreadAttributeListHandle Alloc(uint count)
        {
            if (count == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Count must be greater than zero.");
            }
            UIntPtr lpSize = UIntPtr.Zero; Initialize(default, count, ref lpSize);
            IntPtr handle = Marshal.AllocHGlobal((int)lpSize);
            try
            {
                Initialize((LPPROC_THREAD_ATTRIBUTE_LIST)handle, count, ref lpSize);
                return new(handle, true);
            }
            catch
            {
                Marshal.FreeHGlobal(handle);
                throw;
            }
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
        private SafeProcThreadAttributeListHandle(IntPtr handle, bool ownsHandle) : base(ownsHandle)
        {
            SetHandle(handle);
        }

        /// <summary>
        /// Initializes a list of attributes for process and thread creation.
        /// </summary>
        /// <param name="lpAttributeList">A pointer to a buffer that receives the updated attribute list.</param>
        /// <param name="dwAttributeCount">The number of attributes to be added to the list.</param>
        /// <param name="lpSize">On input, specifies the size of the lpAttributeList buffer. On output, receives the required buffer size if
        /// the function fails.</param>
        /// <returns><see langword="true"/> if the attribute list is successfully initialized; otherwise, <see
        /// langword="false"/>.</returns>
        private static BOOL Initialize(LPPROC_THREAD_ATTRIBUTE_LIST lpAttributeList, uint dwAttributeCount, ref nuint lpSize)
        {
            BOOL res = PInvoke.InitializeProcThreadAttributeList(lpAttributeList, dwAttributeCount, ref lpSize);
            return !res && ((WIN32_ERROR)Marshal.GetLastWin32Error() is WIN32_ERROR lastWin32Error) && (lastWin32Error != WIN32_ERROR.ERROR_INSUFFICIENT_BUFFER || lpAttributeList != default)
                ? throw ExceptionUtilities.GetExceptionForLastWin32Error(lastWin32Error)
                : res;
        }

        /// <summary>
        /// Updates the attributes of a specified process or thread.
        /// </summary>
        /// <param name="Attribute">The attribute key to update. This specifies which attribute to modify in the list.</param>
        /// <param name="lpValue">A pointer to the attribute value. The type and meaning of this value depend on the attribute key specified
        /// by <paramref name="Attribute"/>.</param>
        /// <param name="lpPreviousValue">A pointer to a buffer that receives the previous value of the attribute. This parameter can be <see
        /// langword="null"/> if the previous value is not required.</param>
        /// <param name="lpReturnSize">A pointer to a variable that receives the size of the attribute value. This parameter can be <see
        /// langword="null"/> if the size is not required.</param>
        /// <returns><see langword="true"/> if the function succeeds; otherwise, <see langword="false"/>.</returns>
        internal BOOL Update(PROC_THREAD_ATTRIBUTE Attribute, ReadOnlySpan<byte> lpValue, Span<byte> lpPreviousValue = default, nuint? lpReturnSize = null)
        {
            bool lpAttributeListAddRef = false;
            BOOL res;
            try
            {
                DangerousAddRef(ref lpAttributeListAddRef);
                res = PInvoke.UpdateProcThreadAttribute((LPPROC_THREAD_ATTRIBUTE_LIST)DangerousGetHandle(), 0, (nuint)Attribute, lpValue, lpPreviousValue, lpReturnSize);
            }
            finally
            {
                if (lpAttributeListAddRef)
                {
                    DangerousRelease();
                }
            }
            return !res ? throw new Win32Exception() : res;
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
