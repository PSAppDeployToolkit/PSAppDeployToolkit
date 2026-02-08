using System;
using System.IO;
using System.Security.Cryptography;

namespace PSADT.ClientServer
{
    /// <summary>
    /// Server-side pipe encryption that initiates the key exchange.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The server sends its public key first, then receives the client's public key.
    /// After key derivation, mutual authentication is performed where both parties
    /// prove they have derived the same shared secret.
    /// </para>
    /// </remarks>
    internal sealed class ServerPipeEncryption : PipeEncryption<ServerPipeEncryption>
    {
        /// <inheritdoc />
        /// <remarks>
        /// <para>The server key exchange protocol:</para>
        /// <list type="number">
        /// <item><description>Server sends its ECDH public key</description></item>
        /// <item><description>Server receives client's ECDH public key</description></item>
        /// <item><description>Both derive shared secret via ECDH</description></item>
        /// <item><description>Server sends random challenge</description></item>
        /// <item><description>Client returns encrypted {server_challenge || client_challenge}</description></item>
        /// <item><description>Server verifies its challenge and returns encrypted client_challenge</description></item>
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

            // Server sends public key first.
            byte[] publicKey = GetPublicKey();
            WriteLengthPrefixedBytes(outputStream, publicKey);

            // Server receives client's public key.
            byte[] clientPublicKey = ReadLengthPrefixedBytes(inputStream);

            // Derive the shared key.
            DeriveSharedKey(clientPublicKey);

            // Mutual authentication.
            PerformMutualAuthentication(outputStream, inputStream);
        }

        /// <summary>
        /// Performs a mutual authentication handshake over the specified streams using a challenge-response protocol.
        /// </summary>
        /// <remarks>This method implements a mutual authentication exchange by sending and verifying
        /// random challenges between peers. Both streams must be properly synchronized and connected to the
        /// corresponding endpoints for the handshake to succeed.</remarks>
        /// <param name="outputStream">The stream to which authentication data is written. Must be writable and remain open for the duration of the
        /// handshake.</param>
        /// <param name="inputStream">The stream from which authentication data is read. Must be readable and remain open for the duration of the
        /// handshake.</param>
        /// <exception cref="CryptographicException">Thrown if the authentication handshake fails due to invalid or mismatched challenge data.</exception>
        private void PerformMutualAuthentication(Stream outputStream, Stream inputStream)
        {
            // Generate server's random challenge.
            byte[] serverChallenge = new byte[ChallengeSize];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(serverChallenge);
            }

            // Send the server challenge (unencrypted - it's random data).
            WriteLengthPrefixedBytes(outputStream, serverChallenge);

            // Read the encrypted response containing both challenges.
            byte[] encryptedResponse = ReadLengthPrefixedBytes(inputStream);
            byte[] decryptedResponse = Decrypt(encryptedResponse);

            // Verify response length.
            if (decryptedResponse.Length != ChallengeSize * 2)
            {
                throw new CryptographicException("Key exchange verification failed: invalid response length.");
            }

            // Verify server's challenge.
            byte[] returnedServerChallenge = new byte[ChallengeSize];
            Buffer.BlockCopy(decryptedResponse, 0, returnedServerChallenge, 0, ChallengeSize);
            if (!ConstantTimeEquals(serverChallenge, returnedServerChallenge))
            {
                throw new CryptographicException("Key exchange verification failed: server challenge mismatch.");
            }

            // Return client's challenge encrypted to prove we have the key.
            byte[] clientChallenge = new byte[ChallengeSize];
            Buffer.BlockCopy(decryptedResponse, ChallengeSize, clientChallenge, 0, ChallengeSize);
            byte[] encryptedClientChallenge = Encrypt(clientChallenge);
            WriteLengthPrefixedBytes(outputStream, encryptedClientChallenge);
        }
    }
}
