using Microsoft.Win32.SafeHandles;

namespace PSADT.Interop.Tests.TestHelpers
{
    /// <summary>
    /// A handle that can be put into any of the states the guard helpers distinguish: valid, invalid, or
    /// closed. The production handles reject an invalid value in their own constructors, so none of them
    /// can be used to reach the invalid branch of those guards.
    /// </summary>
    internal sealed class TestSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        /// <summary>
        /// Creates a handle wrapping the given value without taking ownership of it, so disposal never
        /// tries to release anything.
        /// </summary>
        /// <param name="handle">The handle value to wrap. Zero and minus one are treated as invalid.</param>
        internal TestSafeHandle(nint handle)
            : base(ownsHandle: false)
        {
            SetHandle(handle);
        }

        /// <summary>
        /// Does nothing, since this handle never owns what it wraps.
        /// </summary>
        /// <returns>Always <see langword="true"/>.</returns>
        protected override bool ReleaseHandle()
        {
            return true;
        }
    }
}
