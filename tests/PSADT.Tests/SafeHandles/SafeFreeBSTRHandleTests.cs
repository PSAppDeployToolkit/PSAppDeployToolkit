using System;
using System.Runtime.InteropServices;
using PSADT.SafeHandles;
using Xunit;

namespace PSADT.Tests.SafeHandles
{
    /// <summary>
    /// Tests the handle that owns a string allocated for COM.
    /// </summary>
    /// <remarks>
    /// The generic machinery these handles are built on is covered against the abstract base elsewhere.
    /// What is left here is what this type adds: what it allocates, that what it allocates really is a
    /// length-prefixed string rather than a plain pointer, and that releasing it can be asked for twice
    /// without freeing twice.
    /// <para>
    /// That last one is why a type like this exists at all. A double free of unmanaged memory does not
    /// fail where it happens - it corrupts the allocator and takes the process down somewhere else - so
    /// the release path guarding itself is the whole point rather than a nicety.
    /// </para>
    /// </remarks>
    public sealed class SafeFreeBSTRHandleTests
    {
        /// <summary>
        /// Verifies that the string handed in is the string the allocation holds.
        /// </summary>
        [Fact]
        public void Alloc_HoldsTheString()
        {
            // Act
            using SafeFreeBSTRHandle handle = SafeFreeBSTRHandle.Alloc("a string");

            // Assert
            Assert.False(handle.IsInvalid);
            Assert.False(handle.IsClosed);
            Assert.Equal("a string", Marshal.PtrToStringBSTR(handle.DangerousGetHandle()));
        }

        /// <summary>
        /// Verifies that what is allocated is a real length-prefixed string, not merely a pointer to
        /// characters.
        /// </summary>
        /// <remarks>
        /// The distinction matters because the receiving side frees it as one. A COM callee handed a
        /// plain character pointer where it expected a length-prefixed string reads four bytes before it
        /// for a length, gets whatever happened to be there, and either truncates or walks off the end.
        /// The length lives immediately before the pointer, in bytes rather than characters.
        /// </remarks>
        [Fact]
        public void Alloc_AllocatesALengthPrefixedString()
        {
            // Act
            using SafeFreeBSTRHandle handle = SafeFreeBSTRHandle.Alloc("a string");

            // Assert
            Assert.Equal("a string".Length * sizeof(char), Marshal.ReadInt32(handle.DangerousGetHandle(), -4));
        }

        /// <summary>
        /// Verifies that a string carrying a null character keeps it, which the length prefix is what
        /// makes possible.
        /// </summary>
        /// <remarks>
        /// This is the strongest evidence that the allocation is what it claims to be. A string that is
        /// terminated rather than length-prefixed cannot carry an interior null - it would read as ending
        /// there - so a value that survives the round trip proves the length was recorded and honoured.
        /// </remarks>
        [Fact]
        public void Alloc_KeepsAnInteriorNullCharacter()
        {
            // Arrange
            const string value = "before\0after";

            // Act
            using SafeFreeBSTRHandle handle = SafeFreeBSTRHandle.Alloc(value);

            // Assert
            Assert.Equal(value, Marshal.PtrToStringBSTR(handle.DangerousGetHandle()));
            Assert.Equal(value.Length * sizeof(char), Marshal.ReadInt32(handle.DangerousGetHandle(), -4));
        }

        /// <summary>
        /// Verifies that an empty string allocates a usable handle rather than being treated as nothing,
        /// since an empty string is a value a COM callee can be handed.
        /// </summary>
        [Fact]
        public void Alloc_AcceptsAnEmptyString()
        {
            // Act
            using SafeFreeBSTRHandle handle = SafeFreeBSTRHandle.Alloc(string.Empty);

            // Assert
            Assert.False(handle.IsInvalid);
            Assert.Equal(string.Empty, Marshal.PtrToStringBSTR(handle.DangerousGetHandle()));
            Assert.Equal(0, Marshal.ReadInt32(handle.DangerousGetHandle(), -4));
        }

        /// <summary>
        /// Verifies that nothing at all is refused as a null argument rather than as a bad handle.
        /// </summary>
        /// <remarks>
        /// Allocating for a null string hands back a null pointer, which the handle would report as
        /// invalid - true, but it tells a caller nothing about the string it actually passed.
        /// </remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "This is deliberate as part of unit testing.")]
        [Fact]
        public void Alloc_RefusesNothingAtAll()
        {
            _ = Assert.Throws<ArgumentNullException>(static () => SafeFreeBSTRHandle.Alloc(null!));
        }

        /// <summary>
        /// Verifies that releasing the handle closes it, and that asking again is harmless.
        /// </summary>
        /// <remarks>
        /// Idempotence is not a convenience here. Disposal happens once explicitly and once more from the
        /// finalizer if anything went wrong, and freeing the same unmanaged pointer twice corrupts the
        /// allocator rather than failing where it happened.
        /// </remarks>
        [Fact]
        public void Dispose_ClosesTheHandleAndIsIdempotent()
        {
            // Arrange
            SafeFreeBSTRHandle handle = SafeFreeBSTRHandle.Alloc("a string");

            // Act
            handle.Dispose();

            // Assert
            Assert.True(handle.IsClosed);
            Assert.Null(Record.Exception(handle.Dispose));
        }
    }
}
