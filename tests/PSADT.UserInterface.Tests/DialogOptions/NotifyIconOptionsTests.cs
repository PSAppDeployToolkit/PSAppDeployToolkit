using System;
using System.Collections;
using PSADT.UserInterface.DialogOptions;
using PSADT.UserInterface.Tests.TestHelpers;
using Xunit;

namespace PSADT.UserInterface.Tests.DialogOptions
{
    /// <summary>
    /// Tests the options for the tray icon a deployment shows while it runs.
    /// </summary>
    public sealed class NotifyIconOptionsTests
    {
        /// <summary>
        /// Verifies that the values handed in are the ones read back.
        /// </summary>
        [Fact]
        public void Constructor_KeepsWhatItIsGiven()
        {
            // Arrange
            string icon = TestImages.PngBase64(16, 16);
            string taskbarIcon = TestImages.PngBase64(32, 32);
            Hashtable table = SampleOptions.NotifyIcon();
            table["AppIconImage"] = icon;
            table["AppTaskbarIconImage"] = taskbarIcon;

            // Act
            NotifyIconOptions options = new(table);

            // Assert
            Assert.Equal("an application", options.AppTitle);
            Assert.Equal(icon, options.AppIconImage);
            Assert.Equal(taskbarIcon, options.AppTaskbarIconImage);
            Assert.Equal("the tooltip text", options.MessageText);
        }

        /// <summary>
        /// Verifies that a null dictionary is reported as a null argument.
        /// </summary>
        /// <remarks>
        /// This type used to index the dictionary before checking it, so a null one produced a
        /// <see cref="NullReferenceException"/> from inside a constructor rather than the
        /// <see cref="ArgumentNullException"/> its own documentation promised and every sibling type
        /// threw. The sweep in <see cref="DialogOptionsContractTests"/> covers all eleven types; this
        /// stays because it is the one that was wrong.
        /// </remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "This is deliberate as part of unit testing.")]
        [Fact]
        public void Constructor_RefusesANullDictionary()
        {
            _ = Assert.Throws<ArgumentNullException>(static () => new NotifyIconOptions(null!));
        }

        /// <summary>
        /// Verifies that a required key missing from the dictionary is reported as such.
        /// </summary>
        /// <param name="key">The key to remove.</param>
        [Theory]
        [InlineData("AppTitle")]
        [InlineData("AppIconImage")]
        [InlineData("MessageText")]
        public void Constructor_RefusesADictionaryMissingARequiredKey(string key)
        {
            // Arrange
            Hashtable table = SampleOptions.NotifyIcon();
            table.Remove(key);

            // Act & Assert
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new NotifyIconOptions(table));
            Assert.Contains(key, exception.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that the taskbar icon is optional.
        /// </summary>
        [Fact]
        public void Constructor_LeavesTheTaskbarIconNullWhenItIsAbsent()
        {
            Assert.Null(new NotifyIconOptions(SampleOptions.NotifyIcon()).AppTaskbarIconImage);
        }

        /// <summary>
        /// Verifies that both images are validated, the optional one included.
        /// </summary>
        /// <remarks>
        /// This type does its own image validation by calling the base type's validator as a static rather
        /// than by inheriting it, so the checks are a separate piece of code from the ones covered by
        /// <see cref="BaseDialogOptionsTests"/> and are worth their own case.
        /// </remarks>
        /// <param name="key">The image key to make invalid.</param>
        [Theory]
        [InlineData("AppIconImage")]
        [InlineData("AppTaskbarIconImage")]
        public void Constructor_ValidatesBothImages(string key)
        {
            // Arrange
            Hashtable table = SampleOptions.NotifyIcon();
            table[key] = TestImages.NotAnImageBase64();

            // Act & Assert
            BadImageFormatException exception = Assert.Throws<BadImageFormatException>(() => new NotifyIconOptions(table));
            Assert.Contains(key, exception.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that a blank required string is refused.
        /// </summary>
        /// <param name="key">The key to blank out.</param>
        [Theory]
        [InlineData("AppTitle")]
        [InlineData("MessageText")]
        [InlineData("AppIconImage")]
        public void Constructor_RefusesABlankRequiredString(string key)
        {
            // Arrange
            Hashtable table = SampleOptions.NotifyIcon();
            table[key] = "   ";

            // Act & Assert
            _ = Assert.Throws<ArgumentException>(() => new NotifyIconOptions(table));
        }
    }
}
