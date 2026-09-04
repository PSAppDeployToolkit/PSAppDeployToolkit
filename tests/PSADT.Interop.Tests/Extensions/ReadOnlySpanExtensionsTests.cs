using System;
using System.Runtime.InteropServices;
using Xunit;

namespace PSADT.Interop.Tests.Extensions
{
    /// <summary>
    /// Tests the span readers used to interpret native buffers: reinterpreting bytes as a structure,
    /// reading a null-terminated string, and trimming the padding that fixed-size native fields leave
    /// behind.
    /// </summary>
    public sealed class ReadOnlySpanExtensionsTests
    {
        /// <summary>
        /// Verifies that a byte span is reinterpreted field by field, so a caller reading a native
        /// structure out of a buffer gets the fields the buffer actually describes.
        /// </summary>
        [Fact]
        public void AsReadOnlyStructure_ReinterpretsTheLeadingBytes()
        {
            // Arrange
            ReadOnlySpan<byte> buffer = [0x78, 0x56, 0x34, 0x12, 0xFF, 0x00];

            // Act
            ref readonly Pair result = ref buffer.AsReadOnlyStructure<Pair>();

            // Assert
            Assert.Equal(0x12345678u, result.First);
            Assert.Equal(0x00FF, result.Second);
        }

        /// <summary>
        /// Verifies that a span exactly the size of the structure is accepted, since an off-by-one in the
        /// size check would reject a legitimately complete buffer.
        /// </summary>
        [Fact]
        public void AsReadOnlyStructure_AcceptsAnExactlySizedSpan()
        {
            // Arrange
            ReadOnlySpan<byte> buffer = new byte[Marshal.SizeOf<Pair>()];

            // Act & Assert
            Assert.Equal(0u, buffer.AsReadOnlyStructure<Pair>().First);
        }

        /// <summary>
        /// Verifies that a span too small for the structure is rejected rather than reading past its end,
        /// which is the difference between an exception and a corrupt read.
        /// </summary>
        [Fact]
        public void AsReadOnlyStructure_RejectsAnUndersizedSpan()
        {
            _ = Assert.Throws<InvalidOperationException>(static () =>
            {
                ReadOnlySpan<byte> buffer = [0x01, 0x02];
                return buffer.AsReadOnlyStructure<Pair>().First;
            });
        }

        /// <summary>
        /// Verifies that a character span is read up to its terminator and trimmed, which is how a
        /// fixed-size native string field arrives.
        /// </summary>
        /// <param name="value">The raw span contents.</param>
        /// <param name="expected">The expected string.</param>
        [Theory]
        [InlineData("Hello\0", "Hello")]
        [InlineData("Hello\0trailing garbage", "Hello")]
        [InlineData("  Hello  \0", "Hello")]
        [InlineData("\0", null)]
        [InlineData("   \0", null)]
        public void ToStringUni_ReadsUpToTheTerminatorAndTrims(string value, string? expected)
        {
            // Act
            string? result = value.AsSpan().ToStringUni();

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Verifies that a span with no terminator is rejected. A fixed-size native field is always
        /// terminated, so its absence means the buffer is not what the caller thinks it is.
        /// </summary>
        [Fact]
        public void ToStringUni_RejectsAnUnterminatedSpan()
        {
            _ = Assert.Throws<FormatException>(static () => "Hello".AsSpan().ToStringUni());
        }

        /// <summary>
        /// Verifies that trailing nulls are removed along with whitespace on either side, and in that
        /// order, so padding left by a fixed-size field does not survive as trailing spaces.
        /// </summary>
        /// <param name="value">The raw span contents.</param>
        /// <param name="expected">The expected result.</param>
        [Theory]
        [InlineData("Hello", "Hello")]
        [InlineData("Hello\0\0", "Hello")]
        [InlineData("  Hello  ", "Hello")]
        [InlineData("  Hello \0\0 ", "Hello")]
        [InlineData("\0\0", "")]
        [InlineData("", "")]
        public void TrimRemoveEndNull_StripsPaddingFromBothEnds(string value, string expected)
        {
            // Act
            ReadOnlySpan<char> result = value.AsSpan().TrimRemoveEndNull();

            // Assert
            Assert.Equal(expected, result.ToString());
        }

        /// <summary>
        /// A structure with two differently sized fields, so a reinterpretation reading at the wrong
        /// offset produces a visibly wrong answer rather than a plausible one.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private readonly struct Pair
        {
            /// <summary>
            /// The leading 32-bit field.
            /// </summary>
            public readonly uint First;

            /// <summary>
            /// The trailing 16-bit field.
            /// </summary>
            public readonly ushort Second;
        }
    }
}
