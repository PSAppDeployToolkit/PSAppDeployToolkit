using System;
using PSADT.UserInterface.DialogResults;
using Xunit;

namespace PSADT.UserInterface.Tests.DialogResults
{
    /// <summary>
    /// Tests the result of a list selection dialog.
    /// </summary>
    public sealed class ListSelectionDialogResultTests
    {
        /// <summary>
        /// Verifies that the outcome and the selected item are both kept.
        /// </summary>
        [Fact]
        public void Constructor_KeepsWhatItIsGiven()
        {
            // Act
            ListSelectionDialogResult result = new("Continue", "bravo");

            // Assert
            Assert.Equal("Continue", result.Result);
            Assert.Equal("bravo", result.SelectedItem);
        }

        /// <summary>
        /// Verifies that a result with nothing selected is a valid one.
        /// </summary>
        [Fact]
        public void Constructor_AcceptsNoSelection()
        {
            Assert.Null(ListSelectionDialogResult.DefaultResult.SelectedItem);
        }

        /// <summary>
        /// Verifies that a selected item present but blank is refused.
        /// </summary>
        /// <remarks>
        /// Unlike an input dialog's text, a selection has to name something that was in the list, and a
        /// blank string names nothing. Absent and blank are therefore genuinely different states here.
        /// </remarks>
        /// <param name="value">The blank selection to refuse.</param>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_RefusesABlankSelection(string value)
        {
            _ = Assert.Throws<ArgumentException>(() => new ListSelectionDialogResult("Continue", value));
        }

        /// <summary>
        /// Verifies that a result equals itself when nothing was selected.
        /// </summary>
        /// <remarks>
        /// The case that used to fail, for the same reason as
        /// <see cref="InputDialogResultTests.Equality_IsReflexiveWhenThereIsNoText"/>: the comparison read
        /// <c language="csharp">SelectedItem?.Equals(...) is true</c>, which is false when the item is null, so a cancelled
        /// or expired selection was not equal to itself.
        /// </remarks>
        [Fact]
        public void Equality_IsReflexiveWhenThereIsNoSelection()
        {
            // Arrange
            ListSelectionDialogResult result = ListSelectionDialogResult.DefaultResult;

            // Assert
            Assert.Equal(result, result);
            Assert.Equal(result, new ListSelectionDialogResult("Timeout", selectedItem: null));
        }

        /// <summary>
        /// Verifies that two results agreeing on both values are equal, and that the hash agrees.
        /// </summary>
        /// <param name="selectedItem">The item both results carry.</param>
        [Theory]
        [InlineData(null)]
        [InlineData("bravo")]
        public void Equality_IsByTheOutcomeAndTheSelection(string? selectedItem)
        {
            // Arrange
            ListSelectionDialogResult first = new("Continue", selectedItem);
            ListSelectionDialogResult second = new("Continue", selectedItem);

            // Assert
            Assert.Equal(first, second);
            Assert.Equal(first.GetHashCode(), second.GetHashCode());
        }

        /// <summary>
        /// Verifies that a difference in either value makes two results unequal.
        /// </summary>
        /// <param name="leftResult">The first result's outcome.</param>
        /// <param name="leftItem">The first result's selection.</param>
        /// <param name="rightResult">The second result's outcome.</param>
        /// <param name="rightItem">The second result's selection.</param>
        [Theory]
        [InlineData("Continue", "bravo", "Cancel", "bravo")]
        [InlineData("Continue", "bravo", "Continue", "charlie")]
        [InlineData("Continue", null, "Continue", "bravo")]
        [InlineData("Continue", "bravo", "Continue", null)]
        [InlineData("Continue", "bravo", "Continue", "BRAVO")]
        public void Equality_DistinguishesADifferenceInEitherValue(string leftResult, string? leftItem, string rightResult, string? rightItem)
        {
            Assert.NotEqual(new ListSelectionDialogResult(leftResult, leftItem), new ListSelectionDialogResult(rightResult, rightItem));
        }

        /// <summary>
        /// Verifies that the shared default names a timeout with nothing selected.
        /// </summary>
        [Fact]
        public void DefaultResult_IsATimeoutWithNoSelection()
        {
            Assert.Equal("Timeout", ListSelectionDialogResult.DefaultResult.Result);
            Assert.Null(ListSelectionDialogResult.DefaultResult.SelectedItem);
        }
    }
}
