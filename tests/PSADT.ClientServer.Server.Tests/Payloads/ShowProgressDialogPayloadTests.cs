using PSADT.ClientServer.Payloads;
using PSADT.ClientServer.Server.Tests.TestHelpers;
using PSADT.UserInterface;
using Xunit;

namespace PSADT.ClientServer.Server.Tests.Payloads
{
    /// <summary>
    /// Tests the payload asking a client to show a progress dialog.
    /// </summary>
    /// <remarks>
    /// It carries two things rather than one, so the comparison has to take both into account - a payload
    /// asking for the same dialog in a different style is a different request.
    /// </remarks>
    public sealed class ShowProgressDialogPayloadTests
    {
        /// <summary>
        /// Verifies that the style and the options it was built with are both carried.
        /// </summary>
        [Fact]
        public void ShowProgressDialogPayload_CarriesItsStyleAndOptions()
        {
            // Arrange
            ShowProgressDialogPayload payload = new(DialogStyle.Fluent, SampleOptions.ProgressDialog());

            // Assert
            Assert.Equal(DialogStyle.Fluent, payload.DialogStyle);
            Assert.Equal(SampleOptions.ProgressDialog(), payload.Options);
        }

        /// <summary>
        /// Verifies that both the style and the options count towards the comparison.
        /// </summary>
        [Fact]
        public void ShowProgressDialogPayload_ComparesByItsStyleAndOptions()
        {
            Assert.Equal(
                new ShowProgressDialogPayload(DialogStyle.Fluent, SampleOptions.ProgressDialog()),
                new ShowProgressDialogPayload(DialogStyle.Fluent, SampleOptions.ProgressDialog()));
            Assert.NotEqual(
                new ShowProgressDialogPayload(DialogStyle.Fluent, SampleOptions.ProgressDialog()),
                new ShowProgressDialogPayload(DialogStyle.Classic, SampleOptions.ProgressDialog()));
            Assert.NotEqual(
                new ShowProgressDialogPayload(DialogStyle.Fluent, SampleOptions.ProgressDialog()),
                new ShowProgressDialogPayload(DialogStyle.Fluent, SampleOptions.ProgressDialog("something else")));
        }

        /// <summary>
        /// Verifies that it survives the trip to the client with both parts intact.
        /// </summary>
        [Fact]
        public void ShowProgressDialogPayload_SurvivesTheTripToTheClient()
        {
            // Arrange
            ShowProgressDialogPayload original = new(DialogStyle.Fluent, SampleOptions.ProgressDialog());

            // Act
            ShowProgressDialogPayload restored = DataSerialization.DeserializeFromBytes<ShowProgressDialogPayload>(DataSerialization.SerializeToBytes(original));

            // Assert
            Assert.Equal(original, restored);
            Assert.Equal(DialogStyle.Fluent, restored.DialogStyle);
            Assert.Equal("the progress message", restored.Options.ProgressMessageText);
        }
    }
}
