using System;
using PSADT.Interop.SafeHandles;
using Xunit;

namespace PSADT.Interop.Tests.SafeHandles
{
    /// <summary>
    /// Tests the wrapper for handles that are never released, which is what predefined and borrowed
    /// handles need.
    /// </summary>
    public sealed class SafeNoReleaseHandleTests
    {
        /// <summary>
        /// Verifies that both invalid sentinels are rejected, so an unusable handle cannot be wrapped and
        /// passed on as though it were fine.
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
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => new SafeNoReleaseHandle(handle));
        }

        /// <summary>
        /// Verifies that a wrapped usable handle is not reported as invalid, which is the state every
        /// guard downstream keys off.
        /// </summary>
        [Fact]
        public void WrappedHandle_IsNotReportedAsInvalid()
        {
            // Arrange
            using SafeNoReleaseHandle handle = new(1);

            // Assert
            Assert.False(handle.IsInvalid);
            Assert.False(handle.IsClosed);
            Assert.Equal<long>(1, handle.DangerousGetHandle().ToInt64());
        }

        /// <summary>
        /// Verifies that closing does not touch the handle. The value wrapped here is fabricated, so any
        /// attempt to release it would fail and surface as an exception from Dispose.
        /// </summary>
        [Fact]
        public void Dispose_ClosesWithoutReleasing()
        {
            // Arrange
            using SafeNoReleaseHandle handle = new(1);

            // Act & Assert
            Assert.Null(Record.Exception(handle.Dispose));
            Assert.True(handle.IsClosed);
            Assert.Null(Record.Exception(handle.Dispose));
        }
    }
}
