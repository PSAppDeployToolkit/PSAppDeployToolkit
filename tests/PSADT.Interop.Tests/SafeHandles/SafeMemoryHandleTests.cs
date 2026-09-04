using System;
using PSADT.Interop.SafeHandles;
using Windows.Win32.System.Memory;
using Xunit;

namespace PSADT.Interop.Tests.SafeHandles
{
    /// <summary>
    /// Tests the shared memory-region reader and writer through a concrete handle. Every test allocates
    /// its own region and releases it, so nothing outside the test is touched.
    /// </summary>
    /// <remarks>
    /// SafeVirtualAllocHandle is used as the vehicle because it is the only concrete handle that can
    /// allocate a region of a chosen size. VirtualAlloc commits zeroed pages, so an unwritten byte reads
    /// as zero and a write can be shown not to disturb its neighbours.
    /// </remarks>
    public sealed class SafeMemoryHandleTests
    {
        /// <summary>
        /// Verifies that the requested length is what the handle reports, rather than the page-rounded
        /// size the allocator actually reserved. Every bounds check depends on this.
        /// </summary>
        [Fact]
        public void Length_ReportsTheRequestedLengthNotThePageSize()
        {
            // Arrange
            using SafeVirtualAllocHandle handle = Allocate(24);

            // Assert
            Assert.Equal(24, handle.Length);
        }

        /// <summary>
        /// Verifies that a byte array lands at the requested offset and leaves the bytes before it alone.
        /// The offset names a position in the destination, matching every other member on this type.
        /// </summary>
        [Fact]
        public void Write_PlacesTheDataAtTheDestinationOffset()
        {
            // Arrange
            using SafeVirtualAllocHandle handle = Allocate(16);

            // Act
            _ = handle.Write([0xAA, 0xBB, 0xCC, 0xDD], 4);

            // Assert
            Assert.Equal([0x00, 0x00, 0x00, 0x00, 0xAA, 0xBB, 0xCC, 0xDD], handle.AsReadOnlySpan<byte>()[..8].ToArray());
        }

        /// <summary>
        /// Verifies that a write with no offset starts at the beginning, which is the only shape any
        /// caller uses today.
        /// </summary>
        [Fact]
        public void Write_DefaultsToTheStartOfTheRegion()
        {
            // Arrange
            using SafeVirtualAllocHandle handle = Allocate(8);

            // Act
            _ = handle.Write([1, 2, 3, 4]);

            // Assert
            Assert.Equal([1, 2, 3, 4, 0, 0, 0, 0], handle.AsReadOnlySpan<byte>().ToArray());
        }

        /// <summary>
        /// Verifies that a write which would not fit at the given offset is rejected, including the case
        /// where the data alone would fit but the offset pushes it past the end.
        /// </summary>
        [Fact]
        public void Write_RejectsDataThatWouldNotFitAtTheOffset()
        {
            // Arrange
            using SafeVirtualAllocHandle handle = Allocate(8);

            // Assert
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => handle.Write(new byte[8], 1));
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => handle.Write(new byte[9]));
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => handle.Write(new byte[1], 8));
        }

        /// <summary>
        /// Verifies that the write guards reject an absent, empty or negatively offset argument before
        /// any memory is touched.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "This is deliberate as part of unit testing.")]
        [Fact]
        public void Write_RejectsAbsentEmptyAndNegativelyOffsetArguments()
        {
            // Arrange
            using SafeVirtualAllocHandle handle = Allocate(8);

            // Assert
            _ = Assert.Throws<ArgumentNullException>(() => handle.Write(null!));
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => handle.Write([]));
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => handle.Write([1], -1));
        }

        /// <summary>
        /// Verifies that each scalar width round-trips at an offset, so a value written at one position
        /// is read back from the same one and nothing wider is disturbed.
        /// </summary>
        [Fact]
        public void ScalarAccessors_RoundTripAtAnOffset()
        {
            // Arrange
            using SafeVirtualAllocHandle handle = Allocate(32);

            // Act
            _ = handle.WriteByte(0x7F, 0).WriteInt16(-2, 2).WriteInt32(int.MinValue, 4).WriteInt64(long.MaxValue, 8);

            // Assert
            Assert.Equal(0x7F, handle.ReadByte(0));
            Assert.Equal((short)-2, handle.ReadInt16(2));
            Assert.Equal(int.MinValue, handle.ReadInt32(4));
            Assert.Equal(long.MaxValue, handle.ReadInt64(8));
        }

        /// <summary>
        /// Verifies that the scalar accessors reject a negative offset rather than reading behind the
        /// region.
        /// </summary>
        [Fact]
        public void ScalarAccessors_RejectNegativeOffsets()
        {
            // Arrange
            using SafeVirtualAllocHandle handle = Allocate(8);

            // Assert
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => handle.ReadByte(-1));
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => handle.ReadInt16(-1));
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => handle.ReadInt32(-1));
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => handle.ReadInt64(-1));
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => handle.WriteByte(0, -1));
        }

        /// <summary>
        /// Verifies that clearing zeroes the whole region, since it exists to scrub a buffer that held
        /// something sensitive.
        /// </summary>
        [Fact]
        public void Clear_ZeroesTheWholeRegion()
        {
            // Arrange
            using SafeVirtualAllocHandle handle = Allocate(8);
            _ = handle.Write([1, 2, 3, 4, 5, 6, 7, 8]);

            // Act
            handle.Clear();

            // Assert
            Assert.Equal(new byte[8], handle.AsReadOnlySpan<byte>().ToArray());
        }

        /// <summary>
        /// Verifies that a span covers the region from the offset to the end, and that the element count
        /// follows the element size rather than the byte length.
        /// </summary>
        [Fact]
        public void AsReadOnlySpan_CoversTheRemainderOfTheRegion()
        {
            // Arrange
            using SafeVirtualAllocHandle handle = Allocate(16);
            _ = handle.WriteInt32(7, 8);

            // Act & Assert
            Assert.Equal(4, handle.AsReadOnlySpan<int>().Length);
            Assert.Equal(2, handle.AsReadOnlySpan<int>(8).Length);
            Assert.Equal(7, handle.AsReadOnlySpan<int>(8)[0]);
        }

        /// <summary>
        /// Verifies that an offset at or beyond the end is reported as a range problem. Previously the
        /// check divided before comparing, so a small overshoot rounded to zero and surfaced as an
        /// alignment complaint, while landing exactly on the end surfaced a helper's own exception.
        /// </summary>
        [Fact]
        public void AsReadOnlySpan_ReportsAnOutOfRangeOffsetAsSuch()
        {
            // Arrange
            using SafeVirtualAllocHandle handle = Allocate(8);

            // Assert
            foreach (int offset in (int[])[8, 9, 10, 12])
            {
                InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => handle.AsReadOnlySpan<int>(offset).Length);
                Assert.Contains("readable length", exception.Message, StringComparison.Ordinal);
            }
        }

        /// <summary>
        /// Verifies that a region which does not divide evenly by the element size is rejected as
        /// misaligned, rather than silently truncating the last partial element.
        /// </summary>
        [Fact]
        public void AsReadOnlySpan_RejectsAMisalignedRemainder()
        {
            // Arrange
            using SafeVirtualAllocHandle handle = Allocate(10);

            // Act & Assert
            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => handle.AsReadOnlySpan<int>().Length);
            Assert.Contains("aligned", exception.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that a structure is read from the requested offset, and that one too large for the
        /// remaining bytes is refused rather than read past the end.
        /// </summary>
        [Fact]
        public void AsReadOnlyStructure_ReadsAtTheOffsetAndRefusesToOverrun()
        {
            // Arrange
            using SafeVirtualAllocHandle handle = Allocate(16);
            _ = handle.WriteInt64(-1, 8);

            // Assert
            Assert.Equal(-1, handle.AsReadOnlyStructure<long>(8));
            _ = Assert.Throws<InvalidOperationException>(() => handle.AsReadOnlyStructure<long>(9));
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => handle.AsReadOnlyStructure<long>(-1));
        }

        /// <summary>
        /// Verifies that a string written into the region is read back, trimmed of the padding that the
        /// rest of the allocation leaves behind.
        /// </summary>
        [Fact]
        public void ToStringUni_ReadsTheRegionAsAString()
        {
            // Arrange
            using SafeVirtualAllocHandle handle = Allocate(16);
            _ = handle.Write([.. System.Text.Encoding.Unicode.GetBytes("Hi")]);

            // Act & Assert
            Assert.Equal("Hi", handle.ToStringUni());
            Assert.Equal("Hi", handle.ReadNullTerminatedString());
        }

        /// <summary>
        /// Verifies that an offset leaving less than one whole character is reported as a range problem
        /// by this type, rather than surfacing the pointer helper's own argument exception. Note that the
        /// boundary is one character rather than one byte: a single trailing byte cannot form a UTF-16
        /// unit, and dividing it away would otherwise leave a zero-length read for the helper to reject.
        /// </summary>
        /// <param name="offset">The offset expected to be rejected.</param>
        [Theory]
        [InlineData(7)]
        [InlineData(8)]
        [InlineData(9)]
        public void StringReaders_RejectAnOffsetLeavingNoWholeCharacter(int offset)
        {
            // Arrange
            using SafeVirtualAllocHandle handle = Allocate(8);

            // Assert
            Assert.Contains("complete character", Assert.Throws<InvalidOperationException>(() => handle.ToStringUni(offset)).Message, StringComparison.Ordinal);
            Assert.Contains("complete character", Assert.Throws<InvalidOperationException>(() => handle.ReadNullTerminatedString(offset)).Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that the last whole character in the region is still readable, so the guard rejects
        /// only what genuinely cannot be read.
        /// </summary>
        [Fact]
        public void ToStringUni_ReadsTheFinalCharacterOfTheRegion()
        {
            // Arrange
            using SafeVirtualAllocHandle handle = Allocate(8);
            _ = handle.Write([.. System.Text.Encoding.Unicode.GetBytes("Z")], 6);

            // Act & Assert
            Assert.Equal("Z", handle.ToStringUni(6));
        }

        /// <summary>
        /// Verifies that every accessor refuses to touch a released region, which is the whole point of
        /// routing them through the handle rather than a raw pointer.
        /// </summary>
        [Fact]
        public void Accessors_RefuseAReleasedHandle()
        {
            // Arrange
            SafeVirtualAllocHandle handle = Allocate(8);
            handle.Dispose();

            // Assert
            _ = Assert.Throws<InvalidOperationException>(() => handle.ReadByte());
            _ = Assert.Throws<InvalidOperationException>(() => handle.WriteByte(1));
            _ = Assert.Throws<InvalidOperationException>(() => handle.Write([1]));
            _ = Assert.Throws<InvalidOperationException>(handle.Clear);
            _ = Assert.Throws<InvalidOperationException>(() => handle.AsReadOnlySpan<byte>().Length);
            _ = Assert.Throws<InvalidOperationException>(() => handle.AsReadOnlyStructure<byte>());
            _ = Assert.Throws<InvalidOperationException>(() => handle.ToStringUni());
        }

        /// <summary>
        /// Verifies that releasing twice is harmless, since a handle is routinely disposed by a using
        /// block after something already released it explicitly.
        /// </summary>
        [Fact]
        public void Dispose_IsIdempotent()
        {
            // Arrange
            SafeVirtualAllocHandle handle = Allocate(8);

            // Act & Assert
            Assert.Null(Record.Exception(() =>
            {
                handle.Dispose();
                handle.Dispose();
            }));
            Assert.True(handle.IsClosed);
        }

        /// <summary>
        /// Allocates a committed, readable and writable region of the given length.
        /// </summary>
        /// <param name="length">The number of bytes to allocate.</param>
        /// <returns>A handle owning the allocation.</returns>
        private static SafeVirtualAllocHandle Allocate(int length)
        {
            return SafeVirtualAllocHandle.Alloc(
                length,
                VIRTUAL_ALLOCATION_TYPE.MEM_COMMIT | VIRTUAL_ALLOCATION_TYPE.MEM_RESERVE,
                PAGE_PROTECTION_FLAGS.PAGE_READWRITE);
        }
    }
}
