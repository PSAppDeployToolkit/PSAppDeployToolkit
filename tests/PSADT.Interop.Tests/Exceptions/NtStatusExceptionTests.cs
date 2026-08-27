using System;
#if !NET8_0_OR_GREATER
using System.Reflection;
using System.Runtime.Serialization;
#endif
using PSADT.Interop.Exceptions;
using PSADT.Interop.Utilities;
using Windows.Win32.Foundation;
using Xunit;

namespace PSADT.Interop.Tests.Exceptions
{
    /// <summary>
    /// Tests NtStatusException, which carries an NTSTATUS through the managed exception hierarchy and
    /// builds a readable message for it out of ntdll's message table.
    /// </summary>
    /// <remarks>
    /// The symbolic names in its messages come from reflecting over the non-public constants of the
    /// CsWin32-generated NTSTATUS type. That is a dependency on generated code that could silently empty
    /// itself if the generator changed shape, so the message assertions below double as a check that the
    /// lookup is still populated.
    /// </remarks>
    public sealed class NtStatusExceptionTests
    {
        /// <summary>
        /// Verifies that the raw NTSTATUS is preserved on ErrorCode rather than being replaced by the
        /// derived HRESULT, since callers compare it against the status constants.
        /// </summary>
        [Fact]
        public void ErrorCode_CarriesTheRawNtStatus()
        {
            // Act
            NtStatusException exception = new(NTSTATUS.STATUS_ACCESS_DENIED);

            // Assert
            Assert.Equal(NTSTATUS.STATUS_ACCESS_DENIED.Value, exception.ErrorCode);
        }

        /// <summary>
        /// Verifies that HResult carries the NTSTATUS folded under the NT facility, so the exception
        /// interoperates with code that switches on HResult while ErrorCode keeps the original status.
        /// </summary>
        [Fact]
        public void HResult_CarriesTheStatusUnderTheNtFacility()
        {
            // Act
            NtStatusException exception = new(NTSTATUS.STATUS_ACCESS_DENIED);

            // Assert
            Assert.Equal(ExceptionUtilities.HRESULT_FROM_NT(NTSTATUS.STATUS_ACCESS_DENIED).Value, exception.HResult);
            Assert.NotEqual(exception.ErrorCode, exception.HResult);
        }

        /// <summary>
        /// Verifies that the status round-trips back out as an NTSTATUS rather than only as an integer.
        /// </summary>
        [Fact]
        public void NtStatus_RoundTripsTheConstructorArgument()
        {
            // Arrange
            NTSTATUS status = new(unchecked((int)0xC0000022));

            // Act
            NtStatusException exception = new(status);

            // Assert
            Assert.Equal(status, exception.NtStatus);
        }

        /// <summary>
        /// Verifies that the generated message names the status both numerically and symbolically, which
        /// is the whole point of the type over a bare ExternalException.
        /// </summary>
        [Fact]
        public void Message_NamesTheStatusNumericallyAndSymbolically()
        {
            // Act
            NtStatusException exception = new(NTSTATUS.STATUS_ACCESS_DENIED);

            // Assert
            Assert.EndsWith("(Exception from NTSTATUS: 0xC0000022 (STATUS_ACCESS_DENIED))", exception.Message, StringComparison.Ordinal);
            Assert.DoesNotContain("..", exception.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that statuses drawn from across the range are all named symbolically, not just the one
        /// above.
        /// </summary>
        /// <remarks>
        /// The name map is built by reflecting over the non-public static fields of a generated type, which
        /// currently carries over sixteen hundred of them. A change in how the generator emits those
        /// constants could leave the map partly or wholly empty, and nothing else in the type would notice:
        /// the messages would simply stop naming the status and carry the number alone.
        /// </remarks>
        [Fact]
        public void Message_NamesStatusesFromAcrossTheRange()
        {
            // Assert
            AssertNamed(NTSTATUS.STATUS_NOT_IMPLEMENTED, nameof(NTSTATUS.STATUS_NOT_IMPLEMENTED));
            AssertNamed(NTSTATUS.STATUS_INFO_LENGTH_MISMATCH, nameof(NTSTATUS.STATUS_INFO_LENGTH_MISMATCH));
            AssertNamed(NTSTATUS.STATUS_INVALID_HANDLE, nameof(NTSTATUS.STATUS_INVALID_HANDLE));
            AssertNamed(NTSTATUS.STATUS_BUFFER_TOO_SMALL, nameof(NTSTATUS.STATUS_BUFFER_TOO_SMALL));
            AssertNamed(NTSTATUS.STATUS_OBJECT_NAME_NOT_FOUND, nameof(NTSTATUS.STATUS_OBJECT_NAME_NOT_FOUND));
        }

        /// <summary>
        /// Asserts that the message for a status names it symbolically. A private helper rather than theory
        /// data because NTSTATUS is internal and xUnit requires test methods to be public, which rules it
        /// out of a public signature.
        /// </summary>
        /// <param name="ntStatus">The status whose message should name it.</param>
        /// <param name="name">The symbolic name the message is expected to carry.</param>
        private static void AssertNamed(NTSTATUS ntStatus, string name)
        {
            Assert.EndsWith($"(Exception from NTSTATUS: 0x{ntStatus.Value:X8} ({name}))", new NtStatusException(ntStatus).Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that a status ntdll has no text for still produces a usable message carrying the
        /// numeric value, rather than an empty string or a thrown exception from the lookup.
        /// </summary>
        [Fact]
        public void Message_FallsBackForAStatusWithNoDescription()
        {
            // Arrange
            NTSTATUS undefined = new(unchecked((int)0xC0BB0001));

            // Act
            NtStatusException exception = new(undefined);

            // Assert
            Assert.EndsWith("(Exception from NTSTATUS: 0xC0BB0001)", exception.Message, StringComparison.Ordinal);
            Assert.NotEmpty(exception.Message);
        }

        /// <summary>
        /// Verifies that a caller-supplied message replaces the generated one entirely, so a caller with
        /// better context is not forced to accept the generic text.
        /// </summary>
        [Fact]
        public void Message_HonoursAnExplicitMessage()
        {
            // Act
            NtStatusException exception = new(NTSTATUS.STATUS_ACCESS_DENIED, "something specific went wrong");

            // Assert
            Assert.Equal("something specific went wrong", exception.Message);
        }

        /// <summary>
        /// Verifies that a blank message is treated as no message at all, since an exception whose text
        /// is whitespace would be worse than the generated description.
        /// </summary>
        /// <param name="message">The blank message under test.</param>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Message_TreatsBlankAsAbsent(string message)
        {
            // Act
            NtStatusException exception = new(NTSTATUS.STATUS_ACCESS_DENIED, message);

            // Assert
            Assert.EndsWith("(Exception from NTSTATUS: 0xC0000022 (STATUS_ACCESS_DENIED))", exception.Message, StringComparison.Ordinal);
        }

#if !NET8_0_OR_GREATER
        /// <summary>
        /// Verifies that the status survives a serialization round-trip. The base class does not know
        /// about the status, so it is written and read back by this type's own two members, and losing it
        /// would leave a revived exception describing nothing.
        /// </summary>
        /// <remarks>
        /// Exercised on the framework leg only. The formatter-based serialization APIs this depends on are
        /// obsolete from net8.0 onwards, as are the two members under test, so net472 is where they are
        /// still the live code path.
        /// </remarks>
        [Fact]
        public void Serialization_RoundTripsTheStatus()
        {
            // Arrange
            NtStatusException original = new(NTSTATUS.STATUS_ACCESS_DENIED);
            SerializationInfo info = new(typeof(NtStatusException), new FormatterConverter());
            StreamingContext context = new();

            // Act
            original.GetObjectData(info, context);
            ConstructorInfo constructor = typeof(NtStatusException).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                [typeof(SerializationInfo), typeof(StreamingContext)],
                modifiers: null);
            NtStatusException revived = (NtStatusException)constructor.Invoke([info, context]);

            // Assert
            Assert.Equal(original.ErrorCode, revived.ErrorCode);
            Assert.Equal(original.NtStatus, revived.NtStatus);
            Assert.Equal(original.Message, revived.Message);
        }
#endif
    }
}
