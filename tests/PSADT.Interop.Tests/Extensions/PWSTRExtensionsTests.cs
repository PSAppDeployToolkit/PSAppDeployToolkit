using Windows.Win32.Foundation;
using Xunit;

namespace PSADT.Interop.Tests.Extensions
{
    /// <summary>
    /// Tests the two helpers over the native string pointer: the emptiness check every native string
    /// result is gated on, and the address extraction that feeds the span and string readers.
    /// </summary>
    public sealed class PWSTRExtensionsTests
    {
        /// <summary>
        /// Verifies that a pointer holding nothing is reported as null and one holding an address is not,
        /// which is the check every native string result is gated on.
        /// </summary>
        [Fact]
        public void IsNull_DistinguishesNothingFromAnAddress()
        {
            unsafe
            {
                // Arrange
                PWSTR nothing = default;

                // Assert
                Assert.True(nothing.IsNull());

                fixed (char* buffer = "value")
                {
                    Assert.False(((PWSTR)buffer).IsNull());
                }
            }
        }

        /// <summary>
        /// Verifies that the address round-trips out of the pointer, since that is how it reaches the span
        /// and string readers.
        /// </summary>
        [Fact]
        public void ToIntPtr_RoundTripsTheAddress()
        {
            unsafe
            {
                // Assert
                Assert.Equal(0, default(PWSTR).ToIntPtr());

                fixed (char* buffer = "value")
                {
                    Assert.Equal((nint)buffer, ((PWSTR)buffer).ToIntPtr());
                }
            }
        }
    }
}
