using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using PSADT.Utilities;

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
    /// Instances must complete the key exchange via <see cref="PerformKeyExchangeBlocking"/>
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
        /// Performs the key exchange with the remote party using the role-specific protocol, on the calling thread,
        /// blocking at each step until the remote party has answered.
        /// </summary>
        /// <param name="outputStream">The stream to send data to the remote party.</param>
        /// <param name="inputStream">The stream to receive data from the remote party.</param>
        internal abstract void PerformKeyExchangeBlocking(Stream outputStream, Stream inputStream);

        /// <summary>
        /// Performs the key exchange on a thread of its own, for a wait that lasts until the remote party is up and answering.
        /// </summary>
        /// <remarks>An anonymous pipe has no overlapped mode, so an asynchronous exchange would only park a thread-pool thread in
        /// ReadFile at each step until the answer arrived. A dedicated thread costs the same and starves nothing.</remarks>
        /// <param name="outputStream">The stream to send data to the remote party.</param>
        /// <param name="inputStream">The stream to receive data from the remote party.</param>
        /// <returns>A task that completes once the exchange has.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="outputStream"/> or <paramref name="inputStream"/> is null.</exception>
        internal Task PerformKeyExchangeOnOwnThreadAsync(Stream outputStream, Stream inputStream)
        {
            ArgumentNullException.ThrowIfNull(outputStream);
            ArgumentNullException.ThrowIfNull(inputStream);
            return Task.Factory.StartNew(() => PerformKeyExchangeBlocking(outputStream, inputStream), CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        /// <summary>
        /// Reads and decrypts a message on the calling thread, blocking until all of it has arrived.
        /// </summary>
        /// <param name="stream">The stream to read the framed, encrypted message from.</param>
        /// <returns>The decrypted message.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="stream"/> is null.</exception>
        internal byte[] ReadEncryptedBlocking(Stream stream)
        {
            // Read and decrypt.
            ArgumentNullException.ThrowIfNull(stream);
            return Decrypt(ReadLengthPrefixedBytesBlocking(stream));
        }

        /// <summary>
        /// Reads and decrypts a message on a thread of its own, for a wait that may last as long as the far end takes to answer.
        /// </summary>
        /// <remarks>An anonymous pipe has no overlapped mode, so an asynchronous read would only park a thread-pool thread in
        /// ReadFile until the message arrives. A dedicated thread costs the same and starves nothing.</remarks>
        /// <param name="stream">The stream to read the framed, encrypted message from.</param>
        /// <returns>A task that completes with the decrypted message.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="stream"/> is null.</exception>
        internal Task<byte[]> ReadEncryptedOnOwnThreadAsync(Stream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);
            return Task.Factory.StartNew(() => ReadEncryptedBlocking(stream), CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        /// <summary>
        /// Writes encrypted data to the stream.
        /// </summary>
        /// <param name="stream">The output stream.</param>
        /// <param name="plaintext">The plaintext bytes to encrypt and write. Left as it was found; whoever built it
        /// overwrites it once this returns.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="stream"/> or <paramref name="plaintext"/> is null.</exception>
        internal ValueTask WriteEncryptedAsync(Stream stream, byte[] plaintext)
        {
            // Encrypt and write.
            ArgumentNullException.ThrowIfNull(stream);
            ArgumentNullException.ThrowIfNull(plaintext);
            return WriteLengthPrefixedBytesAsync(stream, Encrypt(plaintext));
        }

        /// <summary>
        /// Reads a length-prefixed message on the calling thread, blocking until all of it has arrived.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <returns>The message, without its length prefix.</returns>
        /// <exception cref="EndOfStreamException">Thrown if the stream ends before the prefix or the message has been read in full.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Critical Code Smell", "S2302:\"nameof\" should be used", Justification = "This is a false positive.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0045:Do not use blocking calls, even when the calling method must become async", Justification = "The read is meant to block: the anonymous pipes this carries have no overlapped mode, so the caller gives it a thread of its own rather than park one of the pool's.")]
        private protected static byte[] ReadLengthPrefixedBytesBlocking(Stream stream)
        {
            // Read the 4-byte length prefix
            byte[] lengthBytes = new byte[4]; int bytesRead = 0;
            while (bytesRead < 4)
            {
                int read = stream.Read(lengthBytes, bytesRead, 4 - bytesRead);
                if (read is 0)
                {
                    throw new EndOfStreamException("Unexpected end of stream while reading length prefix.");
                }
                bytesRead += read;
            }

            // Read the data
            byte[] data = new byte[ValidateLengthPrefix(lengthBytes)];
            bytesRead = 0;
            while (bytesRead < data.Length)
            {
                int read = stream.Read(data, bytesRead, data.Length - bytesRead);
                if (read is 0)
                {
                    throw new EndOfStreamException("Unexpected end of stream while reading data.");
                }
                bytesRead += read;
            }
            return data;
        }

        /// <summary>
        /// Checks a length prefix before anything is allocated for it, since it arrives from a stream this end does not control.
        /// </summary>
        /// <param name="lengthBytes">The four bytes of the prefix.</param>
        /// <returns>The length the prefix declares.</returns>
        /// <exception cref="InvalidDataException">Thrown if the prefix is not a positive length within the size allowed.</exception>
        private static int ValidateLengthPrefix(byte[] lengthBytes)
        {
            int length = BitConverter.ToInt32(lengthBytes, 0);
            return length <= 0
                ? throw new InvalidDataException("Invalid length prefix: negative or zero value.")
                : length > MaxMessageSize
                ? throw new InvalidDataException($"Message size {length.ToString(CultureInfo.InvariantCulture)} exceeds maximum allowed size of {MaxMessageSize} bytes.")
                : length;
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
        /// Writes a length-prefixed byte array to the stream on the calling thread, blocking until it has been written.
        /// </summary>
        /// <param name="stream">The output stream.</param>
        /// <param name="data">The data to write.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0045:Do not use blocking calls, even when the calling method must become async", Justification = "The write is meant to block: it is one step of a key exchange run on a thread of its own, as the anonymous pipes it crosses have no overlapped mode.")]
        private protected static void WriteLengthPrefixedBytesBlocking(Stream stream, byte[] data)
        {
            byte[] lengthBytes = BitConverter.GetBytes(data.Length);
            stream.Write(lengthBytes, 0, 4);
            stream.Write(data, 0, data.Length);
            stream.Flush();
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
            // Build CNG EccPublicBlob: BCRYPT_ECCKEY_BLOB header (8 bytes) + X + Y. The curve is fixed at
            // construction, so the sizes are the same constants the far end's blob is checked against.
            ECParameters ecParams = _ecdh.ExportParameters(includePrivateParameters: false);
            byte[] blob = new byte[EccPublicBlobSize];
            // ECDH_PUBLIC_P256 magic, written from the same constant the far end's blob is checked against
            BitConverter.GetBytes(EcdhPublicP256Magic).CopyTo(blob, 0);
            // Key length in bytes
            blob[4] = P256CoordinateSize; blob[5] = 0; blob[6] = 0; blob[7] = 0;
            Buffer.BlockCopy(ecParams.Q.X!, 0, blob, 8, P256CoordinateSize);
            Buffer.BlockCopy(ecParams.Q.Y!, 0, blob, 8 + P256CoordinateSize, P256CoordinateSize);
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
        /// <exception cref="InvalidDataException">Thrown if <paramref name="remotePublicKey"/> is not a P-256 public key blob.</exception>
        private protected void DeriveSharedKey(byte[] remotePublicKey)
        {
            // Verify parameters and state.
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(remotePublicKey);
            if (_encryptionKey is not null)
            {
                throw new InvalidOperationException("Key exchange has already been completed.");
            }

            // Remote key is a CNG EccPublicBlob: an 8-byte header of a magic and a declared size, then X and Y.
            // Nothing has authenticated the far party, so all three are checked before any is used to bound a
            // copy, length first. The magic is read only by the net472 import, so both targets check it here.
            if (remotePublicKey.Length != EccPublicBlobSize)
            {
                throw new InvalidDataException($"The remote public key is {remotePublicKey.Length.ToString(CultureInfo.InvariantCulture)} bytes, but a P-256 public key blob is {EccPublicBlobSize.ToString(CultureInfo.InvariantCulture)} bytes.");
            }
            int declaredMagic = BitConverter.ToInt32(remotePublicKey, 0);
            if (declaredMagic != EcdhPublicP256Magic)
            {
                throw new InvalidDataException($"The remote public key opens with 0x{declaredMagic.ToString("X8", CultureInfo.InvariantCulture)}, but a P-256 public key blob opens with 0x{EcdhPublicP256Magic.ToString("X8", CultureInfo.InvariantCulture)}.");
            }
            int declaredKeySize = BitConverter.ToInt32(remotePublicKey, 4);
            if (declaredKeySize != P256CoordinateSize)
            {
                throw new InvalidDataException($"The remote public key declares a coordinate size of {declaredKeySize.ToString(CultureInfo.InvariantCulture)} bytes, but the P-256 curve requires {P256CoordinateSize.ToString(CultureInfo.InvariantCulture)}.");
            }

            // Import the remote public key and derive shared secret
#if NET8_0_OR_GREATER
            byte[] x = new byte[P256CoordinateSize];
            byte[] y = new byte[P256CoordinateSize];
            Buffer.BlockCopy(remotePublicKey, 8, x, 0, P256CoordinateSize);
            Buffer.BlockCopy(remotePublicKey, 8 + P256CoordinateSize, y, 0, P256CoordinateSize);
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
            CryptographicUtilities.SecureZeroMemory(sharedSecret);
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
                CryptographicUtilities.SecureZeroMemory(prk);
            }
            return output;
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
            using (_ecdh)
            {
                if (_encryptionKey is not null)
                {
                    CryptographicUtilities.SecureZeroMemory(_encryptionKey);
                    _encryptionKey = null;
                }
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

        /// <summary>
        /// Specifies the size, in bytes, of a single P-256 public key coordinate.
        /// </summary>
        /// <remarks>
        /// The curve is fixed at P-256 on both sides of the exchange, so this is the only size either party
        /// may declare for the X and Y coordinates of a public key blob.
        /// </remarks>
        private const int P256CoordinateSize = 32;

        /// <summary>
        /// Specifies the size, in bytes, of a CNG EccPublicBlob carrying a P-256 public key.
        /// </summary>
        /// <remarks>
        /// A BCRYPT_ECCKEY_BLOB header of eight bytes - a four byte magic and a four byte coordinate size -
        /// followed by the X and Y coordinates.
        /// </remarks>
        private const int EccPublicBlobSize = 8 + (P256CoordinateSize << 1);

        /// <summary>
        /// Specifies the magic that opens a CNG EccPublicBlob carrying a P-256 public key.
        /// </summary>
        /// <remarks>
        /// BCRYPT_ECDH_PUBLIC_P256_MAGIC, being the characters <c language="csharp">ECK1</c> read as a little-endian integer.
        /// </remarks>
        private const int EcdhPublicP256Magic = 0x314B4345;
    }
}
