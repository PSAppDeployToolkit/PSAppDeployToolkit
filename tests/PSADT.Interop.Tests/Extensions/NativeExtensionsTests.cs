using System;
using Windows.Win32.Foundation;
using Xunit;

namespace PSADT.Interop.Tests.Extensions
{
    /// <summary>
    /// Tests the thin extensions over the CsWin32 native types: the PWSTR pointer helpers, the
    /// UNICODE_STRING reader, the nullable-to-pointer bridge, and the two status guards that turn a
    /// failure code into the matching managed exception.
    /// </summary>
    public sealed class NativeExtensionsTests
    {
        /// <summary>
        /// Verifies that a PWSTR holding nothing is reported as null and one holding an address is not,
        /// which is the check every native string result is gated on.
        /// </summary>
        [Fact]
        public void PwstrIsNull_DistinguishesNothingFromAnAddress()
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
        /// Verifies that the address round-trips out of a PWSTR, since that is how the pointer reaches
        /// the span and string readers.
        /// </summary>
        [Fact]
        public void PwstrToIntPtr_RoundTripsTheAddress()
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

        /// <summary>
        /// Verifies that a UNICODE_STRING is read using its byte length rather than its buffer capacity,
        /// so trailing capacity is not mistaken for content.
        /// </summary>
        [Fact]
        public void UnicodeStringToManagedString_ReadsTheDeclaredLength()
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
        public void UnicodeStringToManagedString_TrimsPadding()
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

        /// <summary>
        /// Verifies that a nullable with a value yields a pointer to that value, which is what lets a
        /// caller pass an optional structure to native code without copying it.
        /// </summary>
        [Fact]
        public void NullableToPointer_PointsAtTheValue()
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
        public void NullableToPointer_ReturnsNullWhenAbsent()
        {
            unsafe
            {
                // Arrange
                int? absent = null;

                // Act & Assert
                Assert.True(absent.ToPointer() is null);
            }
        }

        /// <summary>
        /// Verifies that a successful status passes through unchanged, so the guard can be chained onto a
        /// native call in an expression.
        /// </summary>
        [Fact]
        public void ThrowOnFailure_PassesSuccessThrough()
        {
            // Assert
            Assert.Equal(NTSTATUS.STATUS_SUCCESS, NTSTATUS.STATUS_SUCCESS.ThrowOnFailure());
            Assert.Equal(WIN32_ERROR.ERROR_SUCCESS, WIN32_ERROR.ERROR_SUCCESS.ThrowOnFailure());
        }

        /// <summary>
        /// Verifies that a failing status is translated the same way as if it had been handed to
        /// ExceptionUtilities directly, so chaining the guard loses nothing.
        /// </summary>
        [Fact]
        public void ThrowOnFailure_TranslatesFailuresToTheManagedEquivalent()
        {
            // Assert
            _ = Assert.Throws<UnauthorizedAccessException>(static () => NTSTATUS.STATUS_ACCESS_DENIED.ThrowOnFailure());
            _ = Assert.Throws<UnauthorizedAccessException>(static () => WIN32_ERROR.ERROR_ACCESS_DENIED.ThrowOnFailure());
        }
    }
}
