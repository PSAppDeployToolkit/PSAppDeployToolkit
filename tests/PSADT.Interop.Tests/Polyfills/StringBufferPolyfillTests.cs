using System;
using System.Globalization;
using Xunit;

namespace PSADT.Interop.Tests.Polyfills
{
    /// <summary>
    /// Tests the span-copy and interpolated-creation polyfills: CopyTo(Span&lt;char&gt;),
    /// TryCopyTo(Span&lt;char&gt;) and Create(IFormatProvider, ref DefaultInterpolatedStringHandler).
    /// The last of these also exercises the DefaultInterpolatedStringHandler type polyfill, which every
    /// interpolated string in PSADT.Interop compiles against on net472.
    /// </summary>
    public sealed class StringBufferPolyfillTests
    {
        /// <summary>
        /// Verifies that a string copies into an exactly sized destination.
        /// </summary>
        [Fact]
        public void CopyTo_FillsExactlySizedDestination()
        {
            // Arrange
            Span<char> destination = new char[3];

            // Act
            "abc".CopyTo(destination);

            // Assert
            Assert.Equal("abc", destination.ToString());
        }

        /// <summary>
        /// Verifies that a larger destination keeps whatever was beyond the copied region, so the copy
        /// does not clear the remainder.
        /// </summary>
        [Fact]
        public void CopyTo_LeavesRemainderOfLargerDestinationUntouched()
        {
            // Arrange
            Span<char> destination = new char[5];
            destination.Fill('.');

            // Act
            "ab".CopyTo(destination);

            // Assert
            Assert.Equal("ab...", destination.ToString());
        }

        /// <summary>
        /// Verifies that a destination too small to hold the string is rejected rather than truncating.
        /// </summary>
        [Fact]
        public void CopyTo_ThrowsWhenDestinationTooSmall()
        {
            _ = Assert.Throws<ArgumentException>(static () =>
            {
                Span<char> destination = new char[2];
                "abc".CopyTo(destination);
            });
        }

        /// <summary>
        /// Verifies that an empty string copies successfully into an empty destination.
        /// </summary>
        [Fact]
        public void CopyTo_AcceptsEmptyStringAndEmptyDestination()
        {
            // Act
            string.Empty.CopyTo([]);

            // Assert
            Assert.True(true);
        }

        /// <summary>
        /// Verifies that the try variant reports success and copies when the destination fits.
        /// </summary>
        [Fact]
        public void TryCopyTo_CopiesWhenDestinationFits()
        {
            // Arrange
            Span<char> destination = new char[3];

            // Act
            bool copied = "abc".TryCopyTo(destination);

            // Assert
            Assert.True(copied);
            Assert.Equal("abc", destination.ToString());
        }

        /// <summary>
        /// Verifies that the try variant reports failure and leaves the destination alone rather than
        /// partially filling it.
        /// </summary>
        [Fact]
        public void TryCopyTo_LeavesDestinationUntouchedWhenTooSmall()
        {
            // Arrange
            Span<char> destination = new char[2];
            destination.Fill('.');

            // Act
            bool copied = "abc".TryCopyTo(destination);

            // Assert
            Assert.False(copied);
            Assert.Equal("..", destination.ToString());
        }

        /// <summary>
        /// Verifies that the provider passed to Create reaches the interpolation, using a culture whose
        /// decimal separator differs from the invariant one so the two cannot be confused.
        /// </summary>
        [Fact]
        public void Create_UsesSuppliedProviderForInterpolation()
        {
            // Arrange
            const double value = 1.5;

            // Act
            string invariant = string.Create(CultureInfo.InvariantCulture, $"{value}");
            string german = string.Create(CultureInfo.GetCultureInfo("de-DE"), $"{value}");

            // Assert
            Assert.Equal("1.5", invariant);
            Assert.Equal("1,5", german);
        }

        /// <summary>
        /// Verifies that alignment and format specifiers inside an interpolated string are honoured,
        /// which is the part of DefaultInterpolatedStringHandler beyond plain concatenation.
        /// </summary>
        [Fact]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "MA0185:Simplify string.Create when all parameters are culture invariant", Justification = "Exercising this API directly is the point of the test.")]
        public void Create_HonoursAlignmentAndFormatSpecifiers()
        {
            // Act
            string aligned = string.Create(CultureInfo.InvariantCulture, $"{42,5}|{"x",-3}|");
            string formatted = string.Create(CultureInfo.InvariantCulture, $"{255:X2}");

            // Assert
            Assert.Equal("   42|x  |", aligned);
            Assert.Equal("FF", formatted);
        }

        /// <summary>
        /// Verifies that a literal-only and an empty interpolated string round-trip, covering the handler
        /// paths that append no formatted values at all.
        /// </summary>
        [Fact]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "MA0185:Simplify string.Create when all parameters are culture invariant", Justification = "Exercising this API directly is the point of the test.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Redundancy", "RCS1214:Unnecessary interpolated string", Justification = "Exercising this API directly is the point of the test.")]
        public void Create_HandlesLiteralOnlyAndEmptyInterpolations()
        {
            // Assert
            Assert.Equal("literal", string.Create(CultureInfo.InvariantCulture, $"literal"));
            Assert.Equal(string.Empty, string.Create(CultureInfo.InvariantCulture, $""));
        }

        /// <summary>
        /// Verifies that a null interpolated value contributes nothing rather than the text "null".
        /// </summary>
        [Fact]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "MA0185:Simplify string.Create when all parameters are culture invariant", Justification = "Exercising this API directly is the point of the test.")]
        public void Create_TreatsNullValueAsEmpty()
        {
            // Arrange
            const string? value = null;

            // Act
            string result = string.Create(CultureInfo.InvariantCulture, $"[{value}]");

            // Assert
            Assert.Equal("[]", result);
        }
    }
}
