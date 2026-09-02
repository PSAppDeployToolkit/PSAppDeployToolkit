using System;
using PSADT.SafeHandles;
using Xunit;

namespace PSADT.Tests.SafeHandles
{
    /// <summary>
    /// Tests the handle that pins a managed array so unmanaged code can be given its address.
    /// </summary>
    /// <remarks>
    /// The generic machinery these handles are built on is covered against the abstract base elsewhere.
    /// What is left here is what this type adds: that the address it hands out is the array's own
    /// storage rather than a copy of it, that the length it reports is in bytes rather than elements,
    /// and that unpinning can be asked for twice.
    /// <para>
    /// The address being the array's own storage is asserted by writing through the handle and reading
    /// the change back out of the managed array. That is decisive and needs no collection to be forced:
    /// a copy would leave the array untouched. Asking the collector to move things and checking the
    /// address afterwards would look like a better test of pinning and would in fact be a worse one,
    /// since nothing can make the collector compact on demand and the test would pass whether or not the
    /// array were pinned.
    /// </para>
    /// </remarks>
    public sealed class SafePinnedGCHandleTests
    {
        /// <summary>
        /// Verifies that the address handed out is the array's own storage, by writing through it and
        /// reading the change back out of the array.
        /// </summary>
        [Fact]
        public void Alloc_PinsTheArrayItself()
        {
            // Arrange
            byte[] value = [1, 2, 3, 4];

            // Act
            using SafePinnedGCHandle handle = SafePinnedGCHandle.Alloc(value);
            _ = handle.WriteByte(0xFF, offset: 2);

            // Assert: the managed array saw the write, so the handle addresses it rather than a copy
            Assert.Equal([1, 2, 0xFF, 4], value);
        }

        /// <summary>
        /// Verifies that the array's contents are readable through the handle, which is what the
        /// unmanaged callee is being handed.
        /// </summary>
        [Fact]
        public void Alloc_ExposesTheArraysContents()
        {
            // Arrange
            int[] value = [10, 20, 30];

            // Act
            using SafePinnedGCHandle handle = SafePinnedGCHandle.Alloc(value);

            // Assert
            Assert.Equal(value, handle.AsReadOnlySpan<int>().ToArray());
        }

        /// <summary>
        /// Verifies that the length reported is the size of the region in bytes rather than the number
        /// of elements, since that is what every caller passing it on to unmanaged code needs.
        /// </summary>
        /// <param name="elements">How many elements to pin.</param>
        [Theory]
        [InlineData(1)]
        [InlineData(4)]
        [InlineData(64)]
        public void Alloc_ReportsTheLengthInBytes(int elements)
        {
            // Act
            using SafePinnedGCHandle bytes = SafePinnedGCHandle.Alloc(new byte[elements]);
            using SafePinnedGCHandle integers = SafePinnedGCHandle.Alloc(new int[elements]);
            using SafePinnedGCHandle longs = SafePinnedGCHandle.Alloc(new long[elements]);

            // Assert
            Assert.Equal(elements, bytes.Length);
            Assert.Equal(elements * sizeof(int), integers.Length);
            Assert.Equal(elements * sizeof(long), longs.Length);
        }

        /// <summary>
        /// Verifies that two arrays pinned at once are pinned separately, so a caller holding both is not
        /// handed the same address twice.
        /// </summary>
        [Fact]
        public void Alloc_PinsEachArraySeparately()
        {
            // Arrange
            byte[] first = [1, 2, 3, 4];
            byte[] second = [1, 2, 3, 4];

            // Act
            using SafePinnedGCHandle firstHandle = SafePinnedGCHandle.Alloc(first);
            using SafePinnedGCHandle secondHandle = SafePinnedGCHandle.Alloc(second);
            _ = firstHandle.WriteByte(0xFF);

            // Assert
            Assert.NotEqual(firstHandle.DangerousGetHandle(), secondHandle.DangerousGetHandle());
            Assert.Equal(0xFF, first[0]);
            Assert.Equal(1, second[0]);
        }

        /// <summary>
        /// Verifies that an empty array is refused, since there is no region to pin and a caller handing
        /// the address on would be handing on nothing.
        /// </summary>
        [Fact]
        public void Alloc_RefusesAnEmptyArray()
        {
            _ = Assert.Throws<ArgumentOutOfRangeException>(static () => SafePinnedGCHandle.Alloc(Array.Empty<int>()));
        }

        /// <summary>
        /// Verifies that nothing at all is refused as a null argument rather than by failing on the
        /// attempt to measure it.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "This is deliberate as part of unit testing.")]
        [Fact]
        public void Alloc_RefusesNothingAtAll()
        {
            _ = Assert.Throws<ArgumentNullException>(static () => SafePinnedGCHandle.Alloc<int>(null!));
        }

        /// <summary>
        /// Verifies that releasing the handle closes it, and that asking again is harmless.
        /// </summary>
        /// <remarks>
        /// Idempotence matters for the opposite reason to a memory-owning handle: freeing the underlying
        /// pin twice throws rather than corrupting anything, but it would throw from a finalizer, where
        /// there is nothing to catch it.
        /// </remarks>
        [Fact]
        public void Dispose_ClosesTheHandleAndIsIdempotent()
        {
            // Arrange
            SafePinnedGCHandle handle = SafePinnedGCHandle.Alloc<byte>([1, 2, 3, 4]);

            // Act
            handle.Dispose();

            // Assert
            Assert.True(handle.IsClosed);
            Assert.Null(Record.Exception(handle.Dispose));
        }

        /// <summary>
        /// Verifies that the array outlives the handle, so a caller that pinned an array it still holds
        /// gets it back intact rather than freed.
        /// </summary>
        /// <remarks>
        /// A pin is not an allocation. Releasing it lets the collector move the array again, and nothing
        /// more - a release path that mistook it for owned memory and freed it would corrupt the managed
        /// heap, so it is worth stating that it does not.
        /// </remarks>
        [Fact]
        public void Dispose_LeavesTheArrayAlone()
        {
            // Arrange
            byte[] value = [1, 2, 3, 4];

            // Act
            SafePinnedGCHandle handle = SafePinnedGCHandle.Alloc(value);
            handle.Dispose();
            GC.Collect();
            GC.WaitForPendingFinalizers();

            // Assert
            Assert.Equal([1, 2, 3, 4], value);
        }
    }
}
