using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using Xunit;

namespace PSADT.Interop.Tests
{
    /// <summary>
    /// Tests the icon directory entry: its sixteen-byte layout and the validity check that rejects an
    /// entry describing no image.
    /// </summary>
    public sealed class ICONDIRENTRYTests
    {
        /// <summary>
        /// Verifies that an entry occupies the sixteen bytes the icon format specifies and that every field
        /// lands where the format puts it. Other tests write field values at fixed offsets, which only
        /// works while the packing holds; a lost <c>Pack = 1</c> or a reordered field would move the fields
        /// without breaking the build.
        /// </summary>
        [Fact]
        public void Layout_MatchesTheIconFormat()
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
        /// Verifies that an entry is judged valid only when it describes a non-empty image, so a
        /// zero-sized or zero-length entry is rejected before anything tries to read it.
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
        public void IsValid_RequiresANonEmptyImage(byte width, byte height, uint bytesInRes, bool expected)
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
    }
}
