using System;
using System.Collections;
using PSADT.UserInterface.DialogOptions;
using PSADT.UserInterface.Tests.TestHelpers;
using Xunit;

namespace PSADT.UserInterface.Tests.DialogOptions
{
    /// <summary>
    /// Tests the options for the progress dialog.
    /// </summary>
    /// <remarks>
    /// Only the four values this type adds are covered here; the fifteen it inherits are tested once in
    /// <see cref="BaseDialogOptionsTests"/>, which uses this type as its vehicle for exactly that reason.
    /// </remarks>
    public sealed class ProgressDialogOptionsTests
    {
        /// <summary>
        /// Verifies that the values handed in are the ones read back.
        /// </summary>
        [Fact]
        public void Constructor_KeepsWhatItIsGiven()
        {
            // Arrange
            Hashtable table = SampleOptions.ProgressDialog();
            table["ProgressPercentage"] = 42.5d;
            table["MessageAlignment"] = DialogMessageAlignment.Center;

            // Act
            ProgressDialogOptions options = new(table);

            // Assert
            Assert.Equal("the progress message", options.ProgressMessageText);
            Assert.Equal("the detail message", options.ProgressDetailMessageText);
            Assert.Equal(42.5d, options.ProgressPercentage);
            Assert.Equal(DialogMessageAlignment.Center, options.MessageAlignment);
        }

        /// <summary>
        /// Verifies that both message strings are required.
        /// </summary>
        /// <remarks>
        /// The detail line is required as well as the headline one, which is not obvious from the dialog:
        /// a progress window with an empty second line looks like a rendering fault rather than a choice,
        /// so the caller is made to supply something even if it is only a space-filling phrase.
        /// </remarks>
        /// <param name="key">The key to remove.</param>
        [Theory]
        [InlineData("ProgressMessageText")]
        [InlineData("ProgressDetailMessageText")]
        public void Constructor_RefusesADictionaryMissingARequiredKey(string key)
        {
            // Arrange
            Hashtable table = SampleOptions.ProgressDialog();
            table.Remove(key);

            // Act & Assert
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new ProgressDialogOptions(table));
            Assert.Contains(key, exception.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that the percentage and the alignment are optional.
        /// </summary>
        /// <remarks>
        /// An absent percentage is how the caller asks for a marquee bar rather than a measured one, so it
        /// has to stay null rather than become a zero that would show an empty bar instead.
        /// </remarks>
        [Fact]
        public void Constructor_LeavesTheOptionalValuesNullWhenTheyAreAbsent()
        {
            // Act
            ProgressDialogOptions options = new(SampleOptions.ProgressDialog());

            // Assert
            Assert.Null(options.ProgressPercentage);
            Assert.Null(options.MessageAlignment);
        }

        /// <summary>
        /// Verifies that a percentage outside nought to a hundred is refused.
        /// </summary>
        /// <remarks>
        /// Not a tidiness check. The classic dialog assigns this straight to a progress bar's value, and
        /// a bar refuses anything outside its range - on the thread drawing the dialog, where the failure
        /// has nothing to do with the caller that caused it. The range is what every doc comment on the
        /// way down already claims, so this makes the claim true at the point the value arrives.
        /// <para>
        /// The two infinities and a NaN are included because they slip past a naive pair of comparisons:
        /// every comparison against NaN is false, so a guard written only as "less than nought or greater
        /// than a hundred" would let it through.
        /// </para>
        /// </remarks>
        /// <param name="percentage">A percentage outside the expected range.</param>
        [Theory]
        [InlineData(-1d)]
        [InlineData(-0.0001d)]
        [InlineData(100.0001d)]
        [InlineData(101d)]
        [InlineData(double.NaN)]
        [InlineData(double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity)]
        public void Constructor_RefusesAPercentageOutsideItsRange(double percentage)
        {
            // Arrange
            Hashtable table = SampleOptions.ProgressDialog();
            table["ProgressPercentage"] = percentage;

            // Act & Assert
            ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new ProgressDialogOptions(table));
            Assert.Equal("progressPercentage", exception.ParamName);
        }

        /// <summary>
        /// Verifies that the ends of the range are accepted.
        /// </summary>
        /// <remarks>
        /// A guard written with the wrong comparison would reject a finished operation reporting a hundred
        /// per cent, which is the one value every determinate progress bar ends on.
        /// </remarks>
        /// <param name="percentage">A percentage at the edge of the range.</param>
        [Theory]
        [InlineData(0d)]
        [InlineData(50d)]
        [InlineData(100d)]
        public void Constructor_AcceptsThePercentagesAtEachEndOfTheRange(double percentage)
        {
            // Arrange
            Hashtable table = SampleOptions.ProgressDialog();
            table["ProgressPercentage"] = percentage;

            // Act & Assert
            Assert.Equal(percentage, new ProgressDialogOptions(table).ProgressPercentage);
        }
    }
}
