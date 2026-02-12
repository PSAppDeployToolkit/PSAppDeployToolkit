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

        /// <summary>
        /// Generates a hash code from multiple parameters using a standard combining algorithm.
        /// </summary>
        /// <remarks>This method provides a consistent way to combine multiple values into a single hash code,
        /// useful for implementing <see cref="object.GetHashCode"/> in types with multiple properties.
        /// Null values contribute zero to the hash.</remarks>
        /// <param name="parameters">The values to combine into a hash code.</param>
        /// <returns>A combined hash code derived from all provided parameters.</returns>
        internal static int GenerateHashCode(params object?[] parameters)
        {
            int hash = 17;
            unchecked
            {
                foreach (object? param in parameters)
                {
                    hash = (hash * 31) + (param?.GetHashCode() ?? 0);
                }
            }
            return hash;
        }
    }
}
