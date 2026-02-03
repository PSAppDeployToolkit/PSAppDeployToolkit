using System;
using System.IO;
using System.Security.Cryptography;

namespace PSADT.ClientServer
{
    /// <summary>
    /// Provides secure, authenticated encryption and key exchange for inter-process communication using Elliptic Curve
    /// Diffie-Hellman (ECDH) and AES-CBC with HMAC-SHA256. This class enables two parties to establish a shared secret
    /// and exchange encrypted messages over a pipe or stream.
    /// </summary>
    /// <remarks>PipeEncryption manages the full lifecycle of key exchange and message encryption for secure
    /// communication channels. It supports both server and client roles in the key exchange process, using ECDH to
    /// derive a shared secret and then expanding it into separate encryption and authentication keys. All messages are
    /// encrypted with AES-CBC and authenticated with HMAC-SHA256 to ensure confidentiality and integrity. Instances
    /// must complete the key exchange before encryption or decryption operations can be performed. This class is not
    /// thread-safe; callers should ensure appropriate synchronization if used concurrently. Dispose the instance when
    /// finished to securely erase sensitive key material.</remarks>
    public sealed record PipeEncryption : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PipeEncryption"/> class with a new ECDH key pair.
        /// </summary>
        public PipeEncryption()
        {
#if NET8_0_OR_GREATER
            _ecdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
#else
            _ecdh = new ECDiffieHellmanCng(256)
            {
                KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash,
                HashAlgorithm = CngAlgorithm.Sha256
            };
#endif
        }

        /// <summary>
        /// Gets a value indicating whether the key exchange has been completed.
        /// </summary>
        public bool IsKeyExchangeComplete => _encryptionKey is not null && _macKey is not null;

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

            // Derive encryption and MAC keys using HKDF-like expansion
            byte[] keyMaterial = DeriveKeyMaterial(sharedSecret, AesKeySize + MacKeySize);
            _encryptionKey = new byte[AesKeySize];
            _macKey = new byte[MacKeySize];
            Buffer.BlockCopy(keyMaterial, 0, _encryptionKey, 0, AesKeySize);
            Buffer.BlockCopy(keyMaterial, AesKeySize, _macKey, 0, MacKeySize);

            // Clear sensitive data
            ZeroMemory(sharedSecret);
            ZeroMemory(keyMaterial);
        }

        /// <summary>
        /// Encrypts raw bytes using AES-CBC with HMAC-SHA256.
        /// </summary>
        /// <param name="plaintext">The plaintext bytes to encrypt.</param>
        /// <returns>A byte array containing the IV, ciphertext, and MAC.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="plaintext"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the key exchange has not been completed.</exception>
        public byte[] Encrypt(byte[] plaintext)
        {
            // Verify state and parameters.
            ThrowIfDisposed();
            ThrowIfKeyExchangeNotComplete();
            if (plaintext is null)
            {
                throw new ArgumentNullException(nameof(plaintext));
            }

            // Set up AES encryption.
            using Aes aes = Aes.Create();
            aes.Key = _encryptionKey!;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.GenerateIV();

            // Encrypt the plain text.
            byte[] ciphertext;
            using (ICryptoTransform encryptor = aes.CreateEncryptor())
            using (MemoryStream ms = new())
            {
                using (CryptoStream cs = new(ms, encryptor, CryptoStreamMode.Write))
                {
                    cs.Write(plaintext, 0, plaintext.Length);
                    cs.FlushFinalBlock();
                }
                ciphertext = ms.ToArray();
            }

            // Compute MAC over IV + cipher text.
            byte[] iv = aes.IV; byte[] mac;
            using (HMACSHA256 hmac = new(_macKey!))
            {
                byte[] dataToMac = new byte[iv.Length + ciphertext.Length];
                Buffer.BlockCopy(iv, 0, dataToMac, 0, iv.Length);
                Buffer.BlockCopy(ciphertext, 0, dataToMac, iv.Length, ciphertext.Length);
                mac = hmac.ComputeHash(dataToMac);
            }

            // Combine: IV (16) + ciphertext (variable) + MAC (32).
            byte[] result = new byte[iv.Length + ciphertext.Length + mac.Length];
            Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
            Buffer.BlockCopy(ciphertext, 0, result, iv.Length, ciphertext.Length);
            Buffer.BlockCopy(mac, 0, result, iv.Length + ciphertext.Length, mac.Length);
            return result;
        }

        /// <summary>
        /// Decrypts raw bytes using AES-CBC with HMAC-SHA256 verification.
        /// </summary>
        /// <param name="encryptedData">The encrypted data containing IV, ciphertext, and MAC.</param>
        /// <returns>The decrypted plaintext bytes.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="encryptedData"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the key exchange has not been completed.</exception>
        /// <exception cref="CryptographicException">Thrown if MAC verification fails or data is corrupted.</exception>
        public byte[] Decrypt(byte[] encryptedData)
        {
            // Verify state and parameters.
            ThrowIfDisposed();
            ThrowIfKeyExchangeNotComplete();
            if (encryptedData is null)
            {
                throw new ArgumentNullException(nameof(encryptedData));
            }

            // Validate input length.
            if (encryptedData.Length < IvSize + MacSize + 1)
            {
                throw new CryptographicException("Encrypted data is too short.");
            }

            // Extract IV, ciphertext, and MAC.
            byte[] iv = new byte[IvSize];
            int ciphertextLength = encryptedData.Length - IvSize - MacSize;
            byte[] ciphertext = new byte[ciphertextLength];
            byte[] receivedMac = new byte[MacSize];

            // Copy IV, ciphertext, and MAC from the encrypted data.
            Buffer.BlockCopy(encryptedData, 0, iv, 0, IvSize);
            Buffer.BlockCopy(encryptedData, IvSize, ciphertext, 0, ciphertextLength);
            Buffer.BlockCopy(encryptedData, IvSize + ciphertextLength, receivedMac, 0, MacSize);

            // Verify MAC first (Encrypt-then-MAC)
            byte[] computedMac;
            using (HMACSHA256 hmac = new(_macKey!))
            {
                byte[] dataToMac = new byte[iv.Length + ciphertext.Length];
                Buffer.BlockCopy(iv, 0, dataToMac, 0, iv.Length);
                Buffer.BlockCopy(ciphertext, 0, dataToMac, iv.Length, ciphertext.Length);
                computedMac = hmac.ComputeHash(dataToMac);
            }

            // Constant-time comparison.
            if (!ConstantTimeEquals(receivedMac, computedMac))
            {
                throw new CryptographicException("MAC verification failed. Data may have been tampered with.");
            }

            // Decrypt
            using Aes aes = Aes.Create();
            aes.Key = _encryptionKey!;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            using ICryptoTransform decryptor = aes.CreateDecryptor();
            using MemoryStream ms = new(ciphertext);
            using CryptoStream cs = new(ms, decryptor, CryptoStreamMode.Read);
            using MemoryStream output = new();
            cs.CopyTo(output);
            return output.ToArray();
        }

        /// <summary>
        /// Derives key material from a shared secret using HKDF-like expansion with SHA-256.
        /// </summary>
        /// <param name="sharedSecret">The raw shared secret.</param>
        /// <param name="outputLength">The desired output length in bytes.</param>
        /// <returns>The derived key material.</returns>
        private static byte[] DeriveKeyMaterial(byte[] sharedSecret, int outputLength)
        {
            // HKDF-Extract: PRK = HMAC-Hash(salt, IKM).
            byte[] info = DefaultEncoding.Value.GetBytes("PSADT-Pipe-Encryption-v1"); byte[] prk;
            using (HMACSHA256 hmac = new(new byte[32])) // salt = zeros
            {
                prk = hmac.ComputeHash(sharedSecret);
            }

            // HKDF-Expand.
            byte[] output = new byte[outputLength];
            byte[] t = [];
            int offset = 0;
            byte counter = 1;
            using (HMACSHA256 hmac = new(prk))
            {
                while (offset < outputLength)
                {
                    // T(i) = HMAC-Hash(PRK, T(i-1) | info | counter)
                    byte[] input = new byte[t.Length + info.Length + 1];
                    Buffer.BlockCopy(t, 0, input, 0, t.Length);
                    Buffer.BlockCopy(info, 0, input, t.Length, info.Length);
                    input[input.Length - 1] = counter++;

                    t = hmac.ComputeHash(input);
                    int copyLength = Math.Min(t.Length, outputLength - offset);
                    Buffer.BlockCopy(t, 0, output, offset, copyLength);
                    offset += copyLength;
                }
            }
            ZeroMemory(prk);
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
        /// Verifies the key exchange as the server by sending a challenge and verifying the client's response.
        /// </summary>
        /// <param name="outputStream">The stream to send data.</param>
        /// <param name="inputStream">The stream to receive data.</param>
        /// <exception cref="CryptographicException">Thrown if the client's response does not match the expected value.</exception>
        private void VerifyKeyExchangeAsServer(Stream outputStream, Stream inputStream)
        {
            // Generate a random challenge
            byte[] challenge = new byte[32];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(challenge);
            }

            // Send the challenge (unencrypted - it's random data)
            WriteLengthPrefixedBytes(outputStream, challenge);

            // Read the encrypted response from the client
            byte[] encryptedResponse = ReadLengthPrefixedBytes(inputStream);

            // Decrypt and verify the response matches the challenge
            byte[] decryptedResponse = Decrypt(encryptedResponse);
            if (!ConstantTimeEquals(challenge, decryptedResponse))
            {
                throw new CryptographicException("Key exchange verification failed: challenge response mismatch.");
            }
        }

        /// <summary>
        /// Verifies the key exchange as the client by receiving a challenge and sending an encrypted response.
        /// </summary>
        /// <param name="outputStream">The stream to send data.</param>
        /// <param name="inputStream">The stream to receive data.</param>
        private void VerifyKeyExchangeAsClient(Stream outputStream, Stream inputStream)
        {
            // Receive the challenge from the server
            byte[] challenge = ReadLengthPrefixedBytes(inputStream);

            // Encrypt the challenge and send it back
            byte[] encryptedResponse = Encrypt(challenge);
            WriteLengthPrefixedBytes(outputStream, encryptedResponse);
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
        /// Compares two byte arrays in constant time to prevent timing attacks.
        /// </summary>
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
        /// Securely zeros a byte array to clear sensitive data.
        /// </summary>
        private static void ZeroMemory(byte[] data)
        {
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = 0;
            }
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
        /// <exception cref="InvalidOperationException">Thrown if the required encryption or MAC key has not been established. This indicates that the key exchange
        /// has not been completed and the shared key has not been derived.</exception>
        private void ThrowIfKeyExchangeNotComplete()
        {
            if (_encryptionKey is null || _macKey is null)
            {
                throw new InvalidOperationException("Key exchange has not been completed. Call DeriveSharedKey first.");
            }
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
                ZeroMemory(_encryptionKey);
                _encryptionKey = null;
            }
            if (_macKey is not null)
            {
                ZeroMemory(_macKey);
                _macKey = null;
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
        private readonly ECDiffieHellman _ecdh;
#else
        private readonly ECDiffieHellmanCng _ecdh;
#endif

        /// <summary>
        /// The AES encryption key used for encrypting pipe communication.
        /// </summary>
        private byte[]? _encryptionKey;

        /// <summary>
        /// The MAC key used for HMAC-SHA256 authentication.
        /// </summary>
        private byte[]? _macKey;

        /// <summary>
        /// Specifies whether the instance has been disposed.
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// Specifies the size, in bytes, of the AES encryption key.
        /// </summary>
        /// <remarks>This constant is typically used to indicate a 256-bit key size for AES encryption
        /// algorithms. Ensure that cryptographic operations using this value are compatible with the expected key
        /// length.</remarks>
        private const int AesKeySize = 32;

        /// <summary>
        /// Specifies the size, in bytes, of the MAC key.
        /// </summary>
        private const int MacKeySize = 32;

        /// <summary>
        /// Represents the size, in bytes, of the initialization vector (IV) used for cryptographic operations.
        /// </summary>
        private const int IvSize = 16;

        /// <summary>
        /// Specifies the size, in bytes, of the message authentication code (MAC) produced by HMAC-SHA256.
        /// </summary>
        private const int MacSize = 32;
    }
}
