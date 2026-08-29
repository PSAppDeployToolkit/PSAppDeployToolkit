using PSADT.UserInterface.Tests.TestHelpers;
using Xunit;

namespace PSADT.UserInterface.Tests
{
    /// <summary>
    /// Tests which of a custom dialog's three buttons is the default.
    /// </summary>
    /// <remarks>
    /// Not to be confused with <see cref="DialogBoxDefaultButton"/>, which carries Win32 style bits for a
    /// message box. This one names the toolkit's own three button positions and is ordinal, so its values
    /// are the toolkit's to choose and the pipe's to preserve.
    /// </remarks>
    public sealed class DialogDefaultButtonTests
    {
        /// <summary>
        /// Verifies the members and their values.
        /// </summary>
        /// <remarks>
        /// The names describe where the button sits rather than which it is, and the order of the members
        /// follows the order they appear on screen. <c>None</c> is zero so that a dialog with no default
        /// gets one by leaving the value unset.
        /// </remarks>
        [Fact]
        public void Members_AreTheSerializedContract()
        {
            // Arrange
            (string Name, ulong Value)[] expected =
            [
                ("None", 0),
                ("Left", 1),
                ("Middle", 2),
                ("Right", 3),
            ];

            // Assert
            Assert.Equal(expected, EnumValues.DeclaredPairs<DialogDefaultButton>());
        }
    }
}
