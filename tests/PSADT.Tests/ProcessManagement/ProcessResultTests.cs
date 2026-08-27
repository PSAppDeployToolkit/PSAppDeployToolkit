using PSADT.ProcessManagement;
using Xunit;

namespace PSADT.Tests.ProcessManagement
{
    /// <summary>
    /// Tests the result of a launched process, and the exception that wraps a failing one.
    /// </summary>
    /// <remarks>
    /// The captured streams are put through the line trimming on the way in, which is the only work this
    /// type does. What matters to a caller is that the collections are never null, so output can be
    /// enumerated without a guard, and that padding a process emitted around its real output is gone
    /// while blank lines inside it are not.
    /// </remarks>
    public sealed class ProcessResultTests
    {
        /// <summary>
        /// Verifies that a result carrying only an exit code exposes empty collections rather than nulls,
        /// so a caller can enumerate the streams unconditionally.
        /// </summary>
        /// <param name="exitCode">The exit code to construct with.</param>
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(1603)]
        [InlineData(-1)]
        [InlineData(ProcessManager.TimeoutExitCode)]
        public void Constructor_ExposesEmptyStreamsWhenGivenOnlyAnExitCode(int exitCode)
        {
            // Act
            using ProcessResult result = new(exitCode);

            // Assert
            Assert.Equal(exitCode, result.ExitCode);
            Assert.Empty(result.StdOut);
            Assert.Empty(result.StdErr);
            Assert.Empty(result.Interleaved);
            Assert.Null(result.Process);
            Assert.Null(result.LaunchInfo);
            Assert.Null(result.CommandLine);
        }

        /// <summary>
        /// Verifies that null and empty stream collections both leave the result's streams empty, rather
        /// than one of them producing a null.
        /// </summary>
        [Fact]
        public void Constructor_TreatsNullAndEmptyStreamsAlike()
        {
            // Act
            using ProcessResult fromNull = new(0, stdOut: null, stdErr: null, interleaved: null);
            using ProcessResult fromEmpty = new(0, [], [], []);

            // Assert
            Assert.Empty(fromNull.StdOut);
            Assert.Empty(fromNull.StdErr);
            Assert.Empty(fromNull.Interleaved);
            Assert.Empty(fromEmpty.StdOut);
            Assert.Empty(fromEmpty.StdErr);
            Assert.Empty(fromEmpty.Interleaved);
        }

        /// <summary>
        /// Verifies that each stream is trimmed of the blank lines around it, which is what makes output
        /// from a process that pads its console usable.
        /// </summary>
        [Fact]
        public void Constructor_TrimsBlankLinesAroundEachStream()
        {
            // Arrange
            string[] padded = ["", "   ", "real output", "", "more output", "  ", ""];

            // Act
            using ProcessResult result = new(0, padded, padded, padded);

            // Assert: padding gone from both ends, the interior blank kept
            Assert.Equal(["real output", "", "more output"], result.StdOut);
            Assert.Equal(["real output", "", "more output"], result.StdErr);
            Assert.Equal(["real output", "", "more output"], result.Interleaved);
        }

        /// <summary>
        /// Verifies that trailing whitespace is removed from each line while leading whitespace is kept,
        /// so indented output such as a stack trace survives.
        /// </summary>
        [Fact]
        public void Constructor_TrimsLineEndsButNotLineStarts()
        {
            // Act
            using ProcessResult result = new(0, ["    at Foo()   ", "    at Bar()\t"], stdErr: null, interleaved: null);

            // Assert
            Assert.Equal(["    at Foo()", "    at Bar()"], result.StdOut);
        }

        /// <summary>
        /// Verifies that a stream consisting only of blank lines collapses to empty, since a process that
        /// wrote nothing but newlines produced no output.
        /// </summary>
        [Fact]
        public void Constructor_CollapsesAnAllBlankStreamToEmpty()
        {
            // Act
            using ProcessResult result = new(0, ["", "  ", "\t"], stdErr: null, interleaved: null);

            // Assert
            Assert.Empty(result.StdOut);
        }

        /// <summary>
        /// Verifies that the streams are independent of each other, so trimming one cannot affect
        /// another sharing the same source.
        /// </summary>
        [Fact]
        public void Constructor_KeepsTheStreamsIndependent()
        {
            // Act
            using ProcessResult result = new(0, ["out"], ["err"], ["out", "err"]);

            // Assert
            Assert.Equal(["out"], result.StdOut);
            Assert.Equal(["err"], result.StdErr);
            Assert.Equal(["out", "err"], result.Interleaved);
        }

        /// <summary>
        /// Verifies that disposing a result with no process is harmless, which is the case for every
        /// result a caller constructs itself.
        /// </summary>
        [Fact]
        public void Dispose_IsHarmlessWithoutAProcess()
        {
            // Arrange
            using ProcessResult result = new(0);

            // Act & Assert: disposing twice is as harmless as disposing once
            Assert.Null(Record.Exception(result.Dispose));
            Assert.Null(Record.Exception(result.Dispose));
        }

        /// <summary>
        /// Verifies that the timeout exit code is the documented sentinel, since callers compare against
        /// it to tell a timeout from a real exit code the process chose.
        /// </summary>
        [Fact]
        public void TimeoutExitCode_IsTheDocumentedSentinel()
        {
            Assert.Equal(-443_991_205, ProcessManager.TimeoutExitCode);
        }

        /// <summary>
        /// Verifies that the process exception reports the result's exit code as its error code, which is
        /// what lets a caller treat it like any other external failure.
        /// </summary>
        /// <param name="exitCode">The exit code the result carries.</param>
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(1603)]
        [InlineData(-1)]
        [InlineData(ProcessManager.TimeoutExitCode)]
        public void ProcessException_ReportsTheResultsExitCodeAsItsErrorCode(int exitCode)
        {
            // Arrange
            using ProcessResult result = new(exitCode);

            // Act
            ProcessException exception = new("The process failed.", result);

            // Assert
            Assert.Equal(exitCode, exception.ErrorCode);
            Assert.Equal("The process failed.", exception.Message);
            Assert.Same(result, exception.Result);
        }

        /// <summary>
        /// Verifies that the exception keeps the whole result rather than only its code, so a caller
        /// handling it can still read what the process wrote.
        /// </summary>
        [Fact]
        public void ProcessException_KeepsTheCapturedOutput()
        {
            // Arrange
            using ProcessResult result = new(1603, ["installing"], ["fatal error"], ["installing", "fatal error"]);

            // Act
            ProcessException exception = new("Install failed.", result);

            // Assert
            Assert.Equal(["installing"], exception.Result.StdOut);
            Assert.Equal(["fatal error"], exception.Result.StdErr);
            Assert.Equal(["installing", "fatal error"], exception.Result.Interleaved);
        }
    }
}
