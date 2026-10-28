using System.Collections;
using PSADT.ClientServer.Payloads;
using PSADT.ClientServer.Server.Tests.TestHelpers;
using PSADT.UserInterface;
using PSADT.UserInterface.DialogOptions;
using Xunit;

namespace PSADT.ClientServer.Server.Tests.Payloads
{
    /// <summary>
    /// Tests the payload asking a client to show a progress dialog.
    /// </summary>
    /// <remarks>
    /// It carries two things rather than one, so the comparison has to take both into account - a payload
    /// asking for the same dialog in a different style is a different request.
    /// </remarks>
    public sealed class ShowProgressDialogPayloadTests
    {
        /// <summary>
        /// Verifies that the style and the options it was built with are both carried.
        /// </summary>
        [Fact]
        public void ShowProgressDialogPayload_CarriesItsStyleAndOptions()
        {
            // Arrange
            ShowProgressDialogPayload payload = new(DialogStyle.Fluent, SampleOptions.ProgressDialog());

            // Assert
            Assert.Equal(DialogStyle.Fluent, payload.DialogStyle);
            Assert.Equal(SampleOptions.ProgressDialog(), payload.Options);
        }

        /// <summary>
        /// Verifies that both the style and the options count towards the comparison.
        /// </summary>
        [Fact]
        public void ShowProgressDialogPayload_ComparesByItsStyleAndOptions()
        {
            Assert.Equal(
                new ShowProgressDialogPayload(DialogStyle.Fluent, SampleOptions.ProgressDialog()),
                new ShowProgressDialogPayload(DialogStyle.Fluent, SampleOptions.ProgressDialog()));
            Assert.NotEqual(
                new ShowProgressDialogPayload(DialogStyle.Fluent, SampleOptions.ProgressDialog()),
                new ShowProgressDialogPayload(DialogStyle.Classic, SampleOptions.ProgressDialog()));
            Assert.NotEqual(
                new ShowProgressDialogPayload(DialogStyle.Fluent, SampleOptions.ProgressDialog()),
                new ShowProgressDialogPayload(DialogStyle.Fluent, SampleOptions.ProgressDialog("something else")));
        }

        /// <summary>
        /// Verifies that it survives the trip to the client with both parts intact.
        /// </summary>
        [Fact]
        public void ShowProgressDialogPayload_SurvivesTheTripToTheClient()
        {
            // Arrange
            ShowProgressDialogPayload original = new(DialogStyle.Fluent, SampleOptions.ProgressDialog());

            // Act
            ShowProgressDialogPayload restored = DataSerialization.DeserializeFromBytes<ShowProgressDialogPayload>(DataSerialization.SerializeToBytes(original));

            // Assert
            Assert.Equal(original, restored);
            Assert.Equal(DialogStyle.Fluent, restored.DialogStyle);
            Assert.Equal("the progress message", restored.Options.ProgressMessageText);
        }

        /// <summary>
        /// Verifies that an Exact request from either color survives the actual client/server transport.
        /// </summary>
        /// <param name="lightExact">The Light preference.</param>
        /// <param name="darkExact">The Dark preference.</param>
        [Theory]
        [InlineData(true, false)]
        [InlineData(false, true)]
        public void ShowProgressDialogPayload_RetainsEitherExactPreference(bool lightExact, bool darkExact)
        {
            ProgressDialogOptions sample = SampleOptions.ProgressDialog();
            ProgressDialogOptions options = new(new Hashtable
            {
                ["AppTitle"] = sample.AppTitle,
                ["Subtitle"] = sample.Subtitle,
                ["AppIconImage"] = sample.AppIconImage,
                ["AppBannerImage"] = sample.AppBannerImage,
                ["DialogTopMost"] = sample.DialogTopMost,
                ["Language"] = sample.Language,
                ["ProgressMessageText"] = sample.ProgressMessageText,
                ["ProgressDetailMessageText"] = sample.ProgressDetailMessageText,
                ["FluentAccentColor"] = new Hashtable { ["Color"] = unchecked((int)0xFF0078D7), ["Exact"] = lightExact },
                ["FluentAccentColorDark"] = new Hashtable { ["Color"] = unchecked((int)0xFF60CDFF), ["Exact"] = darkExact },
            });
            ShowProgressDialogPayload original = new(DialogStyle.Fluent, options);

            ShowProgressDialogPayload restored = DataSerialization.DeserializeFromBytes<ShowProgressDialogPayload>(DataSerialization.SerializeToBytes(original));

            Assert.Equal(original, restored);
            Assert.Equal(options.FluentAccentColor, restored.Options.FluentAccentColor);
            Assert.Equal(options.FluentAccentColorDark, restored.Options.FluentAccentColorDark);
            Assert.True(restored.Options.FluentAccentColorExact);
        }
    }
}
