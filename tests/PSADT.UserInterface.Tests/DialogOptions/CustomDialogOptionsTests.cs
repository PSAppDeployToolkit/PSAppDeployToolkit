using System;
using System.Collections;
using PSADT.UserInterface.DialogOptions;
using PSADT.UserInterface.Tests.TestHelpers;
using Xunit;

namespace PSADT.UserInterface.Tests.DialogOptions
{
    /// <summary>
    /// Tests the options for the general-purpose custom dialog.
    /// </summary>
    /// <remarks>
    /// The only options type in the project that is neither abstract nor sealed: it is both constructed
    /// directly and inherited by the input and list-selection dialogs, so the button rule tested here is
    /// enforced on all three.
    /// </remarks>
    public sealed class CustomDialogOptionsTests
    {
        /// <summary>
        /// Verifies that the values handed in are the ones read back.
        /// </summary>
        [Fact]
        public void Constructor_KeepsWhatItIsGiven()
        {
            // Arrange
            Hashtable table = SampleOptions.CustomDialog();
            table["MessageAlignment"] = DialogMessageAlignment.Right;
            table["ButtonLeftText"] = "Left";
            table["ButtonMiddleText"] = "Middle";
            table["DefaultButton"] = DialogDefaultButton.Middle;
            table["Icon"] = DialogSystemIcon.Shield;
            table["MinimizeWindows"] = true;

            // Act
            CustomDialogOptions options = new(table);

            // Assert
            Assert.Equal("the message", options.MessageText);
            Assert.Equal(DialogMessageAlignment.Right, options.MessageAlignment);
            Assert.Equal("Left", options.ButtonLeftText);
            Assert.Equal("Middle", options.ButtonMiddleText);
            Assert.Equal("OK", options.ButtonRightText);
            Assert.Equal(DialogDefaultButton.Middle, options.DefaultButton);
            Assert.Equal(DialogSystemIcon.Shield, options.Icon);
            Assert.True(options.MinimizeWindows);
        }

        /// <summary>
        /// Verifies that a dialog with no buttons at all is refused.
        /// </summary>
        /// <remarks>
        /// The one rule in this type that is not a per-value check, and the only place in the project
        /// that raises <see cref="NotSupportedException"/>. A dialog with three null buttons would open
        /// with no way to dismiss it, which for a deployment blocking a user's machine is worse than
        /// failing.
        /// </remarks>
        [Fact]
        public void Constructor_RefusesADialogWithNoButtons()
        {
            // Arrange
            Hashtable table = SampleOptions.CustomDialog();
            table.Remove("ButtonRightText");

            // Act & Assert
            _ = Assert.Throws<NotSupportedException>(() => new CustomDialogOptions(table));
        }

        /// <summary>
        /// Verifies that any one button on its own satisfies the rule.
        /// </summary>
        /// <param name="key">The single button to define.</param>
        [Theory]
        [InlineData("ButtonLeftText")]
        [InlineData("ButtonMiddleText")]
        [InlineData("ButtonRightText")]
        public void Constructor_AcceptsAnySingleButton(string key)
        {
            // Arrange
            Hashtable table = SampleOptions.CustomDialog();
            table.Remove("ButtonRightText");
            table[key] = "Continue";

            // Act & Assert
            Assert.Equal("Continue", key switch
            {
                "ButtonLeftText" => new CustomDialogOptions(table).ButtonLeftText,
                "ButtonMiddleText" => new CustomDialogOptions(table).ButtonMiddleText,
                _ => new CustomDialogOptions(table).ButtonRightText,
            });
        }

        /// <summary>
        /// Verifies that a button present but blank is refused rather than treated as absent.
        /// </summary>
        /// <remarks>
        /// A blank button is not the same as no button: it satisfies the "at least one" rule while still
        /// rendering nothing, so it has to be caught before that rule is applied.
        /// </remarks>
        /// <param name="key">The button to blank out.</param>
        [Theory]
        [InlineData("ButtonLeftText")]
        [InlineData("ButtonMiddleText")]
        [InlineData("ButtonRightText")]
        public void Constructor_RefusesABlankButton(string key)
        {
            // Arrange
            Hashtable table = SampleOptions.CustomDialog();
            table[key] = "   ";

            // Act & Assert
            _ = Assert.Throws<ArgumentException>(() => new CustomDialogOptions(table));
        }

        /// <summary>
        /// Verifies that the message is required and cannot be blank.
        /// </summary>
        [Fact]
        public void Constructor_RequiresAMessage()
        {
            // Arrange
            Hashtable missing = SampleOptions.CustomDialog();
            Hashtable blank = SampleOptions.CustomDialog();
            missing.Remove("MessageText");
            blank["MessageText"] = "   ";

            // Act & Assert
            _ = Assert.Throws<ArgumentNullException>(() => new CustomDialogOptions(missing));
            _ = Assert.Throws<ArgumentException>(() => new CustomDialogOptions(blank));
        }

        /// <summary>
        /// Verifies the defaults for the values a caller can leave out.
        /// </summary>
        /// <remarks>
        /// <c language="csharp">MinimizeWindows</c> is the odd one: it is read with a <c language="csharp">?? false</c> rather than kept
        /// nullable, so its absence means "do not minimize" rather than "no preference".
        /// </remarks>
        [Fact]
        public void Constructor_DefaultsTheOptionalValues()
        {
            // Act
            CustomDialogOptions options = new(SampleOptions.CustomDialog());

            // Assert
            Assert.Null(options.MessageAlignment);
            Assert.Null(options.ButtonLeftText);
            Assert.Null(options.ButtonMiddleText);
            Assert.Null(options.DefaultButton);
            Assert.Null(options.Icon);
            Assert.False(options.MinimizeWindows);
        }

        /// <summary>
        /// Records that the default button is not checked against the buttons that exist.
        /// </summary>
        /// <remarks>
        /// Naming the middle button as the default while defining only the right one is accepted. Stated
        /// rather than assumed, for the same reason as the progress percentage: the dialog decides what
        /// to do about it at render time and nothing here says the combination is meaningful. If it
        /// should be refused, this is where that would be said.
        /// </remarks>
        [Fact]
        public void Constructor_DoesNotCheckTheDefaultButtonAgainstTheButtonsDefined()
        {
            // Arrange
            Hashtable table = SampleOptions.CustomDialog();
            table["DefaultButton"] = DialogDefaultButton.Middle;

            // Act
            CustomDialogOptions options = new(table);

            // Assert
            Assert.Equal(DialogDefaultButton.Middle, options.DefaultButton);
            Assert.Null(options.ButtonMiddleText);
        }
    }
}
