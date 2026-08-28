using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using PSADT.ClientServer.Server.Tests.TestHelpers;
using Xunit;

namespace PSADT.ClientServer.Server.Tests
{
    /// <summary>
    /// Tests the client's half of the key exchange.
    /// </summary>
    /// <remarks>
    /// The mirror of the server's half, and the tests are the mirror too: this one has to read before it
    /// writes, and the key it then sends has to be in the same layout the server's is, because each is
    /// read by the other. The type lives here rather than with the client executable because both halves
    /// are one protocol and are only correct with respect to each other.
    /// <para>
    /// The refusal that ends the exchange is driven by a peer written independently of the code under test,
    /// for the reason given alongside the server's own tests: reaching it needs a server holding the right
    /// key and answering with the wrong thing.
    /// </para>
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "RCS1046:Add suffix 'Async' to asynchronous method name", Justification = "Test names describe the scenario under test; the async suffix would obscure them.")]
    public sealed class ClientPipeEncryptionTests
    {
        /// <summary>
        /// Verifies that the client reads the server's public key before sending its own.
        /// </summary>
        /// <remarks>
        /// The other half of the ordering the server's tests assert. Run against a stream with nothing in
        /// it: the client fails at the end of it having written nothing, which it could only do by reading
        /// first.
        /// </remarks>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task PerformKeyExchange_ReadsAPublicKeyBeforeSendingOne()
        {
            // Arrange
            using ClientPipeEncryption client = new();
            using MemoryStream output = new();
            using MemoryStream input = new();

            // Assert
            _ = await Assert.ThrowsAsync<EndOfStreamException>(async () => await client.PerformKeyExchangeAsync(output, input).ConfigureAwait(true)).ConfigureAwait(true);
            Assert.Empty(output.ToArray());
        }

        /// <summary>
        /// Verifies that the client answers with a public key in the same layout the server sends, which is
        /// what lets either end run on either framework.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task PerformKeyExchange_SendsAPublicKeyInTheAgreedLayout()
        {
            // Arrange
            byte[] frame = await KeyExchangeFrames.ClientPublicKeyAsync().ConfigureAwait(true);

            // Assert
            Assert.Equal(4 + 8 + 32 + 32, frame.Length);
            Assert.Equal(8 + 32 + 32, BitConverter.ToInt32(frame, 0));
            Assert.Equal(EcdhPublicP256Magic, BitConverter.ToInt32(frame, 4));
            Assert.Equal(32, BitConverter.ToInt32(frame, 8));
        }

        /// <summary>
        /// Verifies that the two halves put their public keys out in exactly the same shape, since each
        /// reads the other's with code that assumes it.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task PerformKeyExchange_SendsAPublicKeyShapedLikeTheServersOwn()
        {
            // Arrange
            byte[] client = await KeyExchangeFrames.ClientPublicKeyAsync().ConfigureAwait(true);
            byte[] server = await KeyExchangeFrames.ServerPublicKeyAsync().ConfigureAwait(true);

            // Assert: same length and same header, different keys. Taken with LINQ rather than by slicing,
            // since a range over an array needs a runtime helper .NET Framework does not have.
            Assert.Equal(server.Length, client.Length);
            Assert.Equal(server.Take(HeaderLength), client.Take(HeaderLength));
            Assert.NotEqual(server.Skip(HeaderLength), client.Skip(HeaderLength));
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
            using MemoryStream input = new(await KeyExchangeFrames.ServerPublicKeyAsync().ConfigureAwait(true));

            // Assert
            _ = await Assert.ThrowsAsync<InvalidOperationException>(async () => await pair.Client.PerformKeyExchangeAsync(output, input).ConfigureAwait(true)).ConfigureAwait(true);
        }

        /// <summary>
        /// Verifies that the client completes an exchange with a server written from the documented
        /// protocol rather than from the code that implements it.
        /// </summary>
        /// <remarks>
        /// The mirror of the server's own test against an independent peer, and it carries the same weight:
        /// it shows the client implements the protocol as described rather than merely agreeing with the
        /// half it ships beside.
        /// </remarks>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task PerformKeyExchange_CompletesWithAnIndependentlyWrittenServer()
        {
            // Arrange
            using PipePair pipes = new();
            using ClientPipeEncryption client = new();
            using ProtocolReplica server = new();

            // Assert
            Assert.Null(await Record.ExceptionAsync(async () => await PipePair.RunBothAsync(
                async () => await server.RunAsServerAsync(pipes.ServerOutput, pipes.ServerInput, ProtocolReplica.ServerBehaviour.Faithful).ConfigureAwait(false),
                async () => await client.PerformKeyExchangeAsync(pipes.ClientOutput, pipes.ClientInput).ConfigureAwait(false)).ConfigureAwait(true)).ConfigureAwait(true));
        }

        /// <summary>
        /// Verifies that a correctly encrypted proof which is not the challenge the client sent is refused.
        /// </summary>
        /// <remarks>
        /// This is the assertion the client's own challenge exists for, and the only thing standing between
        /// the client and a peer that holds the key without having been asked to prove it. Holding the key
        /// is not by itself the thing being checked - a peer that recorded an earlier exchange would hold
        /// one - so what has to come back is the challenge composed for this exchange and no other.
        /// </remarks>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task PerformKeyExchange_RefusesAProofThatIsNotItsOwnChallenge()
        {
            // Arrange
            using PipePair pipes = new();
            using ClientPipeEncryption client = new();
            using ProtocolReplica server = new();

            // Act
            CryptographicException failure = await Assert.ThrowsAsync<CryptographicException>(async () => await PipePair.RunBothAsync(
                async () => await server.RunAsServerAsync(pipes.ServerOutput, pipes.ServerInput, ProtocolReplica.ServerBehaviour.ProofThatDoesNotMatch).ConfigureAwait(false),
                async () => await client.PerformKeyExchangeAsync(pipes.ClientOutput, pipes.ClientInput).ConfigureAwait(false)).ConfigureAwait(true)).ConfigureAwait(true);

            // Assert
            Assert.Contains("server proof mismatch", failure.Message, StringComparison.Ordinal);
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
            using ClientPipeEncryption client = new();
            using MemoryStream stream = new();

            // Assert
            _ = await Assert.ThrowsAsync<ArgumentNullException>(async () => await client.PerformKeyExchangeAsync(null!, stream).ConfigureAwait(true)).ConfigureAwait(true);
            _ = await Assert.ThrowsAsync<ArgumentNullException>(async () => await client.PerformKeyExchangeAsync(stream, null!).ConfigureAwait(true)).ConfigureAwait(true);
        }

        /// <summary>
        /// The magic number CNG puts at the front of a P-256 elliptic curve public key blob, being the
        /// characters <c>ECK1</c> read as a little-endian integer.
        /// </summary>
        private const int EcdhPublicP256Magic = 0x314B4345;

        /// <summary>
        /// How much of a frame is length prefix and blob header, and so is the same whoever sent it.
        /// </summary>
        private const int HeaderLength = 12;
    }
}
