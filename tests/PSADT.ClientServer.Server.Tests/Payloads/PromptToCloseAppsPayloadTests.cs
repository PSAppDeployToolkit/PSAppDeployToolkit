using System;
using PSADT.ClientServer.Payloads;
using Xunit;

namespace PSADT.ClientServer.Server.Tests.Payloads
{
    /// <summary>
    /// Tests the payload asking a client to prompt the user to close applications.
    /// </summary>
    /// <remarks>
    /// It carries a timeout and nothing else, so what there is to check is that the timeout arrives and
    /// that two payloads carrying different ones are told apart - which the comparison a record generates
    /// gives for free here, because a timespan is a value.
    /// </remarks>
    public sealed class PromptToCloseAppsPayloadTests
    {
        /// <summary>
        /// Verifies that the timeout it was built with is the timeout it carries.
        /// </summary>
        [Fact]
        public void PromptToCloseAppsPayload_CarriesItsTimeout()
        {
            Assert.Equal(TimeSpan.FromMinutes(5), new PromptToCloseAppsPayload(TimeSpan.FromMinutes(5)).Timeout);
        }

        /// <summary>
        /// Verifies that two payloads asking for the same wait are the same, and two asking for different
        /// waits are not.
        /// </summary>
        [Fact]
        public void PromptToCloseAppsPayload_ComparesByItsTimeout()
        {
            Assert.Equal(new PromptToCloseAppsPayload(TimeSpan.FromMinutes(5)), new PromptToCloseAppsPayload(TimeSpan.FromMinutes(5)));
            Assert.NotEqual(new PromptToCloseAppsPayload(TimeSpan.FromMinutes(5)), new PromptToCloseAppsPayload(TimeSpan.FromMinutes(6)));
        }

        /// <summary>
        /// Verifies that it survives the trip to the client with its timeout intact.
        /// </summary>
        [Fact]
        public void PromptToCloseAppsPayload_SurvivesTheTripToTheClient()
        {
            // Arrange
            PromptToCloseAppsPayload original = new(TimeSpan.FromSeconds(90));

            // Act
            PromptToCloseAppsPayload restored = DataSerialization.DeserializeFromBytes<PromptToCloseAppsPayload>(DataSerialization.SerializeToBytes(original));

            // Assert
            Assert.Equal(original, restored);
            Assert.Equal(TimeSpan.FromSeconds(90), restored.Timeout);
        }
    }
}
