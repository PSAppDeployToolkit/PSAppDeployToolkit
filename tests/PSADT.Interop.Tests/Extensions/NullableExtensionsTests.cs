using Xunit;

namespace PSADT.Interop.Tests.Extensions
{
    /// <summary>
    /// Tests the bridge that lets an optional value reach native code as a pointer.
    /// </summary>
    /// <remarks>
    /// The nullable is taken by readonly reference so the pointer refers to the caller's storage. Taken by
    /// value the compiler would copy it into the helper's own frame, and the returned pointer would dangle
    /// the moment the helper returned. That was a real defect here, so the first test below reads the value
    /// back through the pointer rather than merely checking it is not null.
    /// </remarks>
    public sealed class NullableExtensionsTests
    {
        /// <summary>
        /// Verifies that a nullable with a value yields a pointer to that value, which is what lets a
        /// caller pass an optional structure to native code without copying it.
        /// </summary>
        [Fact]
        public void ToPointer_PointsAtTheValue()
        {
            unsafe
            {
                // Arrange
                int? present = 42;
                long? wider = -9_000_000_000L;

                // Act
                int* presentPointer = present.ToPointer();
                long* widerPointer = wider.ToPointer();

                // Assert
                Assert.True(presentPointer is not null);
                Assert.Equal(42, *presentPointer);
                Assert.True(widerPointer is not null);
                Assert.Equal(-9_000_000_000L, *widerPointer);
            }
        }

        /// <summary>
        /// Verifies that a nullable without a value yields a null pointer rather than a pointer to a
        /// default, which is how an optional native argument is omitted.
        /// </summary>
        [Fact]
        public void ToPointer_ReturnsNullWhenAbsent()
        {
            unsafe
            {
                // Arrange
                int? absent = null;

                // Act & Assert
                Assert.True(absent.ToPointer() is null);
            }
        }
    }
}
