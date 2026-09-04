using System;
using System.IO;
using PSADT.Tests.TestHelpers;
using PSADT.Utilities;
using Xunit;

namespace PSADT.Tests.Utilities
{
    /// <summary>
    /// Tests reading a font's display title out of its name table.
    /// </summary>
    /// <remarks>
    /// The title is what a font is registered under, so reading the wrong one leaves an installed font
    /// findable only by file name. Getting it right means walking the name table through several passes -
    /// full name, then typographic family and subfamily, then the plain family - and choosing an encoding
    /// per record, so the fallbacks are what the tests below aim at.
    /// <para>
    /// Registering and unregistering fonts is deliberately not covered: both change machine state that
    /// outlives the test.
    /// </para>
    /// </remarks>
    public sealed class FontUtilitiesTests
    {
        /// <summary>
        /// Verifies that a font shipped with Windows reports the title it is known by.
        /// </summary>
        [Fact(Skip = "No recognised system font was found on this machine.", SkipUnless = nameof(TestEnvironment.HasArialFont), SkipType = typeof(TestEnvironment))]
        public void GetFontTitle_ReadsTheTitleOfASystemFont()
        {
            // Arrange
            FileInfo? font = TestEnvironment.ArialFont;
            Assert.NotNull(font);

            // Act & Assert
            Assert.Equal("Arial", FontUtilities.GetFontTitle(font.FullName));
        }

        /// <summary>
        /// Verifies that a path wrapped in quotes and padded with spaces is accepted, since a path that
        /// came from a command line arrives that way.
        /// </summary>
        [Fact(Skip = "No recognised system font was found on this machine.", SkipUnless = nameof(TestEnvironment.HasArialFont), SkipType = typeof(TestEnvironment))]
        public void GetFontTitle_AcceptsAQuotedAndPaddedPath()
        {
            // Arrange
            FileInfo? font = TestEnvironment.ArialFont;
            Assert.NotNull(font);

            // Act & Assert
            Assert.Equal("Arial", FontUtilities.GetFontTitle($"  \"{font.FullName}\"  "));
        }

        /// <summary>
        /// Verifies that a font collection reports a title, which exercises the branch that walks each
        /// face in turn rather than stopping at the first.
        /// </summary>
        [Fact(Skip = "No font collection was found on this machine.", SkipUnless = nameof(TestEnvironment.HasFontCollection), SkipType = typeof(TestEnvironment))]
        public void GetFontTitle_ReadsTheTitleOfAFontCollection()
        {
            // Arrange
            FileInfo? collection = TestEnvironment.FontCollection;
            Assert.NotNull(collection);

            // Act
            string title = FontUtilities.GetFontTitle(collection.FullName);

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(title));
        }

        /// <summary>
        /// Verifies that a file that is not a font is reported as such rather than read as one.
        /// </summary>
        [Fact]
        public void GetFontTitle_RejectsAFileThatIsNotAFont()
        {
            // Arrange
            using TempDirectory temp = new();
            string path = temp.WriteFile("notafont.ttf", "this is not a font file");

            // Act & Assert
            _ = Assert.Throws<BadImageFormatException>(() => FontUtilities.GetFontTitle(path));
        }

        /// <summary>
        /// Verifies that a file that is not there is reported as missing.
        /// </summary>
        [Fact]
        public void GetFontTitle_ReportsAMissingFile()
        {
            // Arrange
            using TempDirectory temp = new();

            // Act & Assert
            _ = Assert.Throws<FileNotFoundException>(() => FontUtilities.GetFontTitle(temp.GetPath("absent.ttf")));
        }

        /// <summary>
        /// Verifies that a blank path is rejected as an absent argument.
        /// </summary>
        /// <param name="fontPath">The blank path to reject.</param>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void GetFontTitle_RejectsABlankPath(string fontPath)
        {
            _ = Assert.Throws<ArgumentException>(() => FontUtilities.GetFontTitle(fontPath));
        }

        /// <summary>
        /// Verifies that a null path is rejected as absent.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "This is deliberate as part of unit testing.")]
        [Fact]
        public void GetFontTitle_RejectsANullPath()
        {
            _ = Assert.Throws<ArgumentNullException>(static () => FontUtilities.GetFontTitle(null!));
        }

        /// <summary>
        /// Verifies that removing a font resource that was never registered is refused when given nothing
        /// to remove, which is the only part of the registration surface that can be exercised without
        /// changing the machine.
        /// </summary>
        /// <param name="fontFilePath">The blank path to reject.</param>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void RemoveFont_RejectsABlankPath(string fontFilePath)
        {
            _ = Assert.Throws<ArgumentException>(() => FontUtilities.RemoveFont(fontFilePath));
        }

        /// <summary>
        /// Verifies that the batch members reject an empty collection, since a caller passing one has
        /// almost certainly lost its contents somewhere earlier.
        /// </summary>
        [Fact]
        public void BatchMembers_RejectAnEmptyCollection()
        {
            _ = Assert.Throws<ArgumentOutOfRangeException>(static () => FontUtilities.AddFonts([]));
            _ = Assert.Throws<ArgumentOutOfRangeException>(static () => FontUtilities.RemoveFonts([]));
        }

        /// <summary>
        /// Verifies that the batch members reject a null collection.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "This is deliberate as part of unit testing.")]
        [Fact]
        public void BatchMembers_RejectANullCollection()
        {
            _ = Assert.Throws<ArgumentNullException>(static () => FontUtilities.AddFonts(null!));
            _ = Assert.Throws<ArgumentNullException>(static () => FontUtilities.RemoveFonts(null!));
        }
    }
}
