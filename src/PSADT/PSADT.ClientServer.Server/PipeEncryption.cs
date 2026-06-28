using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace PSADT.ClientServer
{
    /// <summary>
    /// Provides secure, authenticated encryption and key exchange for inter-process communication using Elliptic Curve
    /// Diffie-Hellman (ECDH) and AES-256-GCM.
    /// </summary>
    /// <typeparam name="TSelf">The specific subclass type that implements the role-specific key exchange protocol. This allows for
    /// fluent method chaining and type-safe operations within the subclass.</typeparam>
    /// <remarks>
    /// <para>
    /// PipeEncryption manages the full lifecycle of key exchange and message encryption for secure communication
    /// channels. It uses ECDH to derive a shared secret and then expands it into encryption keys using HKDF.
    /// </para>
    /// <para>
    /// Messages are encrypted using AES-256-GCM which provides authenticated encryption with associated data (AEAD),
    /// ensuring both confidentiality and integrity in a single cryptographic operation.
    /// </para>
    /// <para>
    /// Instances must complete the key exchange via <see cref="PerformKeyExchangeAsync"/>
    /// before encryption or decryption operations can be performed.
    /// </para>
    /// <para>
    /// This class is not thread-safe; callers should ensure appropriate synchronization if used concurrently.
    /// Dispose the instance when finished to securely erase sensitive key material.
    /// </para>
    /// </remarks>
    internal abstract class PipeEncryption<TSelf> : IDisposable where TSelf : PipeEncryption<TSelf>
    {
        /// <summary>
        /// Performs the key exchange with the remote party using the role-specific protocol.
        /// </summary>
        /// <param name="outputStream">The stream to send data to the remote party.</param>
        /// <param name="inputStream">The stream to receive data from the remote party.</param>
        internal abstract ValueTask PerformKeyExchangeAsync(Stream outputStream, Stream inputStream);

        /// <summary>
        /// Reads and decrypts data from the stream.
        /// </summary>
        /// <param name="stream">The input stream.</param>
        /// <returns>The decrypted plaintext bytes.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="stream"/> is null.</exception>
        internal ValueTask<byte[]> ReadEncryptedAsync(Stream stream)
        {
            // Internal implementation method.
            async ValueTask<byte[]> ReadEncryptedImplAsync(Stream stream)
            {
                return Decrypt(await ReadLengthPrefixedBytesAsync(stream).ConfigureAwait(false));
            }

            // Read and decrypt.
            ArgumentNullException.ThrowIfNull(stream);
            return ReadEncryptedImplAsync(stream);
        }

        /// <summary>
        /// Writes encrypted data to the stream.
        /// </summary>
        /// <param name="stream">The output stream.</param>
        /// <param name="plaintext">The plaintext bytes to encrypt and write.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="stream"/> or <paramref name="plaintext"/> is null.</exception>
        internal ValueTask WriteEncryptedAsync(Stream stream, byte[] plaintext)
        {
            // Encrypt and write.
            ArgumentNullException.ThrowIfNull(stream);
            ArgumentNullException.ThrowIfNull(plaintext);
            return WriteLengthPrefixedBytesAsync(stream, Encrypt(plaintext));
        }

        /// <summary>
        /// Reads a length-prefixed byte array from the stream.
        /// </summary>
        /// <param name="stream">The input stream.</param>
        /// <returns>The data read from the stream.</returns>
        /// <exception cref="EndOfStreamException">Thrown if the stream ends before the expected data is read.</exception>
        /// <exception cref="InvalidDataException">Thrown if the length prefix is invalid or exceeds maximum allowed size.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Critical Code Smell", "S2302:\"nameof\" should be used", Justification = "This is a false positive.")]
        private protected static async ValueTask<byte[]> ReadLengthPrefixedBytesAsync(Stream stream)
        {
            // Read the 4-byte length prefix
            byte[] lengthBytes = new byte[4]; int bytesRead = 0;
            while (bytesRead < 4)
            {
                int read = await stream.ReadAsync(lengthBytes, bytesRead, 4 - bytesRead, default).ConfigureAwait(false);
                if (read is 0)
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
                throw new InvalidDataException($"Message size {length.ToString(CultureInfo.InvariantCulture)} exceeds maximum allowed size of {MaxMessageSize} bytes.");
            }

            // Read the data
            byte[] data = new byte[length];
            bytesRead = 0;
            while (bytesRead < length)
            {
                int read = await stream.ReadAsync(data, bytesRead, length - bytesRead, default).ConfigureAwait(false);
                if (read is 0)
                {
                    throw new EndOfStreamException("Unexpected end of stream while reading data.");
                }
                bytesRead += read;
            }
            return data;
        }

        /// <summary>
        /// Writes a length-prefixed byte array to the stream.
        /// </summary>
        /// <param name="stream">The output stream.</param>
        /// <param name="data">The data to write.</param>
        private protected static async ValueTask WriteLengthPrefixedBytesAsync(Stream stream, byte[] data)
        {
            byte[] lengthBytes = BitConverter.GetBytes(data.Length);
            await stream.WriteAsync(lengthBytes, 0, 4, default).ConfigureAwait(false);
            await stream.WriteAsync(data, 0, data.Length, default).ConfigureAwait(false);
            await stream.FlushAsync(default).ConfigureAwait(false);
        }

        /// <summary>
        /// Encrypts raw bytes using AES-256-GCM authenticated encryption.
        /// </summary>
        /// <param name="plaintext">The plaintext bytes to encrypt.</param>
        /// <returns>A byte array containing the nonce, ciphertext, and authentication tag.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="plaintext"/> is null.</exception>
        private protected byte[] Encrypt(byte[] plaintext)
        {
            // Verify state and parameters.
            ThrowIfDisposed(); ThrowIfKeyExchangeNotComplete();
            ArgumentNullException.ThrowIfNull(plaintext);

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
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the encrypted data is too short.</exception>
        private protected byte[] Decrypt(byte[] encryptedData)
        {
            // Verify state and parameters.
            ThrowIfDisposed(); ThrowIfKeyExchangeNotComplete();
            ArgumentNullException.ThrowIfNull(encryptedData);

            // Validate minimum input length: Nonce (12) + Tag (16) + at least 1 byte of ciphertext
            if (encryptedData.Length < NonceSize + TagSize + 1)
            {
                throw new ArgumentOutOfRangeException(nameof(encryptedData), encryptedData.Length, "Encrypted data is too short.");
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
        /// Gets the local public key for transmission to the remote party.
        /// </summary>
        /// <returns>A byte array containing the exported public key.</returns>
        private protected byte[] GetPublicKey()
        {
            ThrowIfDisposed();
#if NET8_0_OR_GREATER
            ECParameters ecParams = _ecdh.ExportParameters(includePrivateParameters: false);
            // Build CNG EccPublicBlob: BCRYPT_ECCKEY_BLOB header (8 bytes) + X + Y
            // Magic for ECDH P-256 public key: ECDH_PUBLIC_P256 = 0x314B4345
            int keySize = ecParams.Q.X!.Length;
            byte[] blob = new byte[8 + (keySize * 2)];
            // ECDH_PUBLIC_P256 magic
            blob[0] = 0x45; blob[1] = 0x43; blob[2] = 0x4B; blob[3] = 0x31;
            // Key length in bytes
            blob[4] = (byte)keySize; blob[5] = 0; blob[6] = 0; blob[7] = 0;
            Buffer.BlockCopy(ecParams.Q.X, 0, blob, 8, keySize);
            Buffer.BlockCopy(ecParams.Q.Y!, 0, blob, 8 + keySize, keySize);
            return blob;
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
        private protected void DeriveSharedKey(byte[] remotePublicKey)
        {
            // Verify parameters and state.
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(remotePublicKey);
            if (_encryptionKey is not null)
            {
                throw new InvalidOperationException("Key exchange has already been completed.");
            }

            // Import the remote public key and derive shared secret
#if NET8_0_OR_GREATER
            // Remote key is in CNG EccPublicBlob format: 8-byte header + X + Y
            int keySize = BitConverter.ToInt32(remotePublicKey, 4);
            byte[] x = new byte[keySize];
            byte[] y = new byte[keySize];
            Buffer.BlockCopy(remotePublicKey, 8, x, 0, keySize);
            Buffer.BlockCopy(remotePublicKey, 8 + keySize, y, 0, keySize);
            ECParameters remoteParams = new()
            {
                Curve = ECCurve.NamedCurves.nistP256,
                Q = new ECPoint { X = x, Y = y },
            };
            using ECDiffieHellman remoteEcdh = ECDiffieHellman.Create(remoteParams);
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
        /// Compares two byte arrays in constant time to prevent timing attacks.
        /// </summary>
        /// <param name="a">The first byte array.</param>
        /// <param name="b">The second byte array.</param>
        /// <returns>True if the arrays are equal; otherwise, false.</returns>
        private protected static bool ConstantTimeEquals(byte[] a, byte[] b)
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
            return result is 0;
        }

        /// <summary>
        /// Throws an exception if the current instance has been disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if the object has already been disposed.</exception>
        [StackTraceHidden]
        private protected void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
        }

        /// <summary>
        /// Throws an exception if the key exchange process has not been completed.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the required encryption key has not been established. This indicates that the key exchange
        /// has not been completed and the shared key has not been derived.</exception>
        [StackTraceHidden]
        private void ThrowIfKeyExchangeNotComplete()
        {
            InvalidOperationException.ThrowIfNull(_encryptionKey, "Key exchange has not been completed. Call PerformKeyExchange first.");
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
            byte[] info = DefaultEncoding.Value.GetBytes("PSADT-Pipe-Encryption-v2-GCM");
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
                    input[^1] = counter++;

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
        /// Releases all resources used by the <see cref="PipeEncryption{TSelf}"/> instance.
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
        /// The AES-256 encryption key used for AES-GCM authenticated encryption.
        /// </summary>
        private byte[]? _encryptionKey;

        /// <summary>
        /// Specifies whether the instance has been disposed.
        /// </summary>
        private bool _disposed;

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
            HashAlgorithm = CngAlgorithm.Sha256,
        };
#endif

        /// <summary>
        /// Specifies the size, in bytes, of the challenge used in mutual authentication.
        /// </summary>
        /// <remarks>
        /// A 32-byte (256-bit) challenge provides strong protection against brute-force attacks
        /// and ensures cryptographic uniqueness for each key exchange session.
        /// </remarks>
        private protected const int ChallengeSize = 32;

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
        /// Maximum allowed message size to prevent denial-of-service attacks via memory exhaustion.
        /// </summary>
        /// <remarks>
        /// Set to 16 MB which should be more than sufficient for IPC payloads while preventing
        /// malicious or corrupted length prefixes from causing excessive memory allocation.
        /// </remarks>
        private const int MaxMessageSize = 16 * 1024 * 1024;
    }
}
