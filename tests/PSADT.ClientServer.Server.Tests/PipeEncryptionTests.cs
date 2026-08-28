using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using PSADT.ClientServer.Server.Tests.TestHelpers;
using Xunit;

namespace PSADT.ClientServer.Server.Tests
{
    /// <summary>
    /// Tests what the two halves of the pipe encryption share: the framing that puts a message on the
    /// wire, and the authenticated encryption that protects it.
    /// </summary>
    /// <remarks>
    /// The base type is abstract and most of what it does is reachable only from a subclass, so it is
    /// exercised through the two concrete halves. That is the right level anyway - what matters is that a
    /// message written by one arrives intact at the other, and that everything else is refused.
    /// <para>
    /// The refusals get most of the attention. This is the code that stands between a process running as
    /// the local system account and a stream it does not control, so a corrupt or hostile frame has to
    /// fail rather than be acted on, and it has to fail before it can be turned into an allocation. Each
    /// of the four ways a length prefix can lie is asserted separately for that reason.
    /// </para>
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "RCS1046:Add suffix 'Async' to asynchronous method name", Justification = "Test names describe the scenario under test; the async suffix would obscure them.")]
    public sealed class PipeEncryptionTests
    {
        /// <summary>
        /// Verifies that a message written by one half arrives at the other unchanged.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task WriteEncrypted_RoundTripsThroughReadEncrypted()
        {
            // Arrange
            using EncryptionPair pair = await EncryptionPair.CreateAsync().ConfigureAwait(true);
            byte[] plaintext = DefaultEncoding.Value.GetBytes("the quick brown fox");
            using MemoryStream wire = new();

            // Act
            await pair.Server.WriteEncryptedAsync(wire, plaintext).ConfigureAwait(true);
            wire.Position = 0;

            // Assert
            Assert.Equal(plaintext, await pair.Client.ReadEncryptedAsync(wire).ConfigureAwait(true));
        }

        /// <summary>
        /// Verifies that the same message encrypted twice does not produce the same bytes, since a fresh
        /// nonce is what stops an observer learning that a command has been repeated.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task WriteEncrypted_ProducesDifferentBytesEachTime()
        {
            // Arrange
            using EncryptionPair pair = await EncryptionPair.CreateAsync().ConfigureAwait(true);
            byte[] plaintext = [1, 2, 3, 4];
            using MemoryStream first = new();
            using MemoryStream second = new();

            // Act
            await pair.Server.WriteEncryptedAsync(first, plaintext).ConfigureAwait(true);
            await pair.Server.WriteEncryptedAsync(second, plaintext).ConfigureAwait(true);

            // Assert
            Assert.NotEqual(first.ToArray(), second.ToArray());
        }

        /// <summary>
        /// Verifies that a message whose ciphertext was altered on the way is refused rather than decrypted
        /// into something else.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task ReadEncrypted_RefusesAlteredCiphertext()
        {
            // Arrange: byte 16 is inside the ciphertext, which begins after the 4-byte length and 12-byte nonce
            using EncryptionPair pair = await EncryptionPair.CreateAsync().ConfigureAwait(true);
            byte[] frame = await EncryptToBytesAsync(pair, [1, 2, 3, 4]).ConfigureAwait(true);
            frame[16] ^= 0xFF;
            using MemoryStream wire = new(frame);

            // Assert
            _ = await Assert.ThrowsAnyAsync<CryptographicException>(async () => await pair.Client.ReadEncryptedAsync(wire).ConfigureAwait(true)).ConfigureAwait(true);
        }

        /// <summary>
        /// Verifies that a message whose authentication tag was altered is refused, which is the case that
        /// separates authenticated encryption from the kind that would happily decrypt anything.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task ReadEncrypted_RefusesAnAlteredTag()
        {
            // Arrange: the tag is the last sixteen bytes of the frame
            using EncryptionPair pair = await EncryptionPair.CreateAsync().ConfigureAwait(true);
            byte[] frame = await EncryptToBytesAsync(pair, [1, 2, 3, 4]).ConfigureAwait(true);
            frame[^1] ^= 0xFF;
            using MemoryStream wire = new(frame);

            // Assert
            _ = await Assert.ThrowsAnyAsync<CryptographicException>(async () => await pair.Client.ReadEncryptedAsync(wire).ConfigureAwait(true)).ConfigureAwait(true);
        }

        /// <summary>
        /// Verifies that a message encrypted under a different key is refused, which is what an unrelated
        /// process writing to the pipe would amount to.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task ReadEncrypted_RefusesAMessageFromAStranger()
        {
            // Arrange: two unrelated pairs, so two unrelated keys
            using EncryptionPair pair = await EncryptionPair.CreateAsync().ConfigureAwait(true);
            using EncryptionPair stranger = await EncryptionPair.CreateAsync().ConfigureAwait(true);
            using MemoryStream wire = new(await EncryptToBytesAsync(stranger, [1, 2, 3, 4]).ConfigureAwait(true));

            // Assert
            _ = await Assert.ThrowsAnyAsync<CryptographicException>(async () => await pair.Client.ReadEncryptedAsync(wire).ConfigureAwait(true)).ConfigureAwait(true);
        }

        /// <summary>
        /// Verifies that a length prefix which is not a length is refused.
        /// </summary>
        /// <remarks>
        /// Zero and a negative number are both refused before anything is allocated, and a length past the
        /// ceiling is refused for the same reason: the number comes off the wire, so believing it would let
        /// whatever wrote it decide how much memory to demand.
        /// </remarks>
        /// <param name="length">The length to claim in the prefix.</param>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(int.MinValue)]
        [InlineData((16 * 1024 * 1024) + 1)]
        [InlineData(int.MaxValue)]
        public async Task ReadEncrypted_RefusesALengthPrefixThatIsNotALength(int length)
        {
            // Arrange
            using EncryptionPair pair = await EncryptionPair.CreateAsync().ConfigureAwait(true);
            using MemoryStream wire = new(BitConverter.GetBytes(length));

            // Assert
            _ = await Assert.ThrowsAsync<InvalidDataException>(async () => await pair.Server.ReadEncryptedAsync(wire).ConfigureAwait(true)).ConfigureAwait(true);
        }

        /// <summary>
        /// Verifies that a stream ending part way through the length prefix is refused.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task ReadEncrypted_RefusesATruncatedLengthPrefix()
        {
            // Arrange
            using EncryptionPair pair = await EncryptionPair.CreateAsync().ConfigureAwait(true);
            using MemoryStream wire = new([0x01, 0x00]);

            // Assert
            _ = await Assert.ThrowsAsync<EndOfStreamException>(async () => await pair.Server.ReadEncryptedAsync(wire).ConfigureAwait(true)).ConfigureAwait(true);
        }

        /// <summary>
        /// Verifies that a stream ending part way through the message it promised is refused rather than
        /// leaving a half-read buffer to be decrypted.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task ReadEncrypted_RefusesATruncatedMessage()
        {
            // Arrange: a prefix promising sixty-four bytes, followed by three
            using EncryptionPair pair = await EncryptionPair.CreateAsync().ConfigureAwait(true);
            using MemoryStream wire = new([.. BitConverter.GetBytes(64), 1, 2, 3]);

            // Assert
            _ = await Assert.ThrowsAsync<EndOfStreamException>(async () => await pair.Server.ReadEncryptedAsync(wire).ConfigureAwait(true)).ConfigureAwait(true);
        }

        /// <summary>
        /// Verifies that a message arriving a few bytes at a time is put back together, which is what a
        /// pipe carrying more than its buffer holds actually does.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task ReadEncrypted_ReassemblesAMessageArrivingInPieces()
        {
            // Arrange
            using EncryptionPair pair = await EncryptionPair.CreateAsync().ConfigureAwait(true);
            byte[] plaintext = DefaultEncoding.Value.GetBytes(new string('x', 5000));
            using TrickleStream wire = new(await EncryptToBytesAsync(pair, plaintext).ConfigureAwait(true), bytesPerRead: 7);

            // Assert
            Assert.Equal(plaintext, await pair.Client.ReadEncryptedAsync(wire).ConfigureAwait(true));
        }

        /// <summary>
        /// Verifies that a message of no bytes is written as a frame that cannot then be read back.
        /// </summary>
        /// <remarks>
        /// This is an asymmetry in the type rather than a property worth having. Encrypting nothing yields
        /// a 28-byte frame - a 12-byte nonce and a 16-byte tag, with no ciphertext between them - and the
        /// read side insists on at least one byte of ciphertext, so it refuses what the write side was
        /// willing to produce. Nothing reaches it today: every frame a server sends carries at least a
        /// command byte, and every frame a client sends carries at least a marker. Asserted so that the
        /// behaviour is recorded rather than discovered, and so that closing the gap shows up here as a
        /// test to update rather than passing unnoticed.
        /// </remarks>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task WriteEncrypted_ProducesAnUnreadableFrameForAnEmptyMessage()
        {
            // Arrange
            using EncryptionPair pair = await EncryptionPair.CreateAsync().ConfigureAwait(true);
            using MemoryStream wire = new();

            // Act
            await pair.Server.WriteEncryptedAsync(wire, []).ConfigureAwait(true);
            wire.Position = 0;

            // Assert
            Assert.Equal(4 + 12 + 16, wire.Length);
            _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await pair.Client.ReadEncryptedAsync(wire).ConfigureAwait(true)).ConfigureAwait(true);
        }

        /// <summary>
        /// Verifies that encrypting before a key has been agreed is refused rather than done with nothing.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task Encryption_RefusesToWorkBeforeTheKeyExchange()
        {
            // Arrange
            using ServerPipeEncryption encryption = new();
            using MemoryStream wire = new();

            // Assert
            _ = await Assert.ThrowsAsync<InvalidOperationException>(async () => await encryption.WriteEncryptedAsync(wire, [1, 2, 3]).ConfigureAwait(true)).ConfigureAwait(true);
        }

        /// <summary>
        /// Verifies that everything is refused once the instance has been disposed, since what disposal
        /// does is erase the key.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task Encryption_RefusesToWorkAfterDisposal()
        {
            // Arrange
            EncryptionPair pair = await EncryptionPair.CreateAsync().ConfigureAwait(true);
            using MemoryStream wire = new();
            byte[] frame = await EncryptToBytesAsync(pair, [1, 2, 3]).ConfigureAwait(true);
            pair.Dispose();
            using MemoryStream readable = new(frame);

            // Assert
            _ = await Assert.ThrowsAsync<ObjectDisposedException>(async () => await pair.Server.WriteEncryptedAsync(wire, [1, 2, 3]).ConfigureAwait(true)).ConfigureAwait(true);
            _ = await Assert.ThrowsAsync<ObjectDisposedException>(async () => await pair.Client.ReadEncryptedAsync(readable).ConfigureAwait(true)).ConfigureAwait(true);
            _ = await Assert.ThrowsAsync<ObjectDisposedException>(async () => await pair.Server.PerformKeyExchangeAsync(wire, readable).ConfigureAwait(true)).ConfigureAwait(true);
        }

        /// <summary>
        /// Verifies that disposing twice is harmless, since the instance is held in a using block by the
        /// server and disposed again by hand on some paths.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task Dispose_CanBeCalledTwice()
        {
            // Arrange
            EncryptionPair pair = await EncryptionPair.CreateAsync().ConfigureAwait(true);

            // Act
            pair.Dispose();

            // Assert
            Assert.Null(Record.Exception(pair.Dispose));
        }

        /// <summary>
        /// Verifies that nothing at all is refused, on both the reading and the writing side.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "This is deliberate as part of unit testing.")]
        [Fact]
        public async Task Encryption_RefusesNothingAtAll()
        {
            // Arrange
            using EncryptionPair pair = await EncryptionPair.CreateAsync().ConfigureAwait(true);
            using MemoryStream wire = new();

            // Assert
            _ = await Assert.ThrowsAsync<ArgumentNullException>(async () => await pair.Server.ReadEncryptedAsync(null!).ConfigureAwait(true)).ConfigureAwait(true);
            _ = await Assert.ThrowsAsync<ArgumentNullException>(async () => await pair.Server.WriteEncryptedAsync(null!, [1]).ConfigureAwait(true)).ConfigureAwait(true);
            _ = await Assert.ThrowsAsync<ArgumentNullException>(async () => await pair.Server.WriteEncryptedAsync(wire, null!).ConfigureAwait(true)).ConfigureAwait(true);
        }

        /// <summary>
        /// Encrypts a message and hands back the bytes that would have gone on the wire.
        /// </summary>
        /// <param name="pair">The pair whose server half does the encrypting.</param>
        /// <param name="plaintext">The message to encrypt.</param>
        /// <returns>The framed, encrypted message.</returns>
        private static async Task<byte[]> EncryptToBytesAsync(EncryptionPair pair, byte[] plaintext)
        {
            using MemoryStream wire = new();
            await pair.Server.WriteEncryptedAsync(wire, plaintext).ConfigureAwait(true);
            return wire.ToArray();
        }

        /// <summary>
        /// A stream that hands back only a few bytes at a time, however many were asked for.
        /// </summary>
        /// <remarks>
        /// A memory stream always satisfies a read in full, so reading from one would never take the loop
        /// that reassembles a message across several reads. A pipe carrying more than its buffer holds does
        /// take it, but not reliably enough to write a test against.
        /// </remarks>
        /// <param name="content">The bytes to hand out.</param>
        /// <param name="bytesPerRead">The most to hand back from a single read.</param>
        private sealed class TrickleStream(byte[] content, int bytesPerRead) : Stream
        {
            /// <inheritdoc/>
            public override int Read(byte[] buffer, int offset, int count)
            {
                return Take(buffer, offset, count);
            }

            /// <inheritdoc/>
            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                return Task.FromResult(Take(buffer, offset, count));
            }

            /// <summary>
            /// Hands back the next few bytes.
            /// </summary>
            /// <remarks>Held apart from both read methods rather than shared by one calling the other, so
            /// that neither is an asynchronous method calling a blocking one.</remarks>
            /// <param name="buffer">The buffer to copy into.</param>
            /// <param name="offset">Where in the buffer to start.</param>
            /// <param name="count">The most the caller will take.</param>
            /// <returns>How many bytes were copied.</returns>
            private int Take(byte[] buffer, int offset, int count)
            {
                int available = Math.Min(Math.Min(count, bytesPerRead), content.Length - _position);
                Buffer.BlockCopy(content, _position, buffer, offset, available);
                _position += available;
                return available;
            }

            /// <inheritdoc/>
            public override void Flush()
            {
            }

            /// <inheritdoc/>
            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            /// <inheritdoc/>
            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            /// <inheritdoc/>
            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }

            /// <inheritdoc/>
            public override bool CanRead => true;

            /// <inheritdoc/>
            public override bool CanSeek => false;

            /// <inheritdoc/>
            public override bool CanWrite => false;

            /// <inheritdoc/>
            public override long Length => content.Length;

            /// <inheritdoc/>
            public override long Position
            {
                get => _position;
                set => throw new NotSupportedException();
            }

            /// <summary>
            /// How far through the content the stream has been read.
            /// </summary>
            private int _position;
        }
    }
}
