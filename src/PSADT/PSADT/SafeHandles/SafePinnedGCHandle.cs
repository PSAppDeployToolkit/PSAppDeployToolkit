using System.Runtime.InteropServices;

namespace PSADT.SafeHandles
{
    /// <summary>
    /// Represents a wrapper for a <see cref="GCHandle"/> that pins an object in memory.
    /// </summary>
    internal sealed class SafePinnedGCHandle : SafeMemoryHandle
    {
        /// <summary>
        /// Allocates a new <see cref="SafePinnedGCHandle"/> for the specified object, pinning it in memory.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        internal static SafePinnedGCHandle Alloc(object value, int length)
        {
            return new(GCHandle.Alloc(value, GCHandleType.Pinned), length, true);
        }

        /// <summary>
        /// Constructs a new instance of the <see cref="SafePinnedGCHandle"/> class with the specified <see cref="GCHandle"/>.
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="length"></param>
        /// <param name="ownsHandle"></param>
        private SafePinnedGCHandle(GCHandle handle, int length, bool ownsHandle) : base(handle.AddrOfPinnedObject(), length, ownsHandle)
        {
            pinnedHandle = handle;
        }

        /// <summary>
        /// Releases the handle by freeing the underlying <see cref="GCHandle"/>.
        /// </summary>
        /// <returns></returns>
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
