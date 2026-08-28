using System;
using PSADT.ClientServer.Payloads;
using Xunit;

namespace PSADT.ClientServer.Server.Tests.Payloads
{
    /// <summary>
    /// Tests the payload asking a client to change the text on its notification icon.
    /// </summary>
    /// <remarks>
    /// The other of the two payloads the serializer's list of known types did not name, and the round trip
    /// below is what says that cost nothing while it lasted.
    /// </remarks>
    public sealed class UpdateNotifyIconPayloadTests
    {
        /// <summary>
        /// Verifies that the text it was built with is the text it carries.
        /// </summary>
        [Fact]
        public void UpdateNotifyIconPayload_CarriesItsMessageText()
        {
            Assert.Equal("a new tooltip", new UpdateNotifyIconPayload("a new tooltip").MessageText);
        }

        /// <summary>
        /// Verifies that text of nothing is refused, since an icon with no tooltip is not what the caller
        /// meant to ask for.
        /// </summary>
        /// <param name="messageText">The text to refuse.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "This is deliberate as part of unit testing.")]
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void UpdateNotifyIconPayload_RefusesTextOfNothing(string? messageText)
        {
            _ = Assert.ThrowsAny<ArgumentException>(() => new UpdateNotifyIconPayload(messageText!));
        }

        /// <summary>
        /// Verifies that two payloads asking for the same text are the same, and two asking for different
        /// text are not.
        /// </summary>
        [Fact]
        public void UpdateNotifyIconPayload_ComparesByItsMessageText()
        {
            Assert.Equal(new UpdateNotifyIconPayload("a tooltip"), new UpdateNotifyIconPayload("a tooltip"));
            Assert.NotEqual(new UpdateNotifyIconPayload("a tooltip"), new UpdateNotifyIconPayload("another tooltip"));
        }

        /// <summary>
        /// Verifies that it survives the trip to the client with its text intact.
        /// </summary>
        [Fact]
        public void UpdateNotifyIconPayload_SurvivesTheTripToTheClient()
        {
            // Arrange
            UpdateNotifyIconPayload original = new("a new tooltip");

            // Act
            UpdateNotifyIconPayload restored = DataSerialization.DeserializeFromBytes<UpdateNotifyIconPayload>(DataSerialization.SerializeToBytes(original));

            // Assert
            Assert.Equal(original, restored);
            Assert.Equal("a new tooltip", restored.MessageText);
        }
    }
}
