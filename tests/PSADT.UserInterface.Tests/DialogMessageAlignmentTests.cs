using PSADT.UserInterface.Tests.TestHelpers;
using Xunit;

namespace PSADT.UserInterface.Tests
{
    /// <summary>
    /// Tests how a dialog's message text is aligned.
    /// </summary>
    public sealed class DialogMessageAlignmentTests
    {
        /// <summary>
        /// Verifies the members and their values.
        /// </summary>
        /// <remarks>
        /// <c>Left</c> is zero, which matters beyond the wire format: the options types hold this as a
        /// nullable and an absent value falls through to the dialog's own default, so a member added
        /// ahead of <c>Left</c> would change what unaligned text does.
        /// </remarks>
        [Fact]
        public void Members_AreTheSerializedContract()
        {
            // Arrange
            (string Name, ulong Value)[] expected =
            [
                ("Left", 0),
                ("Center", 1),
                ("Right", 2),
            ];

            // Assert
            Assert.Equal(expected, EnumValues.DeclaredPairs<DialogMessageAlignment>());
        }
    }
}
