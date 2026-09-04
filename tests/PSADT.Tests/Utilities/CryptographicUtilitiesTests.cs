using System;
using System.Collections.Generic;
using PSADT.Utilities;
using Xunit;

namespace PSADT.Tests.Utilities
{
    /// <summary>
    /// Tests the cryptographically-sourced identifier and the hash combiner.
    /// </summary>
    /// <remarks>
    /// The identifier matters because it names the pipe the token broker connects over, so a predictable
    /// one would let another process on the machine answer in its place. The hash combiner is tested
    /// against its arithmetic rather than against itself: the seed and multiplier are part of the
    /// contract for anything that persists a hash, so the expected values below are worked out by hand.
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

        /// <summary>
        /// Verifies the arithmetic of the combiner: a seed of seventeen, multiplied by thirty-one and
        /// added to each element's own hash in turn.
        /// </summary>
        /// <param name="expected">The hash the inputs should combine to.</param>
        /// <param name="values">The values to combine.</param>
        [Theory]
        [MemberData(nameof(HashCodeCases))]
        public void GenerateHashCode_FollowsTheDocumentedArithmetic(int expected, object?[] values)
        {
            Assert.Equal(expected, CryptographicUtilities.GenerateHashCode(values));
        }

        /// <summary>
        /// Inputs paired with the hash the documented arithmetic produces for them.
        /// </summary>
        /// <remarks>
        /// Supplied through a member rather than inline because the method under test takes a parameter
        /// collection, and an inline case ending in a null is ambiguous about whether the null is the
        /// collection or its only element.
        /// </remarks>
        public static TheoryData<int, object?[]> HashCodeCases => new()
        {
            // Seed alone, with nothing to combine into it.
            { 17, [] },

            // 17 * 31 + 0, a null contributing zero.
            { 527, [null] },

            // 17 * 31 + 1.
            { 528, [1] },

            // 528 * 31 + 2.
            { 16_370, [1, 2] },

            // 529 * 31 + 1, the same pair the other way round.
            { 16_400, [2, 1] },

            // 16370 * 31 + 3.
            { 507_473, [1, 2, 3] },
        };

        /// <summary>
        /// Verifies that combining is order sensitive, since a caller using this to key a cache relies on
        /// two differently-ordered inputs being distinguishable.
        /// </summary>
        [Fact]
        public void GenerateHashCode_IsOrderSensitive()
        {
            Assert.NotEqual(
                CryptographicUtilities.GenerateHashCode(1, 2, 3),
                CryptographicUtilities.GenerateHashCode(3, 2, 1));
        }

        /// <summary>
        /// Verifies that the same inputs always combine to the same value within a run, which is the
        /// minimum a hash has to offer.
        /// </summary>
        [Fact]
        public void GenerateHashCode_IsRepeatable()
        {
            // Arrange
            object?[] values = ["text", 42, null, Guid.Empty, true];

            // Act & Assert
            Assert.Equal(
                CryptographicUtilities.GenerateHashCode(values),
                CryptographicUtilities.GenerateHashCode(values));
        }

        /// <summary>
        /// Verifies that a null element contributes zero rather than throwing, so a record with unset
        /// members can still be hashed.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "S3878:Remove this array creation and simply pass the elements.", Justification = "Required for the test case.")]
        [Fact]
        public void GenerateHashCode_TreatsANullElementAsZero()
        {
            Assert.Equal(
                CryptographicUtilities.GenerateHashCode(0),
                CryptographicUtilities.GenerateHashCode([null]));
        }
    }
}
