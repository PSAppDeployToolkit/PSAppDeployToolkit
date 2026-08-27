using System;
using PSADT.Interop.Tests.TestHelpers;
using Windows.Win32.Foundation;
using Xunit;

namespace PSADT.Interop.Tests.Extensions
{
    /// <summary>
    /// Tests the state guards PSADT.Interop adds to InvalidOperationException. Unlike the argument
    /// guards these all raise the same exception type, so what matters is which values each one rejects
    /// and that the caller's message survives.
    /// </summary>
    public sealed class InvalidOperationExceptionExtensionsTests
    {
        /// <summary>
        /// The message threaded through the guards, to prove it reaches the exception rather than being
        /// replaced by a generic one.
        /// </summary>
        private const string Message = "the handle was not usable";

        /// <summary>
        /// Verifies that zero is rejected at every width the guard is offered for, and that anything else
        /// passes. Each width is a separate overload, so a copy-and-paste error in one would not show up
        /// in the others.
        /// </summary>
        [Fact]
        public void ThrowIfZero_RejectsZeroAtEveryWidth()
        {
            // Arrange
            nint signedZero = 0;
            nuint unsignedZero = 0;
            nint signedOne = 1;
            nuint unsignedOne = 1;

            // Assert
            Assert.Equal(Message, Assert.Throws<InvalidOperationException>(static () => InvalidOperationException.ThrowIfZero(0u, Message)).Message);
            _ = Assert.Throws<InvalidOperationException>(() => InvalidOperationException.ThrowIfZero(signedZero, Message));
            _ = Assert.Throws<InvalidOperationException>(() => InvalidOperationException.ThrowIfZero(unsignedZero, Message));

            Assert.Null(Record.Exception(static () => InvalidOperationException.ThrowIfZero(1u, Message)));
            Assert.Null(Record.Exception(() => InvalidOperationException.ThrowIfZero(signedOne, Message)));
            Assert.Null(Record.Exception(() => InvalidOperationException.ThrowIfZero(unsignedOne, Message)));
        }

        /// <summary>
        /// Verifies that the all-ones sentinel is rejected while zero is accepted, which is what
        /// separates this guard from the zero one.
        /// </summary>
        [Fact]
        public void ThrowIfInvalid_RejectsOnlyTheAllOnesSentinel()
        {
            // Arrange
            nint signedSentinel = -1;
            nuint unsignedSentinel = unchecked((nuint)(-1));
            nint signedZero = 0;
            nuint unsignedZero = 0;

            // Assert
            _ = Assert.Throws<InvalidOperationException>(() => InvalidOperationException.ThrowIfInvalid(signedSentinel, Message));
            _ = Assert.Throws<InvalidOperationException>(() => InvalidOperationException.ThrowIfInvalid(unsignedSentinel, Message));
            Assert.Null(Record.Exception(() => InvalidOperationException.ThrowIfInvalid(signedZero, Message)));
            Assert.Null(Record.Exception(() => InvalidOperationException.ThrowIfInvalid(unsignedZero, Message)));
        }

        /// <summary>
        /// Verifies that the combined guard rejects both sentinels, since a caller holding a raw address
        /// cannot usually tell which of the two it has.
        /// </summary>
        [Fact]
        public void ThrowIfZeroOrInvalid_RejectsBothSentinels()
        {
            // Arrange
            nint signedZero = 0;
            nint signedSentinel = -1;
            nuint unsignedZero = 0;
            nuint unsignedSentinel = unchecked((nuint)(-1));
            nint signedOne = 1;

            // Assert
            _ = Assert.Throws<InvalidOperationException>(() => InvalidOperationException.ThrowIfZeroOrInvalid(signedZero, Message));
            _ = Assert.Throws<InvalidOperationException>(() => InvalidOperationException.ThrowIfZeroOrInvalid(signedSentinel, Message));
            _ = Assert.Throws<InvalidOperationException>(() => InvalidOperationException.ThrowIfZeroOrInvalid(unsignedZero, Message));
            _ = Assert.Throws<InvalidOperationException>(() => InvalidOperationException.ThrowIfZeroOrInvalid(unsignedSentinel, Message));
            Assert.Null(Record.Exception(() => InvalidOperationException.ThrowIfZeroOrInvalid(signedOne, Message)));
        }

        /// <summary>
        /// Verifies that a null reference is rejected and anything else accepted.
        /// </summary>
        [Fact]
        public void ThrowIfNull_RejectsNullReferences()
        {
            // Arrange
            object? absent = null;
            object? present = new();

            // Assert
            _ = Assert.Throws<InvalidOperationException>(() => InvalidOperationException.ThrowIfNull(absent, Message));
            Assert.Null(Record.Exception(() => InvalidOperationException.ThrowIfNull(present, Message)));
        }

        /// <summary>
        /// Verifies the string-pointer guards. A PWSTR can be unusable in two distinct ways, holding
        /// nothing or holding the invalid-handle sentinel, and the three guards divide that space
        /// differently.
        /// </summary>
        [Fact]
        public void PwstrGuards_RejectNullAndSentinelPointers()
        {
            // Assert: nothing at all
            _ = Assert.Throws<InvalidOperationException>(static () =>
            {
                PWSTR nothing = default;
                InvalidOperationException.ThrowIfNull(nothing, Message);
            });
            _ = Assert.Throws<InvalidOperationException>(static () =>
            {
                PWSTR nothing = default;
                InvalidOperationException.ThrowIfNullOrInvalid(nothing, Message);
            });

            // Assert: the invalid-handle sentinel
            _ = Assert.Throws<InvalidOperationException>(static () =>
            {
                unsafe
                {
                    InvalidOperationException.ThrowIfInvalid(unchecked((PWSTR)(char*)(nint)(-1)), Message);
                }
            });
            _ = Assert.Throws<InvalidOperationException>(static () =>
            {
                unsafe
                {
                    InvalidOperationException.ThrowIfNullOrInvalid(unchecked((PWSTR)(char*)(nint)(-1)), Message);
                }
            });

            // Assert: a real pointer passes all three
            Assert.Null(Record.Exception(static () =>
            {
                unsafe
                {
                    fixed (char* buffer = "value")
                    {
                        InvalidOperationException.ThrowIfNull((PWSTR)buffer, Message);
                        InvalidOperationException.ThrowIfInvalid((PWSTR)buffer, Message);
                        InvalidOperationException.ThrowIfNullOrInvalid((PWSTR)buffer, Message);
                    }
                }
            }));
        }

        /// <summary>
        /// Verifies that the handle guard rejects all three unusable states. The implementation folds
        /// them into a single expression, so each state needs reaching separately to know that the
        /// expression covers it.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "This is deliberate as part of unit testing.")]
        [Fact]
        public void ThrowIfNullOrInvalid_RejectsAbsentClosedAndInvalidHandles()
        {
            // Arrange
            using TestSafeHandle valid = new(1);
            using TestSafeHandle invalid = new(0);
            TestSafeHandle closed = new(1);
            closed.Dispose();

            // Assert
            _ = Assert.Throws<InvalidOperationException>(static () => InvalidOperationException.ThrowIfNullOrInvalid<TestSafeHandle>(null!, Message));
            _ = Assert.Throws<InvalidOperationException>(() => InvalidOperationException.ThrowIfNullOrInvalid(closed, Message));
            _ = Assert.Throws<InvalidOperationException>(() => InvalidOperationException.ThrowIfNullOrInvalid(invalid, Message));
            Assert.Null(Record.Exception(() => InvalidOperationException.ThrowIfNullOrInvalid(valid, Message)));
        }

        /// <summary>
        /// Verifies that odd lengths are rejected and even ones accepted, including zero. This guards
        /// buffer sizes that must divide by the size of a character, so zero has to pass.
        /// </summary>
        /// <param name="length">The length under test.</param>
        /// <param name="shouldThrow">Whether the guard is expected to reject it.</param>
        [Theory]
        [InlineData(0u, false)]
        [InlineData(1u, true)]
        [InlineData(2u, false)]
        [InlineData(3u, true)]
        [InlineData(uint.MaxValue, true)]
        public void ThrowIfNotEven_RejectsOddLengths(uint length, bool shouldThrow)
        {
            // Act
            Exception? exception = Record.Exception(() => InvalidOperationException.ThrowIfNotEven(length, Message));

            // Assert
            if (shouldThrow)
            {
                _ = Assert.IsType<InvalidOperationException>(exception);
            }
            else
            {
                Assert.Null(exception);
            }
        }

        /// <summary>
        /// Verifies that the boundary itself is accepted and only values beyond it rejected.
        /// </summary>
        /// <param name="value">The value under test.</param>
        /// <param name="shouldThrow">Whether the guard is expected to reject it.</param>
        [Theory]
        [InlineData(5u, true)]
        [InlineData(4u, false)]
        [InlineData(0u, false)]
        public void ThrowIfGreaterThan_RejectsOnlyValuesBeyondTheBoundary(uint value, bool shouldThrow)
        {
            // Act
            Exception? exception = Record.Exception(() => InvalidOperationException.ThrowIfGreaterThan(value, 4u, Message));

            // Assert
            if (shouldThrow)
            {
                _ = Assert.IsType<InvalidOperationException>(exception);
            }
            else
            {
                Assert.Null(exception);
            }
        }
    }
}
