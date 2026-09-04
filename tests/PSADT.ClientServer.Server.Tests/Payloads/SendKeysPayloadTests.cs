using PSADT.ClientServer.Payloads;
using PSADT.ClientServer.Server.Tests.TestHelpers;
using Xunit;

namespace PSADT.ClientServer.Server.Tests.Payloads
{
    /// <summary>
    /// Tests the payload asking a client to send keystrokes to a window.
    /// </summary>
    public sealed class SendKeysPayloadTests
    {
        /// <summary>
        /// Verifies that the options it was built with are the options it carries.
        /// </summary>
        [Fact]
        public void SendKeysPayload_CarriesItsOptions()
        {
            Assert.Equal(SampleOptions.SendKeys(), new SendKeysPayload(SampleOptions.SendKeys()).Options);
        }

        /// <summary>
        /// Verifies that two payloads sending the same keys are the same, and two sending different ones
        /// are not.
        /// </summary>
        [Fact]
        public void SendKeysPayload_ComparesByItsOptions()
        {
            Assert.Equal(new SendKeysPayload(SampleOptions.SendKeys()), new SendKeysPayload(SampleOptions.SendKeys()));
            Assert.NotEqual(new SendKeysPayload(SampleOptions.SendKeys()), new SendKeysPayload(SampleOptions.SendKeys("^c")));
        }

        /// <summary>
        /// Verifies that it survives the trip to the client with both the keys and the window they are
        /// meant for intact.
        /// </summary>
        /// <remarks>
        /// The window handle is asserted alongside the keys because it is a native integer, which is the
        /// member most likely to be lost or narrowed in a serializer that only sees a number.
        /// </remarks>
        [Fact]
        public void SendKeysPayload_SurvivesTheTripToTheClient()
        {
            // Arrange
            SendKeysPayload original = new(SampleOptions.SendKeys());

            // Act
            SendKeysPayload restored = DataSerialization.DeserializeFromBytes<SendKeysPayload>(DataSerialization.SerializeToBytes(original));

            // Assert
            Assert.Equal(original, restored);
            Assert.Equal("^s", restored.Options.Keys);
            Assert.Equal(0x1234, restored.Options.WindowHandle);
        }
    }
}
