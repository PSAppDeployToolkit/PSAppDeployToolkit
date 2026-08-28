using System;
using PSADT.Interop.Tests.TestHelpers;
using Windows.Win32.Foundation;
using Xunit;

namespace PSADT.Interop.Tests.Extensions
{
    /// <summary>
    /// Tests the argument guards added to ArgumentException, which validate the span, handle and counted
    /// string shapes the Win32 layer deals in.
    /// </summary>
    /// <remarks>
    /// Each guard's contract is the exception type it chooses, since callers distinguish "you passed
    /// nothing" from "you passed something unusable" from "you passed something already disposed". The
    /// tests therefore assert the type as much as the fact that something was thrown.
    /// </remarks>
    public sealed class ArgumentExceptionExtensionsTests
    {
        /// <summary>
        /// Verifies that an empty or whitespace-only span is rejected, including the whitespace forms a
        /// length check alone would accept.
        /// </summary>
        [Fact]
        public void ThrowIfEmptyOrWhiteSpace_RejectsEmptyAndWhiteSpace()
        {
            // Assert
            _ = Assert.Throws<ArgumentException>(static () => ArgumentException.ThrowIfEmptyOrWhiteSpace(string.Empty.AsSpan()));
            _ = Assert.Throws<ArgumentException>(static () => ArgumentException.ThrowIfEmptyOrWhiteSpace(" ".AsSpan()));
            _ = Assert.Throws<ArgumentException>(static () => ArgumentException.ThrowIfEmptyOrWhiteSpace("\t\r\n".AsSpan()));
        }

        /// <summary>
        /// Verifies that any non-whitespace content is accepted, including content that is only partly
        /// whitespace.
        /// </summary>
        [Fact]
        public void ThrowIfEmptyOrWhiteSpace_AcceptsContent()
        {
            // Assert
            Assert.Null(Record.Exception(static () => ArgumentException.ThrowIfEmptyOrWhiteSpace("a".AsSpan())));
            Assert.Null(Record.Exception(static () => ArgumentException.ThrowIfEmptyOrWhiteSpace("  a  ".AsSpan())));
        }

        /// <summary>
        /// Verifies that an explicitly supplied parameter name reaches the exception, since the captured
        /// caller expression for a span argument is rarely useful on its own.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S3236:Caller information arguments should not be provided explicitly", Justification = "This is intentional as we're testing a parameter member.")]
        [Fact]
        public void ThrowIfEmptyOrWhiteSpace_HonoursExplicitParameterName()
        {
            // Act & Assert
            ArgumentException exception = Assert.Throws<ArgumentException>(static () => ArgumentException.ThrowIfEmptyOrWhiteSpace(string.Empty.AsSpan(), "explicitName"));
            Assert.Equal("explicitName", exception.ParamName);
        }

        /// <summary>
        /// Verifies that a handle guard distinguishes absent from disposed, since a caller recovers from
        /// those differently.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "This is deliberate as part of unit testing.")]
        [Fact]
        public void ThrowIfNullOrClosed_DistinguishesNullFromDisposed()
        {
            // Arrange
            TestSafeHandle closed = new(1);
            closed.Dispose();

            // Assert
            _ = Assert.Throws<ArgumentNullException>(static () => ArgumentException.ThrowIfNullOrClosed(null!));
            _ = Assert.Throws<ObjectDisposedException>(() => ArgumentException.ThrowIfNullOrClosed(closed));
            using TestSafeHandle open = new(1);
            Assert.Null(Record.Exception(() => ArgumentException.ThrowIfNullOrClosed(open)));
        }

        /// <summary>
        /// Verifies that the stricter handle guard adds the invalid case on top of absent and disposed,
        /// and reports it as out of range rather than as null.
        /// </summary>
        /// <param name="handle">The invalid handle value under test.</param>
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void ThrowIfNullOrInvalid_RejectsInvalidHandles(int handle)
        {
            // Arrange
            using TestSafeHandle invalid = new(handle);

            // Assert
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => ArgumentException.ThrowIfNullOrInvalid(invalid));
        }

        /// <summary>
        /// Verifies that a usable handle passes the stricter guard.
        /// </summary>
        [Fact]
        public void ThrowIfNullOrInvalid_AcceptsUsableHandles()
        {
            // Assert
            using TestSafeHandle open = new(1);
            Assert.Null(Record.Exception(() => ArgumentException.ThrowIfNullOrInvalid(open)));
        }

        /// <summary>
        /// Verifies that a UNICODE_STRING must have both a buffer and a length to be usable, and that the
        /// two failures are reported differently.
        /// </summary>
        [Fact]
        public void ThrowIfNullOrInvalid_ValidatesUnicodeString()
        {
            // Assert: a null buffer is an absent argument
            _ = Assert.Throws<ArgumentNullException>(static () => ArgumentException.ThrowIfNullOrInvalid(new UNICODE_STRING { Length = 4, MaximumLength = 6 }));

            // Assert: a present but empty string is a bad argument
            _ = Assert.Throws<ArgumentException>(static () =>
            {
                unsafe
                {
                    fixed (char* buffer = "ab")
                    {
                        ArgumentException.ThrowIfNullOrInvalid(new UNICODE_STRING { Length = 0, MaximumLength = 6, Buffer = buffer });
                    }
                }
            });

            // Assert: a populated string is accepted
            Assert.Null(Record.Exception(static () =>
            {
                unsafe
                {
                    fixed (char* buffer = "ab")
                    {
                        ArgumentException.ThrowIfNullOrInvalid(new UNICODE_STRING { Length = 4, MaximumLength = 6, Buffer = buffer });
                    }
                }
            }));
        }

        /// <summary>
        /// Verifies the weaker UNICODE_STRING guard, which permits an empty string but not a buffer and
        /// capacity that contradict each other.
        /// </summary>
        [Fact]
        public void ThrowIfInvalid_RejectsContradictoryUnicodeString()
        {
            // Assert: capacity claimed with nothing to hold it
            _ = Assert.Throws<ArgumentNullException>(static () => ArgumentException.ThrowIfInvalid(new UNICODE_STRING { MaximumLength = 6 }));

            // Assert: a buffer with no capacity
            _ = Assert.Throws<ArgumentException>(static () =>
            {
                unsafe
                {
                    fixed (char* buffer = "ab")
                    {
                        ArgumentException.ThrowIfInvalid(new UNICODE_STRING { MaximumLength = 0, Buffer = buffer });
                    }
                }
            });

            // Assert: a wholly empty string is consistent, and so is a populated one
            Assert.Null(Record.Exception(static () => ArgumentException.ThrowIfInvalid(default)));
            Assert.Null(Record.Exception(static () =>
            {
                unsafe
                {
                    fixed (char* buffer = "ab")
                    {
                        ArgumentException.ThrowIfInvalid(new UNICODE_STRING { Length = 4, MaximumLength = 6, Buffer = buffer });
                    }
                }
            }));
        }
    }
}
