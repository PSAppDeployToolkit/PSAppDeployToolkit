using System;
using Xunit;

namespace PSADT.Interop.Tests.Polyfills
{
    /// <summary>
    /// Tests StringComparer.FromComparison. The contract is that each StringComparison maps onto the
    /// comparer with matching semantics, so the mapping is asserted by behaviour rather than by identity;
    /// only the ordinal comparers are documented singletons.
    /// </summary>
    public sealed class StringComparerPolyfillTests
    {
        /// <summary>
        /// Verifies that the ordinal comparisons map onto the corresponding singleton comparers.
        /// </summary>
        [Fact]
        public void FromComparison_MapsOrdinalComparisonsToSingletons()
        {
            // Assert
            Assert.Same(StringComparer.Ordinal, StringComparer.FromComparison(StringComparison.Ordinal));
            Assert.Same(StringComparer.OrdinalIgnoreCase, StringComparer.FromComparison(StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Verifies that each comparison maps onto a comparer with matching case sensitivity, which is
        /// the property callers depend on when they pass a comparison through to a dictionary.
        /// </summary>
        /// <param name="comparisonType">The comparison to map.</param>
        /// <param name="expectedEqual">Whether "ABC" and "abc" should compare equal under it.</param>
        [Theory]
        [InlineData(StringComparison.Ordinal, false)]
        [InlineData(StringComparison.OrdinalIgnoreCase, true)]
        [InlineData(StringComparison.InvariantCulture, false)]
        [InlineData(StringComparison.InvariantCultureIgnoreCase, true)]
        [InlineData(StringComparison.CurrentCulture, false)]
        [InlineData(StringComparison.CurrentCultureIgnoreCase, true)]
        public void FromComparison_PreservesCaseSensitivity(StringComparison comparisonType, bool expectedEqual)
        {
            // Act
            StringComparer comparer = StringComparer.FromComparison(comparisonType);

            // Assert
            Assert.Equal(expectedEqual, comparer.Equals("ABC", "abc"));
        }

        /// <summary>
        /// Verifies that the culture-sensitive comparisons map onto comparers that apply collation
        /// equivalence, distinguishing them from the ordinal ones by more than case handling.
        /// </summary>
        /// <param name="comparisonType">The culture-sensitive comparison to map.</param>
        [Theory]
        [InlineData(StringComparison.InvariantCulture)]
        [InlineData(StringComparison.InvariantCultureIgnoreCase)]
        [InlineData(StringComparison.CurrentCulture)]
        [InlineData(StringComparison.CurrentCultureIgnoreCase)]
        public void FromComparison_MapsCultureComparisonsToCollatingComparers(StringComparison comparisonType)
        {
            // Act
            StringComparer comparer = StringComparer.FromComparison(comparisonType);

            // Assert
            Assert.True(comparer.Equals("Stra\u00DFe", "Strasse"));
        }

        /// <summary>
        /// Verifies that the ordinal comparisons do not apply collation equivalence, which is the
        /// contrast that makes the previous assertion meaningful.
        /// </summary>
        /// <param name="comparisonType">The ordinal comparison to map.</param>
        [Theory]
        [InlineData(StringComparison.Ordinal)]
        [InlineData(StringComparison.OrdinalIgnoreCase)]
        public void FromComparison_MapsOrdinalComparisonsToNonCollatingComparers(StringComparison comparisonType)
        {
            // Act
            StringComparer comparer = StringComparer.FromComparison(comparisonType);

            // Assert
            Assert.False(comparer.Equals("Stra\u00DFe", "Strasse"));
        }

        /// <summary>
        /// Verifies that an unrecognised comparison is rejected rather than mapped onto a default.
        /// </summary>
        [Fact]
        public void FromComparison_ThrowsOnUndefinedComparisonType()
        {
            _ = Assert.Throws<ArgumentException>(static () => StringComparer.FromComparison((StringComparison)42));
        }
    }
}
