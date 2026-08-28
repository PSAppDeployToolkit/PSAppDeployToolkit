using System;
using PSADT.Interop.SafeHandles;
using Xunit;

namespace PSADT.Interop.Tests.SafeHandles
{
    /// <summary>
    /// Tests the wrapper for a memory block returned by the terminal services APIs.
    /// </summary>
    public sealed class SafeWtsHandleTests
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
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => new SafeWtsHandle(handle, 8, ownsHandle: false));
        }

        /// <summary>
        /// Verifies that a length describing no readable region is rejected, since every bounds check
        /// downstream is derived from it.
        /// </summary>
        /// <param name="length">The length expected to be rejected.</param>
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Constructor_RejectsANonPositiveLength(int length)
        {
            // Assert
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => new SafeWtsHandle(1, length, ownsHandle: false));
        }
    }
}
