using System;
using System.Collections.Generic;
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
            return new(randomBytes);
        }

        /// <summary>
        /// Generates a hash code from multiple parameters using a standard combining algorithm.
        /// </summary>
        /// <remarks>This method provides a consistent way to combine multiple values into a single hash code,
        /// useful for implementing <see cref="object.GetHashCode"/> in types with multiple properties.
        /// Null values contribute zero to the hash.</remarks>
        /// <param name="parameters">The values to combine into a hash code.</param>
        /// <returns>A combined hash code derived from all provided parameters.</returns>
        internal static int GenerateHashCode(params IReadOnlyList<object?> parameters)
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

        /// <summary>
        /// Generates a hash code from a sequence of items using the same combining algorithm.
        /// </summary>
        /// <remarks>The sequence is walked as it is, rather than being copied into an array of objects first, so a
        /// caller holding a collection of value types hashes it without boxing every element. Null items contribute
        /// zero to the hash, as they do for the other overload.</remarks>
        /// <typeparam name="T">The type of the items.</typeparam>
        /// <param name="items">The items to combine into a hash code.</param>
        /// <param name="comparer">The comparer to take each item's hash code from.</param>
        /// <returns>A combined hash code derived from every item in the sequence.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="items"/> or <paramref name="comparer"/> is null.</exception>
        internal static int GenerateHashCode<T>(IEnumerable<T> items, IEqualityComparer<T> comparer)
        {
            ArgumentNullException.ThrowIfNull(items);
            ArgumentNullException.ThrowIfNull(comparer);
            int hash = 17;
            unchecked
            {
                foreach (T item in items)
                {
                    hash = (hash * 31) + (item is not null ? comparer.GetHashCode(item) : 0);
                }
            }
            return hash;
        }
    }
}
