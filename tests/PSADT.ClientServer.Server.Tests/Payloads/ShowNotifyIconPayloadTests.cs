using PSADT.ClientServer.Payloads;
using PSADT.ClientServer.Server.Tests.TestHelpers;
using Xunit;

namespace PSADT.ClientServer.Server.Tests.Payloads
{
    /// <summary>
    /// Tests the payload asking a client to put an icon in the notification area.
    /// </summary>
    /// <remarks>
    /// One of the two payloads the serializer's list of known types did not name, until the odd one out
    /// among thirteen was noticed and put back. It cost nothing while it lasted - a payload is always the
    /// type the serializer is asked for rather than something found inside another one, which is what that
    /// list is for - and the round trip below is what says so either way.
    /// </remarks>
    public sealed class ShowNotifyIconPayloadTests
    {
        /// <summary>
        /// Verifies that the options it was built with are the options it carries.
        /// </summary>
        [Fact]
        public void ShowNotifyIconPayload_CarriesItsOptions()
        {
            Assert.Equal(SampleOptions.NotifyIcon(), new ShowNotifyIconPayload(SampleOptions.NotifyIcon()).Options);
        }

        /// <summary>
        /// Verifies that two payloads showing the same icon are the same, and two showing different ones
        /// are not.
        /// </summary>
        [Fact]
        public void ShowNotifyIconPayload_ComparesByItsOptions()
        {
            Assert.Equal(new ShowNotifyIconPayload(SampleOptions.NotifyIcon()), new ShowNotifyIconPayload(SampleOptions.NotifyIcon()));
            Assert.NotEqual(new ShowNotifyIconPayload(SampleOptions.NotifyIcon()), new ShowNotifyIconPayload(SampleOptions.NotifyIcon("something else")));
        }

        /// <summary>
        /// Verifies that it survives the trip to the client with its options intact.
        /// </summary>
        [Fact]
        public void ShowNotifyIconPayload_SurvivesTheTripToTheClient()
        {
            // Arrange
            ShowNotifyIconPayload original = new(SampleOptions.NotifyIcon());

            // Act
            ShowNotifyIconPayload restored = DataSerialization.DeserializeFromBytes<ShowNotifyIconPayload>(DataSerialization.SerializeToBytes(original));

            // Assert
            Assert.Equal(original, restored);
            Assert.Equal("the tooltip text", restored.Options.MessageText);
        }
    }
}
