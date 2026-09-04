using System;
using Xunit;

namespace PSADT.Interop.Tests.Polyfills
{
    /// <summary>
    /// Tests the character search and hashing polyfills: Contains(char, StringComparison),
    /// IndexOf(char, StringComparison), StartsWith(char), EndsWith(char) and
    /// GetHashCode(StringComparison). On net472 these bind to the polyfill; on net8.0 to the framework,
    /// which acts as the oracle.
    /// </summary>
    public sealed class StringSearchPolyfillTests
    {
        /// <summary>
        /// U+00DF LATIN SMALL LETTER SHARP S, which NLS collates equal to "ss".
        /// </summary>
        private const string SharpS = "\u00DF";

        /// <summary>
        /// U+00AD SOFT HYPHEN, a character with no collation weight.
        /// </summary>
        private const char SoftHyphen = '\u00AD';

        /// <summary>
        /// Verifies IndexOf over the ordinal and culture-sensitive paths, including the expansion case
        /// where a single character matches a two-character region.
        /// </summary>
        /// <param name="value">The string to search.</param>
        /// <param name="search">The character to find.</param>
        /// <param name="comparisonType">The comparison to use.</param>
        /// <param name="expected">The expected index.</param>
        [Theory]
        [InlineData("abcabc", 'b', StringComparison.Ordinal, 1)]
        [InlineData("abc", 'z', StringComparison.Ordinal, -1)]
        [InlineData("", 'a', StringComparison.Ordinal, -1)]
        [InlineData("ABC", 'b', StringComparison.Ordinal, -1)]
        [InlineData("ABC", 'b', StringComparison.OrdinalIgnoreCase, 1)]
        [InlineData("abc", 'b', StringComparison.InvariantCulture, 1)]
        [InlineData("ABC", 'b', StringComparison.InvariantCulture, -1)]
        [InlineData("ABC", 'b', StringComparison.InvariantCultureIgnoreCase, 1)]
        [InlineData("abc", 'b', StringComparison.CurrentCulture, 1)]
        [InlineData("ABC", 'b', StringComparison.CurrentCultureIgnoreCase, 1)]
        public void IndexOf_MatchesFrameworkBehaviour(string value, char search, StringComparison comparisonType, int expected)
        {
            // Act
            int result = value.IndexOf(search, comparisonType);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Verifies that a culture-sensitive character search finds a region whose length differs from
        /// one, which is the case the ordinal fast path cannot reach.
        /// </summary>
        [Fact]
        public void IndexOf_CultureSensitive_FindsExpandedRegion()
        {
            // Act
            int result = "Strasse".IndexOf(SharpS[0], StringComparison.InvariantCulture);

            // Assert
            Assert.Equal(4, result);
        }

        /// <summary>
        /// Verifies that the ordinal path does not apply collation equivalence, so the expansion above
        /// is genuinely a property of the culture-sensitive path.
        /// </summary>
        [Fact]
        public void IndexOf_Ordinal_DoesNotFindExpandedRegion()
        {
            // Act
            int result = "Strasse".IndexOf(SharpS[0], StringComparison.Ordinal);

            // Assert
            Assert.Equal(-1, result);
        }

        /// <summary>
        /// Verifies that a character with no collation weight is reported as present at the start of any
        /// string under a culture-sensitive comparison. This is surprising but is what the framework
        /// does, so it is pinned here rather than left to be discovered.
        /// </summary>
        [Fact]
        public void IndexOf_CultureSensitive_FindsWeightlessCharacterAtStart()
        {
            // Act
            int result = "abc".IndexOf(SoftHyphen, StringComparison.InvariantCulture);

            // Assert
            Assert.Equal(0, result);
        }

        /// <summary>
        /// Verifies that Contains agrees with IndexOf across both the ordinal and culture-sensitive
        /// paths, since it is defined in terms of it.
        /// </summary>
        /// <param name="value">The string to search.</param>
        /// <param name="search">The character to find.</param>
        /// <param name="comparisonType">The comparison to use.</param>
        /// <param name="expected">Whether the character is expected to be found.</param>
        [Theory]
        [InlineData("abc", 'b', StringComparison.Ordinal, true)]
        [InlineData("abc", 'z', StringComparison.Ordinal, false)]
        [InlineData("", 'a', StringComparison.Ordinal, false)]
        [InlineData("ABC", 'b', StringComparison.Ordinal, false)]
        [InlineData("ABC", 'b', StringComparison.OrdinalIgnoreCase, true)]
        [InlineData("Strasse", '\u00DF', StringComparison.InvariantCulture, true)]
        [InlineData("Strasse", '\u00DF', StringComparison.Ordinal, false)]
        [InlineData("ABC", 'b', StringComparison.InvariantCultureIgnoreCase, true)]
        public void Contains_MatchesFrameworkBehaviour(string value, char search, StringComparison comparisonType, bool expected)
        {
            // Act
            bool result = value.Contains(search, comparisonType);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Verifies that an unrecognised comparison is rejected by the search methods rather than being
        /// treated as one of the valid ones.
        /// </summary>
        [Fact]
        public void IndexOf_ThrowsOnUndefinedComparisonType()
        {
            _ = Assert.Throws<ArgumentException>(static () => "abc".IndexOf('b', (StringComparison)42));
        }

        /// <summary>
        /// Verifies the single-character prefix test, including the empty string, which must not index
        /// out of range.
        /// </summary>
        /// <param name="value">The string to test.</param>
        /// <param name="search">The character to look for.</param>
        /// <param name="expected">The expected result.</param>
        [Theory]
        [InlineData("abc", 'a', true)]
        [InlineData("abc", 'c', false)]
        [InlineData("a", 'a', true)]
        [InlineData("", 'a', false)]
        [InlineData("Abc", 'a', false)]
        public void StartsWith_MatchesFrameworkBehaviour(string value, char search, bool expected)
        {
            // Act
            bool result = value.StartsWith(search);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Verifies the single-character suffix test, including the empty string, which must not index
        /// out of range.
        /// </summary>
        /// <param name="value">The string to test.</param>
        /// <param name="search">The character to look for.</param>
        /// <param name="expected">The expected result.</param>
        [Theory]
        [InlineData("abc", 'c', true)]
        [InlineData("abc", 'a', false)]
        [InlineData("a", 'a', true)]
        [InlineData("", 'a', false)]
        [InlineData("abC", 'c', false)]
        public void EndsWith_MatchesFrameworkBehaviour(string value, char search, bool expected)
        {
            // Act
            bool result = value.EndsWith(search);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Verifies that strings a comparison treats as equal hash equally under that comparison, which
        /// is the contract a hash keyed on a comparison has to keep. Hash values themselves are not
        /// stable across processes or frameworks, so only the relationships are asserted.
        /// </summary>
        /// <param name="left">The first string.</param>
        /// <param name="right">The second string, equal to the first under the comparison.</param>
        /// <param name="comparisonType">The comparison to use.</param>
        [Theory]
        [InlineData("abc", "abc", StringComparison.Ordinal)]
        [InlineData("ABC", "abc", StringComparison.OrdinalIgnoreCase)]
        [InlineData("abc", "abc", StringComparison.InvariantCulture)]
        [InlineData("ABC", "abc", StringComparison.InvariantCultureIgnoreCase)]
        [InlineData("Stra\u00DFe", "Strasse", StringComparison.InvariantCulture)]
        [InlineData("STRASSE", "Stra\u00DFe", StringComparison.InvariantCultureIgnoreCase)]
        [InlineData("abc", "abc", StringComparison.CurrentCulture)]
        [InlineData("ABC", "abc", StringComparison.CurrentCultureIgnoreCase)]
        public void GetHashCode_EqualStringsHashEqually(string left, string right, StringComparison comparisonType)
        {
            // Act
            int leftHash = left.GetHashCode(comparisonType);
            int rightHash = right.GetHashCode(comparisonType);

            // Assert
            Assert.Equal(leftHash, rightHash);
        }

        /// <summary>
        /// Verifies that an unrecognised comparison is rejected rather than silently hashing under one
        /// of the valid ones.
        /// </summary>
        [Fact]
        public void GetHashCode_ThrowsOnUndefinedComparisonType()
        {
            _ = Assert.Throws<ArgumentException>(static () => "abc".GetHashCode((StringComparison)42));
        }
    }
}
