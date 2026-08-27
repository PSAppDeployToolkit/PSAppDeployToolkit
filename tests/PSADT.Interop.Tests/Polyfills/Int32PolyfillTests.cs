using System;
using System.Globalization;
using Xunit;

namespace PSADT.Interop.Tests.Polyfills
{
    /// <summary>
    /// Tests int.TryParse(ReadOnlySpan&lt;char&gt;, IFormatProvider, out int), the only TryParse overload
    /// the toolkit uses. The polyfill parses NumberStyles.Integer, so the styles that are and are not
    /// permitted matter as much as the values themselves.
    /// </summary>
    public sealed class Int32PolyfillTests
    {
        /// <summary>
        /// Verifies the values and formats NumberStyles.Integer accepts: surrounding whitespace and a
        /// leading sign, at both ends of the range.
        /// </summary>
        /// <param name="text">The text to parse.</param>
        /// <param name="expected">The expected parsed value.</param>
        [Theory]
        [InlineData("123", 123)]
        [InlineData("  123  ", 123)]
        [InlineData("-123", -123)]
        [InlineData("+123", 123)]
        [InlineData("0", 0)]
        [InlineData("2147483647", int.MaxValue)]
        [InlineData("-2147483648", int.MinValue)]
        public void TryParse_AcceptsIntegerStyles(string text, int expected)
        {
            // Act
            bool parsed = int.TryParse(text.AsSpan(), CultureInfo.InvariantCulture, out int result);

            // Assert
            Assert.True(parsed);
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Verifies the formats NumberStyles.Integer rejects, and that the out parameter is left at zero
        /// on failure rather than holding a partial result.
        /// </summary>
        /// <param name="text">The text to parse.</param>
        [Theory]
        [InlineData("")]
        [InlineData("abc")]
        [InlineData("1,234")]
        [InlineData("0x1F")]
        [InlineData("1.5")]
        [InlineData("(123)")]
        [InlineData("2147483648")]
        [InlineData("-2147483649")]
        [InlineData("1 2")]
        public void TryParse_RejectsOtherStylesAndOutOfRange(string text)
        {
            // Act
            bool parsed = int.TryParse(text.AsSpan(), CultureInfo.InvariantCulture, out int result);

            // Assert
            Assert.False(parsed);
            Assert.Equal(0, result);
        }

        /// <summary>
        /// Verifies that parsing reads only the span it is given, so a sliced span is not affected by the
        /// characters around it.
        /// </summary>
        [Fact]
        public void TryParse_ReadsOnlyTheGivenSlice()
        {
            // Act
            bool parsed = int.TryParse("x123y".AsSpan(1, 3), CultureInfo.InvariantCulture, out int result);

            // Assert
            Assert.True(parsed);
            Assert.Equal(123, result);
        }

        /// <summary>
        /// Verifies that the supplied provider is honoured rather than the current culture, using a
        /// format with a negative sign no real culture uses so the result cannot be a coincidence.
        /// </summary>
        [Fact]
        public void TryParse_HonoursSuppliedProvider()
        {
            // Arrange
            NumberFormatInfo format = new() { NegativeSign = "!" };

            // Act
            bool parsed = int.TryParse("!42".AsSpan(), format, out int result);

            // Assert
            Assert.True(parsed);
            Assert.Equal(-42, result);
        }

        /// <summary>
        /// Verifies that an empty span is rejected rather than parsed as zero.
        /// </summary>
        [Fact]
        public void TryParse_RejectsEmptySpan()
        {
            // Act
            bool parsed = int.TryParse(string.Empty.AsSpan(), CultureInfo.InvariantCulture, out int result);

            // Assert
            Assert.False(parsed);
            Assert.Equal(0, result);
        }
    }
}
