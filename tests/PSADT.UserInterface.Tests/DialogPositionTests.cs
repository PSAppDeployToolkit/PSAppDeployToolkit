using PSADT.UserInterface.Tests.TestHelpers;
using Xunit;

namespace PSADT.UserInterface.Tests
{
    /// <summary>
    /// Tests where on screen a dialog is placed.
    /// </summary>
    /// <remarks>
    /// The longest of these enums and the one most likely to gain a member, which is the case worth
    /// guarding: a new position inserted in the middle rather than appended renumbers every position
    /// after it, and a dialog then opens somewhere other than where it was configured to.
    /// </remarks>
    public sealed class DialogPositionTests
    {
        /// <summary>
        /// Verifies the members and their values.
        /// </summary>
        /// <remarks>
        /// The ordering is not alphabetical and not obviously systematic - the four corners and four
        /// edges come first in reading order, then the two offset positions - so it is written out here
        /// rather than derived.
        /// </remarks>
        [Fact]
        public void Members_AreTheSerializedContract()
        {
            // Arrange
            (string Name, ulong Value)[] expected =
            [
                ("Default", 0),
                ("TopLeft", 1),
                ("Top", 2),
                ("TopRight", 3),
                ("TopCenter", 4),
                ("Center", 5),
                ("BottomLeft", 6),
                ("Bottom", 7),
                ("BottomRight", 8),
                ("BottomCenter", 9),
                ("Oobe", 10),
            ];

            // Assert
            Assert.Equal(expected, EnumValues.DeclaredPairs<DialogPosition>());
        }
    }
}
