using PSADT.UserInterface.Tests.TestHelpers;
using Xunit;

namespace PSADT.UserInterface.Tests
{
    /// <summary>
    /// Tests which of the two dialog presentations is used.
    /// </summary>
    /// <remarks>
    /// This one decides which implementation renders a dialog - the WinForms classic set or the WPF
    /// fluent set - so its values select between two entirely separate code paths in
    /// <c language="csharp">PSADT.UserInterface.Interfaces</c>. The strings tables in
    /// <c language="csharp">CloseAppsDialogOptions</c> and elsewhere are likewise split into a <c language="csharp">Classic</c> and a
    /// <c language="csharp">Fluent</c> half that correspond to these members by name.
    /// </remarks>
    public sealed class DialogStyleTests
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
                ("Classic", 0),
                ("Fluent", 1),
            ];

            // Assert
            Assert.Equal(expected, EnumValues.DeclaredPairs<DialogStyle>());
        }
    }
}
