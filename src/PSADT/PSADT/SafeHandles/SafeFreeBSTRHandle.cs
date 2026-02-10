using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace PSADT.SafeHandles
{
    /// <summary>
    /// Provides a safe handle for managing and releasing BSTR string pointers allocated in unmanaged code.
    /// </summary>
    /// <remarks>This class ensures that BSTR handles are properly freed using the appropriate mechanism when
    /// no longer needed. It is intended for internal use to prevent memory leaks when working with interop scenarios
    /// involving BSTR strings. The handle is released by calling Marshal.FreeBSTR during disposal.</remarks>
    internal sealed class SafeFreeBSTRHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        /// <summary>
        /// Allocates a new BSTR handle containing the specified string.
        /// </summary>
        /// <remarks>The returned handle encapsulates a BSTR allocated using Marshal.StringToBSTR. Use
        /// this method when interoperating with COM components that require BSTR strings.</remarks>
        /// <param name="str">The string to be copied into the allocated BSTR. Can be null, in which case an empty BSTR is allocated.</param>
        /// <returns>A SafeFreeBSTRHandle representing the allocated BSTR. The caller is responsible for releasing the handle
        /// when it is no longer needed.</returns>
        internal static SafeFreeBSTRHandle Alloc(string str)
        {
            return new(Marshal.StringToBSTR(str), true);
        }

        /// <summary>
        /// Initializes a new instance of the SafeFreeBSTRHandle class with the specified handle and ownership flag.
        /// </summary>
        /// <param name="handle">The pointer to the BSTR handle to be managed by this instance.</param>
        /// <param name="ownsHandle">A value indicating whether the SafeFreeBSTRHandle instance is responsible for releasing the handle.</param>
        private SafeFreeBSTRHandle(nint handle, bool ownsHandle) : base(ownsHandle)
        {
            SetHandle(handle);
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
            Marshal.FreeBSTR(handle);
            handle = default;
            return true;
        }
    }
}
