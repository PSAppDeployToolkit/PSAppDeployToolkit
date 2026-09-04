using PSADT.ClientServer.Payloads;
using PSADT.ClientServer.Server.Tests.TestHelpers;
using Xunit;

namespace PSADT.ClientServer.Server.Tests.Payloads
{
    /// <summary>
    /// Tests the payload asking a client which windows its processes have open.
    /// </summary>
    /// <remarks>
    /// It carries one options object, so its comparison is entirely the options object's comparison. That
    /// is worth asserting rather than assuming: the options hold their filters as lists, and a record
    /// holding a list of any kind the framework offers would compare by reference and make two payloads
    /// asking the same question unequal.
    /// </remarks>
    public sealed class GetProcessWindowInfoPayloadTests
    {
        /// <summary>
        /// Verifies that the options it was built with are the options it carries.
        /// </summary>
        [Fact]
        public void GetProcessWindowInfoPayload_CarriesItsOptions()
        {
            Assert.Equal(SampleOptions.WindowInfo(), new GetProcessWindowInfoPayload(SampleOptions.WindowInfo()).Options);
        }

        /// <summary>
        /// Verifies that two payloads asking the same question are the same, and two asking different ones
        /// are not.
        /// </summary>
        [Fact]
        public void GetProcessWindowInfoPayload_ComparesByItsOptions()
        {
            Assert.Equal(new GetProcessWindowInfoPayload(SampleOptions.WindowInfo()), new GetProcessWindowInfoPayload(SampleOptions.WindowInfo()));
            Assert.NotEqual(new GetProcessWindowInfoPayload(SampleOptions.WindowInfo()), new GetProcessWindowInfoPayload(SampleOptions.WindowInfo("^Something Else")));
        }

        /// <summary>
        /// Verifies that it survives the trip to the client with its options intact.
        /// </summary>
        [Fact]
        public void GetProcessWindowInfoPayload_SurvivesTheTripToTheClient()
        {
            // Arrange
            GetProcessWindowInfoPayload original = new(SampleOptions.WindowInfo());

            // Act
            GetProcessWindowInfoPayload restored = DataSerialization.DeserializeFromBytes<GetProcessWindowInfoPayload>(DataSerialization.SerializeToBytes(original));

            // Assert
            Assert.Equal(original, restored);
            Assert.Equal("^Untitled", restored.Options.WindowTitleRegex);
        }
    }
}
