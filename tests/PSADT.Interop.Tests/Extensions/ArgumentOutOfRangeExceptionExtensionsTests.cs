using System;
using Xunit;

namespace PSADT.Interop.Tests.Extensions
{
    /// <summary>
    /// Tests the sentinel guards added to ArgumentOutOfRangeException, which reject the two values a
    /// native call uses to mean "this is not a usable handle": zero and all ones.
    /// </summary>
    /// <remarks>
    /// The unsigned overloads are the interesting half. Their sentinel comparison used to be written in a
    /// way that overflowed under this repository's checked arithmetic, which made one of them throw for
    /// every non-zero input it was given. The acceptance tests below are what pin that.
    /// </remarks>
    public sealed class ArgumentOutOfRangeExceptionExtensionsTests
    {
        /// <summary>
        /// Verifies that the native-integer guard rejects both sentinels and carries the offending value.
        /// </summary>
        /// <param name="value">The value expected to be rejected.</param>
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void ThrowIfZeroOrInvalid_RejectsBothSentinels(int value)
        {
            // Act & Assert
            nint sentinel = value;
            ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => ArgumentOutOfRangeException.ThrowIfZeroOrInvalid(sentinel));
            Assert.Equal(sentinel, exception.ActualValue);
        }

        /// <summary>
        /// Verifies that a usable address passes, so the guard is not simply rejecting everything.
        /// </summary>
        [Fact]
        public void ThrowIfZeroOrInvalid_AcceptsUsableValues()
        {
            // Assert
            nint signed = 1;
            nuint unsigned = 1;
            Assert.Null(Record.Exception(() => ArgumentOutOfRangeException.ThrowIfZeroOrInvalid(signed)));
            Assert.Null(Record.Exception(() => ArgumentOutOfRangeException.ThrowIfZeroOrInvalid(unsigned)));
        }

        /// <summary>
        /// Verifies that the invalid-only guard rejects the all-ones sentinel while accepting zero, which
        /// is the difference between it and the guard above.
        /// </summary>
        [Fact]
        public void ThrowIfInvalid_RejectsOnlyTheAllOnesSentinel()
        {
            // Assert
            nint signedSentinel = -1;
            nuint unsignedSentinel = unchecked((nuint)(-1));
            nint signedZero = 0;
            nuint unsignedZero = 0;
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => ArgumentOutOfRangeException.ThrowIfInvalid(signedSentinel));
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => ArgumentOutOfRangeException.ThrowIfInvalid(unsignedSentinel));
            Assert.Null(Record.Exception(() => ArgumentOutOfRangeException.ThrowIfInvalid(signedZero)));
            Assert.Null(Record.Exception(() => ArgumentOutOfRangeException.ThrowIfInvalid(unsignedZero)));
        }

        /// <summary>
        /// Verifies that the two unsigned guards agree on what the invalid sentinel is. They spell it
        /// differently in the source, one casting INVALID_HANDLE_VALUE and the other using an unchecked
        /// minus one, so this pins that the two spellings describe the same value.
        /// </summary>
        [Fact]
        public void UnsignedGuards_AgreeOnTheInvalidSentinel()
        {
            // Arrange
            nuint sentinel = unchecked((nuint)(-1));

            // Assert
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => ArgumentOutOfRangeException.ThrowIfZeroOrInvalid(sentinel));
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => ArgumentOutOfRangeException.ThrowIfInvalid(sentinel));
        }
    }
}
