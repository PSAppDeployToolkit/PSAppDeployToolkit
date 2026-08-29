using System;
using System.Collections;
using PSADT.UserInterface.DialogOptions;
using PSADT.UserInterface.Tests.TestHelpers;
using Xunit;

namespace PSADT.UserInterface.Tests.DialogOptions
{
    /// <summary>
    /// Tests the options for the Windows 9x-style message box.
    /// </summary>
    /// <remarks>
    /// Stands alone rather than deriving from <c language="csharp">BaseDialogOptions</c>, because a message box is drawn by
    /// Win32 rather than by the toolkit and takes none of the branding a toolkit dialog does. What it
    /// does take is the three Win32 style enumerations, which is most of what there is to check.
    /// </remarks>
    public sealed class DialogBoxOptionsTests
    {
        /// <summary>
        /// Verifies that the values handed in are the ones read back.
        /// </summary>
        [Fact]
        public void Constructor_KeepsWhatItIsGiven()
        {
            // Arrange
            Hashtable table = SampleOptions.DialogBox();
            table["DialogButtons"] = DialogBoxButtons.YesNoCancel;
            table["DialogDefaultButton"] = DialogBoxDefaultButton.Second;
            table["DialogIcon"] = DialogBoxIcon.Question;
            table["DialogExpiryDuration"] = TimeSpan.FromSeconds(45);

            // Act
            DialogBoxOptions options = new(table);

            // Assert
            Assert.Equal("an application", options.AppTitle);
            Assert.Equal("the message", options.MessageText);
            Assert.Equal(DialogBoxButtons.YesNoCancel, options.DialogButtons);
            Assert.Equal(DialogBoxDefaultButton.Second, options.DialogDefaultButton);
            Assert.Equal(DialogBoxIcon.Question, options.DialogIcon);
            Assert.True(options.DialogTopMost);
            Assert.Equal(TimeSpan.FromSeconds(45), options.DialogExpiryDuration);
        }

        /// <summary>
        /// Verifies that a required key missing from the dictionary is reported as such.
        /// </summary>
        /// <remarks>
        /// The expiry duration is required here where every other dialog treats it as optional. That is
        /// the difference worth pinning: a message box is shown by a blocking Win32 call, so there has to
        /// be a timeout for the deployment to ever regain control.
        /// </remarks>
        /// <param name="key">The key to remove.</param>
        [Theory]
        [InlineData("AppTitle")]
        [InlineData("MessageText")]
        [InlineData("DialogButtons")]
        [InlineData("DialogDefaultButton")]
        [InlineData("DialogTopMost")]
        [InlineData("DialogExpiryDuration")]
        public void Constructor_RefusesADictionaryMissingARequiredKey(string key)
        {
            // Arrange
            Hashtable table = SampleOptions.DialogBox();
            table.Remove(key);

            // Act & Assert
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new DialogBoxOptions(table));
            Assert.Contains(key, exception.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that the icon is the one optional part.
        /// </summary>
        /// <remarks>
        /// Win32 has no constant for "no icon" - passing zero is what suppresses it - so the absence has
        /// to survive as a null rather than as a value. See
        /// <see cref="DialogBoxIconTests.Members_DoNotIncludeAZeroValue"/> for the other half of this.
        /// </remarks>
        [Fact]
        public void Constructor_LeavesTheIconNullWhenItIsAbsent()
        {
            Assert.Null(new DialogBoxOptions(SampleOptions.DialogBox()).DialogIcon);
        }

        /// <summary>
        /// Verifies that a blank title or message is refused.
        /// </summary>
        /// <param name="key">The key to blank out.</param>
        [Theory]
        [InlineData("AppTitle")]
        [InlineData("MessageText")]
        public void Constructor_RefusesABlankString(string key)
        {
            // Arrange
            Hashtable table = SampleOptions.DialogBox();
            table[key] = "   ";

            // Act & Assert
            _ = Assert.Throws<ArgumentException>(() => new DialogBoxOptions(table));
        }
    }
}
