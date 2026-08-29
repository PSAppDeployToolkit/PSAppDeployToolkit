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
        /// Records that the percentage is not range-checked.
        /// </summary>
        /// <remarks>
        /// A value outside nought to a hundred is stored and passed on rather than refused. This is
        /// asserted so the behaviour is stated rather than assumed: the module's own parameter validation
        /// is what keeps a sane value out of here today, and a caller reaching this type directly - which
        /// the client executable does, from a deserialized payload - gets no second opinion. If that is
        /// not what is wanted, the guard belongs in the constructor and this test becomes the one that
        /// says so.
        /// </remarks>
        /// <param name="percentage">A percentage outside the expected range.</param>
        [Theory]
        [InlineData(-1d)]
        [InlineData(101d)]
        public void Constructor_DoesNotRangeCheckThePercentage(double percentage)
        {
            // Arrange
            Hashtable table = SampleOptions.ProgressDialog();
            table["ProgressPercentage"] = percentage;

            // Act & Assert
            Assert.Equal(percentage, new ProgressDialogOptions(table).ProgressPercentage);
        }
    }
}
