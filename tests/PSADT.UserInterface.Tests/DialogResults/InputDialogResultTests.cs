using PSADT.UserInterface.DialogResults;
using Xunit;

namespace PSADT.UserInterface.Tests.DialogResults
{
    /// <summary>
    /// Tests the result of an input dialog.
    /// </summary>
    public sealed class InputDialogResultTests
    {
        /// <summary>
        /// Verifies that the outcome and the text are both kept.
        /// </summary>
        [Fact]
        public void Constructor_KeepsWhatItIsGiven()
        {
            // Act
            InputDialogResult result = new("Continue", "what the user typed");

            // Assert
            Assert.Equal("Continue", result.Result);
            Assert.Equal("what the user typed", result.Text);
        }

        /// <summary>
        /// Verifies that a result with no text is a valid one.
        /// </summary>
        /// <remarks>
        /// A dialog the user cancelled, or one that timed out, has an outcome but nothing typed. That is
        /// the normal case for the shared default rather than an edge case.
        /// </remarks>
        [Fact]
        public void Constructor_AcceptsNoText()
        {
            Assert.Null(InputDialogResult.DefaultResult.Text);
        }

        /// <summary>
        /// Verifies that a result equals itself when it holds no text.
        /// </summary>
        /// <remarks>
        /// This is the case that used to fail. The comparison read
        /// <c>Text?.Equals(other.Text, ...) is true</c>, which evaluates to false when <c>Text</c> is
        /// null - so a result with no text was not equal to itself, and
        /// <see cref="InputDialogResult.DefaultResult"/> in particular failed every comparison the module
        /// made against it while its hash code went on claiming the opposite.
        /// </remarks>
        [Fact]
        public void Equality_IsReflexiveWhenThereIsNoText()
        {
            // Arrange
            InputDialogResult result = InputDialogResult.DefaultResult;

            // Assert
            Assert.Equal(result, result);
            Assert.Equal(result, new InputDialogResult("Timeout", text: null));
        }

        /// <summary>
        /// Verifies that two results agreeing on both values are equal, and that the hash agrees.
        /// </summary>
        /// <param name="text">The text both results carry.</param>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("typed")]
        public void Equality_IsByTheOutcomeAndTheText(string? text)
        {
            // Arrange
            InputDialogResult first = new("Continue", text);
            InputDialogResult second = new("Continue", text);

            // Assert
            Assert.Equal(first, second);
            Assert.Equal(first.GetHashCode(), second.GetHashCode());
        }

        /// <summary>
        /// Verifies that a difference in either value makes two results unequal.
        /// </summary>
        /// <remarks>
        /// The null cases in both directions are the ones the old comparison got right by accident: it
        /// returned false for them, but for the wrong reason, and the same expression returned false for
        /// two equal nulls as well.
        /// </remarks>
        /// <param name="leftResult">The first result's outcome.</param>
        /// <param name="leftText">The first result's text.</param>
        /// <param name="rightResult">The second result's outcome.</param>
        /// <param name="rightText">The second result's text.</param>
        [Theory]
        [InlineData("Continue", "typed", "Cancel", "typed")]
        [InlineData("Continue", "typed", "Continue", "different")]
        [InlineData("Continue", null, "Continue", "typed")]
        [InlineData("Continue", "typed", "Continue", null)]
        [InlineData("Continue", "typed", "Continue", "TYPED")]
        public void Equality_DistinguishesADifferenceInEitherValue(string leftResult, string? leftText, string rightResult, string? rightText)
        {
            Assert.NotEqual(new InputDialogResult(leftResult, leftText), new InputDialogResult(rightResult, rightText));
        }

        /// <summary>
        /// Verifies that a result is not equal to one of a different derived type carrying the same values.
        /// </summary>
        /// <remarks>
        /// Both derived results hold an outcome and one nullable string, so a comparison written in terms
        /// of those two values alone would call them equal. They are different answers to different
        /// questions and must not be interchangeable.
        /// </remarks>
        [Fact]
        public void Equality_IsRefusedAgainstTheOtherDerivedResult()
        {
            Assert.NotEqual<object>(new InputDialogResult("Continue", "value"), new ListSelectionDialogResult("Continue", "value"));
        }

        /// <summary>
        /// Verifies that the shared default names a timeout with nothing typed.
        /// </summary>
        [Fact]
        public void DefaultResult_IsATimeoutWithNoText()
        {
            Assert.Equal("Timeout", InputDialogResult.DefaultResult.Result);
            Assert.Null(InputDialogResult.DefaultResult.Text);
        }

        /// <summary>
        /// Records that the text is not checked for blankness the way the list selection's item is.
        /// </summary>
        /// <remarks>
        /// <see cref="ListSelectionDialogResult"/> refuses a blank selected item;
        /// <see cref="InputDialogResult"/> accepts blank text. That asymmetry looks right rather than
        /// accidental - a user genuinely can type nothing but spaces into a text box, whereas a blank
        /// entry cannot be selected from a list - so it is recorded rather than corrected.
        /// </remarks>
        [Fact]
        public void Constructor_AcceptsBlankText()
        {
            Assert.Equal("   ", new InputDialogResult("Continue", "   ").Text);
        }
    }
}
