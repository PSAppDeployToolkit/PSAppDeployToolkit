using PSADT.UserInterface.Tests.TestHelpers;
using Xunit;

namespace PSADT.UserInterface.Tests
{
    /// <summary>
    /// Tests the stock system icons a custom dialog can display.
    /// </summary>
    /// <remarks>
    /// Every member has to appear in the lookup table inside <c language="csharp">SystemIcons</c>, which is checked by
    /// <see cref="SystemIconsTests"/> rather than here: the two tests together are what makes adding a
    /// member without mapping it fail at build-and-test time instead of at the moment a dialog asks for
    /// the icon.
    /// </remarks>
    public sealed class DialogSystemIconTests
    {
        /// <summary>
        /// Verifies the members and their values.
        /// </summary>
        /// <remarks>
        /// The names are the ones <c language="csharp">System.Drawing.SystemIcons</c> uses, which is why <c language="csharp">Warning</c>
        /// sits last and out of alphabetical order, and why the set contains near-synonyms such as
        /// <c language="csharp">Error</c> and <c language="csharp">Hand</c>. Those resolve to the same shell icon; that they are distinct
        /// members here is what lets a caller keep using whichever name it already used.
        /// </remarks>
        [Fact]
        public void Members_AreTheSerializedContract()
        {
            // Arrange
            (string Name, ulong Value)[] expected =
            [
                ("Application", 0),
                ("Asterisk", 1),
                ("Error", 2),
                ("Exclamation", 3),
                ("Hand", 4),
                ("Information", 5),
                ("Question", 6),
                ("Shield", 7),
                ("Warning", 8),
                ("WinLogo", 9),
            ];

            // Assert
            Assert.Equal(expected, EnumValues.DeclaredPairs<DialogSystemIcon>());
        }
    }
}
