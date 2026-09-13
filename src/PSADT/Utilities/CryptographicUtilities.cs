using System;
using System.Security.Cryptography;
using System.Threading;

namespace PSADT.Utilities
{
    /// <summary>
    /// Provides utility methods for cryptographic operations.
    /// </summary>
    /// <remarks>This class contains methods designed to assist with cryptographic tasks, such as generating
    /// cryptographically secure random values and clearing the buffers that held them. It is intended for scenarios
    /// where enhanced security and unpredictability are required.</remarks>
    internal static class CryptographicUtilities
    {
        /// <summary>
        /// Generates a cryptographically secure random <see cref="Guid"/>.
        /// </summary>
        /// <remarks>This method uses a <see cref="RandomNumberGenerator"/>
        /// to ensure the generated <see cref="Guid"/> is based on high-quality random data, suitable for scenarios
        /// requiring enhanced security or unpredictability.</remarks>
        /// <returns>A <see cref="Guid"/> created using cryptographically secure random data.</returns>
        internal static Guid SecureNewGuid()
        {
            using RandomNumberGenerator rng = RandomNumberGenerator.Create();
            byte[] randomBytes = new byte[16]; rng.GetBytes(randomBytes);
            return new(randomBytes);
        }

        /// <summary>
        /// Overwrites a buffer that held unprotected data.
        /// </summary>
        /// <remarks>Callers hand plaintext to the serializer and the cipher in ordinary managed arrays, which the
        /// garbage collector will neither scrub nor keep still. Overwriting one as soon as it has served its purpose
        /// is the only control available over how long its contents remain legible in the process.</remarks>
        /// <param name="data">The buffer to overwrite. A null buffer is ignored.</param>
        internal static void SecureZeroMemory(byte[]? data)
        {
            if (data is null)
            {
                return;
            }
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = 0;
            }

            // Barrier stops the loop above being optimised away as a dead store.
            Thread.MemoryBarrier();
        }
    }
}
