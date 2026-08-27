using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace PSADT.ClientServer
{
    /// <summary>
    /// Client-side pipe encryption that responds to the server's key exchange.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The client receives the server's public key first, then sends its own public key.
    /// After key derivation, mutual authentication is performed where both parties
    /// prove they have derived the same shared secret.
    /// </para>
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0182: Avoid unused internal types.", Justification = "This is used across InternalsVisibleTo boundaries.")]
    internal sealed class ClientPipeEncryption : PipeEncryption<ClientPipeEncryption>
    {
        /// <inheritdoc />
        /// <remarks>
        /// <para>The client key exchange protocol:</para>
        /// <list type="number">
        /// <item><description>Client receives server's ECDH public key</description></item>
        /// <item><description>Client sends its ECDH public key</description></item>
        /// <item><description>Both derive shared secret via ECDH</description></item>
        /// <item><description>Client receives server's challenge</description></item>
        /// <item><description>Client generates its own challenge and returns encrypted {server_challenge || client_challenge}</description></item>
        /// <item><description>Client receives and verifies encrypted client_challenge from server</description></item>
        /// </list>
        /// </remarks>
        internal override ValueTask PerformKeyExchangeAsync(Stream outputStream, Stream inputStream)
        {
            // Internal implementation
            async ValueTask PerformKeyExchangeImplAsync()
            {
                // Client receives server's public key first.
                byte[] serverPublicKey = await ReadLengthPrefixedBytesAsync(inputStream).ConfigureAwait(false);

                // Client sends its public key.
                byte[] publicKey = GetPublicKey();
                await WriteLengthPrefixedBytesAsync(outputStream, publicKey).ConfigureAwait(false);

                // Derive the shared key.
                DeriveSharedKey(serverPublicKey);

                // Mutual authentication.
                await PerformMutualAuthenticationAsync(outputStream, inputStream).ConfigureAwait(false);
            }

            // Verify state and parameters.
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(outputStream);
            ArgumentNullException.ThrowIfNull(inputStream);
            return PerformKeyExchangeImplAsync();
        }

        /// <summary>
        /// Performs a mutual authentication handshake over the specified streams, verifying that both parties possess
        /// the correct cryptographic keys.
        /// </summary>
        /// <remarks>This method implements a challenge-response protocol to ensure both client and server
        /// can prove knowledge of a shared secret or derived key. Both streams must be connected to the remote party
        /// and support synchronous read and write operations. The method does not close or dispose the provided
        /// streams.</remarks>
        /// <param name="outputStream">The stream to which authentication data is written. Must be writable and remain open for the duration of the
        /// handshake.</param>
        /// <param name="inputStream">The stream from which authentication data is read. Must be readable and remain open for the duration of the
        /// handshake.</param>
        /// <exception cref="CryptographicException">Thrown if the server fails to prove possession of the correct cryptographic key, indicating that mutual
        /// authentication has failed.</exception>
        private async ValueTask PerformMutualAuthenticationAsync(Stream outputStream, Stream inputStream)
        {
            // Receive server's challenge
            byte[] serverChallenge = await ReadLengthPrefixedBytesAsync(inputStream).ConfigureAwait(false);

            // Generate client's own challenge
            byte[] clientChallenge = new byte[ChallengeSize];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(clientChallenge);
            }

            // Combine both challenges: {server_challenge || client_challenge}
            byte[] combinedChallenges = new byte[ChallengeSize * 2];
            Buffer.BlockCopy(serverChallenge, 0, combinedChallenges, 0, ChallengeSize);
            Buffer.BlockCopy(clientChallenge, 0, combinedChallenges, ChallengeSize, ChallengeSize);

            // Encrypt and send the combined challenges
            byte[] encryptedResponse = Encrypt(combinedChallenges);
            await WriteLengthPrefixedBytesAsync(outputStream, encryptedResponse).ConfigureAwait(false);

            // Receive server's proof
            byte[] encryptedServerProof = await ReadLengthPrefixedBytesAsync(inputStream).ConfigureAwait(false);
            byte[] decryptedServerProof = Decrypt(encryptedServerProof);

            // Verify server returned our challenge correctly
            if (!ConstantTimeEquals(clientChallenge, decryptedServerProof))
            {
                throw new CryptographicException("Key exchange verification failed: server proof mismatch. Server may not have derived the correct key.");
            }
        }
    }
}
