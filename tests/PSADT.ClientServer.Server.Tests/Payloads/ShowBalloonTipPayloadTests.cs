using PSADT.ClientServer.Payloads;
using PSADT.ClientServer.Server.Tests.TestHelpers;
using Xunit;

namespace PSADT.ClientServer.Server.Tests.Payloads
{
    /// <summary>
    /// Tests the payload asking a client to show a balloon tip.
    /// </summary>
    public sealed class ShowBalloonTipPayloadTests
    {
        /// <summary>
        /// Verifies that the options it was built with are the options it carries.
        /// </summary>
        [Fact]
        public void ShowBalloonTipPayload_CarriesItsOptions()
        {
            Assert.Equal(SampleOptions.BalloonTip(), new ShowBalloonTipPayload(SampleOptions.BalloonTip()).Options);
        }

        /// <summary>
        /// Verifies that two payloads showing the same balloon are the same, and two showing different ones
        /// are not.
        /// </summary>
        [Fact]
        public void ShowBalloonTipPayload_ComparesByItsOptions()
        {
            Assert.Equal(new ShowBalloonTipPayload(SampleOptions.BalloonTip()), new ShowBalloonTipPayload(SampleOptions.BalloonTip()));
            Assert.NotEqual(new ShowBalloonTipPayload(SampleOptions.BalloonTip()), new ShowBalloonTipPayload(SampleOptions.BalloonTip("something else")));
        }

        /// <summary>
        /// Verifies that it survives the trip to the client with its options intact.
        /// </summary>
        [Fact]
        public void ShowBalloonTipPayload_SurvivesTheTripToTheClient()
        {
            // Arrange
            ShowBalloonTipPayload original = new(SampleOptions.BalloonTip());

            // Act
            ShowBalloonTipPayload restored = DataSerialization.DeserializeFromBytes<ShowBalloonTipPayload>(DataSerialization.SerializeToBytes(original));

            // Assert
            Assert.Equal(original, restored);
            Assert.Equal("the balloon text", restored.Options.Text);
        }
    }
}
