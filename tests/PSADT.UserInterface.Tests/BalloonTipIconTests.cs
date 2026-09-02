using PSADT.UserInterface.Tests.TestHelpers;
using Xunit;

namespace PSADT.UserInterface.Tests
{
    /// <summary>
    /// Tests the icon shown on a balloon tip.
    /// </summary>
    /// <remarks>
    /// Held by <see cref="UserInterface.DialogOptions.BalloonTipOptions"/> as a data member, so the
    /// numeric values travel the pipe between the deployment process and the client that shows the tip.
    /// A renumbering therefore shows the wrong icon rather than failing, and the module addresses these
    /// by name from PowerShell, so both halves of each member are pinned.
    /// </remarks>
    public sealed class BalloonTipIconTests
    {
        /// <summary>
        /// Verifies the members and their values.
        /// </summary>
        [Fact]
        public void Members_AreTheSerializedContract()
        {
            // Arrange
            (string Name, ulong Value)[] expected =
            [
                ("None", 0),
                ("Info", 1),
                ("Warning", 2),
                ("Error", 3),
            ];

            // Assert
            Assert.Equal(expected, EnumValues.DeclaredPairs<BalloonTipIcon>());
        }
    }
}
