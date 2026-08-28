using System;
using PSADT.Interop.SafeHandles;
using Xunit;

namespace PSADT.Interop.Tests.SafeHandles
{
    /// <summary>
    /// Tests the wrapper for an environment block returned by the user profile APIs.
    /// </summary>
    public sealed class SafeEnvironmentBlockHandleTests
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
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => new SafeEnvironmentBlockHandle(handle, ownsHandle: false));
        }

        /// <summary>
        /// Verifies that a non-owning handle closes without invoking its release path. The value wrapped
        /// here is fabricated, so destroying it would fail and surface as an exception from Dispose.
        /// </summary>
        [Fact]
        public void NonOwningHandle_ClosesWithoutReleasing()
        {
            // Arrange
            using SafeEnvironmentBlockHandle handle = new(1, ownsHandle: false);

            // Act & Assert
            Assert.Null(Record.Exception(handle.Dispose));
            Assert.True(handle.IsClosed);
        }
    }
}
