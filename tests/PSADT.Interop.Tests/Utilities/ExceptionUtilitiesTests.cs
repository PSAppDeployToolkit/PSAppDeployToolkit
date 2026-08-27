using System;
using System.ComponentModel;
using System.IO;
using PSADT.Interop.Exceptions;
using PSADT.Interop.Utilities;
using Windows.Win32.Foundation;
using Windows.Win32.System.Diagnostics.Debug;
using Xunit;

namespace PSADT.Interop.Tests.Utilities
{
    /// <summary>
    /// Tests ExceptionUtilities: the HRESULT, NTSTATUS and Win32 error code arithmetic, the mapping from
    /// each code family onto the most specific managed exception, and the exception text tidy-up.
    /// </summary>
    public sealed class ExceptionUtilitiesTests
    {
        /// <summary>
        /// Verifies that a failing Win32 error is folded into an HRESULT under the Win32 facility.
        /// </summary>
        [Fact]
        public void HRESULT_FROM_WIN32_WrapsFailureUnderWin32Facility()
        {
            // Act
            HRESULT result = ExceptionUtilities.HRESULT_FROM_WIN32(WIN32_ERROR.ERROR_FILE_NOT_FOUND);

            // Assert
            Assert.Equal(unchecked((int)0x80070002), result.Value);
        }

        /// <summary>
        /// Verifies that success is passed through unchanged rather than being marked as a failure.
        /// </summary>
        [Fact]
        public void HRESULT_FROM_WIN32_LeavesSuccessUnchanged()
        {
            // Act
            HRESULT result = ExceptionUtilities.HRESULT_FROM_WIN32(WIN32_ERROR.ERROR_SUCCESS);

            // Assert
            Assert.Equal(0, result.Value);
        }

        /// <summary>
        /// Verifies that the facility is extracted from bits 16 through 28 of an HRESULT.
        /// </summary>
        [Fact]
        public void HRESULT_FACILITY_ExtractsWin32Facility()
        {
            // Arrange
            HRESULT hResult = new(unchecked((int)0x80070002));

            // Act
            FACILITY_CODE facility = ExceptionUtilities.HRESULT_FACILITY(hResult);

            // Assert
            Assert.Equal(FACILITY_CODE.FACILITY_WIN32, facility);
        }

        /// <summary>
        /// Verifies that the code is extracted from the low word of an HRESULT.
        /// </summary>
        [Fact]
        public void HRESULT_CODE_ExtractsLowWord()
        {
            // Arrange
            HRESULT hResult = new(unchecked((int)0x80070002));

            // Act
            uint code = ExceptionUtilities.HRESULT_CODE(hResult);

            // Assert
            Assert.Equal((uint)WIN32_ERROR.ERROR_FILE_NOT_FOUND, code);
        }

        /// <summary>
        /// Verifies that an NTSTATUS is folded into an HRESULT by setting the NT facility bit and
        /// leaving the remaining bits alone.
        /// </summary>
        [Fact]
        public void HRESULT_FROM_NT_SetsNtFacilityBit()
        {
            // Act
            HRESULT result = ExceptionUtilities.HRESULT_FROM_NT(NTSTATUS.STATUS_ACCESS_DENIED);

            // Assert
            Assert.Equal(unchecked((int)0xD0000022), result.Value);
        }

        /// <summary>
        /// Verifies that an NTSTATUS with a documented Win32 equivalent maps onto it. This queries
        /// ntdll through RtlNtStatusToDosError and does not modify any system state.
        /// </summary>
        [Fact]
        public void WIN32_FROM_NT_MapsKnownStatusToWin32Error()
        {
            // Act
            WIN32_ERROR? result = ExceptionUtilities.WIN32_FROM_NT(NTSTATUS.STATUS_ACCESS_DENIED);

            // Assert
            Assert.Equal(WIN32_ERROR.ERROR_ACCESS_DENIED, result);
        }

        /// <summary>
        /// Verifies that a status with no DOS equivalent is reported as absent rather than as the
        /// ERROR_MR_MID_NOT_FOUND sentinel that ntdll returns, since callers branch on null.
        /// </summary>
        [Fact]
        public void WIN32_FROM_NT_ReturnsNullWhenNoWin32EquivalentExists()
        {
            // Arrange
            NTSTATUS undefined = new(unchecked((int)0xC0BB0001));

            // Act
            WIN32_ERROR? result = ExceptionUtilities.WIN32_FROM_NT(undefined);

            // Assert
            Assert.Null(result);
        }

        /// <summary>
        /// Verifies that the message for a Win32 error carries the code and its symbolic name, which is
        /// what makes an otherwise opaque failure diagnosable.
        /// </summary>
        [Fact]
        public void GetMessageForWin32Error_AppendsTheCodeAndSymbolicName()
        {
            // Act
            string message = ExceptionUtilities.GetMessageForWin32Error(WIN32_ERROR.ERROR_FILE_NOT_FOUND);

            // Assert
            Assert.EndsWith("(Exception from WIN32_ERROR: 0x00000002 (ERROR_FILE_NOT_FOUND))", message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that the suffix can be suppressed, leaving a single trailing full stop rather than
        /// the doubled one a naive concatenation would produce.
        /// </summary>
        [Fact]
        public void GetMessageForWin32Error_OmitsTheSuffixWhenAsked()
        {
            // Act
            string message = ExceptionUtilities.GetMessageForWin32Error(WIN32_ERROR.ERROR_FILE_NOT_FOUND, disableSuffix: true);

            // Assert
            Assert.DoesNotContain("Exception from WIN32_ERROR", message, StringComparison.Ordinal);
            Assert.EndsWith(".", message, StringComparison.Ordinal);
            Assert.DoesNotContain("..", message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that building a message restores the thread's last error to the code it describes.
        /// </summary>
        /// <remarks>
        /// This looks incidental but it is load bearing. GetException(NTSTATUS) builds its Win32Exception
        /// with the constructor that takes a message and an inner exception, which reads its
        /// NativeErrorCode from the thread's last error rather than from an argument. That code is only
        /// correct because this method sets it on the way out, so the two must stay adjacent.
        /// </remarks>
        [Fact]
        public void GetMessageForWin32Error_RestoresTheLastErrorItDescribes()
        {
            // Act
            _ = ExceptionUtilities.GetMessageForWin32Error(WIN32_ERROR.ERROR_ACCESS_DENIED);

            // Assert
            Assert.Equal(WIN32_ERROR.ERROR_ACCESS_DENIED, ExceptionUtilities.GetLastWin32Error());
        }

        /// <summary>
        /// Verifies that a Win32 error with a documented managed equivalent is surfaced as that
        /// equivalent, with the Win32Exception retained underneath so the original code is not lost.
        /// </summary>
        [Fact]
        public void GetException_Win32Error_PrefersTheManagedEquivalent()
        {
            // Assert
            AssertManagedEquivalent<FileNotFoundException>(WIN32_ERROR.ERROR_FILE_NOT_FOUND);
            AssertManagedEquivalent<DirectoryNotFoundException>(WIN32_ERROR.ERROR_PATH_NOT_FOUND);
            AssertManagedEquivalent<UnauthorizedAccessException>(WIN32_ERROR.ERROR_ACCESS_DENIED);
        }

        /// <summary>
        /// Asserts that a Win32 error translates to the given managed exception with the Win32Exception
        /// retained underneath. A private helper rather than theory data because WIN32_ERROR is internal
        /// and xUnit requires test methods to be public, which rules it out of a public signature.
        /// </summary>
        /// <typeparam name="TExpected">The managed exception type expected.</typeparam>
        /// <param name="win32Error">The Win32 error to translate.</param>
        private static void AssertManagedEquivalent<TExpected>(WIN32_ERROR win32Error) where TExpected : Exception
        {
            Exception result = ExceptionUtilities.GetException(win32Error);
            _ = Assert.IsType<TExpected>(result);
            Win32Exception inner = Assert.IsType<Win32Exception>(result.InnerException);
            Assert.Equal(unchecked((int)win32Error), inner.NativeErrorCode);
        }

        /// <summary>
        /// Verifies that a Win32 error with no managed equivalent falls back to the Win32Exception
        /// itself rather than surfacing the COMException that the runtime would otherwise hand back.
        /// </summary>
        [Fact]
        public void GetException_Win32Error_FallsBackToWin32Exception()
        {
            // Act
            Exception result = ExceptionUtilities.GetException(WIN32_ERROR.ERROR_MR_MID_NOT_FOUND);

            // Assert
            Win32Exception win32Exception = Assert.IsType<Win32Exception>(result);
            Assert.Equal(unchecked((int)WIN32_ERROR.ERROR_MR_MID_NOT_FOUND), win32Exception.NativeErrorCode);
        }

        /// <summary>
        /// Verifies that a caller-supplied message replaces the generated one.
        /// </summary>
        [Fact]
        public void GetException_Win32Error_HonoursAnExplicitMessage()
        {
            // Act
            Exception result = ExceptionUtilities.GetException(WIN32_ERROR.ERROR_MR_MID_NOT_FOUND, "custom text");

            // Assert
            Assert.Equal("custom text", result.Message);
        }

        /// <summary>
        /// Verifies the full translation chain for an NTSTATUS that has a Win32 equivalent: the managed
        /// exception on the outside, the Win32Exception carrying the correct native code beneath it, and
        /// the original NtStatusException at the bottom so nothing is discarded.
        /// </summary>
        [Fact]
        public void GetException_NtStatus_BuildsTheFullTranslationChain()
        {
            // Act
            Exception result = ExceptionUtilities.GetException(NTSTATUS.STATUS_ACCESS_DENIED);

            // Assert
            _ = Assert.IsType<UnauthorizedAccessException>(result);
            Win32Exception win32Exception = Assert.IsType<Win32Exception>(result.InnerException);
            Assert.Equal(unchecked((int)WIN32_ERROR.ERROR_ACCESS_DENIED), win32Exception.NativeErrorCode);
            NtStatusException ntStatusException = Assert.IsType<NtStatusException>(win32Exception.InnerException);
            Assert.Equal(NTSTATUS.STATUS_ACCESS_DENIED, ntStatusException.NtStatus);
        }

        /// <summary>
        /// Verifies that an NTSTATUS with no Win32 equivalent still yields an exception carrying the
        /// original status, rather than falling through to nothing.
        /// </summary>
        [Fact]
        public void GetException_NtStatus_FallsBackToNtStatusException()
        {
            // Arrange
            NTSTATUS undefined = new(unchecked((int)0xC0BB0001));

            // Act
            Exception result = ExceptionUtilities.GetException(undefined);

            // Assert
            NtStatusException ntStatusException = Assert.IsType<NtStatusException>(result);
            Assert.Equal(undefined, ntStatusException.NtStatus);
        }

        /// <summary>
        /// Verifies that asking for an exception from a success HRESULT is rejected, since there is no
        /// failure to describe and silently returning something would hide a caller's mistake.
        /// </summary>
        [Fact]
        public void GetException_HResult_RejectsSuccessCodes()
        {
            _ = Assert.Throws<NotSupportedException>(static () => ExceptionUtilities.GetException(new HRESULT(0)));
        }

        /// <summary>
        /// Verifies that an HRESULT under the Win32 facility is unwrapped back to its Win32 error and
        /// translated the same way as if the error had been supplied directly.
        /// </summary>
        [Fact]
        public void GetException_HResult_UnwrapsTheWin32Facility()
        {
            // Act
            Exception result = ExceptionUtilities.GetException(new HRESULT(unchecked((int)0x80070002)));

            // Assert
            _ = Assert.IsType<FileNotFoundException>(result);
        }

        /// <summary>
        /// Verifies that an HRESULT outside the Win32 facility is translated by the runtime rather than
        /// being forced through the Win32 path.
        /// </summary>
        [Fact]
        public void GetException_HResult_DefersToTheRuntimeForOtherFacilities()
        {
            // Act
            Exception result = ExceptionUtilities.GetException(new HRESULT(unchecked((int)0x80004001)));

            // Assert
            _ = Assert.IsType<NotImplementedException>(result);
        }

        /// <summary>
        /// Verifies that the last error is read back as a Win32 error rather than a raw integer, so
        /// callers can compare it against the enumeration without casting.
        /// </summary>
        [Fact]
        public void GetExceptionForLastWin32Error_TranslatesWhateverTheThreadLastRecorded()
        {
            // Arrange
            _ = ExceptionUtilities.GetMessageForWin32Error(WIN32_ERROR.ERROR_ACCESS_DENIED);

            // Act
            Exception result = ExceptionUtilities.GetExceptionForLastWin32Error();

            // Assert
            _ = Assert.IsType<UnauthorizedAccessException>(result);
        }

        /// <summary>
        /// Verifies that a single line of text is returned untouched apart from the line ending.
        /// </summary>
        [Fact]
        public void CollapseInnerExceptionTraceMarkers_LeavesASingleLineAlone()
        {
            // Assert
            Assert.Equal("System.Exception: boom", ExceptionUtilities.CollapseInnerExceptionTraceMarkers("System.Exception: boom"));
        }

        /// <summary>
        /// Verifies that marker lines immediately following the first line are dropped. Those are the
        /// ones left stranded when an exception is rebuilt without its original stack, and they describe
        /// a trace that is no longer there.
        /// </summary>
        [Fact]
        public void CollapseInnerExceptionTraceMarkers_DropsMarkersLeadingTheText()
        {
            // Arrange
            string text = string.Join("\r\n", "System.Exception: boom", " --- End of inner exception stack trace ---", "   --- End of inner exception stack trace ---", "   at Foo.Bar()");

            // Act
            string result = ExceptionUtilities.CollapseInnerExceptionTraceMarkers(text);

            // Assert
            Assert.Equal(string.Join(Environment.NewLine, "System.Exception: boom", "   at Foo.Bar()"), result);
        }

        /// <summary>
        /// Verifies that a marker appearing after real content is kept but left-aligned, so it reads as
        /// a separator between traces rather than as another frame.
        /// </summary>
        [Fact]
        public void CollapseInnerExceptionTraceMarkers_LeftAlignsMarkersFollowingContent()
        {
            // Arrange
            string text = string.Join("\r\n", "System.Exception: boom", "   at Foo.Bar()", "   --- End of inner exception stack trace ---", "   at Baz.Qux()");

            // Act
            string result = ExceptionUtilities.CollapseInnerExceptionTraceMarkers(text);

            // Assert
            Assert.Equal(string.Join(Environment.NewLine, "System.Exception: boom", "   at Foo.Bar()", "--- End of inner exception stack trace ---", "   at Baz.Qux()"), result);
        }

        /// <summary>
        /// Verifies that both line ending conventions are accepted and that blank lines are dropped,
        /// since the text can arrive from either a managed trace or a native one.
        /// </summary>
        [Fact]
        public void CollapseInnerExceptionTraceMarkers_NormalisesMixedLineEndingsAndDropsBlanks()
        {
            // Arrange
            const string text = "System.Exception: boom\n\r\n   at Foo.Bar()\n";

            // Act
            string result = ExceptionUtilities.CollapseInnerExceptionTraceMarkers(text);

            // Assert
            Assert.Equal(string.Join(Environment.NewLine, "System.Exception: boom", "   at Foo.Bar()"), result);
        }

        /// <summary>
        /// Verifies that text consisting only of a first line and markers collapses to that line, which
        /// is the degenerate case a naive implementation would leave an empty trailing line on.
        /// </summary>
        [Fact]
        public void CollapseInnerExceptionTraceMarkers_CollapsesToTheFirstLineWhenOnlyMarkersFollow()
        {
            // Arrange
            string text = string.Join("\r\n", "System.Exception: boom", "   --- End of inner exception stack trace ---", "--- End of stack trace ---");

            // Act
            string result = ExceptionUtilities.CollapseInnerExceptionTraceMarkers(text);

            // Assert
            Assert.Equal("System.Exception: boom", result);
        }

        /// <summary>
        /// Verifies that a null or blank input is rejected, since there is no meaningful result and the
        /// implementation indexes the first line unconditionally.
        /// </summary>
        [Fact]
        public void CollapseInnerExceptionTraceMarkers_RejectsNullOrBlankInput()
        {
            // Arrange
            const string? nullText = null;

            // Assert
            _ = Assert.Throws<ArgumentNullException>(static () => ExceptionUtilities.CollapseInnerExceptionTraceMarkers(nullText!));
            _ = Assert.Throws<ArgumentException>(static () => ExceptionUtilities.CollapseInnerExceptionTraceMarkers(string.Empty));
            _ = Assert.Throws<ArgumentException>(static () => ExceptionUtilities.CollapseInnerExceptionTraceMarkers("   \r\n  "));
        }
    }
}
