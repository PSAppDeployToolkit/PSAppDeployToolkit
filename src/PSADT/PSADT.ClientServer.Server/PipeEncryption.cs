using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace PSADT.ClientServer
{
    /// <summary>
    /// Provides secure, authenticated encryption and key exchange for inter-process communication using Elliptic Curve
    /// Diffie-Hellman (ECDH) and AES-256-GCM. This class enables two parties to establish a shared secret and exchange
    /// encrypted messages over a pipe or stream.
    /// </summary>
    /// <remarks>
    /// <para>
    /// PipeEncryption manages the full lifecycle of key exchange and message encryption for secure communication
    /// channels. It supports both server and client roles in the key exchange process, using ECDH to derive a shared
    /// secret and then expanding it into encryption keys using HKDF.
    /// </para>
    /// <para>
    /// Messages are encrypted using AES-256-GCM which provides authenticated encryption with associated data (AEAD),
    /// ensuring both confidentiality and integrity in a single cryptographic operation. This is more efficient and
    /// secure than separate encryption and MAC operations.
    /// </para>
    /// <para>
    /// Instances must complete the key exchange before encryption or decryption operations can be performed. This
    /// class is not thread-safe; callers should ensure appropriate synchronization if used concurrently. Dispose the
    /// instance when finished to securely erase sensitive key material.
    /// </para>
    /// </remarks>
    public sealed record PipeEncryption : IDisposable
    {
        /// <summary>
        /// Gets a value indicating whether the key exchange has been completed.
        /// </summary>
        public bool IsKeyExchangeComplete => _encryptionKey is not null;

        /// <summary>
        /// Gets the local public key for transmission to the remote party.
        /// </summary>
        /// <returns>A byte array containing the exported public key.</returns>
        public byte[] GetPublicKey()
        {
            ThrowIfDisposed();
#if NET8_0_OR_GREATER
            return _ecdh.PublicKey.ExportSubjectPublicKeyInfo();
#else
            return _ecdh.PublicKey.ToByteArray();
#endif
        }

        /// <summary>
        /// Completes the key exchange by deriving a shared secret from the remote party's public key.
        /// </summary>
        /// <param name="remotePublicKey">The remote party's public key bytes.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="remotePublicKey"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the key exchange has already been completed.</exception>
        public void DeriveSharedKey(byte[] remotePublicKey)
        {
            // Verify parameters and state.
            ThrowIfDisposed();
            if (remotePublicKey is null)
            {
                throw new ArgumentNullException(nameof(remotePublicKey));
            }
            if (_encryptionKey is not null)
            {
                throw new InvalidOperationException("Key exchange has already been completed.");
            }

            // Import the remote public key and derive shared secret
#if NET8_0_OR_GREATER
            using ECDiffieHellman remoteEcdh = ECDiffieHellman.Create();
            remoteEcdh.ImportSubjectPublicKeyInfo(remotePublicKey, out _);
            byte[] sharedSecret = _ecdh.DeriveKeyMaterial(remoteEcdh.PublicKey);
#else
            ECDiffieHellmanPublicKey remotePubKey = ECDiffieHellmanCngPublicKey.FromByteArray(remotePublicKey, CngKeyBlobFormat.EccPublicBlob);
            byte[] sharedSecret = _ecdh.DeriveKeyMaterial(remotePubKey);
#endif

            // Derive encryption key using HKDF (only need encryption key for GCM, no separate MAC key)
            _encryptionKey = DeriveKeyMaterial(sharedSecret, AesKeySize);

            // Clear sensitive data
            SecureZeroMemory(sharedSecret);
        }

        /// <summary>
        /// Encrypts raw bytes using AES-256-GCM authenticated encryption.
        /// </summary>
        /// <param name="plaintext">The plaintext bytes to encrypt.</param>
        /// <returns>A byte array containing the nonce, ciphertext, and authentication tag.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="plaintext"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the key exchange has not been completed.</exception>
        public byte[] Encrypt(byte[] plaintext)
        {
            // Verify state and parameters.
            ThrowIfDisposed(); ThrowIfKeyExchangeNotComplete();
            if (plaintext is null)
            {
                throw new ArgumentNullException(nameof(plaintext));
            }

            // Generate a unique nonce for this encryption operation.
            byte[] nonce = new byte[NonceSize];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(nonce);
            }

            // Allocate output buffer: Nonce (12) + Ciphertext (same as plaintext) + Tag (16)
            byte[] result = new byte[NonceSize + plaintext.Length + TagSize];
            byte[] ciphertext = new byte[plaintext.Length];
            byte[] tag = new byte[TagSize];

            // Encrypt using AES-GCM
            using (AesGcm aesGcm = new(_encryptionKey!, TagSize))
            {
                aesGcm.Encrypt(nonce, plaintext, ciphertext, tag);
            }

            // Combine: Nonce (12) + Ciphertext (variable) + Tag (16)
            Buffer.BlockCopy(nonce, 0, result, 0, NonceSize);
            Buffer.BlockCopy(ciphertext, 0, result, NonceSize, ciphertext.Length);
            Buffer.BlockCopy(tag, 0, result, NonceSize + ciphertext.Length, TagSize);
            return result;
        }

        /// <summary>
        /// Decrypts raw bytes using AES-256-GCM authenticated encryption.
        /// </summary>
        /// <param name="encryptedData">The encrypted data containing nonce, ciphertext, and authentication tag.</param>
        /// <returns>The decrypted plaintext bytes.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="encryptedData"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the key exchange has not been completed.</exception>
        /// <exception cref="CryptographicException">Thrown if authentication fails or data is corrupted.</exception>
        public byte[] Decrypt(byte[] encryptedData)
        {
            // Verify state and parameters.
            ThrowIfDisposed(); ThrowIfKeyExchangeNotComplete();
            if (encryptedData is null)
            {
                throw new ArgumentNullException(nameof(encryptedData));
            }

            // Validate minimum input length: Nonce (12) + Tag (16) + at least 1 byte of ciphertext
            if (encryptedData.Length < NonceSize + TagSize + 1)
            {
                throw new CryptographicException("Encrypted data is too short.");
            }

            // Extract nonce, ciphertext, and tag
            int ciphertextLength = encryptedData.Length - NonceSize - TagSize;
            byte[] nonce = new byte[NonceSize];
            byte[] ciphertext = new byte[ciphertextLength];
            byte[] tag = new byte[TagSize];
            byte[] plaintext = new byte[ciphertextLength];

            Buffer.BlockCopy(encryptedData, 0, nonce, 0, NonceSize);
            Buffer.BlockCopy(encryptedData, NonceSize, ciphertext, 0, ciphertextLength);
            Buffer.BlockCopy(encryptedData, NonceSize + ciphertextLength, tag, 0, TagSize);

            // Decrypt and verify authentication tag
            using (AesGcm aesGcm = new(_encryptionKey!, TagSize))
            {
                aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);
            }
            return plaintext;
        }

        /// <summary>
        /// Derives key material from a shared secret using HKDF with SHA-256.
        /// </summary>
        /// <param name="sharedSecret">The raw shared secret from ECDH key agreement.</param>
        /// <param name="outputLength">The desired output length in bytes.</param>
        /// <returns>The derived key material.</returns>
        private static byte[] DeriveKeyMaterial(byte[] sharedSecret, int outputLength)
        {
            // Use proper HKDF with a context-specific info parameter
            byte[] info = DefaultEncoding.GetBytes("PSADT-Pipe-Encryption-v2-GCM");
            byte[] salt = new byte[32]; // Zero salt is acceptable per RFC 5869

            // HKDF-Extract: PRK = HMAC-Hash(salt, IKM)
            byte[] prk;
            using (HMACSHA256 hmac = new(salt))
            {
                prk = hmac.ComputeHash(sharedSecret);
            }

            // HKDF-Expand
            byte[] output = new byte[outputLength];
            byte[] previousBlock = [];
            int offset = 0;
            byte counter = 1;
            try
            {
                using HMACSHA256 hmac = new(prk);
                while (offset < outputLength)
                {
                    // T(i) = HMAC-Hash(PRK, T(i-1) | info | counter)
                    byte[] input = new byte[previousBlock.Length + info.Length + 1];
                    Buffer.BlockCopy(previousBlock, 0, input, 0, previousBlock.Length);
                    Buffer.BlockCopy(info, 0, input, previousBlock.Length, info.Length);
                    input[input.Length - 1] = counter++;

                    previousBlock = hmac.ComputeHash(input);
                    int copyLength = Math.Min(previousBlock.Length, outputLength - offset);
                    Buffer.BlockCopy(previousBlock, 0, output, offset, copyLength);
                    offset += copyLength;
                }
            }
            finally
            {
                SecureZeroMemory(prk);
            }
            return output;
        }

        /// <summary>
        /// Performs key exchange as the server (initiator).
        /// Sends public key first, then receives client's public key, then verifies key agreement.
        /// </summary>
        /// <param name="outputStream">The stream to send data.</param>
        /// <param name="inputStream">The stream to receive data.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="outputStream"/> or <paramref name="inputStream"/> is null.</exception>
        /// <exception cref="CryptographicException">Thrown if key verification fails.</exception>
        public void PerformServerKeyExchange(Stream outputStream, Stream inputStream)
        {
            // Verify parameters and state.
            ThrowIfDisposed();
            if (outputStream is null)
            {
                throw new ArgumentNullException(nameof(outputStream));
            }
            if (inputStream is null)
            {
                throw new ArgumentNullException(nameof(inputStream));
            }

            // Server sends public key first
            byte[] publicKey = GetPublicKey();
            WriteLengthPrefixedBytes(outputStream, publicKey);

            // Server receives client's public key
            byte[] clientPublicKey = ReadLengthPrefixedBytes(inputStream);

            // Derive the shared key
            DeriveSharedKey(clientPublicKey);

            // Verify key agreement with challenge-response
            VerifyKeyExchangeAsServer(outputStream, inputStream);
        }

        /// <summary>
        /// Performs key exchange as the client (responder).
        /// Receives server's public key first, then sends own public key, then verifies key agreement.
        /// </summary>
        /// <param name="outputStream">The stream to send data.</param>
        /// <param name="inputStream">The stream to receive data.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="outputStream"/> or <paramref name="inputStream"/> is null.</exception>
        /// <exception cref="CryptographicException">Thrown if key verification fails.</exception>
        public void PerformClientKeyExchange(Stream outputStream, Stream inputStream)
        {
            // Verify parameters and state.
            ThrowIfDisposed();
            if (outputStream is null)
            {
                throw new ArgumentNullException(nameof(outputStream));
            }
            if (inputStream is null)
            {
                throw new ArgumentNullException(nameof(inputStream));
            }

            // Client receives server's public key first
            byte[] serverPublicKey = ReadLengthPrefixedBytes(inputStream);

            // Client sends its public key
            byte[] publicKey = GetPublicKey();
            WriteLengthPrefixedBytes(outputStream, publicKey);

            // Derive the shared key
            DeriveSharedKey(serverPublicKey);

            // Verify key agreement with challenge-response
            VerifyKeyExchangeAsClient(outputStream, inputStream);
        }

        /// <summary>
        /// Verifies the key exchange as the server using mutual challenge-response authentication.
        /// Both parties prove they have derived the same shared key.
        /// </summary>
        /// <param name="outputStream">The stream to send data.</param>
        /// <param name="inputStream">The stream to receive data.</param>
        /// <exception cref="CryptographicException">Thrown if the client's response does not match the expected value.</exception>
        /// <remarks>
        /// <para>
        /// The mutual authentication protocol works as follows:
        /// </para>
        /// <list type="number">
        /// <item><description>Server generates and sends a random challenge (challenge_s)</description></item>
        /// <item><description>Client generates its own challenge (challenge_c) and sends encrypted {challenge_s || challenge_c}</description></item>
        /// <item><description>Server decrypts and verifies challenge_s, proving the client has the correct key</description></item>
        /// <item><description>Server sends encrypted challenge_c back to client</description></item>
        /// <item><description>Client decrypts and verifies challenge_c, proving the server has the correct key</description></item>
        /// </list>
        /// <para>
        /// This ensures both parties have derived the same shared secret and prevents man-in-the-middle attacks
        /// where an attacker might relay public keys but not be able to derive the shared secret.
        /// </para>
        /// </remarks>
        private void VerifyKeyExchangeAsServer(Stream outputStream, Stream inputStream)
        {
            // Generate server's random challenge
            byte[] serverChallenge = new byte[ChallengeSize];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(serverChallenge);
            }

            // Send the server challenge (unencrypted - it's random data)
            WriteLengthPrefixedBytes(outputStream, serverChallenge);

            // Read the encrypted response from the client containing both challenges
            byte[] encryptedResponse = ReadLengthPrefixedBytes(inputStream);
            byte[] decryptedResponse = Decrypt(encryptedResponse);

            // Verify response length (should contain both challenges)
            if (decryptedResponse.Length != ChallengeSize * 2)
            {
                throw new CryptographicException("Key exchange verification failed: invalid response length.");
            }

            // Extract and verify server's challenge from the response
            byte[] returnedServerChallenge = new byte[ChallengeSize];
            Buffer.BlockCopy(decryptedResponse, 0, returnedServerChallenge, 0, ChallengeSize);
            if (!ConstantTimeEquals(serverChallenge, returnedServerChallenge))
            {
                throw new CryptographicException("Key exchange verification failed: server challenge mismatch.");
            }

            // Extract client's challenge and send it back encrypted to prove we have the key
            byte[] clientChallenge = new byte[ChallengeSize];
            Buffer.BlockCopy(decryptedResponse, ChallengeSize, clientChallenge, 0, ChallengeSize);
            byte[] encryptedClientChallenge = Encrypt(clientChallenge);
            WriteLengthPrefixedBytes(outputStream, encryptedClientChallenge);
        }

        /// <summary>
        /// Verifies the key exchange as the client using mutual challenge-response authentication.
        /// Both parties prove they have derived the same shared key.
        /// </summary>
        /// <param name="outputStream">The stream to send data.</param>
        /// <param name="inputStream">The stream to receive data.</param>
        /// <exception cref="CryptographicException">Thrown if the server's response does not match the expected value.</exception>
        /// <remarks>
        /// <para>
        /// See <see cref="VerifyKeyExchangeAsServer"/> for the full protocol description.
        /// </para>
        /// </remarks>
        private void VerifyKeyExchangeAsClient(Stream outputStream, Stream inputStream)
        {
            // Receive server's challenge
            byte[] serverChallenge = ReadLengthPrefixedBytes(inputStream);

            // Generate client's own challenge for mutual authentication
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

            // Receive server's proof - the encrypted client challenge
            byte[] encryptedServerProof = ReadLengthPrefixedBytes(inputStream);
            byte[] decryptedServerProof = Decrypt(encryptedServerProof);

            // Verify server returned our challenge correctly (proves server has the same key)
            if (!ConstantTimeEquals(clientChallenge, decryptedServerProof))
            {
                throw new CryptographicException("Key exchange verification failed: server proof mismatch. Server may not have derived the correct key.");
            }
        }

        /// <summary>
        /// Writes encrypted data to the stream.
        /// </summary>
        /// <param name="stream">The output stream.</param>
        /// <param name="plaintext">The plaintext bytes to encrypt and write.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="stream"/> or <paramref name="plaintext"/> is null.</exception>
        public void WriteEncrypted(Stream stream, byte[] plaintext)
        {
            // Verify state and parameters.
            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
            }
            if (plaintext is null)
            {
                throw new ArgumentNullException(nameof(plaintext));
            }

            // Encrypt and write.
            byte[] encrypted = Encrypt(plaintext);
            WriteLengthPrefixedBytes(stream, encrypted);
        }

        /// <summary>
        /// Reads and decrypts data from the stream.
        /// </summary>
        /// <param name="stream">The input stream.</param>
        /// <returns>The decrypted plaintext bytes.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="stream"/> is null.</exception>
        public byte[] ReadEncrypted(Stream stream)
        {
            // Verify parameters.
            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            // Read and decrypt.
            byte[] encrypted = ReadLengthPrefixedBytes(stream);
            return Decrypt(encrypted);
        }

        /// <summary>
        /// Writes a length-prefixed byte array to the stream.
        /// </summary>
        /// <param name="stream">The output stream.</param>
        /// <param name="data">The data to write.</param>
        private static void WriteLengthPrefixedBytes(Stream stream, byte[] data)
        {
            byte[] lengthBytes = BitConverter.GetBytes(data.Length);
            stream.Write(lengthBytes, 0, 4);
            stream.Write(data, 0, data.Length);
            stream.Flush();
        }

        /// <summary>
        /// Reads a length-prefixed byte array from the stream.
        /// </summary>
        /// <param name="stream">The input stream.</param>
        /// <returns>The data read from the stream.</returns>
        /// <exception cref="EndOfStreamException">Thrown if the stream ends before the expected data is read.</exception>
        /// <exception cref="InvalidDataException">Thrown if the length prefix is invalid or exceeds maximum allowed size.</exception>
        private static byte[] ReadLengthPrefixedBytes(Stream stream)
        {
            // Read the 4-byte length prefix
            byte[] lengthBytes = new byte[4];
            int bytesRead = 0;
            while (bytesRead < 4)
            {
                int read = stream.Read(lengthBytes, bytesRead, 4 - bytesRead);
                if (read == 0)
                {
                    throw new EndOfStreamException("Unexpected end of stream while reading length prefix.");
                }
                bytesRead += read;
            }

            // Verify we've received a correct value.
            int length = BitConverter.ToInt32(lengthBytes, 0);
            if (length <= 0)
            {
                throw new InvalidDataException("Invalid length prefix: negative or zero value.");
            }
            if (length > MaxMessageSize)
            {
                throw new InvalidDataException($"Message size {length} exceeds maximum allowed size of {MaxMessageSize} bytes.");
            }

            // Read the data
            byte[] data = new byte[length];
            bytesRead = 0;
            while (bytesRead < length)
            {
                int read = stream.Read(data, bytesRead, length - bytesRead);
                if (read == 0)
                {
                    throw new EndOfStreamException("Unexpected end of stream while reading data.");
                }
                bytesRead += read;
            }
            return data;
        }

        /// <summary>
        /// Throws an exception if the current instance has been disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if the object has already been disposed.</exception>
        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(PipeEncryption));
            }
        }

        /// <summary>
        /// Throws an exception if the key exchange process has not been completed.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the required encryption key has not been established. This indicates that the key exchange
        /// has not been completed and the shared key has not been derived.</exception>
        private void ThrowIfKeyExchangeNotComplete()
        {
            if (_encryptionKey is null)
            {
                throw new InvalidOperationException("Key exchange has not been completed. Call DeriveSharedKey first.");
            }
        }

        /// <summary>
        /// Securely zeros a byte array to clear sensitive data from memory.
        /// </summary>
        /// <param name="data">The byte array to zero.</param>
        private static void SecureZeroMemory(byte[] data)
        {
            // Use volatile write to prevent compiler optimization from removing the zeroing operation
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = 0;
            }
            // Memory barrier to ensure the writes are visible
            System.Threading.Thread.MemoryBarrier();
        }

        /// <summary>
        /// Compares two byte arrays in constant time to prevent timing attacks.
        /// </summary>
        /// <param name="a">The first byte array.</param>
        /// <param name="b">The second byte array.</param>
        /// <returns>True if the arrays are equal; otherwise, false.</returns>
        private static bool ConstantTimeEquals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
            {
                return false;
            }

            int result = 0;
            for (int i = 0; i < a.Length; i++)
            {
                result |= a[i] ^ b[i];
            }
            return result == 0;
        }

        /// <summary>
        /// Releases all resources used by the <see cref="PipeEncryption"/> instance.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _ecdh.Dispose();
            if (_encryptionKey is not null)
            {
                SecureZeroMemory(_encryptionKey);
                _encryptionKey = null;
            }
            _disposed = true;
        }

        /// <summary>
        /// Provides the Elliptic Curve Diffie-Hellman (ECDH) cryptographic implementation used for key agreement
        /// operations.
        /// </summary>
        /// <remarks>This field holds the platform-specific ECDH implementation. On .NET 8.0 or later, it
        /// uses <see cref="ECDiffieHellman"/>; on earlier versions, it uses <see
        /// cref="ECDiffieHellmanCng"/>. The specific implementation may affect
        /// compatibility and available features.</remarks>
#if NET8_0_OR_GREATER
        private readonly ECDiffieHellman _ecdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
#else
        private readonly ECDiffieHellmanCng _ecdh = new(256)
        {
            KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash,
            HashAlgorithm = CngAlgorithm.Sha256
        };
#endif

        /// <summary>
        /// The AES-256 encryption key used for AES-GCM authenticated encryption.
        /// </summary>
        private byte[]? _encryptionKey;

        /// <summary>
        /// Specifies whether the instance has been disposed.
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// Represents the default UTF-8 encoding used for text operations within the library.
        /// </summary>
        /// <remarks>This encoding instance does not emit a byte order mark (BOM) and throws exceptions on
        /// invalid bytes. Use this encoding when you require strict UTF-8 validation and do not want a BOM prefix in
        /// encoded output.</remarks>
        internal static readonly UTF8Encoding DefaultEncoding = new(false, true);

        /// <summary>
        /// Specifies the size, in bytes, of the AES-256 encryption key.
        /// </summary>
        private const int AesKeySize = 32;

        /// <summary>
        /// Specifies the size, in bytes, of the GCM nonce (also known as IV).
        /// </summary>
        /// <remarks>
        /// The recommended nonce size for AES-GCM is 12 bytes (96 bits). This is the most efficient
        /// size and provides optimal security characteristics when using a random nonce.
        /// </remarks>
        private const int NonceSize = 12;

        /// <summary>
        /// Specifies the size, in bytes, of the GCM authentication tag.
        /// </summary>
        /// <remarks>
        /// Using the maximum tag size of 16 bytes (128 bits) provides the highest level of
        /// authentication security.
        /// </remarks>
        private const int TagSize = 16;

        /// <summary>
        /// Specifies the size, in bytes, of the challenge used in mutual authentication.
        /// </summary>
        /// <remarks>
        /// A 32-byte (256-bit) challenge provides strong protection against brute-force attacks
        /// and ensures cryptographic uniqueness for each key exchange session.
        /// </remarks>
        private const int ChallengeSize = 32;

        /// <summary>
        /// Maximum allowed message size to prevent denial-of-service attacks via memory exhaustion.
        /// </summary>
        /// <remarks>
        /// Set to 16 MB which should be more than sufficient for IPC payloads while preventing
        /// malicious or corrupted length prefixes from causing excessive memory allocation.
        /// </remarks>
        private const int MaxMessageSize = 16 * 1024 * 1024;
    }
}
