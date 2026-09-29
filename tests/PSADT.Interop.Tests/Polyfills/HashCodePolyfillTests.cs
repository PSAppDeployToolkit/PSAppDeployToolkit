using System;
using Xunit;

namespace PSADT.Interop.Tests.Polyfills
{
    /// <summary>
    /// Tests the System.HashCode type polyfill: the static Combine overloads and the streaming Add / ToHashCode
    /// pair. On net472 these bind to the type PSADT.Interop generates; on net8.0 to the framework.
    /// </summary>
    /// <remarks>
    /// HashCode seeds itself once per process, so a hash is stable within a run but not across runs and not a fixed
    /// number. The assertions are therefore about the contract that holds either way - the same inputs hash the same,
    /// different inputs generally do not, and the order of inputs matters - rather than about any particular value.
    /// </remarks>
    public sealed class HashCodePolyfillTests
    {
        /// <summary>
        /// Verifies that Combine returns the same hash for the same inputs within a run, which is the whole point of it.
        /// </summary>
        [Fact]
        public void Combine_IsDeterministicForTheSameInputs()
        {
            // Assert
            Assert.Equal(HashCode.Combine(1, 2, 3), HashCode.Combine(1, 2, 3));
            Assert.Equal(HashCode.Combine("a", "b"), HashCode.Combine("a", "b"));
        }

        /// <summary>
        /// Verifies that Combine responds to both which values are combined and the order they are combined in, so it
        /// is not collapsing its inputs.
        /// </summary>
        [Fact]
        public void Combine_RespondsToValueAndOrder()
        {
            // Assert
            Assert.NotEqual(HashCode.Combine(1, 2), HashCode.Combine(1, 3));
            Assert.NotEqual(HashCode.Combine(1, 2), HashCode.Combine(2, 1));
        }

        /// <summary>
        /// Verifies that every Combine arity from one through eight produces a hash and does so deterministically.
        /// </summary>
        [Fact]
        public void Combine_AllAritiesAreDeterministic()
        {
            // Assert
            Assert.Equal(HashCode.Combine(1), HashCode.Combine(1));
            Assert.Equal(HashCode.Combine(1, 2), HashCode.Combine(1, 2));
            Assert.Equal(HashCode.Combine(1, 2, 3), HashCode.Combine(1, 2, 3));
            Assert.Equal(HashCode.Combine(1, 2, 3, 4), HashCode.Combine(1, 2, 3, 4));
            Assert.Equal(HashCode.Combine(1, 2, 3, 4, 5), HashCode.Combine(1, 2, 3, 4, 5));
            Assert.Equal(HashCode.Combine(1, 2, 3, 4, 5, 6), HashCode.Combine(1, 2, 3, 4, 5, 6));
            Assert.Equal(HashCode.Combine(1, 2, 3, 4, 5, 6, 7), HashCode.Combine(1, 2, 3, 4, 5, 6, 7));
            Assert.Equal(HashCode.Combine(1, 2, 3, 4, 5, 6, 7, 8), HashCode.Combine(1, 2, 3, 4, 5, 6, 7, 8));
        }

        /// <summary>
        /// Verifies that the streaming Add / ToHashCode pair is deterministic for the same sequence and, like Combine,
        /// sensitive to what is added.
        /// </summary>
        [Fact]
        public void AddThenToHashCode_IsDeterministicAndSensitive()
        {
            // Assert
            Assert.Equal(Build(1, 2, 3), Build(1, 2, 3));
            Assert.NotEqual(Build(1, 2, 3), Build(3, 2, 1));
        }

        /// <summary>
        /// Verifies that Add honours a supplied comparer, so two values a comparer treats as equal hash the same.
        /// </summary>
        [Fact]
        public void Add_WithComparer_UsesTheComparer()
        {
            // Arrange
            HashCode upper = new();
            upper.Add("VALUE", StringComparer.OrdinalIgnoreCase);
            HashCode lower = new();
            lower.Add("value", StringComparer.OrdinalIgnoreCase);

            // Assert
            Assert.Equal(upper.ToHashCode(), lower.ToHashCode());
        }

        /// <summary>
        /// Builds a hash by adding each value in turn, mirroring how the toolkit's equality types accumulate one.
        /// </summary>
        /// <param name="values">The values to add.</param>
        /// <returns>The finalized hash.</returns>
        private static int Build(params int[] values)
        {
            HashCode hash = new();
            foreach (int value in values)
            {
                hash.Add(value);
            }
            return hash.ToHashCode();
        }
    }
}
