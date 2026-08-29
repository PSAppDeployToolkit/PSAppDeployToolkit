using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace PSADT.Interop.Tests.Polyfills
{
    /// <summary>
    /// Tests the char-separator String.Join polyfills: the array overload and the generic enumerable
    /// overload. Both are reachable from the toolkit, so both are covered; the array overload is listed
    /// explicitly in the polyfill set so that array call sites keep binding to it rather than being
    /// absorbed by the generic one.
    /// </summary>
    public sealed class StringJoinPolyfillTests
    {
        /// <summary>
        /// Verifies the array overload: separators appear only between elements, and null or empty
        /// elements contribute nothing while still occupying a position.
        /// </summary>
        /// <param name="separator">The separator character.</param>
        /// <param name="values">The values to join.</param>
        /// <param name="expected">The expected result.</param>
        [Theory]
        [InlineData(',', new[] { "a", "b", "c" }, "a,b,c")]
        [InlineData(',', new[] { "a" }, "a")]
        [InlineData(',', new string[0], "")]
        [InlineData(',', new[] { "", "" }, ",")]
        [InlineData(',', new[] { "a", null, "c" }, "a,,c")]
        [InlineData('\\', new[] { "x", "y" }, @"x\y")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "MA0109:Consider adding an overload with a Span<T> or Memory<T>", Justification = "The parameter shape is dictated by the InlineData cases under test.")]
        public void Join_Array_MatchesFrameworkBehaviour(char separator, string?[] values, string expected)
        {
            // Act
            string result = string.Join(separator, values);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Verifies the generic enumerable overload over a sequence that is not an array, which is the
        /// shape that cannot bind to the array overload.
        /// </summary>
        [Fact]
        public void Join_Enumerable_JoinsLazySequence()
        {
            // Arrange
            IEnumerable<string> values = new[] { "a", "b", "c" }.Where(static x => !string.Equals(x, "b", StringComparison.Ordinal));

            // Act
            string result = string.Join('-', values);

            // Assert
            Assert.Equal("a-c", result);
        }

        /// <summary>
        /// Verifies that the generic overload formats non-string elements through ToString, and that an
        /// empty sequence produces an empty string rather than a stray separator.
        /// </summary>
        [Fact]
        public void Join_Enumerable_FormatsNonStringElements()
        {
            // Assert
            Assert.Equal("1-2-3", string.Join('-', new List<int>([1, 2, 3])));
            Assert.Equal(string.Empty, string.Join('-', new List<int>()));
        }

        /// <summary>
        /// Verifies that a null element in a generic sequence contributes nothing rather than the text
        /// "null" or a thrown exception.
        /// </summary>
        [Fact]
        public void Join_Enumerable_TreatsNullElementsAsEmpty()
        {
            // Arrange
            List<string?> values = ["a", null, "c"];

            // Act
            string result = string.Join(',', values);

            // Assert
            Assert.Equal("a,,c", result);
        }

        /// <summary>
        /// Verifies that a null array or sequence is rejected with ArgumentNullException naming the
        /// parameter, which is what the framework overloads do.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "This is deliberate as part of unit testing.")]
        [Fact]
        public void Join_ThrowsOnNullValues()
        {
            // Arrange
            string?[] nullArray = null!;
            IEnumerable<string> nullSequence = null!;

            // Act & Assert
            Assert.Equal("value", Assert.Throws<ArgumentNullException>(() => string.Join(',', nullArray)).ParamName);
            Assert.Equal("values", Assert.Throws<ArgumentNullException>(() => string.Join(',', nullSequence)).ParamName);
        }
    }
}
