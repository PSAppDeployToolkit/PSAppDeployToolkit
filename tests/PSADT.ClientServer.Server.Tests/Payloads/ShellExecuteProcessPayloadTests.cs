using PSADT.ClientServer.Payloads;
using PSADT.ClientServer.Server.Tests.TestHelpers;
using Xunit;

namespace PSADT.ClientServer.Server.Tests.Payloads
{
    /// <summary>
    /// Tests the payload asking a client to run something on the user's behalf.
    /// </summary>
    /// <remarks>
    /// The options it carries hold an argument list, so its comparison depends on that list comparing by
    /// its contents rather than by reference - which is the case a record holding a framework collection
    /// gets wrong, and the reason the options keep theirs in a list that compares by value.
    /// </remarks>
    public sealed class ShellExecuteProcessPayloadTests
    {
        /// <summary>
        /// Verifies that the options it was built with are the options it carries.
        /// </summary>
        [Fact]
        public void ShellExecuteProcessPayload_CarriesItsOptions()
        {
            Assert.Equal(SampleOptions.ShellExecute(), new ShellExecuteProcessPayload(SampleOptions.ShellExecute()).Options);
        }

        /// <summary>
        /// Verifies that two payloads running the same thing are the same, and two running different things
        /// are not.
        /// </summary>
        [Fact]
        public void ShellExecuteProcessPayload_ComparesByItsOptions()
        {
            Assert.Equal(new ShellExecuteProcessPayload(SampleOptions.ShellExecute()), new ShellExecuteProcessPayload(SampleOptions.ShellExecute()));
            Assert.NotEqual(new ShellExecuteProcessPayload(SampleOptions.ShellExecute()), new ShellExecuteProcessPayload(SampleOptions.ShellExecute(@"C:\Windows\System32\notepad.exe")));
        }

        /// <summary>
        /// Verifies that it survives the trip to the client with its options intact.
        /// </summary>
        [Fact]
        public void ShellExecuteProcessPayload_SurvivesTheTripToTheClient()
        {
            // Arrange
            ShellExecuteProcessPayload original = new(SampleOptions.ShellExecute());

            // Act
            ShellExecuteProcessPayload restored = DataSerialization.DeserializeFromBytes<ShellExecuteProcessPayload>(DataSerialization.SerializeToBytes(original));

            // Assert
            Assert.Equal(original, restored);
            Assert.Equal(@"C:\Windows\System32\cmd.exe", restored.Options.FilePath);
        }
    }
}
