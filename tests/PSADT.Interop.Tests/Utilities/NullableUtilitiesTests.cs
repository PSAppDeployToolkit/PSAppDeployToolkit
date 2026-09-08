using System.Reflection;
using PSADT.Interop.Utilities;
using Xunit;

namespace PSADT.Interop.Tests.Utilities
{
    /// <summary>
    /// Tests the bridge that lets an optional value reach native code as a pointer.
    /// </summary>
    /// <remarks>
    /// The nullable is taken by <c language="csharp">ref readonly</c> so the pointer refers to the caller's storage. Taken by
    /// value the compiler would copy it into the helper's own frame, and the returned pointer would dangle
    /// the moment the helper returned. That was a real defect here, so the first test below reads the value
    /// back through the pointer rather than merely checking it is not null.
    /// <para>
    /// The parameter kind is what enforces that now rather than a convention: an argument passed by value
    /// is refused outright, which the last test asserts against the signature since it cannot be written.
    /// </para>
    /// </remarks>
    public sealed class NullableUtilitiesTests
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
                int* presentPointer = NullableUtilities.ToPointer(in present);
                long* widerPointer = NullableUtilities.ToPointer(in wider);

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
                Assert.True(NullableUtilities.ToPointer(in absent) is null);
            }
        }

        /// <summary>
        /// Verifies that the value is taken in a way that obliges the caller to hand over storage of their
        /// own, since that is the whole guarantee the returned pointer rests on.
        /// </summary>
        /// <remarks>
        /// Asserted against the signature because the alternative cannot be written: an argument passed by
        /// value to a <c language="csharp">ref readonly</c> parameter is a compile error, so there is no call to make here
        /// that would fail at runtime. An <c language="csharp">in</c> parameter would accept one and copy it silently,
        /// which is what this is guarding against.
        /// </remarks>
        [Fact]
        public void ToPointer_TakesTheValueByReadOnlyReference()
        {
            // Arrange
            MethodInfo? method = typeof(NullableUtilities).GetMethod(nameof(NullableUtilities.ToPointer), BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(method);

            // Act
            ParameterInfo parameter = Assert.Single(method.GetParameters());

            // Assert
            Assert.True(parameter.ParameterType.IsByRef);
            Assert.True(parameter.IsIn);
        }
    }
}
