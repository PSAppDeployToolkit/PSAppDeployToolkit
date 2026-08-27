using System;
using Xunit;

namespace PSADT.Interop.Tests.Polyfills
{
    /// <summary>
    /// Tests the argument-validation polyfills. These carry by far the most call sites in the toolkit,
    /// and they also exercise the CallerArgumentExpressionAttribute type polyfill, since every one of
    /// them derives its parameter name from the caller's expression.
    /// </summary>
    /// <remarks>
    /// The generic ArgumentOutOfRangeException helpers are constrained differently on each target: the
    /// polyfill takes any comparable struct and switches over the concrete numeric types, while the
    /// framework requires INumber&lt;T&gt;. The types exercised below satisfy both, so the same assertions
    /// compile and run on either leg. A type the polyfill's switch does not know throws
    /// InvalidOperationException rather than failing to compile, which narrows the framework contract but
    /// is not reached by any toolkit call site.
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "MA0015:The expression does not match a parameter", Justification = "These guards capture the caller expression, and the tests deliberately pass locals rather than parameters.")]
    public sealed class ThrowHelperPolyfillTests
    {
        /// <summary>
        /// Verifies that null is rejected as ArgumentNullException rather than ArgumentException, which is
        /// the distinction the framework draws for this helper.
        /// </summary>
        [Fact]
        public void ArgumentException_ThrowIfNullOrWhiteSpace_ThrowsArgumentNullExceptionForNull()
        {
            // Arrange
            const string? argument = null;

            // Act & Assert
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(static () => ArgumentException.ThrowIfNullOrWhiteSpace(argument));
            Assert.Equal("argument", exception.ParamName);
        }

        /// <summary>
        /// Verifies that empty and whitespace-only values are rejected, across the whitespace forms that
        /// a naive length check would miss.
        /// </summary>
        /// <param name="argument">The value expected to be rejected.</param>
        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("   ")]
        [InlineData("\t")]
        [InlineData("\r\n")]
        [InlineData("\u00A0")]
        public void ArgumentException_ThrowIfNullOrWhiteSpace_ThrowsForEmptyOrWhiteSpace(string argument)
        {
            // Act & Assert
            ArgumentException exception = Assert.Throws<ArgumentException>(() => ArgumentException.ThrowIfNullOrWhiteSpace(argument));
            Assert.Equal(nameof(argument), exception.ParamName);
        }

        /// <summary>
        /// Verifies that a value with any non-whitespace content is accepted, including one that is only
        /// partly whitespace and one whose only content is a null character.
        /// </summary>
        /// <param name="argument">The value expected to be accepted.</param>
        [Theory]
        [InlineData("a")]
        [InlineData(" a ")]
        [InlineData("\0")]
        public void ArgumentException_ThrowIfNullOrWhiteSpace_AcceptsNonWhiteSpace(string argument)
        {
            // Act & Assert
            Assert.Null(Record.Exception(() => ArgumentException.ThrowIfNullOrWhiteSpace(argument)));
        }

        /// <summary>
        /// Verifies that a null reference is rejected and that the parameter name is taken from the
        /// caller's expression rather than being left null.
        /// </summary>
        [Fact]
        public void ArgumentNullException_ThrowIfNull_ThrowsAndNamesTheCallerExpression()
        {
            // Arrange
            object? argument = null;

            // Act & Assert
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => ArgumentNullException.ThrowIfNull(argument));
            Assert.Equal("argument", exception.ParamName);
        }

        /// <summary>
        /// Verifies that an explicitly supplied parameter name wins over the captured expression.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S3236:Caller information arguments should not be provided explicitly", Justification = "This is intentional as we're testing a parameter member.")]
        [Fact]
        public void ArgumentNullException_ThrowIfNull_HonoursExplicitParameterName()
        {
            // Act & Assert
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(static () => ArgumentNullException.ThrowIfNull((object?)null, paramName: "explicitName"));
            Assert.Equal("explicitName", exception.ParamName);
        }

        /// <summary>
        /// Verifies that a non-null reference is accepted.
        /// </summary>
        [Fact]
        public void ArgumentNullException_ThrowIfNull_AcceptsNonNull()
        {
            // Arrange
            object? argument = new();

            // Act & Assert
            Assert.Null(Record.Exception(() => ArgumentNullException.ThrowIfNull(argument)));
        }

        /// <summary>
        /// Verifies the unmanaged pointer overload, which PSADT.Interop needs for the CsWin32 pointer
        /// types and which cannot share a code path with the object overload.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2264:Do not pass a non-nullable value to 'ArgumentNullException.ThrowIfNull'", Justification = "Exercising the pointer overload with a valid pointer is the point of the test.")]
        [Fact]
        public void ArgumentNullException_ThrowIfNull_HandlesPointers()
        {
            unsafe
            {
                // Arrange
                int local = 1;
                void* nullPointer = null;
                void* validPointer = &local;

                // Act & Assert
                _ = Assert.Throws<ArgumentNullException>(() => ArgumentNullException.ThrowIfNull(nullPointer));
                ArgumentNullException.ThrowIfNull(validPointer);
                Assert.Equal(1, local);
            }
        }

        /// <summary>
        /// Verifies that zero is rejected and any other value accepted, and that the rejected value is
        /// carried on the exception so callers can report it.
        /// </summary>
        /// <param name="value">The value passed to the guard.</param>
        /// <param name="shouldThrow">Whether the guard is expected to reject the value.</param>
        [Theory]
        [InlineData(0, true)]
        [InlineData(1, false)]
        [InlineData(-1, false)]
        public void ArgumentOutOfRangeException_ThrowIfZero(int value, bool shouldThrow)
        {
            // Act & Assert
            AssertThrowsWhen(shouldThrow, value, static v => ArgumentOutOfRangeException.ThrowIfZero(v));
        }

        /// <summary>
        /// Verifies that negative values are rejected while zero and positive values are accepted.
        /// </summary>
        /// <param name="value">The value passed to the guard.</param>
        /// <param name="shouldThrow">Whether the guard is expected to reject the value.</param>
        [Theory]
        [InlineData(-1, true)]
        [InlineData(int.MinValue, true)]
        [InlineData(0, false)]
        [InlineData(1, false)]
        public void ArgumentOutOfRangeException_ThrowIfNegative(int value, bool shouldThrow)
        {
            // Act & Assert
            AssertThrowsWhen(shouldThrow, value, static v => ArgumentOutOfRangeException.ThrowIfNegative(v));
        }

        /// <summary>
        /// Verifies that zero joins the negative values in being rejected, which is the only difference
        /// from ThrowIfNegative and an easy one to get wrong.
        /// </summary>
        /// <param name="value">The value passed to the guard.</param>
        /// <param name="shouldThrow">Whether the guard is expected to reject the value.</param>
        [Theory]
        [InlineData(-1, true)]
        [InlineData(0, true)]
        [InlineData(1, false)]
        public void ArgumentOutOfRangeException_ThrowIfNegativeOrZero(int value, bool shouldThrow)
        {
            // Act & Assert
            AssertThrowsWhen(shouldThrow, value, static v => ArgumentOutOfRangeException.ThrowIfNegativeOrZero(v));
        }

        /// <summary>
        /// Verifies that the boundary itself is accepted and only values beyond it are rejected.
        /// </summary>
        /// <param name="value">The value passed to the guard.</param>
        /// <param name="shouldThrow">Whether the guard is expected to reject the value.</param>
        [Theory]
        [InlineData(5, true)]
        [InlineData(4, false)]
        [InlineData(3, false)]
        public void ArgumentOutOfRangeException_ThrowIfGreaterThan(int value, bool shouldThrow)
        {
            // Act & Assert
            AssertThrowsWhen(shouldThrow, value, static v => ArgumentOutOfRangeException.ThrowIfGreaterThan(v, 4));
        }

        /// <summary>
        /// Verifies that the boundary itself is accepted and only values below it are rejected.
        /// </summary>
        /// <param name="value">The value passed to the guard.</param>
        /// <param name="shouldThrow">Whether the guard is expected to reject the value.</param>
        [Theory]
        [InlineData(3, true)]
        [InlineData(4, false)]
        [InlineData(5, false)]
        public void ArgumentOutOfRangeException_ThrowIfLessThan(int value, bool shouldThrow)
        {
            // Act & Assert
            AssertThrowsWhen(shouldThrow, value, static v => ArgumentOutOfRangeException.ThrowIfLessThan(v, 4));
        }

        /// <summary>
        /// Verifies equality comparison rather than ordering, which is what separates this helper from
        /// the range ones.
        /// </summary>
        /// <param name="value">The value passed to the guard.</param>
        /// <param name="shouldThrow">Whether the guard is expected to reject the value.</param>
        [Theory]
        [InlineData(3, true)]
        [InlineData(4, false)]
        public void ArgumentOutOfRangeException_ThrowIfNotEqual(int value, bool shouldThrow)
        {
            // Act & Assert
            AssertThrowsWhen(shouldThrow, value, static v => ArgumentOutOfRangeException.ThrowIfNotEqual(v, 4));
        }

        /// <summary>
        /// Verifies that the helpers work across the numeric types the toolkit uses, not just int. The
        /// polyfill switches over concrete types rather than using a numeric interface, so each width and
        /// signedness is a separate code path there.
        /// </summary>
        [Fact]
        public void ArgumentOutOfRangeException_ThrowIfNegativeOrZero_CoversNumericTypes()
        {
            // Act & Assert
            _ = Assert.Throws<ArgumentOutOfRangeException>(static () => ArgumentOutOfRangeException.ThrowIfNegativeOrZero(0L));
            _ = Assert.Throws<ArgumentOutOfRangeException>(static () => ArgumentOutOfRangeException.ThrowIfNegativeOrZero(0u));
            _ = Assert.Throws<ArgumentOutOfRangeException>(static () => ArgumentOutOfRangeException.ThrowIfNegativeOrZero(0d));
            _ = Assert.Throws<ArgumentOutOfRangeException>(static () => ArgumentOutOfRangeException.ThrowIfNegativeOrZero(0m));
            _ = Assert.Throws<ArgumentOutOfRangeException>(static () => ArgumentOutOfRangeException.ThrowIfNegativeOrZero((short)0));

            Assert.Null(Record.Exception(static () =>
            {
                ArgumentOutOfRangeException.ThrowIfNegativeOrZero(1L);
                ArgumentOutOfRangeException.ThrowIfNegativeOrZero(1u);
                ArgumentOutOfRangeException.ThrowIfNegativeOrZero(1d);
                ArgumentOutOfRangeException.ThrowIfNegativeOrZero(1m);
                ArgumentOutOfRangeException.ThrowIfNegativeOrZero((short)1);
            }));
        }

        /// <summary>
        /// Verifies that the disposal guard names the instance's type, since that name is all a caller
        /// sees in the resulting message.
        /// </summary>
        [Fact]
        public void ObjectDisposedException_ThrowIf_NamesTheInstanceType()
        {
            // Act & Assert
            ObjectDisposedException exception = Assert.Throws<ObjectDisposedException>(() => ObjectDisposedException.ThrowIf(condition: true, this));
            Assert.Equal(typeof(ThrowHelperPolyfillTests).FullName, exception.ObjectName);
        }

        /// <summary>
        /// Verifies that a false condition does not throw, which is the overwhelmingly common path given
        /// this helper guards every public member of a disposable type.
        /// </summary>
        [Fact]
        public void ObjectDisposedException_ThrowIf_DoesNotThrowWhenNotDisposed()
        {
            // Act & Assert
            Assert.Null(Record.Exception(() => ObjectDisposedException.ThrowIf(condition: false, this)));
        }

        /// <summary>
        /// Invokes a guard and asserts either that it threw and carried the offending value, or that it
        /// accepted the value, according to the expectation.
        /// </summary>
        /// <param name="shouldThrow">Whether the guard is expected to reject the value.</param>
        /// <param name="value">The value passed to the guard.</param>
        /// <param name="guard">The guard to invoke.</param>
        private static void AssertThrowsWhen(bool shouldThrow, int value, Action<int> guard)
        {
            if (shouldThrow)
            {
                ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => guard(value));
                Assert.Equal(value, exception.ActualValue);
            }
            else
            {
                Assert.Null(Record.Exception(() => guard(value)));
            }
        }
    }
}
