using System;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;
using PSADT.ProcessManagement;
using Xunit;

namespace PSADT.Tests.ProcessManagement
{
    /// <summary>
    /// Tests the stream a process's output is read through, which stops reading the output once it has ended.
    /// </summary>
    public sealed class EndOfStreamLatchingStreamTests
    {
        /// <summary>
        /// Verifies that what the stream being read holds comes through unchanged, up to its end.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task ReadAsync_ReadsWhatTheStreamHoldsAsync()
        {
            // Arrange
            using MemoryStream source = new([1, 2]);
            using EndOfStreamLatchingStream stream = new(source);
            byte[] buffer = new byte[3];

            // Act & Assert
            Assert.Equal(2, await stream.ReadAsync(buffer, TestContext.Current.CancellationToken).ConfigureAwait(true));
            Assert.Equal([1, 2, 0], buffer);
            Assert.Equal(0, await stream.ReadAsync(buffer, TestContext.Current.CancellationToken).ConfigureAwait(true));
        }

        /// <summary>
        /// Verifies that the stream being read is not read again once it has ended, even if it has more to give.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task ReadAsync_StaysAtTheEndOnceTheStreamHasEndedAsync()
        {
            // Arrange
            using MemoryStream source = new([1]);
            using EndOfStreamLatchingStream stream = new(source);
            byte[] buffer = new byte[1];
            _ = await stream.ReadAsync(buffer, TestContext.Current.CancellationToken).ConfigureAwait(true);
            Assert.Equal(0, await stream.ReadAsync(buffer, TestContext.Current.CancellationToken).ConfigureAwait(true));

            // Act: rewound, the stream being read has something to give again
            source.Position = 0;

            // Assert
            Assert.Equal(0, await stream.ReadAsync(buffer, TestContext.Current.CancellationToken).ConfigureAwait(true));
        }

        /// <summary>
        /// Verifies that a read asking for nothing is not taken for the end, since it returns nothing whatever the
        /// stream being read holds.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task ReadAsync_DoesNotTakeAnEmptyReadForTheEndAsync()
        {
            // Arrange
            using MemoryStream source = new([1]);
            using EndOfStreamLatchingStream stream = new(source);
            byte[] buffer = new byte[1];

            // Act
            int read = await stream.ReadAsync(Memory<byte>.Empty, TestContext.Current.CancellationToken).ConfigureAwait(true);

            // Assert
            Assert.Equal(0, read);
            Assert.Equal(1, await stream.ReadAsync(buffer, TestContext.Current.CancellationToken).ConfigureAwait(true));
        }

        /// <summary>
        /// Verifies that an anonymous pipe whose last line has no line break is read to its end, which is how a
        /// process's output ends when the process is terminated part-way through a line.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task ReadAsync_ReadsAPipeWhoseLastLineHasNoLineBreakAsync()
        {
            // Arrange: written to and closed, with no line break after what was written
            using AnonymousPipeServerStream pipe = new(PipeDirection.In);
            using (AnonymousPipeClientStream client = new(PipeDirection.Out, pipe.ClientSafePipeHandle))
            using (StreamWriter writer = new(client))
            {
                await writer.WriteAsync("no-line-break").ConfigureAwait(true);
            }
            using EndOfStreamLatchingStream stream = new(pipe);
            using StreamReader reader = new(stream);

            // Act & Assert: the second read is the one .NET Framework's pipe throws on when read directly
            Assert.Equal("no-line-break", await reader.ReadLineAsync(TestContext.Current.CancellationToken).ConfigureAwait(true));
            Assert.Null(await reader.ReadLineAsync(TestContext.Current.CancellationToken).ConfigureAwait(true));
        }

        /// <summary>
        /// Verifies that the stream can be read exactly when the stream being read can.
        /// </summary>
        [Fact]
        public void CanRead_FollowsTheStreamBeingRead()
        {
            // Arrange
            using MemoryStream readable = new();
            using AnonymousPipeServerStream writeOnly = new(PipeDirection.Out);
            using EndOfStreamLatchingStream readableStream = new(readable);
            using EndOfStreamLatchingStream writeOnlyStream = new(writeOnly);

            // Act & Assert
            Assert.True(readableStream.CanRead);
            Assert.False(writeOnlyStream.CanRead);
        }

        /// <summary>
        /// Verifies that the stream cannot be positioned, since it only reads forward.
        /// </summary>
        [Fact]
        public void CanSeek_IsFalseAndPositioningIsRefused()
        {
            // Arrange
            using MemoryStream source = new([1]);
            using EndOfStreamLatchingStream stream = new(source);

            // Act & Assert: refused even though the stream being read could be positioned
            Assert.False(stream.CanSeek);
            _ = Assert.Throws<NotSupportedException>(() => stream.Length);
            _ = Assert.Throws<NotSupportedException>(() => stream.Position);
            _ = Assert.Throws<NotSupportedException>(() => stream.Position = 0);
            _ = Assert.Throws<NotSupportedException>(() => stream.Seek(0, SeekOrigin.Begin));
            _ = Assert.Throws<NotSupportedException>(() => stream.SetLength(0));
        }

        /// <summary>
        /// Verifies that the stream cannot be written to, and that flushing it does nothing rather than failing,
        /// since flushing a read-only stream is allowed.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0045:Do not use blocking calls in a sync method", Justification = "The synchronous members are the ones under test.")]
        [Fact]
        public void CanWrite_IsFalseAndWritingIsRefused()
        {
            // Arrange
            using MemoryStream source = new();
            using EndOfStreamLatchingStream stream = new(source);

            // Act & Assert: refused even though the stream being read could be written to
            Assert.False(stream.CanWrite);
            _ = Assert.Throws<NotSupportedException>(() => stream.Write([1], 0, 1));
            stream.Flush();
            Assert.Equal(0, source.Length);
        }

        /// <summary>
        /// Verifies that disposing the stream disposes the stream being read, so that disposing a reader over it
        /// closes what it reads.
        /// </summary>
        [Fact]
        public void Dispose_DisposesTheStreamBeingRead()
        {
            // Arrange
            using MemoryStream source = new();
            EndOfStreamLatchingStream stream = new(source);

            // Act
            stream.Dispose();

            // Assert
            Assert.False(source.CanRead);
        }
    }
}
