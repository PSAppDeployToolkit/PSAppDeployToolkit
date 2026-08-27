using System;
#if !NET8_0_OR_GREATER
using System.Reflection;
#endif
using Xunit;

namespace PSADT.Interop.Tests.Polyfills
{
    /// <summary>
    /// Tests String.Replace(string, string, StringComparison). On net472 the call binds to the polyfill
    /// PSADT.Interop generates; on net8.0 it binds to the framework. Both legs run the same assertions,
    /// so the framework leg acts as the oracle for the polyfill.
    /// </summary>
    /// <remarks>
    /// This is the method behind Meziantou.Polyfill issue 303, where the scan position advanced by
    /// oldValue.Length instead of the real match length. Only ordinal comparisons are covered here;
    /// the culture-sensitive cases that exposed that bug need the collation engine pinned first, so
    /// they are deliberately left for the wider polyfill suite.
    /// </remarks>
    public sealed class StringReplacePolyfillTests
    {
        /// <summary>
        /// Verifies ordinal replacement over the cases that distinguish a correct scan from one that
        /// mis-advances: repeated matches, adjacent matches, a match at each boundary, and a
        /// replacement longer and shorter than what it replaces.
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
        [InlineData("abcabc", "abc", "", "")]
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
        /// Verifies that a null replacement removes the matched text rather than throwing, which is the
        /// documented behaviour of the framework overload.
        /// </summary>
        [Fact]
        public void Replace_Ordinal_TreatsNullReplacementAsRemoval()
        {
            // Act
            string result = "a-b-c".Replace("-", newValue: null, StringComparison.Ordinal);

            // Assert
            Assert.Equal("abc", result);
        }

        /// <summary>
        /// Verifies that an empty value to replace is rejected, since every position would match it.
        /// </summary>
        [Fact]
        public void Replace_Ordinal_ThrowsOnEmptyOldValue()
        {
            _ = Assert.Throws<ArgumentException>(static () => "abc".Replace(string.Empty, "x", StringComparison.Ordinal));
        }

        /// <summary>
        /// Verifies that a null value to replace is rejected.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "This is deliberate as part of unit testing.")]
        [Fact]
        public void Replace_Ordinal_ThrowsOnNullOldValue()
        {
            _ = Assert.Throws<ArgumentNullException>(static () => "abc".Replace(null!, "x", StringComparison.Ordinal));
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
