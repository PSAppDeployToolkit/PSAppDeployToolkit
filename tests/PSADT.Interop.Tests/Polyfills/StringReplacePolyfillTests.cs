using System;
#if !NET8_0_OR_GREATER
using System.Reflection;
#endif
using PSADT.Interop.Tests.TestHelpers;
using Xunit;

namespace PSADT.Interop.Tests.Polyfills
{
    /// <summary>
    /// Tests String.Replace(string, string, StringComparison). On net472 the call binds to the polyfill
    /// PSADT.Interop generates; on net8.0 it binds to the framework. Both legs run the same assertions,
    /// so a failure on net8.0 means the expectation is wrong and a failure on net472 alone means the
    /// polyfill is wrong.
    /// </summary>
    /// <remarks>
    /// This is the method behind Meziantou.Polyfill issue 303, where the scan position advanced by
    /// oldValue.Length rather than by the length of the region that actually matched. Ordinal
    /// comparisons cannot expose that, because there the two are always equal; the culture-sensitive
    /// paths below are where it went wrong, either overshooting the end of the string, swallowing
    /// characters, or leaving strays behind. Non-ASCII characters are declared as escaped constants and
    /// composed into the cases, because several of them are invisible in source.
    /// </remarks>
    public sealed class StringReplacePolyfillTests
    {
        /// <summary>
        /// U+00DF LATIN SMALL LETTER SHARP S. NLS collates this equal to "ss", so a search for "ss"
        /// matches one character and a search for this matches two.
        /// </summary>
        private const string SharpS = "\u00DF";

        /// <summary>
        /// U+00AD SOFT HYPHEN. Carries no collation weight, so it pads a matched region without
        /// contributing to it.
        /// </summary>
        private const string SoftHyphen = "\u00AD";

        /// <summary>
        /// U+0301 COMBINING ACUTE ACCENT, used to build a decomposed "e" with an acute accent.
        /// </summary>
        private const string CombiningAcute = "\u0301";

        /// <summary>
        /// U+00E9 LATIN SMALL LETTER E WITH ACUTE, the precomposed form of "e" plus U+0301.
        /// </summary>
        private const string EAcute = "\u00E9";

        /// <summary>
        /// U+0131 LATIN SMALL LETTER DOTLESS I, which Turkish casing pairs with "I".
        /// </summary>
        private const string DotlessI = "\u0131";

        /// <summary>
        /// Verifies ordinal replacement over the cases that distinguish a correct scan from one that
        /// mis-advances: repeated matches, adjacent matches, a match at each boundary, and replacements
        /// both longer and shorter than what they replace.
        /// </summary>
        /// <param name="value">The string to search.</param>
        /// <param name="oldValue">The value to replace.</param>
        /// <param name="newValue">The replacement value.</param>
        /// <param name="expected">The expected result.</param>
        [Theory]
        [InlineData("hello world", "o", "0", "hell0 w0rld")]
        [InlineData("aaa", "a", "b", "bbb")]
        [InlineData("aaaa", "aa", "b", "bb")]
        [InlineData("abab", "ab", "", "")]
        [InlineData("xabx", "ab", "cdef", "xcdefx")]
        [InlineData("abc", "abc", "x", "x")]
        [InlineData("banana", "ana", "X", "bXna")]
        [InlineData("hello", "z", "y", "hello")]
        [InlineData("", "a", "b", "")]
        public void Replace_Ordinal_MatchesFrameworkBehaviour(string value, string oldValue, string newValue, string expected)
        {
            // Act
            string result = value.Replace(oldValue, newValue, StringComparison.Ordinal);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Verifies ordinal case-insensitive replacement. Case folding never changes the length of the
        /// matched region, so this shares the fixed-length scan with the ordinal path.
        /// </summary>
        /// <param name="value">The string to search.</param>
        /// <param name="oldValue">The value to replace.</param>
        /// <param name="newValue">The replacement value.</param>
        /// <param name="expected">The expected result.</param>
        [Theory]
        [InlineData("Hello", "L", "_", "He__o")]
        [InlineData("HELLO", "hello", "x", "x")]
        [InlineData("aAaA", "A", "-", "----")]
        [InlineData("MiXeD", "xed", "!", "Mi!")]
        public void Replace_OrdinalIgnoreCase_MatchesFrameworkBehaviour(string value, string oldValue, string newValue, string expected)
        {
            // Act
            string result = value.Replace(oldValue, newValue, StringComparison.OrdinalIgnoreCase);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Verifies that the ordinal paths do not apply collation equivalence. This is the contrast that
        /// makes the culture-sensitive cases below meaningful rather than incidental.
        /// </summary>
        /// <param name="comparisonType">The ordinal comparison under test.</param>
        [Theory]
        [InlineData(StringComparison.Ordinal)]
        [InlineData(StringComparison.OrdinalIgnoreCase)]
        public void Replace_Ordinal_DoesNotApplyCollationEquivalence(StringComparison comparisonType)
        {
            // Act
            string result = ("Stra" + SharpS + "e").Replace("ss", "X", comparisonType);

            // Assert
            Assert.Equal("Stra" + SharpS + "e", result);
        }

        /// <summary>
        /// Verifies culture-sensitive replacement where the matched region is a different length from
        /// the value searched for. These are the cases issue 303 got wrong, and each one is chosen so a
        /// scan advancing by oldValue.Length produces an observably different answer.
        /// </summary>
        /// <param name="value">The string to search.</param>
        /// <param name="oldValue">The value to replace.</param>
        /// <param name="newValue">The replacement value.</param>
        /// <param name="expected">The expected result.</param>
        [Theory]
        [InlineData("Stra" + SharpS + "e", "ss", "X", "StraXe")]
        [InlineData(SharpS + "abc", "ss", "X", "Xabc")]
        [InlineData("abc" + SharpS, "ss", "X", "abcX")]
        [InlineData("Strasse", SharpS, "X", "StraXe")]
        [InlineData("ssabc", SharpS, "X", "Xabc")]
        [InlineData("abcss", SharpS, "X", "abcX")]
        [InlineData("a" + SoftHyphen + "b", "ab", "X", "X")]
        [InlineData("xa" + SoftHyphen + "bx", "ab", "X", "xXx")]
        [InlineData("ab", "a" + SoftHyphen + "b", "X", "X")]
        [InlineData("cafe" + CombiningAcute, EAcute, "X", "cafX")]
        [InlineData("caf" + EAcute, "e" + CombiningAcute, "X", "cafX")]
        public void Replace_InvariantCulture_HandlesVariableLengthMatches(string value, string oldValue, string newValue, string expected)
        {
            // Act
            string result = value.Replace(oldValue, newValue, StringComparison.InvariantCulture);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Verifies that every occurrence is replaced even when the matched regions differ in length
        /// from one another, which is where a mis-advancing scan drifts progressively further out of
        /// position.
        /// </summary>
        [Fact]
        public void Replace_InvariantCulture_ReplacesOccurrencesOfDifferingMatchLengths()
        {
            // Act
            string result = ("Stra" + SharpS + "e Strasse").Replace("ss", "X", StringComparison.InvariantCulture);

            // Assert
            Assert.Equal("StraXe StraXe", result);
        }

        /// <summary>
        /// Verifies that a match can be removed entirely under a culture-sensitive comparison, which
        /// runs the same scan with a zero-length replacement.
        /// </summary>
        [Fact]
        public void Replace_InvariantCulture_RemovesMatchWhenReplacementIsEmpty()
        {
            // Act
            string result = ("Stra" + SharpS + "e").Replace("ss", string.Empty, StringComparison.InvariantCulture);

            // Assert
            Assert.Equal("Strae", result);
        }

        /// <summary>
        /// Verifies culture-sensitive case-insensitive replacement, where case folding and collation
        /// equivalence apply together.
        /// </summary>
        /// <param name="value">The string to search.</param>
        /// <param name="oldValue">The value to replace.</param>
        /// <param name="newValue">The replacement value.</param>
        /// <param name="expected">The expected result.</param>
        [Theory]
        [InlineData("STRASSE", SharpS, "X", "STRAXE")]
        [InlineData("Stra" + SharpS + "e", "SS", "X", "StraXe")]
        [InlineData("Hello", "L", "_", "He__o")]
        public void Replace_InvariantCultureIgnoreCase_HandlesVariableLengthMatches(string value, string oldValue, string newValue, string expected)
        {
            // Act
            string result = value.Replace(oldValue, newValue, StringComparison.InvariantCultureIgnoreCase);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Verifies that a search value carrying no collation weight leaves the target alone. Every
        /// position matches such a value with a zero-length region, so a scan that does not stop would
        /// either loop forever or insert the replacement endlessly.
        /// </summary>
        /// <param name="comparisonType">The culture-sensitive comparison under test.</param>
        [Theory]
        [InlineData(StringComparison.InvariantCulture)]
        [InlineData(StringComparison.InvariantCultureIgnoreCase)]
        [InlineData(StringComparison.CurrentCulture)]
        [InlineData(StringComparison.CurrentCultureIgnoreCase)]
        public void Replace_CultureSensitive_LeavesTargetUnchangedForWeightlessSearchValue(StringComparison comparisonType)
        {
            // Act
            string result = "abc".Replace(SoftHyphen, "X", comparisonType);

            // Assert
            Assert.Equal("abc", result);
        }

        /// <summary>
        /// Verifies that a target with no match is returned unchanged under every comparison, covering
        /// the path where the scan finds nothing at all.
        /// </summary>
        /// <param name="comparisonType">The comparison under test.</param>
        [Theory]
        [InlineData(StringComparison.Ordinal)]
        [InlineData(StringComparison.OrdinalIgnoreCase)]
        [InlineData(StringComparison.InvariantCulture)]
        [InlineData(StringComparison.InvariantCultureIgnoreCase)]
        [InlineData(StringComparison.CurrentCulture)]
        [InlineData(StringComparison.CurrentCultureIgnoreCase)]
        public void Replace_AnyComparison_LeavesTargetUnchangedWhenNothingMatches(StringComparison comparisonType)
        {
            // Act
            string result = "abcdef".Replace("zzz", "X", comparisonType);

            // Assert
            Assert.Equal("abcdef", result);
        }

        /// <summary>
        /// Verifies that the current-culture comparisons read the thread's culture rather than the
        /// invariant culture, using a case where the two disagree. Turkish pairs "I" with U+0131 when
        /// ignoring case; the invariant culture does not.
        /// </summary>
        [Fact]
        public void Replace_CurrentCultureIgnoreCase_HonoursThreadCulture()
        {
            // Act
            string turkish;
            using (new CultureScope("tr-TR"))
            {
                turkish = "BIG".Replace(DotlessI, "X", StringComparison.CurrentCultureIgnoreCase);
            }

            string english;
            using (new CultureScope("en-US"))
            {
                english = "BIG".Replace(DotlessI, "X", StringComparison.CurrentCultureIgnoreCase);
            }

            // Assert
            Assert.Equal("BXG", turkish);
            Assert.Equal("BIG", english);
        }

        /// <summary>
        /// Verifies that the current-culture comparison applies collation equivalence the same way the
        /// invariant one does, so the culture plumbing does not silently fall back to ordinal.
        /// </summary>
        [Fact]
        public void Replace_CurrentCulture_AppliesCollationEquivalence()
        {
            // Act
            string result;
            using (new CultureScope("de-DE"))
            {
                result = ("Stra" + SharpS + "e").Replace("ss", "X", StringComparison.CurrentCulture);
            }

            // Assert
            Assert.Equal("StraXe", result);
        }

        /// <summary>
        /// Verifies that a null replacement removes the matched text rather than throwing, which is the
        /// documented behaviour of the framework overload.
        /// </summary>
        /// <param name="comparisonType">The comparison under test.</param>
        [Theory]
        [InlineData(StringComparison.Ordinal)]
        [InlineData(StringComparison.InvariantCulture)]
        public void Replace_TreatsNullReplacementAsRemoval(StringComparison comparisonType)
        {
            // Act
            string result = "a-b-c".Replace("-", newValue: null, comparisonType);

            // Assert
            Assert.Equal("abc", result);
        }

        /// <summary>
        /// Verifies that an empty value to replace is rejected, since every position would match it.
        /// </summary>
        /// <param name="comparisonType">The comparison under test.</param>
        [Theory]
        [InlineData(StringComparison.Ordinal)]
        [InlineData(StringComparison.InvariantCulture)]
        public void Replace_ThrowsOnEmptyOldValue(StringComparison comparisonType)
        {
            _ = Assert.Throws<ArgumentException>(() => "abc".Replace(string.Empty, "x", comparisonType));
        }

        /// <summary>
        /// Verifies that a null value to replace is rejected.
        /// </summary>
        /// <param name="comparisonType">The comparison under test.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "This is deliberate as part of unit testing.")]
        [Theory]
        [InlineData(StringComparison.Ordinal)]
        [InlineData(StringComparison.InvariantCulture)]
        public void Replace_ThrowsOnNullOldValue(StringComparison comparisonType)
        {
            _ = Assert.Throws<ArgumentNullException>(() => "abc".Replace(null!, "x", comparisonType));
        }

        /// <summary>
        /// Verifies that an unrecognised comparison is rejected rather than silently treated as one of
        /// the valid ones.
        /// </summary>
        [Fact]
        public void Replace_ThrowsOnUndefinedComparisonType()
        {
            _ = Assert.Throws<ArgumentException>(static () => "abc".Replace("b", "x", (StringComparison)42));
        }

#if !NET8_0_OR_GREATER
        /// <summary>
        /// Confirms the net472 leg really is exercising the shipped polyfill. .NET Framework has no
        /// three-argument Replace, so the calls above can only be binding to the PolyfillExtensions
        /// class PSADT.Interop generates, which this project reaches through InternalsVisibleTo. If
        /// that ever stops being true, the assertions above quietly become a test of the framework.
        /// </summary>
        [Fact]
        public void Replace_OnNetFramework_BindsToTheShippedPolyfill()
        {
            // Assert
            Assert.Null(typeof(string).GetMethod(nameof(string.Replace), [typeof(string), typeof(string), typeof(StringComparison)]));
            Assert.NotNull(typeof(PolyfillExtensions).GetMethod(nameof(string.Replace), BindingFlags.Public | BindingFlags.Static));
        }
#endif
    }
}
