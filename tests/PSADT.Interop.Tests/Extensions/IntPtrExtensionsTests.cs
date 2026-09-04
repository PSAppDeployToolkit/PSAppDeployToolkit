using System;
using Xunit;

namespace PSADT.Interop.Tests.Extensions
{
    /// <summary>
    /// Tests the unmanaged memory readers exposed as extension methods on nint. These allocate nothing
    /// outside the test's own stack and read only memory the test itself pins.
    /// </summary>
    public sealed class IntPtrExtensionsTests
    {
        /// <summary>
        /// Verifies that a pinned, null-terminated UTF-16 buffer is read back up to the terminator.
        /// </summary>
        [Fact]
        public void ToStringUni_StopsAtNullTerminator()
        {
            unsafe
            {
                fixed (char* buffer = "Hello\0")
                {
                    // Act
                    string result = ((nint)buffer).ToStringUni(6);

                    // Assert
                    Assert.Equal("Hello", result);
                }
            }
        }

        /// <summary>
        /// Verifies that trailing whitespace is trimmed along with the terminator.
        /// </summary>
        [Fact]
        public void ToStringUni_TrimsTrailingWhiteSpace()
        {
            unsafe
            {
                fixed (char* buffer = "Hello  \0")
                {
                    // Act
                    string result = ((nint)buffer).ToStringUni(8);

                    // Assert
                    Assert.Equal("Hello", result);
                }
            }
        }

        /// <summary>
        /// Verifies that a buffer holding nothing but padding is rejected rather than returned as an
        /// empty string.
        /// </summary>
        [Fact]
        public void ToStringUni_ThrowsWhenBufferHoldsNoText()
        {
            unsafe
            {
                fixed (char* buffer = "   \0")
                {
                    nint handle = (nint)buffer;

                    // Act & Assert
                    _ = Assert.Throws<FormatException>(() => handle.ToStringUni(4));
                }
            }
        }

        /// <summary>
        /// Verifies that a null pointer is rejected before it is dereferenced.
        /// </summary>
        [Fact]
        public void ToStringUni_ThrowsOnNullPointer()
        {
            _ = Assert.Throws<InvalidOperationException>(static () => ((nint)0).ToStringUni(4));
        }

        /// <summary>
        /// Verifies that the INVALID_HANDLE_VALUE sentinel is rejected before it is dereferenced.
        /// </summary>
        [Fact]
        public void ToStringUni_ThrowsOnInvalidHandleSentinel()
        {
            _ = Assert.Throws<InvalidOperationException>(static () => ((nint)(-1)).ToStringUni(4));
        }

        /// <summary>
        /// Verifies that a zero or negative length is rejected, since neither can describe a readable
        /// region.
        /// </summary>
        /// <param name="length">The invalid length under test.</param>
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void ToStringUni_ThrowsOnNonPositiveLength(int length)
        {
            unsafe
            {
                fixed (char* buffer = "Hello\0")
                {
                    nint handle = (nint)buffer;

                    // Act & Assert
                    _ = Assert.Throws<ArgumentOutOfRangeException>(() => handle.ToStringUni(length));
                }
            }
        }

        /// <summary>
        /// Verifies that a span projected over pinned memory sees exactly the requested elements.
        /// </summary>
        [Fact]
        public void AsReadOnlySpan_ProjectsRequestedElementCount()
        {
            unsafe
            {
                fixed (char* buffer = "ABCD")
                {
                    // Act
                    ReadOnlySpan<char> span = ((nint)buffer).AsReadOnlySpan<char>(2);

                    // Assert
                    Assert.Equal("AB", span.ToString());
                }
            }
        }
    }
}
