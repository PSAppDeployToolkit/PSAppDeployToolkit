using System;
using PSADT.ClientServer.Payloads;
using PSAppDeployToolkit.Logging;
using Xunit;

namespace PSADT.ClientServer.Server.Tests.Payloads
{
    /// <summary>
    /// Tests the payload carrying a log message from a client back to the server.
    /// </summary>
    /// <remarks>
    /// The only payload that travels the other way, on its own pipe, and the only one written from a
    /// context where nobody is waiting for an answer - so a malformed one is dropped rather than reported.
    /// That is why both the message and its source are refused when blank at the point they are built: a
    /// log line with no text, or no idea where it came from, is worse than no line at all.
    /// </remarks>
    public sealed class LogMessagePayloadTests
    {
        /// <summary>
        /// Verifies that everything it was built with is carried.
        /// </summary>
        [Fact]
        public void LogMessagePayload_CarriesEverythingItWasGiven()
        {
            // Arrange
            LogMessagePayload payload = new("something happened", LogSeverity.Warning, "Show-ADTInstallationProgress");

            // Assert
            Assert.Equal("something happened", payload.Message);
            Assert.Equal(LogSeverity.Warning, payload.Severity);
            Assert.Equal("Show-ADTInstallationProgress", payload.Source);
        }

        /// <summary>
        /// Verifies that a message of nothing is refused.
        /// </summary>
        /// <param name="message">The message to refuse.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "This is deliberate as part of unit testing.")]
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void LogMessagePayload_RefusesAMessageOfNothing(string? message)
        {
            _ = Assert.ThrowsAny<ArgumentException>(() => new LogMessagePayload(message!, LogSeverity.Info, "a source"));
        }

        /// <summary>
        /// Verifies that a source of nothing is refused.
        /// </summary>
        /// <param name="source">The source to refuse.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "This is deliberate as part of unit testing.")]
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void LogMessagePayload_RefusesASourceOfNothing(string? source)
        {
            _ = Assert.ThrowsAny<ArgumentException>(() => new LogMessagePayload("a message", LogSeverity.Info, source!));
        }

        /// <summary>
        /// Verifies that every part of it counts towards the comparison.
        /// </summary>
        [Fact]
        public void LogMessagePayload_ComparesByEverythingItCarries()
        {
            // Arrange
            LogMessagePayload payload = new("a message", LogSeverity.Info, "a source");

            // Assert
            Assert.Equal(payload, new LogMessagePayload("a message", LogSeverity.Info, "a source"));
            Assert.NotEqual(payload, new LogMessagePayload("another message", LogSeverity.Info, "a source"));
            Assert.NotEqual(payload, new LogMessagePayload("a message", LogSeverity.Error, "a source"));
            Assert.NotEqual(payload, new LogMessagePayload("a message", LogSeverity.Info, "another source"));
        }

        /// <summary>
        /// Verifies that it survives the trip back to the server with everything intact.
        /// </summary>
        /// <remarks>
        /// Built with the surrounding space the server trims off on arrival, since what is asserted here is
        /// that the payload carries what it was given - the trimming happens where the line is written to
        /// the log, not on the way.
        /// </remarks>
        [Fact]
        public void LogMessagePayload_SurvivesTheTripBackToTheServer()
        {
            // Arrange
            LogMessagePayload original = new("  something happened  ", LogSeverity.Error, "Close-ADTSession");

            // Act
            LogMessagePayload restored = DataSerialization.DeserializeFromBytes<LogMessagePayload>(DataSerialization.SerializeToBytes(original));

            // Assert
            Assert.Equal(original, restored);
            Assert.Equal("  something happened  ", restored.Message);
            Assert.Equal(LogSeverity.Error, restored.Severity);
        }
    }
}
