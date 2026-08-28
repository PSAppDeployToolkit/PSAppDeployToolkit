using System;
using PSADT.ClientServer.Payloads;
using PSADT.UserInterface;
using Xunit;

namespace PSADT.ClientServer.Server.Tests.Payloads
{
    /// <summary>
    /// Tests the payload asking a client to change what a progress dialog is showing.
    /// </summary>
    /// <remarks>
    /// Everything it carries is optional, and nothing means "leave this as it is" rather than "clear this".
    /// So a payload carrying nothing at all is legal - it asks for no change - while a message of
    /// whitespace is not, because a caller passing one meant to say something and did not.
    /// </remarks>
    public sealed class UpdateProgressDialogPayloadTests
    {
        /// <summary>
        /// Verifies that everything it was built with is carried.
        /// </summary>
        [Fact]
        public void UpdateProgressDialogPayload_CarriesEverythingItWasGiven()
        {
            // Arrange
            UpdateProgressDialogPayload payload = new("a message", "a detail message", 42.5, DialogMessageAlignment.Center);

            // Assert
            Assert.Equal("a message", payload.Message);
            Assert.Equal("a detail message", payload.DetailMessage);
            Assert.Equal(42.5, payload.Percentage);
            Assert.Equal(DialogMessageAlignment.Center, payload.Alignment);
        }

        /// <summary>
        /// Verifies that a payload asking for nothing is accepted and carries nothing.
        /// </summary>
        /// <remarks>
        /// Legal because each part means "leave this alone" when absent. A caller updating only the
        /// percentage sends the rest as nothing, and the extreme of that is a payload that changes nothing
        /// at all.
        /// </remarks>
        [Fact]
        public void UpdateProgressDialogPayload_AcceptsAskingForNothing()
        {
            // Arrange
            UpdateProgressDialogPayload payload = new();

            // Assert
            Assert.Null(payload.Message);
            Assert.Null(payload.DetailMessage);
            Assert.Null(payload.Percentage);
            Assert.Null(payload.Alignment);
        }

        /// <summary>
        /// Verifies that a message of nothing but space is refused, on either of the two that take text.
        /// </summary>
        /// <param name="text">The text to refuse.</param>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void UpdateProgressDialogPayload_RefusesAMessageOfNothing(string text)
        {
            _ = Assert.Throws<ArgumentException>(() => new UpdateProgressDialogPayload(text));
            _ = Assert.Throws<ArgumentException>(() => new UpdateProgressDialogPayload(detailMessage: text));
        }

        /// <summary>
        /// Verifies that a percentage outside the range a progress bar can show is carried rather than
        /// refused.
        /// </summary>
        /// <remarks>
        /// Recorded rather than endorsed. The payload does not police the range, so whatever a caller sends
        /// reaches the client and the dialog decides what to do with it. Asserted so that adding a check
        /// here shows up as a test to update rather than as a silent change in what the client receives.
        /// </remarks>
        /// <param name="percentage">The percentage to carry.</param>
        [Theory]
        [InlineData(-1.0)]
        [InlineData(101.0)]
        public void UpdateProgressDialogPayload_CarriesAPercentageItCannotShow(double percentage)
        {
            Assert.Equal(percentage, new UpdateProgressDialogPayload(percentage: percentage).Percentage);
        }

        /// <summary>
        /// Verifies that every part of it counts towards the comparison, including the difference between
        /// nothing and a value.
        /// </summary>
        [Fact]
        public void UpdateProgressDialogPayload_ComparesByEverythingItCarries()
        {
            // Arrange
            UpdateProgressDialogPayload payload = new("a message", "a detail message", 42.5, DialogMessageAlignment.Center);

            // Assert
            Assert.Equal(payload, new UpdateProgressDialogPayload("a message", "a detail message", 42.5, DialogMessageAlignment.Center));
            Assert.NotEqual(payload, new UpdateProgressDialogPayload("another message", "a detail message", 42.5, DialogMessageAlignment.Center));
            Assert.NotEqual(payload, new UpdateProgressDialogPayload("a message", "another detail message", 42.5, DialogMessageAlignment.Center));
            Assert.NotEqual(payload, new UpdateProgressDialogPayload("a message", "a detail message", 42.6, DialogMessageAlignment.Center));
            Assert.NotEqual(payload, new UpdateProgressDialogPayload("a message", "a detail message", 42.5, DialogMessageAlignment.Left));
            Assert.NotEqual(payload, new UpdateProgressDialogPayload("a message", "a detail message", 42.5, alignment: null));
        }

        /// <summary>
        /// Verifies that it survives the trip to the client with everything intact, including the parts it
        /// is not carrying.
        /// </summary>
        [Fact]
        public void UpdateProgressDialogPayload_SurvivesTheTripToTheClient()
        {
            // Arrange
            UpdateProgressDialogPayload original = new("a message", percentage: 42.5);

            // Act
            UpdateProgressDialogPayload restored = DataSerialization.DeserializeFromBytes<UpdateProgressDialogPayload>(DataSerialization.SerializeToBytes(original));

            // Assert
            Assert.Equal(original, restored);
            Assert.Equal("a message", restored.Message);
            Assert.Null(restored.DetailMessage);
            Assert.Equal(42.5, restored.Percentage);
            Assert.Null(restored.Alignment);
        }
    }
}
