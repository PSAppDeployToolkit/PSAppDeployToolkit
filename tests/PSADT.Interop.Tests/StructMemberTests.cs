using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using Xunit;

namespace PSADT.Interop.Tests
{
    /// <summary>
    /// Tests the members the native structures add on top of their fields: the two icon directory
    /// validity checks and the language/code page identifier formatting.
    /// </summary>
    /// <remarks>
    /// These structures declare readonly fields and no constructor, because they are only ever
    /// materialised over bytes that came from a file or an API. The tests build them the same way, which
    /// also exercises the span reinterpretation they depend on.
    /// </remarks>
    public sealed class StructMemberTests
    {
        /// <summary>
        /// Verifies that an icon directory is judged valid only when the reserved field is clear, the
        /// type is one of the two documented values, and it claims at least one image.
        /// </summary>
        /// <param name="reserved">The reserved field, which must be zero.</param>
        /// <param name="type">The resource type: 1 for icons, 2 for cursors.</param>
        /// <param name="count">The number of images claimed.</param>
        /// <param name="expected">Whether the header is expected to be judged valid.</param>
        [Theory]
        [InlineData(0, 1, 1, true)]
        [InlineData(0, 2, 1, true)]
        [InlineData(0, 1, 255, true)]
        [InlineData(1, 1, 1, false)]
        [InlineData(0, 0, 1, false)]
        [InlineData(0, 3, 1, false)]
        [InlineData(0, 1, 0, false)]
        public void IconDirectory_IsValid_RequiresAClearReservedFieldAKnownTypeAndAtLeastOneImage(ushort reserved, ushort type, ushort count, bool expected)
        {
            // Arrange
            Span<byte> buffer = new byte[64];
            BinaryPrimitives.WriteUInt16LittleEndian(buffer[..2], reserved);
            BinaryPrimitives.WriteUInt16LittleEndian(buffer[2..4], type);
            BinaryPrimitives.WriteUInt16LittleEndian(buffer[4..6], count);

            // Act
            ref readonly ICONDIR header = ref buffer.AsReadOnlyStructure<ICONDIR>();

            // Assert
            Assert.Equal(expected, header.IsValid);
        }

        /// <summary>
        /// Verifies that a directory entry occupies the sixteen bytes the icon format specifies and that
        /// every field lands where the format puts it. The tests around this one write field values at
        /// fixed offsets, which only works while the packing holds; a lost <c>Pack = 1</c> or a reordered
        /// field would move the fields without breaking the build.
        /// </summary>
        [Fact]
        public void IconDirectoryEntry_MatchesTheFormatLayout()
        {
            // Arrange: each field gets a value that could not be mistaken for a neighbour's
            Span<byte> buffer = new byte[16];
            buffer[0] = 0x10;
            buffer[1] = 0x20;
            buffer[2] = 0x30;
            buffer[3] = 0x40;
            BinaryPrimitives.WriteUInt16LittleEndian(buffer[4..6], 0x5051);
            BinaryPrimitives.WriteUInt16LittleEndian(buffer[6..8], 0x6061);
            BinaryPrimitives.WriteUInt32LittleEndian(buffer[8..12], 0x70717273);
            BinaryPrimitives.WriteUInt32LittleEndian(buffer[12..16], 0x80818283);

            // Act
            ref readonly ICONDIRENTRY entry = ref buffer.AsReadOnlyStructure<ICONDIRENTRY>();

            // Assert
            Assert.Equal(16, Unsafe.SizeOf<ICONDIRENTRY>());
            Assert.Equal(0x10, entry.bWidth);
            Assert.Equal(0x20, entry.bHeight);
            Assert.Equal(0x30, entry.bColorCount);
            Assert.Equal(0x40, entry.bReserved);
            Assert.Equal(0x5051, entry.wPlanes);
            Assert.Equal(0x6061, entry.wBitCount);
            Assert.Equal(0x70717273u, entry.dwBytesInRes);
            Assert.Equal(0x80818283u, entry.dwImageOffset);
        }

        /// <summary>
        /// Verifies that the entries of a directory begin at the seventh byte, which is the same as saying
        /// its header is exactly the six bytes the format specifies. The entry array is declared as a
        /// variable-length inline array, so nothing about the type itself forces that.
        /// </summary>
        [Fact]
        public void IconDirectory_PlacesItsEntriesAfterASixByteHeader()
        {
            // Arrange: a header claiming one image, followed by an entry with a recognisable width
            Span<byte> buffer = new byte[6 + 16];
            BinaryPrimitives.WriteUInt16LittleEndian(buffer[2..4], 1);
            BinaryPrimitives.WriteUInt16LittleEndian(buffer[4..6], 1);
            buffer[6] = 0x5A;

            // Act
            ref readonly ICONDIR header = ref buffer.AsReadOnlyStructure<ICONDIR>();

            // Assert
            Assert.True(header.IsValid);
            Assert.Equal(0x5A, header.idEntries[0].bWidth);
        }

        /// <summary>
        /// Verifies that a language and code page pair occupies the four bytes the version resource
        /// specifies, since the translation block is read as an array of them.
        /// </summary>
        [Fact]
        public void LangAndCodePage_MatchesTheVersionResourceLayout()
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
        /// Verifies that a directory entry is judged valid only when it describes a non-empty image, so
        /// a zero-sized or zero-length entry is rejected before anything tries to read it.
        /// </summary>
        /// <param name="width">The image width.</param>
        /// <param name="height">The image height.</param>
        /// <param name="bytesInRes">The image length in bytes.</param>
        /// <param name="expected">Whether the entry is expected to be judged valid.</param>
        [Theory]
        [InlineData(32, 32, 4096, true)]
        [InlineData(1, 1, 1, true)]
        [InlineData(0, 32, 4096, false)]
        [InlineData(32, 0, 4096, false)]
        [InlineData(32, 32, 0, false)]
        public void IconDirectoryEntry_IsValid_RequiresANonEmptyImage(byte width, byte height, uint bytesInRes, bool expected)
        {
            // Arrange
            Span<byte> buffer = new byte[32];
            buffer[0] = width;
            buffer[1] = height;
            BinaryPrimitives.WriteUInt32LittleEndian(buffer[8..12], bytesInRes);

            // Act
            ref readonly ICONDIRENTRY entry = ref buffer.AsReadOnlyStructure<ICONDIRENTRY>();

            // Assert
            Assert.Equal(expected, entry.IsValid);
        }

        /// <summary>
        /// Verifies that the language and code page pair formats as the eight hex digits the version
        /// resource APIs expect, with both halves padded. A value needing fewer digits must still occupy
        /// four, since the string is used to index into a version block by name.
        /// </summary>
        /// <param name="language">The language identifier.</param>
        /// <param name="codePage">The code page identifier.</param>
        /// <param name="expected">The expected formatted value.</param>
        [Theory]
        [InlineData(0x0409, 0x04B0, "040904B0")]
        [InlineData(0x0000, 0x0000, "00000000")]
        [InlineData(0x000C, 0x0001, "000C0001")]
        [InlineData(0xFFFF, 0xFFFF, "FFFFFFFF")]
        public void LangAndCodePage_ToString_FormatsAsEightHexDigits(ushort language, ushort codePage, string expected)
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
