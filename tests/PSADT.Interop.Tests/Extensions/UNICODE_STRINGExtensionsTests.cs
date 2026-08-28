using Windows.Win32.Foundation;
using Xunit;

namespace PSADT.Interop.Tests.Extensions
{
    /// <summary>
    /// Tests the reader that turns a counted native string into a managed one. The structure carries both
    /// a length and a capacity, and reading the wrong one is the mistake this guards against.
    /// </summary>
    public sealed class UNICODE_STRINGExtensionsTests
    {
        /// <summary>
        /// Verifies that the string is read using its byte length rather than its buffer capacity, so
        /// trailing capacity is not mistaken for content.
        /// </summary>
        [Fact]
        public void ToManagedString_ReadsTheDeclaredLength()
        {
            unsafe
            {
                fixed (char* buffer = "Hello there")
                {
                    // Arrange: ten bytes is five characters
                    UNICODE_STRING value = new() { Length = 10, MaximumLength = 22, Buffer = buffer };

                    // Act & Assert
                    Assert.Equal("Hello", value.ToManagedString());
                }
            }
        }

        /// <summary>
        /// Verifies that padding within the declared length is trimmed, which is how a fixed-size native
        /// field arrives when its content is shorter than the field.
        /// </summary>
        [Fact]
        public void ToManagedString_TrimsPadding()
        {
            unsafe
            {
                fixed (char* buffer = "Hi   ")
                {
                    // Arrange
                    UNICODE_STRING value = new() { Length = 10, MaximumLength = 10, Buffer = buffer };

                    // Act & Assert
                    Assert.Equal("Hi", value.ToManagedString());
                }
            }
        }
    }
}
