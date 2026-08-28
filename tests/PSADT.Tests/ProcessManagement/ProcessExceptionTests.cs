using System.Runtime.InteropServices;
using PSADT.ProcessManagement;
using Xunit;

namespace PSADT.Tests.ProcessManagement
{
    /// <summary>
    /// Tests the failure raised when a launched process reports one.
    /// </summary>
    /// <remarks>
    /// This carries the whole result rather than only a message, so that a caller catching it can still
    /// read what the process wrote before it failed. It also presents the exit code as the external error
    /// code, which is what a PowerShell caller reads off an exception without knowing anything about this
    /// library.
    /// </remarks>
    public sealed class ProcessExceptionTests
    {
        /// <summary>
        /// Verifies that the result the failure was raised for is carried on it, so nothing about the
        /// process is lost by the time it reaches whoever handles it.
        /// </summary>
        [Fact]
        public void ProcessException_CarriesTheResult()
        {
            // Arrange
            using ProcessResult result = new(3, ["some output"], ["some error"], ["some output", "some error"]);

            // Act
            ProcessException exception = new("It went wrong.", result);

            // Assert
            Assert.Same(result, exception.Result);
            Assert.Equal("It went wrong.", exception.Message);
            Assert.Equal(["some output"], exception.Result.StdOut);
            Assert.Equal(["some error"], exception.Result.StdErr);
        }

        /// <summary>
        /// Verifies that the exit code is what the failure reports as its error code, since that is the
        /// number a caller sees without reaching into the result.
        /// </summary>
        /// <param name="exitCode">The code the process ended with.</param>
        [Theory]
        [InlineData(1)]
        [InlineData(3010)]
        [InlineData(-1)]
        [InlineData(ProcessManager.TimeoutExitCode)]
        public void ErrorCode_IsTheExitCode(int exitCode)
        {
            // Arrange
            using ProcessResult result = new(exitCode);

            // Act & Assert
            Assert.Equal(exitCode, new ProcessException("It went wrong.", result).ErrorCode);
        }

        /// <summary>
        /// Verifies that it is an external failure rather than a fault in this library, which is what
        /// decides how a caller treats it: something the launched process did, not something we did.
        /// </summary>
        [Fact]
        public void ProcessException_IsAnExternalFailure()
        {
            // Arrange
            using ProcessResult result = new(1);

            // Act & Assert
            _ = Assert.IsAssignableFrom<ExternalException>(new ProcessException("It went wrong.", result));
        }
    }
}
