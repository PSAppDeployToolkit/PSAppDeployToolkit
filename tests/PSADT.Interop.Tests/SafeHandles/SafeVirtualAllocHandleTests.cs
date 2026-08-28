using System;
using PSADT.Interop.SafeHandles;
using Windows.Win32.System.Memory;
using Xunit;

namespace PSADT.Interop.Tests.SafeHandles
{
    /// <summary>
    /// Tests the wrapper for process-local memory reserved through VirtualAlloc. It is the vehicle the
    /// SafeMemoryHandle tests use, so what is checked here is only what belongs to this type: its
    /// allocation factory and its release.
    /// </summary>
    public sealed class SafeVirtualAllocHandleTests
    {
        /// <summary>
        /// The allocation flags every reservation here uses.
        /// </summary>
        private const VIRTUAL_ALLOCATION_TYPE Reserve = VIRTUAL_ALLOCATION_TYPE.MEM_COMMIT | VIRTUAL_ALLOCATION_TYPE.MEM_RESERVE;

        /// <summary>
        /// Verifies that a request for memory yields a usable region of the requested length.
        /// </summary>
        [Fact]
        public void Alloc_ReservesTheRequestedLength()
        {
            // Act
            using SafeVirtualAllocHandle handle = SafeVirtualAllocHandle.Alloc(64, Reserve, PAGE_PROTECTION_FLAGS.PAGE_READWRITE);

            // Assert
            Assert.False(handle.IsInvalid);
            Assert.Equal(64, handle.Length);
        }

        /// <summary>
        /// Verifies that a request Windows refuses is raised rather than wrapped. A zero-length reservation
        /// yields nothing, and the constructor rejects the empty address it would otherwise hold.
        /// </summary>
        [Fact]
        public void Alloc_RaisesARefusedRequest()
        {
            // Assert
            _ = Assert.Throws<ArgumentOutOfRangeException>(static () => SafeVirtualAllocHandle.Alloc(0, Reserve, PAGE_PROTECTION_FLAGS.PAGE_READWRITE));
        }

        /// <summary>
        /// Verifies that the region is released cleanly and that releasing twice is tolerated.
        /// </summary>
        [Fact]
        public void Dispose_ReleasesIdempotently()
        {
            // Arrange
            SafeVirtualAllocHandle handle = SafeVirtualAllocHandle.Alloc(64, Reserve, PAGE_PROTECTION_FLAGS.PAGE_READWRITE);

            // Act & Assert
            Assert.Null(Record.Exception(handle.Dispose));
            Assert.Null(Record.Exception(handle.Dispose));
            Assert.True(handle.IsClosed);
        }
    }
}
