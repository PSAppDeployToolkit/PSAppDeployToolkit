using System;
using System.Collections;
using PSADT.UserInterface.DialogOptions;
using PSADT.UserInterface.Tests.TestHelpers;
using Xunit;

namespace PSADT.UserInterface.Tests.DialogOptions
{
    /// <summary>
    /// Tests the options for a balloon tip.
    /// </summary>
    /// <remarks>
    /// One of the three options types that stand alone rather than deriving from <c language="csharp">BaseDialogOptions</c>,
    /// which is why nothing here concerns images, culture or window placement: a balloon tip borrows the
    /// notify icon that is already in the tray.
    /// </remarks>
    public sealed class BalloonTipOptionsTests
    {
        /// <summary>
        /// Verifies that the values handed in are the ones read back.
        /// </summary>
        [Fact]
        public void Constructor_KeepsWhatItIsGiven()
        {
            // Arrange
            Hashtable table = SampleOptions.BalloonTip();
            table["Icon"] = BalloonTipIcon.Warning;

            // Act
            BalloonTipOptions options = new(table);

            // Assert
            Assert.Equal("a title", options.Title);
            Assert.Equal("the balloon text", options.Text);
            Assert.Equal(BalloonTipIcon.Warning, options.Icon);
        }

        /// <summary>
        /// Verifies that every key is required, the icon included.
        /// </summary>
        /// <remarks>
        /// The icon is required rather than defaulted, which is worth pinning: <see cref="BalloonTipIcon"/>
        /// has a zero member named <c language="csharp">None</c>, so an absent icon could plausibly have been read as that
        /// instead. It is not, and a caller has to say which it wants.
        /// </remarks>
        /// <param name="key">The key to remove.</param>
        [Theory]
        [InlineData("Title")]
        [InlineData("Text")]
        [InlineData("Icon")]
        public void Constructor_RefusesADictionaryMissingARequiredKey(string key)
        {
            // Arrange
            Hashtable table = SampleOptions.BalloonTip();
            table.Remove(key);

            // Act & Assert
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new BalloonTipOptions(table));
            Assert.Contains(key, exception.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that a blank title or text is refused.
        /// </summary>
        /// <remarks>
        /// A balloon tip with no text is a notification the user cannot read, so it is refused rather than
        /// shown empty.
        /// </remarks>
        /// <param name="key">The key to blank out.</param>
        /// <param name="value">The blank value to use.</param>
        [Theory]
        [InlineData("Title", "")]
        [InlineData("Title", "   ")]
        [InlineData("Text", "")]
        [InlineData("Text", "   ")]
        public void Constructor_RefusesABlankString(string key, string value)
        {
            // Arrange
            Hashtable table = SampleOptions.BalloonTip();
            table[key] = value;

            // Act & Assert
            _ = Assert.Throws<ArgumentException>(() => new BalloonTipOptions(table));
        }

        /// <summary>
        /// Verifies that two tips saying the same thing are equal and two saying different things are not.
        /// </summary>
        /// <remarks>
        /// Equality matters because these cross a process boundary; the general round trip is covered by
        /// <see cref="DataContractRoundTripTests"/>, and what is checked here is that the icon takes part
        /// in the comparison rather than only the two strings.
        /// </remarks>
        [Fact]
        public void Equality_IncludesTheIcon()
        {
            // Arrange
            Hashtable differentIcon = SampleOptions.BalloonTip();
            differentIcon["Icon"] = BalloonTipIcon.Error;

            // Assert
            Assert.Equal(new BalloonTipOptions(SampleOptions.BalloonTip()), new BalloonTipOptions(SampleOptions.BalloonTip()));
            Assert.NotEqual(new BalloonTipOptions(SampleOptions.BalloonTip()), new BalloonTipOptions(differentIcon));
        }
    }
}
