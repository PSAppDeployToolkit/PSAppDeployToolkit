using System.Reflection;
using PSADT.Interop.Tests.TestHelpers;
using Windows.Win32.Foundation;
using Xunit;

namespace PSADT.Interop.Tests
{
    /// <summary>
    /// Tests the pointer-valued constant base, which exists to keep the string-pointer conversion away
    /// from the integer-valued families that share the same generic base.
    /// </summary>
    public sealed class PointerTypedConstantTests
    {
        /// <summary>
        /// Verifies that a constant built from a string pointer keeps the pointer intact in both
        /// directions, which is how the resource and MSI constant families carry their values.
        /// </summary>
        [Fact]
        public void PcwstrRoundTrip_PreservesThePointer()
        {
            unsafe
            {
                // Arrange
                fixed (char* buffer = "value")
                {
                    PointerTestConstant constant = new((PCWSTR)buffer, "Pointer");

                    // Act
                    PCWSTR result = constant.ToPCWSTR();

                    // Assert
                    Assert.Equal((nint)buffer, constant.ToIntPtr());
                    Assert.True(result.Value == buffer);
                }
            }
        }

        /// <summary>
        /// Verifies that the pointer conversion is reachable only from the pointer-valued base, so an
        /// integer-valued family such as a dialog result cannot reinterpret its value as a string
        /// pointer. That separation is the reason this type exists, and it is easy to undo by accident.
        /// </summary>
        [Fact]
        public void ToPCWSTR_IsDeclaredOnlyOnThePointerValuedBase()
        {
            // Arrange
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            // Assert
            Assert.Null(typeof(TypedConstant<TestConstant>).GetMethod("ToPCWSTR", flags));
            Assert.NotNull(typeof(PointerTypedConstant<PointerTestConstant>).GetMethod("ToPCWSTR", flags));
        }
    }
}
