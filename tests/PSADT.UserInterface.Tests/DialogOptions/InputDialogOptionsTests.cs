using System;
using System.Collections;
using PSADT.UserInterface.DialogOptions;
using PSADT.UserInterface.Tests.TestHelpers;
using Xunit;

namespace PSADT.UserInterface.Tests.DialogOptions
{
    /// <summary>
    /// Tests the options for the dialog that asks the user to type something.
    /// </summary>
    public sealed class InputDialogOptionsTests
    {
        /// <summary>
        /// Verifies that the two values this type adds are kept.
        /// </summary>
        [Fact]
        public void Constructor_KeepsWhatItIsGiven()
        {
            // Arrange
            Hashtable table = SampleOptions.InputDialog();
            table["InitialInputText"] = "prefilled";
            table["SecureInput"] = true;

            // Act
            InputDialogOptions options = new(table);

            // Assert
            Assert.Equal("prefilled", options.InitialInputText);
            Assert.True(options.SecureInput);
        }

        /// <summary>
        /// Verifies the defaults for the two values this type adds.
        /// </summary>
        /// <remarks>
        /// <c language="csharp">SecureInput</c> defaulting to false is the safe direction only because false means an
        /// ordinary text box. A caller wanting a masked field has to ask for one, and asking is what the
        /// module does when collecting a credential.
        /// </remarks>
        [Fact]
        public void Constructor_DefaultsTheOptionalValues()
        {
            // Act
            InputDialogOptions options = new(SampleOptions.InputDialog());

            // Assert
            Assert.Null(options.InitialInputText);
            Assert.False(options.SecureInput);
        }

        /// <summary>
        /// Verifies that an initial value present but blank is refused.
        /// </summary>
        /// <remarks>
        /// Blank and absent would render the same empty box, so the distinction is not visible to a user;
        /// refusing it keeps a caller from believing it prefilled something when it did not.
        /// </remarks>
        /// <param name="value">The blank value to use.</param>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_RefusesABlankInitialValue(string value)
        {
            // Arrange
            Hashtable table = SampleOptions.InputDialog();
            table["InitialInputText"] = value;

            // Act & Assert
            _ = Assert.Throws<ArgumentException>(() => new InputDialogOptions(table));
        }

        /// <summary>
        /// Verifies that the button rule from the base type still applies here.
        /// </summary>
        /// <remarks>
        /// Worth one case rather than none: this type passes its arguments through a second constructor
        /// on the way to the one that enforces the rule, and a parameter dropped in that hand-off would
        /// leave the rule unenforced for input dialogs alone.
        /// </remarks>
        [Fact]
        public void Constructor_StillRequiresAtLeastOneButton()
        {
            // Arrange
            Hashtable table = SampleOptions.InputDialog();
            table.Remove("ButtonRightText");

            // Act & Assert
            _ = Assert.Throws<NotSupportedException>(() => new InputDialogOptions(table));
        }
    }
}
