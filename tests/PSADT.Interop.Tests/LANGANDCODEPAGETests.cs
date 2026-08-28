using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using Xunit;

namespace PSADT.Interop.Tests
{
    /// <summary>
    /// Tests the language and code page pair read out of a version resource: its four-byte layout, and the
    /// eight hex digit rendering used to index back into the version block by name.
    /// </summary>
    public sealed class LANGANDCODEPAGETests
    {
        /// <summary>
        /// Verifies that the pair occupies the four bytes the version resource specifies, since the
        /// translation block is read as an array of them.
        /// </summary>
        [Fact]
        public void Layout_MatchesTheVersionResource()
        {
            // Arrange
            Span<byte> buffer = new byte[4];
            BinaryPrimitives.WriteUInt16LittleEndian(buffer[..2], 0x0409);
            BinaryPrimitives.WriteUInt16LittleEndian(buffer[2..4], 0x04B0);

            // Act
            ref readonly LANGANDCODEPAGE translation = ref buffer.AsReadOnlyStructure<LANGANDCODEPAGE>();

            // Assert
            Assert.Equal(4, Unsafe.SizeOf<LANGANDCODEPAGE>());
            Assert.Equal(0x0409, translation.wLanguage);
            Assert.Equal(0x04B0, translation.wCodePage);
        }

        /// <summary>
        /// Verifies that the pair formats as the eight hex digits the version resource APIs expect, with
        /// both halves padded. A value needing fewer digits must still occupy four, since the string is
        /// used to index into a version block by name.
        /// </summary>
        /// <param name="language">The language identifier.</param>
        /// <param name="codePage">The code page identifier.</param>
        /// <param name="expected">The expected formatted value.</param>
        [Theory]
        [InlineData(0x0409, 0x04B0, "040904B0")]
        [InlineData(0x0000, 0x0000, "00000000")]
        [InlineData(0x000C, 0x0001, "000C0001")]
        [InlineData(0xFFFF, 0xFFFF, "FFFFFFFF")]
        public void ToString_FormatsAsEightHexDigits(ushort language, ushort codePage, string expected)
        {
            // Arrange
            Span<byte> buffer = new byte[8];
            BinaryPrimitives.WriteUInt16LittleEndian(buffer[..2], language);
            BinaryPrimitives.WriteUInt16LittleEndian(buffer[2..4], codePage);

            // Act
            ref readonly LANGANDCODEPAGE value = ref buffer.AsReadOnlyStructure<LANGANDCODEPAGE>();

            // Assert
            Assert.Equal(expected, value.ToString());
        }
    }
}
