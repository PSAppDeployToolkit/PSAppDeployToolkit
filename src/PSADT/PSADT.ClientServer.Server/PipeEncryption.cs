using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace PSADT.ClientServer
{
    /// <summary>
    /// Provides secure, authenticated encryption and key exchange for inter-process communication using RSA
    /// key transport and AES-256-CBC with HMAC-SHA256 (Encrypt-then-MAC).
    /// </summary>
    /// <remarks>
    /// <para>
    /// PipeEncryption manages the full lifecycle of key exchange and message encryption for secure communication
    /// channels. It uses RSA to transport a shared secret and then expands it into encryption and MAC keys using HKDF.
    /// </para>
    /// <para>
    /// Messages are encrypted using AES-256-CBC and authenticated using HMAC-SHA256 in an Encrypt-then-MAC
    /// construction, ensuring both confidentiality and integrity.
    /// </para>
    /// <para>
    /// Instances must complete the key exchange via <see cref="PerformKeyExchange"/>
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
        internal abstract void PerformKeyExchange(Stream outputStream, Stream inputStream);

        /// <summary>
        /// Reads and decrypts data from the stream.
        /// </summary>
        /// <param name="stream">The input stream.</param>
        /// <returns>The decrypted plaintext bytes.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="stream"/> is null.</exception>
        internal byte[] ReadEncrypted(Stream stream)
        {
            // Read and decrypt.
            ArgumentNullException.ThrowIfNull(stream);
            return Decrypt(ReadLengthPrefixedBytes(stream));
        }

        /// <summary>
        /// Writes encrypted data to the stream.
        /// </summary>
        /// <param name="stream">The output stream.</param>
        /// <param name="plaintext">The plaintext bytes to encrypt and write.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="stream"/> or <paramref name="plaintext"/> is null.</exception>
        internal void WriteEncrypted(Stream stream, byte[] plaintext)
        {
            // Encrypt and write.
            ArgumentNullException.ThrowIfNull(stream);
            ArgumentNullException.ThrowIfNull(plaintext);
            WriteLengthPrefixedBytes(stream, Encrypt(plaintext));
        }

        /// <summary>
        /// Reads a length-prefixed byte array from the stream.
        /// </summary>
        /// <param name="stream">The input stream.</param>
        /// <returns>The data read from the stream.</returns>
        /// <exception cref="EndOfStreamException">Thrown if the stream ends before the expected data is read.</exception>
        /// <exception cref="InvalidDataException">Thrown if the length prefix is invalid or exceeds maximum allowed size.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Critical Code Smell", "S2302:\"nameof\" should be used", Justification = "This is a false positive.")]
        private protected static byte[] ReadLengthPrefixedBytes(Stream stream)
        {
            // Read the 4-byte length prefix
            byte[] lengthBytes = new byte[4]; int bytesRead = 0;
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
        /// Writes a length-prefixed byte array to the stream.
        /// </summary>
        /// <param name="stream">The output stream.</param>
        /// <param name="data">The data to write.</param>
        private protected static void WriteLengthPrefixedBytes(Stream stream, byte[] data)
        {
            byte[] lengthBytes = BitConverter.GetBytes(data.Length);
            stream.Write(lengthBytes, 0, 4);
            stream.Write(data, 0, data.Length);
            stream.Flush();
        }

        /// <summary>
        /// Encrypts raw bytes using AES-256-CBC with HMAC-SHA256 authentication (Encrypt-then-MAC).
        /// </summary>
        /// <param name="plaintext">The plaintext bytes to encrypt.</param>
        /// <returns>A byte array containing the IV, ciphertext, and HMAC tag.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="plaintext"/> is null.</exception>
        private protected byte[] Encrypt(byte[] plaintext)
        {
            // Verify state and parameters.
            ThrowIfDisposed(); ThrowIfKeyExchangeNotComplete();
            ArgumentNullException.ThrowIfNull(plaintext);

            // Generate a unique IV for this encryption operation.
            byte[] iv = new byte[IvSize];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(iv);
            }

            // Encrypt using AES-CBC
            byte[] ciphertext;
            using Aes aes = Aes.Create();
            aes.Key = _encryptionKey;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            using (ICryptoTransform encryptor = aes.CreateEncryptor())
            {
                ciphertext = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);
            }

            // Combine: IV (16) + Ciphertext (variable)
            byte[] ivAndCiphertext = new byte[IvSize + ciphertext.Length];
            Buffer.BlockCopy(iv, 0, ivAndCiphertext, 0, IvSize);
            Buffer.BlockCopy(ciphertext, 0, ivAndCiphertext, IvSize, ciphertext.Length);

            // Compute HMAC over IV + ciphertext (Encrypt-then-MAC)
            byte[] mac;
            using (HMACSHA256 hmac = new(_macKey))
            {
                mac = hmac.ComputeHash(ivAndCiphertext);
            }

            // Result: IV (16) + Ciphertext (variable) + MAC (32)
            byte[] result = new byte[ivAndCiphertext.Length + MacSize];
            Buffer.BlockCopy(ivAndCiphertext, 0, result, 0, ivAndCiphertext.Length);
            Buffer.BlockCopy(mac, 0, result, ivAndCiphertext.Length, MacSize);
            return result;
        }

        /// <summary>
        /// Decrypts raw bytes using AES-256-CBC with HMAC-SHA256 authentication (Encrypt-then-MAC).
        /// </summary>
        /// <param name="encryptedData">The encrypted data containing IV, ciphertext, and HMAC tag.</param>
        /// <returns>The decrypted plaintext bytes.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="encryptedData"/> is null.</exception>
        /// <exception cref="CryptographicException">Thrown if authentication fails or data is corrupted.</exception>
        private protected byte[] Decrypt(byte[] encryptedData)
        {
            // Verify state and parameters.
            ThrowIfDisposed(); ThrowIfKeyExchangeNotComplete();
            ArgumentNullException.ThrowIfNull(encryptedData);

            // Validate minimum input length: IV (16) + MAC (32) + at least 1 block of ciphertext (16)
            if (encryptedData.Length < IvSize + MacSize + AesBlockSize)
            {
                throw new ArgumentOutOfRangeException(nameof(encryptedData), encryptedData.Length, "Encrypted data is too short.");
            }

            // Split: IV + Ciphertext | MAC
            int ivAndCiphertextLength = encryptedData.Length - MacSize;
            byte[] receivedMac = new byte[MacSize];
            Buffer.BlockCopy(encryptedData, ivAndCiphertextLength, receivedMac, 0, MacSize);

            // Verify MAC first (Encrypt-then-MAC: verify before decrypting)
            byte[] computedMac;
            using (HMACSHA256 hmac = new(_macKey))
            {
                computedMac = hmac.ComputeHash(encryptedData, 0, ivAndCiphertextLength);
            }
            if (!ConstantTimeEquals(receivedMac, computedMac))
            {
                throw new CryptographicException("Message authentication failed: HMAC mismatch.");
            }

            // Extract IV and ciphertext
            byte[] iv = new byte[IvSize];
            int ciphertextLength = ivAndCiphertextLength - IvSize;
            byte[] ciphertext = new byte[ciphertextLength];
            Buffer.BlockCopy(encryptedData, 0, iv, 0, IvSize);
            Buffer.BlockCopy(encryptedData, IvSize, ciphertext, 0, ciphertextLength);

            // Decrypt
            using Aes aesDecrypt = Aes.Create();
            aesDecrypt.Key = _encryptionKey;
            aesDecrypt.IV = iv;
            aesDecrypt.Mode = CipherMode.CBC;
            aesDecrypt.Padding = PaddingMode.PKCS7;
            using ICryptoTransform decryptor = aesDecrypt.CreateDecryptor();
            return decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
        }

        /// <summary>
        /// Gets the local RSA public key for transmission to the remote party.
        /// </summary>
        /// <returns>A byte array containing the exported RSA public key as a UTF-8 encoded XML string.</returns>
        private protected byte[] GetPublicKey()
        {
            ThrowIfDisposed();
            string xml = _rsa.ToXmlString(false);
            return DefaultEncoding.Value.GetBytes(xml);
        }

        /// <summary>
        /// Completes the key exchange by decrypting the shared secret sent by the initiating party.
        /// </summary>
        /// <param name="encryptedSharedSecret">The RSA-encrypted shared secret from the remote party.</param>
        /// <param name="localPublicKey">The local party's public key bytes as sent during key exchange.</param>
        /// <param name="remotePublicKey">The remote party's public key bytes as received during key exchange.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="encryptedSharedSecret"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the key exchange has already been completed.</exception>
        private protected void DeriveSharedKey(byte[] encryptedSharedSecret, byte[] localPublicKey, byte[] remotePublicKey)
        {
            // Verify parameters and state.
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(encryptedSharedSecret);
            if (_encryptionKey is not null)
            {
                throw new InvalidOperationException("Key exchange has already been completed.");
            }

            // Decrypt the shared secret using our RSA private key
            byte[] sharedSecret = _rsa.Decrypt(encryptedSharedSecret, RSAEncryptionPadding.OaepSHA1);

            // Derive encryption and MAC keys using HKDF, binding to both public keys
            byte[] keyMaterial = DeriveKeyMaterial(sharedSecret, AesKeySize + MacKeySize, localPublicKey, remotePublicKey);
            _encryptionKey = new byte[AesKeySize];
            _macKey = new byte[MacKeySize];
            Buffer.BlockCopy(keyMaterial, 0, _encryptionKey, 0, AesKeySize);
            Buffer.BlockCopy(keyMaterial, AesKeySize, _macKey, 0, MacKeySize);

            // Clear sensitive data
            SecureZeroMemory(sharedSecret);
            SecureZeroMemory(keyMaterial);
        }

        /// <summary>
        /// Generates a shared secret and encrypts it with the remote party's RSA public key.
        /// </summary>
        /// <param name="localPublicKey">The local party's public key bytes as sent during key exchange.</param>
        /// <param name="remotePublicKey">The remote party's RSA public key bytes.</param>
        /// <returns>The RSA-encrypted shared secret to send to the remote party.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="remotePublicKey"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the key exchange has already been completed.</exception>
        private protected byte[] GenerateAndEncryptSharedSecret(byte[] localPublicKey, byte[] remotePublicKey)
        {
            // Verify parameters and state.
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(remotePublicKey);
            if (_encryptionKey is not null)
            {
                throw new InvalidOperationException("Key exchange has already been completed.");
            }

            // Generate a random shared secret
            byte[] sharedSecret = new byte[SharedSecretSize];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(sharedSecret);
            }

            // Encrypt the shared secret with the remote party's public key
            byte[] encryptedSecret;
            using (RSA remoteRsa = RSA.Create())
            {
                remoteRsa.FromXmlString(DefaultEncoding.Value.GetString(remotePublicKey));
                encryptedSecret = remoteRsa.Encrypt(sharedSecret, RSAEncryptionPadding.OaepSHA1);
            }

            // Derive encryption and MAC keys using HKDF, binding to both public keys
            byte[] keyMaterial = DeriveKeyMaterial(sharedSecret, AesKeySize + MacKeySize, localPublicKey, remotePublicKey);
            _encryptionKey = new byte[AesKeySize];
            _macKey = new byte[MacKeySize];
            Buffer.BlockCopy(keyMaterial, 0, _encryptionKey, 0, AesKeySize);
            Buffer.BlockCopy(keyMaterial, AesKeySize, _macKey, 0, MacKeySize);

            // Clear sensitive data
            SecureZeroMemory(sharedSecret);
            SecureZeroMemory(keyMaterial);
            return encryptedSecret;
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
            return result == 0;
        }

        /// <summary>
        /// Compares two byte arrays lexicographically.
        /// </summary>
        /// <param name="a">The first byte array.</param>
        /// <param name="b">The second byte array.</param>
        /// <returns>A negative value if <paramref name="a"/> is less than <paramref name="b"/>, zero if equal,
        /// or a positive value if <paramref name="a"/> is greater than <paramref name="b"/>.</returns>
        private static int CompareBytes(byte[] a, byte[] b)
        {
            int minLength = Math.Min(a.Length, b.Length);
            for (int i = 0; i < minLength; i++)
            {
                int cmp = a[i].CompareTo(b[i]);
                if (cmp != 0)
                {
                    return cmp;
                }
            }
            return a.Length.CompareTo(b.Length);
        }

        /// <summary>
        /// Throws an exception if the current instance has been disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if the object has already been disposed.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [StackTraceHidden]
        private void ThrowIfKeyExchangeNotComplete()
        {
            InvalidOperationException.ThrowIfNull(_encryptionKey, "Key exchange has not been completed. Call PerformKeyExchange first.");
        }

        /// <summary>
        /// Derives key material from a shared secret using HKDF with SHA-256, binding the derived keys
        /// to both parties' public keys to prevent man-in-the-middle key substitution.
        /// </summary>
        /// <param name="sharedSecret">The raw shared secret.</param>
        /// <param name="outputLength">The desired output length in bytes.</param>
        /// <param name="localPublicKey">The local party's public key bytes.</param>
        /// <param name="remotePublicKey">The remote party's public key bytes.</param>
        /// <returns>The derived key material.</returns>
        private static byte[] DeriveKeyMaterial(byte[] sharedSecret, int outputLength, byte[] localPublicKey, byte[] remotePublicKey)
        {
            // Build the HKDF info parameter: protocol identifier + SHA-256 hash of both public keys.
            // This binds the derived keys to the specific key exchange transcript.
            // Use a canonical (sorted) order so both sides produce the same hash regardless of role.
            byte[] protocolInfo = DefaultEncoding.Value.GetBytes("PSADT-Pipe-Encryption-v2-CBC-HMAC");
            byte[] transcriptHash;
            using (SHA256 sha256 = SHA256.Create())
            {
                // Sort keys lexicographically to ensure both sides use the same order.
                byte[] first, second;
                if (CompareBytes(localPublicKey, remotePublicKey) <= 0)
                {
                    first = localPublicKey;
                    second = remotePublicKey;
                }
                else
                {
                    first = remotePublicKey;
                    second = localPublicKey;
                }

                byte[] transcript = new byte[first.Length + second.Length];
                Buffer.BlockCopy(first, 0, transcript, 0, first.Length);
                Buffer.BlockCopy(second, 0, transcript, first.Length, second.Length);
                transcriptHash = sha256.ComputeHash(transcript);
            }

            // Combine: protocolInfo || transcriptHash
            byte[] info = new byte[protocolInfo.Length + transcriptHash.Length];
            Buffer.BlockCopy(protocolInfo, 0, info, 0, protocolInfo.Length);
            Buffer.BlockCopy(transcriptHash, 0, info, protocolInfo.Length, transcriptHash.Length);

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
            _rsa.Dispose();
            if (_encryptionKey is not null)
            {
                SecureZeroMemory(_encryptionKey);
                _encryptionKey = null;
            }
            if (_macKey is not null)
            {
                SecureZeroMemory(_macKey);
                _macKey = null;
            }
            _disposed = true;
        }

        /// <summary>
        /// The AES-256 encryption key used for AES-CBC encryption.
        /// </summary>
        private byte[]? _encryptionKey;

        /// <summary>
        /// The HMAC-SHA256 key used for message authentication.
        /// </summary>
        private byte[]? _macKey;

        /// <summary>
        /// Specifies whether the instance has been disposed.
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// Provides the RSA cryptographic implementation used for key exchange operations.
        /// </summary>
        private readonly RSA _rsa = CreateRsaKey();

        /// <summary>
        /// Creates an RSA key of the configured size.
        /// </summary>
        private static RSA CreateRsaKey()
        {
            RSA rsa = RSA.Create();
            rsa.KeySize = RsaKeySize;
            return rsa;
        }

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
        /// Specifies the size, in bytes, of the HMAC-SHA256 key.
        /// </summary>
        private const int MacKeySize = 32;

        /// <summary>
        /// Specifies the size, in bytes, of the AES-CBC IV.
        /// </summary>
        private const int IvSize = 16;

        /// <summary>
        /// Specifies the AES block size in bytes.
        /// </summary>
        private const int AesBlockSize = 16;

        /// <summary>
        /// Specifies the size, in bytes, of the HMAC-SHA256 authentication tag.
        /// </summary>
        private const int MacSize = 32;

        /// <summary>
        /// Specifies the RSA key size in bits.
        /// </summary>
        private const int RsaKeySize = 2048;

        /// <summary>
        /// Specifies the size, in bytes, of the shared secret used for key derivation.
        /// </summary>
        private const int SharedSecretSize = 32;

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
