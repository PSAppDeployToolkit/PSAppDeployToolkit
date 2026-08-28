using System;
using System.IO;
using System.Threading.Tasks;
using PSADT.ClientServer.Server.Tests.TestHelpers;
using Xunit;

namespace PSADT.ClientServer.Server.Tests
{
    /// <summary>
    /// Tests the server's half of the key exchange.
    /// </summary>
    /// <remarks>
    /// The server speaks first, and that ordering is the whole reason there are two halves rather than one
    /// - both sides reading first would deadlock, and both writing first would work by accident until a
    /// pipe buffer filled. So it is asserted directly rather than inferred from a successful exchange.
    /// <para>
    /// The shape of the public key it sends is asserted for a reason that a single-framework test would
    /// miss. The two target frameworks arrive at that blob by different routes - .NET Framework exports it
    /// from CNG, .NET assembles it by hand from the curve parameters - and a server on one has to be
    /// understood by a client on the other. Nothing in a single process can put those two together, so
    /// what each one produces is measured against the layout they both have to agree on.
    /// </para>
    /// <para>
    /// Two refusals are not covered here: the response of the wrong length, and, on the client's side, the
    /// server proof that does not match. Both need a peer that has derived the correct key and then answers
    /// with the wrong thing, which cannot be built without a second implementation of the key derivation
    /// living in the tests. Corrupting the bytes in flight does not reach them, because a message altered
    /// on the wire fails its authentication tag first - which is itself asserted, over in the tests for
    /// what both halves share.
    /// </para>
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "RCS1046:Add suffix 'Async' to asynchronous method name", Justification = "Test names describe the scenario under test; the async suffix would obscure them.")]
    public sealed class ServerPipeEncryptionTests
    {
        /// <summary>
        /// Verifies that a completed exchange leaves both halves able to read what the other writes.
        /// </summary>
        /// <remarks>
        /// Both directions are asserted. A key agreement that only worked one way would still pass the
        /// exchange itself, because the mutual authentication that ends it happens to encrypt in one
        /// direction and decrypt in the other, and would then fail on the first real command.
        /// </remarks>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task PerformKeyExchange_AgreesAKeyWithTheClient()
        {
            // Arrange
            using EncryptionPair pair = await EncryptionPair.CreateAsync().ConfigureAwait(true);
            byte[] fromServer = DefaultEncoding.Value.GetBytes("a command");
            byte[] fromClient = DefaultEncoding.Value.GetBytes("a response");
            using MemoryStream outbound = new();
            using MemoryStream inbound = new();

            // Act
            await pair.Server.WriteEncryptedAsync(outbound, fromServer).ConfigureAwait(true);
            await pair.Client.WriteEncryptedAsync(inbound, fromClient).ConfigureAwait(true);
            outbound.Position = 0;
            inbound.Position = 0;

            // Assert
            Assert.Equal(fromServer, await pair.Client.ReadEncryptedAsync(outbound).ConfigureAwait(true));
            Assert.Equal(fromClient, await pair.Server.ReadEncryptedAsync(inbound).ConfigureAwait(true));
        }

        /// <summary>
        /// Verifies that the server writes its public key before it reads anything.
        /// </summary>
        /// <remarks>
        /// Run against a stream with nothing in it, so the only way the frame can be there is if it was
        /// written before the read that then failed.
        /// </remarks>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task PerformKeyExchange_SendsItsPublicKeyBeforeReadingOne()
        {
            // Arrange
            using ServerPipeEncryption server = new();
            using MemoryStream output = new();
            using MemoryStream input = new();

            // Assert
            _ = await Assert.ThrowsAsync<EndOfStreamException>(async () => await server.PerformKeyExchangeAsync(output, input).ConfigureAwait(true)).ConfigureAwait(true);
            Assert.NotEmpty(output.ToArray());
        }

        /// <summary>
        /// Verifies that the public key goes out in the layout both frameworks have to agree on.
        /// </summary>
        /// <remarks>
        /// A CNG elliptic curve public key blob: a four-byte magic number naming the curve and whether the
        /// private part is included, a four-byte key size, and then the two coordinates one after the
        /// other. For P-256 each coordinate is 32 bytes, so the whole thing is 72 and the frame carrying it
        /// is four longer.
        /// </remarks>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task PerformKeyExchange_SendsAPublicKeyInTheAgreedLayout()
        {
            // Arrange
            byte[] frame = await KeyExchangeFrames.ServerPublicKeyAsync().ConfigureAwait(true);

            // Assert
            Assert.Equal(4 + 8 + 32 + 32, frame.Length);
            Assert.Equal(8 + 32 + 32, BitConverter.ToInt32(frame, 0));
            Assert.Equal(EcdhPublicP256Magic, BitConverter.ToInt32(frame, 4));
            Assert.Equal(32, BitConverter.ToInt32(frame, 8));
        }

        /// <summary>
        /// Verifies that a second exchange on the same instance is refused rather than quietly replacing
        /// the key everything already sent was encrypted under.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task PerformKeyExchange_RefusesToRunTwice()
        {
            // Arrange
            using EncryptionPair pair = await EncryptionPair.CreateAsync().ConfigureAwait(true);
            using MemoryStream output = new();
            using MemoryStream input = new(await KeyExchangeFrames.ClientPublicKeyAsync().ConfigureAwait(true));

            // Assert
            _ = await Assert.ThrowsAsync<InvalidOperationException>(async () => await pair.Server.PerformKeyExchangeAsync(output, input).ConfigureAwait(true)).ConfigureAwait(true);
        }

        /// <summary>
        /// Verifies that nothing at all is refused for either stream.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "This is deliberate as part of unit testing.")]
        [Fact]
        public async Task PerformKeyExchange_RefusesNothingAtAll()
        {
            // Arrange
            using ServerPipeEncryption server = new();
            using MemoryStream stream = new();

            // Assert
            _ = await Assert.ThrowsAsync<ArgumentNullException>(async () => await server.PerformKeyExchangeAsync(null!, stream).ConfigureAwait(true)).ConfigureAwait(true);
            _ = await Assert.ThrowsAsync<ArgumentNullException>(async () => await server.PerformKeyExchangeAsync(stream, null!).ConfigureAwait(true)).ConfigureAwait(true);
        }

        /// <summary>
        /// The magic number CNG puts at the front of a P-256 elliptic curve public key blob, being the
        /// characters <c>ECK1</c> read as a little-endian integer.
        /// </summary>
        private const int EcdhPublicP256Magic = 0x314B4345;
    }
}
