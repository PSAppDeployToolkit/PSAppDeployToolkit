using System;
using System.IO;
using System.Security.Cryptography;

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
        internal override void PerformKeyExchange(Stream outputStream, Stream inputStream)
        {
            // Verify state and parameters.
            ThrowIfDisposed();
            if (outputStream is null)
            {
                throw new ArgumentNullException(nameof(outputStream));
            }
            if (inputStream is null)
            {
                throw new ArgumentNullException(nameof(inputStream));
            }

            // Client receives server's public key first.
            byte[] serverPublicKey = ReadLengthPrefixedBytes(inputStream);

            // Client sends its public key.
            byte[] publicKey = GetPublicKey();
            WriteLengthPrefixedBytes(outputStream, publicKey);

            // Derive the shared key.
            DeriveSharedKey(serverPublicKey);

            // Mutual authentication.
            PerformMutualAuthentication(outputStream, inputStream);
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
        private void PerformMutualAuthentication(Stream outputStream, Stream inputStream)
        {
            // Receive server's challenge
            byte[] serverChallenge = ReadLengthPrefixedBytes(inputStream);

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
            WriteLengthPrefixedBytes(outputStream, encryptedResponse);

            // Receive server's proof
            byte[] encryptedServerProof = ReadLengthPrefixedBytes(inputStream);
            byte[] decryptedServerProof = Decrypt(encryptedServerProof);

            // Verify server returned our challenge correctly
            if (!ConstantTimeEquals(clientChallenge, decryptedServerProof))
            {
                throw new CryptographicException("Key exchange verification failed: server proof mismatch. Server may not have derived the correct key.");
            }
        }
    }
}
