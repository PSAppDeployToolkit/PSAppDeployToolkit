using System;
using PSADT.Interop.SafeHandles;
using Windows.Win32.System.RemoteDesktop;
using Xunit;

namespace PSADT.Interop.Tests.SafeHandles
{
    /// <summary>
    /// Tests the wrapper for a typed memory block returned by the extended terminal services APIs. It
    /// carries the record type alongside the block, because releasing it needs an element count that only
    /// the type can supply.
    /// </summary>
    public sealed class SafeWtsExHandleTests
    {
        /// <summary>
        /// Verifies that both invalid sentinels are rejected by the constructor.
        /// </summary>
        /// <param name="value">The sentinel expected to be rejected.</param>
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Constructor_RejectsBothInvalidSentinels(int value)
        {
            // Arrange
            nint handle = value;

            // Assert
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => new SafeWtsExHandle(handle, WTS_TYPE_CLASS.WTSTypeProcessInfoLevel0, 8, ownsHandle: false));
        }

        /// <summary>
        /// Verifies that a length describing no readable region is rejected, since both the bounds checks
        /// and the element count used to release the block are derived from it.
        /// </summary>
        /// <param name="length">The length expected to be rejected.</param>
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Constructor_RejectsANonPositiveLength(int length)
        {
            // Assert
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => new SafeWtsExHandle(1, WTS_TYPE_CLASS.WTSTypeProcessInfoLevel0, length, ownsHandle: false));
        }
    }
}
