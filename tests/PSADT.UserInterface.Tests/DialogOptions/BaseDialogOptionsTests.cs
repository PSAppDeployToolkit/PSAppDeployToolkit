using System;
using System.Collections;
using System.Globalization;
using System.IO;
using PSADT.UserInterface.DialogOptions;
using PSADT.UserInterface.Tests.TestHelpers;
using Xunit;

namespace PSADT.UserInterface.Tests.DialogOptions
{
    /// <summary>
    /// Tests the fifteen options every dialog shares.
    /// </summary>
    /// <remarks>
    /// <c language="csharp">BaseDialogOptions</c> is abstract and both its constructors are <c language="csharp">private protected</c>, so it
    /// is exercised through <see cref="ProgressDialogOptions"/> - the derivative that adds least of its
    /// own and therefore obscures least. The base behaviour is tested once here and not restated in the
    /// other ten options tests, which cover only what their own type adds.
    /// </remarks>
    public sealed class BaseDialogOptionsTests
    {
        /// <summary>
        /// Verifies that the values handed in are the ones read back.
        /// </summary>
        /// <remarks>
        /// Every optional key is supplied here, which no other test does: the point is to prove each one
        /// is read from the dictionary under the name the module writes it under, and a key silently
        /// misspelled on either side would otherwise leave the field at its default and look correct.
        /// </remarks>
        [Fact]
        public void Constructor_KeepsEveryValueItIsGiven()
        {
            // Arrange
            string icon = TestImages.PngBase64(16, 16);
            string darkIcon = TestImages.PngBase64(24, 24);
            string banner = TestImages.PngBase64(32, 8);
            string taskbarIcon = TestImages.PngBase64(48, 48);
            Hashtable table = new()
            {
                ["AppTitle"] = "an application",
                ["Subtitle"] = "a subtitle",
                ["AppIconImage"] = icon,
                ["AppIconDarkImage"] = darkIcon,
                ["AppBannerImage"] = banner,
                ["AppTaskbarIconImage"] = taskbarIcon,
                ["DialogTopMost"] = true,
                ["Language"] = new CultureInfo("fr-FR"),
                ["FluentAccentColor"] = 0x0078D4,
                ["FluentAccentColorDark"] = 0x60CDFF,
                ["DialogPosition"] = DialogPosition.BottomRight,
                ["DialogAllowMove"] = true,
                ["DialogAllowMinimize"] = true,
                ["DialogExpiryDuration"] = TimeSpan.FromMinutes(30),
                ["DialogPersistInterval"] = TimeSpan.FromSeconds(90),
                ["ProgressMessageText"] = "the progress message",
                ["ProgressDetailMessageText"] = "the detail message",
            };

            // Act
            ProgressDialogOptions options = new(table);

            // Assert
            Assert.Equal("an application", options.AppTitle);
            Assert.Equal("a subtitle", options.Subtitle);
            Assert.Equal(icon, options.AppIconImage);
            Assert.Equal(darkIcon, options.AppIconDarkImage);
            Assert.Equal(banner, options.AppBannerImage);
            Assert.Equal(taskbarIcon, options.AppTaskbarIconImage);
            Assert.True(options.DialogTopMost);
            Assert.Equal("fr-FR", options.Language.Name);
            Assert.Equal(0x0078D4, options.FluentAccentColor);
            Assert.Equal(0x60CDFF, options.FluentAccentColorDark);
            Assert.Equal(DialogPosition.BottomRight, options.DialogPosition);
            Assert.True(options.DialogAllowMove);
            Assert.True(options.DialogAllowMinimize);
            Assert.Equal(TimeSpan.FromMinutes(30), options.DialogExpiryDuration);
            Assert.Equal(TimeSpan.FromSeconds(90), options.DialogPersistInterval);
        }

        /// <summary>
        /// Verifies that the optional keys are genuinely optional and come back as null when absent.
        /// </summary>
        /// <remarks>
        /// Null rather than a default matters for the three nullable booleans in particular. A dialog
        /// reads <c language="csharp">DialogAllowMinimize</c> as "only an explicit true opts in", so null and false are the
        /// same to it - but <c language="csharp">DialogAllowMove</c> and <c language="csharp">DialogPosition</c> fall through to a per-dialog
        /// default that a false or a zero would override.
        /// </remarks>
        [Fact]
        public void Constructor_LeavesTheOptionalValuesNullWhenTheyAreAbsent()
        {
            // Act
            ProgressDialogOptions options = new(SampleOptions.ProgressDialog());

            // Assert
            Assert.Null(options.AppIconDarkImage);
            Assert.Null(options.AppTaskbarIconImage);
            Assert.Null(options.FluentAccentColor);
            Assert.Null(options.FluentAccentColorDark);
            Assert.Null(options.DialogPosition);
            Assert.Null(options.DialogAllowMove);
            Assert.Null(options.DialogAllowMinimize);
            Assert.Null(options.DialogExpiryDuration);
            Assert.Null(options.DialogPersistInterval);
        }

        /// <summary>
        /// Verifies that a required key missing from the dictionary is reported as such.
        /// </summary>
        /// <param name="key">The key to remove.</param>
        [Theory]
        [InlineData("AppTitle")]
        [InlineData("Subtitle")]
        [InlineData("AppIconImage")]
        [InlineData("AppBannerImage")]
        [InlineData("DialogTopMost")]
        [InlineData("Language")]
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
        /// Verifies that a required string present but blank is refused.
        /// </summary>
        /// <remarks>
        /// Separate from the missing-key case because it fails somewhere else: a blank value survives the
        /// dictionary read and is caught by the guard in the constructor body, so the two paths could
        /// diverge without this.
        /// </remarks>
        /// <param name="key">The key to blank out.</param>
        /// <param name="value">The blank value to use.</param>
        [Theory]
        [InlineData("AppTitle", "")]
        [InlineData("AppTitle", "   ")]
        [InlineData("Subtitle", "")]
        [InlineData("Subtitle", "   ")]
        [InlineData("ProgressMessageText", "")]
        [InlineData("ProgressDetailMessageText", "   ")]
        public void Constructor_RefusesABlankRequiredString(string key, string value)
        {
            // Arrange
            Hashtable table = SampleOptions.ProgressDialog();
            table[key] = value;

            // Act & Assert
            _ = Assert.Throws<ArgumentException>(() => new ProgressDialogOptions(table));
        }

        /// <summary>
        /// Verifies that the language is rebuilt on each read rather than stored.
        /// </summary>
        /// <remarks>
        /// This is what keeps the record's equality honest. <see cref="CultureInfo"/> compares by
        /// reference, so holding one in a field would reduce the whole record to reference equality; the
        /// name is stored instead and the culture constructed on demand. That the property hands out a
        /// different instance each time is the observable consequence, and asserting it here is what
        /// stops the field being reintroduced as a convenience.
        /// </remarks>
        [Fact]
        public void Language_IsRebuiltFromTheStoredNameOnEachRead()
        {
            // Arrange
            Hashtable table = SampleOptions.ProgressDialog();
            table["Language"] = new CultureInfo("de-DE");

            // Act
            ProgressDialogOptions options = new(table);

            // Assert
            Assert.Equal("de-DE", options.Language.Name);
            Assert.NotSame(options.Language, options.Language);
        }

        /// <summary>
        /// Verifies that two sets of options describing the same dialog are equal despite each holding
        /// its own culture object.
        /// </summary>
        /// <remarks>
        /// The other half of the point above: the reason for storing a name is that this comparison has
        /// to succeed, and it would not if the culture itself were a field.
        /// </remarks>
        [Fact]
        public void Equality_IsNotDefeatedByTheCulture()
        {
            // Arrange
            Hashtable first = SampleOptions.ProgressDialog();
            Hashtable second = SampleOptions.ProgressDialog();
            first["Language"] = new CultureInfo("en-AU");
            second["Language"] = new CultureInfo("en-AU");
            second["AppIconImage"] = first["AppIconImage"];
            second["AppBannerImage"] = first["AppBannerImage"];

            // Assert
            Assert.Equal(new ProgressDialogOptions(first), new ProgressDialogOptions(second));
        }

        /// <summary>
        /// Verifies that a valid image is accepted whether it arrives as base64 or as a path on disk.
        /// </summary>
        /// <remarks>
        /// Both forms reach this from the module - an icon configured as a file, a banner embedded in the
        /// configuration - and they take different branches through the validator.
        /// </remarks>
        [Fact]
        public void ImageValidation_AcceptsBase64AndFilePathsAlike()
        {
            // Arrange
            using TempDirectory directory = new();
            string path = directory.WriteFile("icon.png", TestImages.PngBytes(32, 32));
            Hashtable table = SampleOptions.ProgressDialog();
            table["AppIconImage"] = path;
            table["AppBannerImage"] = TestImages.PngBase64(64, 16);

            // Act
            ProgressDialogOptions options = new(table);

            // Assert - the string is kept as given, not resolved or re-encoded.
            Assert.Equal(path, options.AppIconImage);
        }

        /// <summary>
        /// Verifies that an icon file is accepted, which takes the branch a bitmap does not.
        /// </summary>
        /// <remarks>
        /// The validator asks whether the stream is an icon before deciding how to load it, so an ICO and
        /// a PNG prove different halves of it.
        /// </remarks>
        [Fact]
        public void ImageValidation_AcceptsAnIcon()
        {
            // Arrange
            using TempDirectory directory = new();
            string path = directory.WriteFile("icon.ico", TestImages.IcoBytes(32));
            Hashtable table = SampleOptions.ProgressDialog();
            table["AppIconImage"] = path;

            // Act & Assert
            Assert.Equal(path, new ProgressDialogOptions(table).AppIconImage);
        }

        /// <summary>
        /// Verifies that something that is not an image is refused, and named.
        /// </summary>
        /// <remarks>
        /// The identifier in the message is the only thing telling a deployer which of the four images
        /// was the bad one, so it is asserted rather than just the exception type.
        /// </remarks>
        [Fact]
        public void ImageValidation_RefusesContentThatIsNotAnImage()
        {
            // Arrange
            Hashtable table = SampleOptions.ProgressDialog();
            table["AppIconImage"] = TestImages.NotAnImageBase64();

            // Act & Assert
            BadImageFormatException exception = Assert.Throws<BadImageFormatException>(() => new ProgressDialogOptions(table));
            Assert.Contains("AppIconImage", exception.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that a path to a file that is not there is refused as a bad image rather than
        /// escaping as a file error.
        /// </summary>
        [Fact]
        public void ImageValidation_RefusesAPathThatDoesNotExist()
        {
            // Arrange
            using TempDirectory directory = new();
            Hashtable table = SampleOptions.ProgressDialog();
            table["AppBannerImage"] = directory.GetPath("no-such-file.png");

            // Act & Assert
            BadImageFormatException exception = Assert.Throws<BadImageFormatException>(() => new ProgressDialogOptions(table));
            Assert.Contains("AppBannerImage", exception.Message, StringComparison.Ordinal);
            _ = Assert.IsType<FileNotFoundException>(exception.InnerException);
        }

        /// <summary>
        /// Verifies that a file that exists but holds something else is refused.
        /// </summary>
        [Fact]
        public void ImageValidation_RefusesAFileThatIsNotAnImage()
        {
            // Arrange
            using TempDirectory directory = new();
            Hashtable table = SampleOptions.ProgressDialog();
            table["AppIconImage"] = directory.WriteFile("not-an-image.png", TestImages.NotAnImage());

            // Act & Assert
            _ = Assert.Throws<BadImageFormatException>(() => new ProgressDialogOptions(table));
        }

        /// <summary>
        /// Verifies that an optional image is validated too when one is supplied.
        /// </summary>
        /// <remarks>
        /// The two optional images are validated inside a conditional rather than alongside the required
        /// ones, which is the kind of place a check gets left out.
        /// </remarks>
        /// <param name="key">The optional image key to make invalid.</param>
        [Theory]
        [InlineData("AppIconDarkImage")]
        [InlineData("AppTaskbarIconImage")]
        public void ImageValidation_AppliesToTheOptionalImagesWhenTheyArePresent(string key)
        {
            // Arrange
            Hashtable table = SampleOptions.ProgressDialog();
            table[key] = TestImages.NotAnImageBase64();

            // Act & Assert
            BadImageFormatException exception = Assert.Throws<BadImageFormatException>(() => new ProgressDialogOptions(table));
            Assert.Contains(key, exception.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that an optional image present but blank is refused rather than treated as absent.
        /// </summary>
        /// <param name="key">The optional image key to blank out.</param>
        [Theory]
        [InlineData("AppIconDarkImage")]
        [InlineData("AppTaskbarIconImage")]
        public void ImageValidation_RefusesABlankOptionalImage(string key)
        {
            // Arrange
            Hashtable table = SampleOptions.ProgressDialog();
            table[key] = "   ";

            // Act & Assert
            _ = Assert.Throws<ArgumentException>(() => new ProgressDialogOptions(table));
        }
    }
}
