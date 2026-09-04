using PSADT.TerminalServices;
using Xunit;

namespace PSADT.Tests.TerminalServices
{
    /// <summary>
    /// Tests the terminal server install mode query.
    /// </summary>
    /// <remarks>
    /// Switching a machine into install mode is a change to it, so only the query is covered. On a machine
    /// that is not a terminal server the answer is a plain no, and answering rather than failing is the
    /// contract that matters - the query runs on every deployment, on machines that mostly are not
    /// terminal servers at all.
    /// </remarks>
    public sealed class TerminalServerUtilitiesTests
    {
        /// <summary>
        /// Verifies that asking whether the terminal server is in installation mode answers rather than
        /// failing, on a machine that is not a terminal server.
        /// </summary>
        [Fact]
        public void InAppInstallMode_AnswersWithoutFailing()
        {
            Assert.Null(Record.Exception(static () => _ = TerminalServerUtilities.InAppInstallMode()));
        }

        /// <summary>
        /// Verifies that the answer is stable between two readings taken a moment apart, since nothing
        /// here changes the mode and a caller branches on it.
        /// </summary>
        [Fact]
        public void InAppInstallMode_IsStableBetweenReadings()
        {
            Assert.Equal(TerminalServerUtilities.InAppInstallMode(), TerminalServerUtilities.InAppInstallMode());
        }
    }
}
