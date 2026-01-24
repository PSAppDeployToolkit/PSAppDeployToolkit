using System;
using System.Security.Cryptography;

namespace PSADT.Utilities
{
    /// <summary>
    /// Provides utility methods for cryptographic operations.
    /// </summary>
    /// <remarks>This class contains methods designed to assist with cryptographic tasks, such as generating
    /// cryptographically secure random values. It is intended for scenarios where enhanced security and
    /// unpredictability are required.</remarks>
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
            return new Guid(randomBytes);
        }
    }
}
