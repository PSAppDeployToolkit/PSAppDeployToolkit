using System;
using System.IO;
using Xunit;

namespace PSADT.Interop.Tests.Polyfills
{
    /// <summary>
    /// Tests the synchronous Stream.Read(Span&lt;byte&gt;) polyfill. On net472 the call binds to the extension
    /// PSADT.Interop generates; on net8.0 to the framework. Both legs run the same assertions, so a failure on
    /// net8.0 means the expectation is wrong and a failure on net472 alone means the polyfill diverges from it.
    /// </summary>
    public sealed class StreamReadPolyfillTests
    {
        /// <summary>
        /// Verifies that a read fills the span from the stream and advances the position by the count returned.
        /// </summary>
        [Fact]
        public void Read_FillsTheSpanAndAdvances()
        {
            // Arrange
            using MemoryStream stream = new([1, 2, 3, 4, 5]);
            Span<byte> buffer = new byte[3];

            // Act
            int read = stream.Read(buffer);

            // Assert
            Assert.Equal(3, read);
            Assert.Equal([1, 2, 3], buffer.ToArray());
            Assert.Equal(3, stream.Position);
        }

        /// <summary>
        /// Verifies that a read near the end returns only what remains rather than filling the whole span.
        /// </summary>
        [Fact]
        public void Read_AtTheEnd_ReturnsOnlyWhatRemains()
        {
            // Arrange
            using MemoryStream stream = new([1, 2, 3, 4, 5]);
            Span<byte> buffer = new byte[4];
            _ = stream.Read(buffer);

            // Act: only one byte is left
            int read = stream.Read(buffer);

            // Assert
            Assert.Equal(1, read);
            Assert.Equal(5, buffer[0]);
        }

        /// <summary>
        /// Verifies that a read of an exhausted stream returns zero, which is how the caller learns it is at the end.
        /// </summary>
        [Fact]
        public void Read_PastTheEnd_ReturnsZero()
        {
            // Arrange
            using MemoryStream stream = new([1, 2]);
            Span<byte> buffer = new byte[2];
            _ = stream.Read(buffer);

            // Act
            int read = stream.Read(buffer);

            // Assert
            Assert.Equal(0, read);
        }

        /// <summary>
        /// Verifies that reading into an empty span reads nothing and leaves the position where it was.
        /// </summary>
        [Fact]
        public void Read_EmptySpan_ReadsNothing()
        {
            // Arrange
            using MemoryStream stream = new([1, 2, 3]);

            // Act
            int read = stream.Read([]);

            // Assert
            Assert.Equal(0, read);
            Assert.Equal(0, stream.Position);
        }
    }
}
