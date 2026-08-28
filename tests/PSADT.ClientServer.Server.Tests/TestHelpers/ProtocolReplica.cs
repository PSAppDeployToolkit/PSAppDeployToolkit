using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace PSADT.ClientServer.Server.Tests.TestHelpers
{
    /// <summary>
    /// A second implementation of the key exchange, written from the protocol as it is documented rather
    /// than from the code that implements it, and able to answer wrongly on purpose.
    /// </summary>
    /// <remarks>
    /// It exists for the refusals that nothing else can reach. Three of them - a response of the wrong
    /// length, a response carrying the wrong challenge, and a proof that does not match the challenge it
    /// answers - are only reachable by a peer that has derived the <em>correct</em> key and then says the
    /// wrong thing with it. Corrupting bytes in flight does not get there, because a message altered on the
    /// wire fails its authentication tag first, and the real counterpart cannot be made to misbehave
    /// because the whole of its half runs inside a single call.
    /// <para>
    /// The independence is in the protocol, not in the platform. Framing, the challenge exchange, the key
    /// derivation and the authenticated encryption are all written out here from their descriptions - the
    /// key derivation in particular is written as RFC 5869 states it rather than as the loop that
    /// implements it - so a peer built from the documentation talking to the real thing is a test of
    /// whether that documentation is true. What is not rewritten is the elliptic curve agreement itself,
    /// which both ends have to perform identically for any of this to mean anything, and where .NET
    /// Framework offers no way to import a raw CNG blob other than the one the code under test uses.
    /// </para>
    /// <para>
    /// The public key it sends is assembled by hand on both frameworks, where the code under test assembles
    /// it by hand only on .NET and exports it from CNG on .NET Framework. That is deliberate: it makes an
    /// exchange between this and the real thing on .NET Framework a check that the two routes to that blob
    /// agree, which is the compatibility that lets a server on one framework talk to a client on the other.
    /// </para>
    /// </remarks>
    internal sealed class ProtocolReplica : IDisposable
    {
        /// <summary>
        /// Plays the client's part of the exchange.
        /// </summary>
        /// <remarks>Returns as soon as a misbehaving response has been sent, since the peer will refuse it
        /// and never send the proof a well-behaved client would go on to wait for.</remarks>
        /// <param name="output">The stream to write to.</param>
        /// <param name="input">The stream to read from.</param>
        /// <param name="behaviour">What sort of client to be.</param>
        /// <returns>A task that completes when this side of the exchange is done.</returns>
        internal async Task RunAsClientAsync(Stream output, Stream input, ClientBehaviour behaviour)
        {
            // The server speaks first, so read its key, answer with ours, and derive.
            DeriveKey(await ReadFrameAsync(input).ConfigureAwait(false));
            await WriteFrameAsync(output, ExportPublicKey()).ConfigureAwait(false);

            // Read the server's challenge and answer it with both challenges encrypted together.
            byte[] serverChallenge = await ReadFrameAsync(input).ConfigureAwait(false);
            byte[] clientChallenge = RandomBytes(ChallengeSize);
            await WriteFrameAsync(output, Encrypt(BuildResponse(behaviour, serverChallenge, clientChallenge))).ConfigureAwait(false);
            if (behaviour is not ClientBehaviour.Faithful)
            {
                return;
            }

            // Verify the server proved it holds the same key by returning our own challenge.
            byte[] proof = Decrypt(await ReadFrameAsync(input).ConfigureAwait(false));
            Assert.Equal(clientChallenge, proof);
        }

        /// <summary>
        /// Plays the server's part of the exchange.
        /// </summary>
        /// <param name="output">The stream to write to.</param>
        /// <param name="input">The stream to read from.</param>
        /// <param name="behaviour">What sort of server to be.</param>
        /// <returns>A task that completes when this side of the exchange is done.</returns>
        internal async Task RunAsServerAsync(Stream output, Stream input, ServerBehaviour behaviour)
        {
            // We speak first, so send our key, read the client's, and derive.
            await WriteFrameAsync(output, ExportPublicKey()).ConfigureAwait(false);
            DeriveKey(await ReadFrameAsync(input).ConfigureAwait(false));

            // Send a challenge in the clear and read back both challenges encrypted together.
            byte[] serverChallenge = RandomBytes(ChallengeSize);
            await WriteFrameAsync(output, serverChallenge).ConfigureAwait(false);
            byte[] response = Decrypt(await ReadFrameAsync(input).ConfigureAwait(false));
            Assert.Equal(ChallengeSize * 2, response.Length);
            Assert.Equal(serverChallenge, Slice(response, 0, ChallengeSize));

            // Prove we hold the same key by returning the client's own challenge, or fail to.
            byte[] clientChallenge = Slice(response, ChallengeSize, ChallengeSize);
            await WriteFrameAsync(output, Encrypt(behaviour is ServerBehaviour.ProofThatDoesNotMatch ? Corrupt(clientChallenge) : clientChallenge)).ConfigureAwait(false);
        }

        /// <summary>
        /// Releases the key agreement.
        /// </summary>
        public void Dispose()
        {
            _ecdh.Dispose();
        }

        /// <summary>
        /// Builds the answer to the server's challenge, well-formed or otherwise.
        /// </summary>
        /// <param name="behaviour">What sort of client to be.</param>
        /// <param name="serverChallenge">The challenge the server sent.</param>
        /// <param name="clientChallenge">The challenge to send back for the server to prove itself against.</param>
        /// <returns>The plaintext to encrypt and send.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="behaviour"/> is not one of the declared behaviours.</exception>
        private static byte[] BuildResponse(ClientBehaviour behaviour, byte[] serverChallenge, byte[] clientChallenge)
        {
            return behaviour switch
            {
                // Both challenges, one after the other, as the protocol asks for.
                ClientBehaviour.Faithful => Concat(serverChallenge, clientChallenge),

                // Only half of what is expected, which the peer should measure before reading any of it.
                ClientBehaviour.ResponseOfTheWrongLength => serverChallenge,

                // The right length, but not carrying back the challenge that was sent.
                ClientBehaviour.ChallengeThatDoesNotMatch => Concat(Corrupt(serverChallenge), clientChallenge),
                _ => throw new ArgumentOutOfRangeException(nameof(behaviour), behaviour, "Unknown client behaviour."),
            };
        }

        /// <summary>
        /// Assembles the public key in the CNG blob layout both ends read.
        /// </summary>
        /// <remarks>A four-byte magic number naming the curve, a four-byte coordinate size, then the two
        /// coordinates one after the other.</remarks>
        /// <returns>The blob to send.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the exported key is missing a coordinate.</exception>
        private byte[] ExportPublicKey()
        {
            ECParameters parameters = _ecdh.ExportParameters(includePrivateParameters: false);
            byte[] x = parameters.Q.X ?? throw new InvalidOperationException("The exported key has no X coordinate.");
            byte[] y = parameters.Q.Y ?? throw new InvalidOperationException("The exported key has no Y coordinate.");
            byte[] blob = new byte[8 + x.Length + y.Length];
            Buffer.BlockCopy(BitConverter.GetBytes(EcdhPublicP256Magic), 0, blob, 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(x.Length), 0, blob, 4, 4);
            Buffer.BlockCopy(x, 0, blob, 8, x.Length);
            Buffer.BlockCopy(y, 0, blob, 8 + x.Length, y.Length);
            return blob;
        }

        /// <summary>
        /// Agrees a secret with the peer and expands it into the encryption key.
        /// </summary>
        /// <param name="remotePublicKey">The peer's public key blob.</param>
        private void DeriveKey(byte[] remotePublicKey)
        {
            int keySize = BitConverter.ToInt32(remotePublicKey, 4);
#if NET8_0_OR_GREATER
            ECParameters remoteParameters = new()
            {
                Curve = ECCurve.NamedCurves.nistP256,
                Q = new ECPoint
                {
                    X = Slice(remotePublicKey, 8, keySize),
                    Y = Slice(remotePublicKey, 8 + keySize, keySize),
                },
            };
            using ECDiffieHellman remote = ECDiffieHellman.Create(remoteParameters);
            byte[] sharedSecret = _ecdh.DeriveKeyMaterial(remote.PublicKey);
#else
            using ECDiffieHellmanPublicKey remote = ECDiffieHellmanCngPublicKey.FromByteArray(remotePublicKey, CngKeyBlobFormat.EccPublicBlob);
            byte[] sharedSecret = _ecdh.DeriveKeyMaterial(remote);
#endif
            _key = ExpandKey(sharedSecret);
        }

        /// <summary>
        /// Expands the agreed secret into an AES key, as RFC 5869 describes.
        /// </summary>
        /// <remarks>
        /// Extract, then expand. The salt is thirty-two zero bytes, which the specification permits and the
        /// protocol uses; the info string ties the key to this protocol and version. One block of expansion
        /// is enough, since HMAC-SHA256 produces exactly the thirty-two bytes an AES-256 key needs.
        /// </remarks>
        /// <param name="sharedSecret">The secret agreed with the peer.</param>
        /// <returns>The encryption key.</returns>
        private static byte[] ExpandKey(byte[] sharedSecret)
        {
            byte[] info = Encoding.UTF8.GetBytes(KeyDerivationInfo);
            byte[] pseudoRandomKey;
            using (HMACSHA256 extract = new(new byte[32]))
            {
                pseudoRandomKey = extract.ComputeHash(sharedSecret);
            }
            using HMACSHA256 expand = new(pseudoRandomKey);
            byte[] block = new byte[info.Length + 1];
            Buffer.BlockCopy(info, 0, block, 0, info.Length);
            block[info.Length] = 1;
            return expand.ComputeHash(block);
        }

        /// <summary>
        /// Encrypts a message into a nonce, a ciphertext and an authentication tag, one after the other.
        /// </summary>
        /// <param name="plaintext">The message to encrypt.</param>
        /// <returns>The encrypted message.</returns>
        /// <exception cref="InvalidOperationException">Thrown if no key has been agreed yet.</exception>
        private byte[] Encrypt(byte[] plaintext)
        {
            byte[] nonce = RandomBytes(NonceSize);
            byte[] ciphertext = new byte[plaintext.Length];
            byte[] tag = new byte[TagSize];
            using (AesGcm aes = new(_key ?? throw new InvalidOperationException("No key has been derived."), TagSize))
            {
                aes.Encrypt(nonce, plaintext, ciphertext, tag);
            }
            byte[] message = new byte[NonceSize + ciphertext.Length + TagSize];
            Buffer.BlockCopy(nonce, 0, message, 0, NonceSize);
            Buffer.BlockCopy(ciphertext, 0, message, NonceSize, ciphertext.Length);
            Buffer.BlockCopy(tag, 0, message, NonceSize + ciphertext.Length, TagSize);
            return message;
        }

        /// <summary>
        /// Decrypts a message laid out as a nonce, a ciphertext and an authentication tag.
        /// </summary>
        /// <param name="message">The encrypted message.</param>
        /// <returns>The message.</returns>
        /// <exception cref="InvalidOperationException">Thrown if no key has been agreed yet.</exception>
        private byte[] Decrypt(byte[] message)
        {
            int ciphertextLength = message.Length - NonceSize - TagSize;
            byte[] plaintext = new byte[ciphertextLength];
            using (AesGcm aes = new(_key ?? throw new InvalidOperationException("No key has been derived."), TagSize))
            {
                aes.Decrypt(Slice(message, 0, NonceSize), Slice(message, NonceSize, ciphertextLength), Slice(message, NonceSize + ciphertextLength, TagSize), plaintext);
            }
            return plaintext;
        }

        /// <summary>
        /// Reads one length-prefixed message.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <returns>The message.</returns>
        private static async Task<byte[]> ReadFrameAsync(Stream stream)
        {
            return await ReadExactlyAsync(stream, BitConverter.ToInt32(await ReadExactlyAsync(stream, 4).ConfigureAwait(false), 0)).ConfigureAwait(false);
        }

        /// <summary>
        /// Writes one length-prefixed message.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        /// <param name="data">The message.</param>
        /// <returns>A task that completes when the message has been written and flushed.</returns>
        private static async Task WriteFrameAsync(Stream stream, byte[] data)
        {
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            await stream.WriteAsync(BitConverter.GetBytes(data.Length), 0, 4, cancellationToken).ConfigureAwait(false);
            await stream.WriteAsync(data, 0, data.Length, cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Reads exactly the number of bytes asked for, however many reads that takes.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="count">How many bytes to read.</param>
        /// <returns>The bytes read.</returns>
        /// <exception cref="EndOfStreamException">Thrown if the stream ends before that many bytes arrive.</exception>
        private static async Task<byte[]> ReadExactlyAsync(Stream stream, int count)
        {
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            byte[] buffer = new byte[count];
            int read = 0;
            while (read < count)
            {
                int received = await stream.ReadAsync(buffer, read, count - read, cancellationToken).ConfigureAwait(false);
                if (received is 0)
                {
                    throw new EndOfStreamException("The peer stopped talking part way through a message.");
                }
                read += received;
            }
            return buffer;
        }

        /// <summary>
        /// Takes a run of bytes out of an array.
        /// </summary>
        /// <remarks>Written out rather than sliced with a range, which needs a runtime helper that .NET
        /// Framework does not have.</remarks>
        /// <param name="source">The array to take from.</param>
        /// <param name="offset">Where to start.</param>
        /// <param name="count">How many to take.</param>
        /// <returns>The bytes taken.</returns>
        private static byte[] Slice(byte[] source, int offset, int count)
        {
            byte[] result = new byte[count];
            Buffer.BlockCopy(source, offset, result, 0, count);
            return result;
        }

        /// <summary>
        /// Joins two runs of bytes.
        /// </summary>
        /// <param name="first">The bytes to put first.</param>
        /// <param name="second">The bytes to put after them.</param>
        /// <returns>The two, one after the other.</returns>
        private static byte[] Concat(byte[] first, byte[] second)
        {
            byte[] result = new byte[first.Length + second.Length];
            Buffer.BlockCopy(first, 0, result, 0, first.Length);
            Buffer.BlockCopy(second, 0, result, first.Length, second.Length);
            return result;
        }

        /// <summary>
        /// Returns a copy of a challenge with one byte changed, which is enough to make it the wrong one.
        /// </summary>
        /// <param name="challenge">The challenge to alter.</param>
        /// <returns>The altered copy.</returns>
        private static byte[] Corrupt(byte[] challenge)
        {
            byte[] altered = Slice(challenge, 0, challenge.Length);
            altered[0] ^= 0xFF;
            return altered;
        }

        /// <summary>
        /// Produces cryptographically random bytes.
        /// </summary>
        /// <param name="count">How many to produce.</param>
        /// <returns>The bytes.</returns>
        private static byte[] RandomBytes(int count)
        {
            byte[] bytes = new byte[count];
            using RandomNumberGenerator rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return bytes;
        }

        /// <summary>
        /// The kinds of client this can be.
        /// </summary>
        internal enum ClientBehaviour
        {
            /// <summary>
            /// Answers the challenge the way the protocol asks.
            /// </summary>
            Faithful = 0,

            /// <summary>
            /// Answers with half of what is expected, correctly encrypted.
            /// </summary>
            ResponseOfTheWrongLength = 1,

            /// <summary>
            /// Answers with the right amount of data, correctly encrypted, but not carrying back the
            /// challenge that was sent.
            /// </summary>
            ChallengeThatDoesNotMatch = 2,
        }

        /// <summary>
        /// The kinds of server this can be.
        /// </summary>
        internal enum ServerBehaviour
        {
            /// <summary>
            /// Proves itself the way the protocol asks.
            /// </summary>
            Faithful = 0,

            /// <summary>
            /// Returns something correctly encrypted that is not the challenge it was asked to return.
            /// </summary>
            ProofThatDoesNotMatch = 1,
        }

        /// <summary>
        /// This party's key agreement.
        /// </summary>
        private readonly ECDiffieHellman _ecdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);

        /// <summary>
        /// The encryption key agreed with the peer, once there is one.
        /// </summary>
        private byte[]? _key;

        /// <summary>
        /// The magic number CNG puts at the front of a P-256 elliptic curve public key blob, being the
        /// characters <c>ECK1</c> read as a little-endian integer.
        /// </summary>
        private const int EcdhPublicP256Magic = 0x314B4345;

        /// <summary>
        /// The string tying a derived key to this protocol and version.
        /// </summary>
        private const string KeyDerivationInfo = "PSADT-Pipe-Encryption-v2-GCM";

        /// <summary>
        /// The size, in bytes, of a challenge.
        /// </summary>
        private const int ChallengeSize = 32;

        /// <summary>
        /// The size, in bytes, of an AES-GCM nonce.
        /// </summary>
        private const int NonceSize = 12;

        /// <summary>
        /// The size, in bytes, of an AES-GCM authentication tag.
        /// </summary>
        private const int TagSize = 16;
    }
}
