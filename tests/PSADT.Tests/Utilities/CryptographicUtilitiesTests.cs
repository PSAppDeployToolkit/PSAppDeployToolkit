using System;
using System.Collections.Generic;
using PSADT.Utilities;
using Xunit;

namespace PSADT.Tests.Utilities
{
    /// <summary>
    /// Tests the cryptographically-sourced identifier and the buffer scrubbing beside it.
    /// </summary>
    /// <remarks>
    /// The identifier matters because it names the pipe the token broker connects over, so a predictable
    /// one would let another process on the machine answer in its place.
    /// </remarks>
    public sealed class CryptographicUtilitiesTests
    {
        /// <summary>
        /// Verifies that a generated identifier is never the empty one, which is what a caller would get
        /// if the random bytes were never written.
        /// </summary>
        [Fact]
        public void SecureNewGuid_IsNeverEmpty()
        {
            for (int i = 0; i < 64; i++)
            {
                Assert.NotEqual(Guid.Empty, CryptographicUtilities.SecureNewGuid());
            }
        }

        /// <summary>
        /// Verifies that successive identifiers differ, which rules out a generator that returns the
        /// same buffer each time.
        /// </summary>
        [Fact]
        public void SecureNewGuid_DoesNotRepeat()
        {
            // Arrange
            HashSet<Guid> generated = [];

            // Act & Assert
            for (int i = 0; i < 256; i++)
            {
                Assert.True(generated.Add(CryptographicUtilities.SecureNewGuid()), "SecureNewGuid returned a value it had already returned.");
            }
        }

        /// <summary>
        /// Verifies that the whole identifier is populated rather than only part of the buffer, by
        /// checking that every byte position varies across a sample.
        /// </summary>
        /// <remarks>
        /// A generator that filled only the first few bytes would still pass the uniqueness test above,
        /// so this looks at each position independently. With 256 samples the chance of a genuinely
        /// random position holding one constant value throughout is vanishingly small.
        /// </remarks>
        [Fact]
        public void SecureNewGuid_PopulatesEveryBytePosition()
        {
            // Arrange
            HashSet<byte>[] seen = new HashSet<byte>[16];
            for (int i = 0; i < seen.Length; i++)
            {
                seen[i] = [];
            }

            // Act
            for (int sample = 0; sample < 256; sample++)
            {
                byte[] bytes = CryptographicUtilities.SecureNewGuid().ToByteArray();
                for (int i = 0; i < bytes.Length; i++)
                {
                    _ = seen[i].Add(bytes[i]);
                }
            }

            // Assert
            for (int i = 0; i < seen.Length; i++)
            {
                Assert.True(seen[i].Count > 1, $"Byte {i.ToString(System.Globalization.CultureInfo.InvariantCulture)} of the identifier never varied.");
            }
        }
    }
}
