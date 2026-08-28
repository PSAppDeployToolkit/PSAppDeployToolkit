using System;
using System.Buffers.Binary;
using Xunit;

namespace PSADT.Interop.Tests
{
    /// <summary>
    /// Tests the icon directory header: the validity check that gates every read of an icon resource, and
    /// the six-byte layout the entries that follow depend on.
    /// </summary>
    /// <remarks>
    /// The structure declares readonly fields and no constructor, because it is only ever materialised
    /// over bytes that came from a file or a resource. The tests build it the same way, which also
    /// exercises the span reinterpretation it depends on.
    /// </remarks>
    public sealed class ICONDIRTests
    {
        /// <summary>
        /// Verifies that a header is judged valid only when the reserved field is clear, the type is one of
        /// the two documented values, and it claims at least one image.
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
        public void IsValid_RequiresAClearReservedFieldAKnownTypeAndAtLeastOneImage(ushort reserved, ushort type, ushort count, bool expected)
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
        /// Verifies that the entries begin at the seventh byte, which is the same as saying the header is
        /// exactly the six bytes the format specifies. The entry array is declared as a variable-length
        /// inline array, so nothing about the type itself forces that.
        /// </summary>
        [Fact]
        public void Entries_BeginAfterASixByteHeader()
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
    }
}
