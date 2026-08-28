using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using PSADT.Tests.TestHelpers;
using PSADT.Utilities;
using Xunit;

namespace PSADT.Tests.Utilities
{
    /// <summary>
    /// Tests reading and writing INI files.
    /// </summary>
    /// <remarks>
    /// These wrap the profile string APIs, which are old and have several behaviours worth pinning: paths
    /// are resolved against the Windows directory unless fully qualified, absent values come back as empty
    /// rather than as an error, and a section is stored as one buffer of null-separated entries. Every file
    /// written here goes into a temporary directory that is removed with the test.
    /// </remarks>
    public sealed class IniUtilitiesTests
    {
        /// <summary>
        /// Verifies that a written value is read back unchanged.
        /// </summary>
        /// <param name="value">The value to write and read back.</param>
        [Theory]
        [InlineData("plain")]
        [InlineData("with spaces")]
        [InlineData("with=equals")]
        [InlineData(@"C:\Program Files\App")]
        [InlineData("123")]
        [InlineData("with;semicolon")]
        public void WriteSectionKeyValue_RoundTripsAValue(string value)
        {
            // Arrange
            using TempDirectory temp = new();
            string path = temp.GetPath("round.ini");

            // Act
            IniUtilities.WriteSectionKeyValue(path, "Section", "Key", value);

            // Assert
            Assert.Equal(value, IniUtilities.GetSectionKeyValue(path, "Section", "Key"));
        }

        /// <summary>
        /// Verifies that surrounding whitespace is stripped on the way back out, which is what the trim in
        /// the reader is for and what makes a hand-edited file usable.
        /// </summary>
        [Fact]
        public void GetSectionKeyValue_TrimsSurroundingWhitespace()
        {
            // Arrange
            using TempDirectory temp = new();
            string path = temp.WriteFile("spaced.ini", "[Section]\r\nKey =   spaced value   \r\n");

            // Act & Assert
            Assert.Equal("spaced value", IniUtilities.GetSectionKeyValue(path, "Section", "Key"));
        }

        /// <summary>
        /// Verifies that section and key names are matched without regard to case, as the profile APIs do.
        /// </summary>
        /// <param name="section">The section name to look up.</param>
        /// <param name="key">The key name to look up.</param>
        [Theory]
        [InlineData("Section", "Key")]
        [InlineData("SECTION", "KEY")]
        [InlineData("section", "key")]
        [InlineData("SeCtIoN", "kEy")]
        public void GetSectionKeyValue_MatchesNamesWithoutRegardToCase(string section, string key)
        {
            // Arrange
            using TempDirectory temp = new();
            string path = temp.WriteFile("case.ini", "[Section]\r\nKey=value\r\n");

            // Act & Assert
            Assert.Equal("value", IniUtilities.GetSectionKeyValue(path, section, key));
        }

        /// <summary>
        /// Verifies that anything the file does not hold reads back as absent rather than as empty, so a
        /// caller can tell "not set" from "set to nothing".
        /// </summary>
        /// <remarks>
        /// The profile API reports a missing section, key or file by setting the last error to
        /// ERROR_FILE_NOT_FOUND while still returning successfully, and the wrapper turns that into a
        /// null. Worth pinning: it is a better contract than the empty string the raw API hands back, and
        /// one that would be lost by a rewrite that read the returned length instead.
        /// </remarks>
        [Fact]
        public void GetSectionKeyValue_ReturnsNullForAnythingTheFileDoesNotHold()
        {
            // Arrange
            using TempDirectory temp = new();
            string path = temp.WriteFile("missing.ini", "[Section]\r\nKey=value\r\n");

            // Assert: a key that is not in a section that is
            Assert.Null(IniUtilities.GetSectionKeyValue(path, "Section", "Absent"));

            // Assert: a section that is not in the file
            Assert.Null(IniUtilities.GetSectionKeyValue(path, "Absent", "Key"));

            // Assert: a file that is not there at all
            Assert.Null(IniUtilities.GetSectionKeyValue(temp.GetPath("absent.ini"), "Section", "Key"));
        }

        /// <summary>
        /// Verifies that a value can be removed by writing nothing in its place.
        /// </summary>
        [Fact]
        public void WriteSectionKeyValue_RemovesAKeyWhenGivenNoValue()
        {
            // Arrange
            using TempDirectory temp = new();
            string path = temp.GetPath("remove.ini");
            IniUtilities.WriteSectionKeyValue(path, "Section", "Key", "value");
            IniUtilities.WriteSectionKeyValue(path, "Section", "Other", "kept");

            // Act
            IniUtilities.WriteSectionKeyValue(path, "Section", "Key", value: null);

            // Assert
            Assert.Null(IniUtilities.GetSectionKeyValue(path, "Section", "Key"));
            Assert.Equal("kept", IniUtilities.GetSectionKeyValue(path, "Section", "Other"));
        }

        /// <summary>
        /// Verifies that a whole section can be removed by writing no key at all.
        /// </summary>
        [Fact]
        public void WriteSectionKeyValue_RemovesASectionWhenGivenNoKey()
        {
            // Arrange
            using TempDirectory temp = new();
            string path = temp.GetPath("removesection.ini");
            IniUtilities.WriteSectionKeyValue(path, "Doomed", "Key", "value");
            IniUtilities.WriteSectionKeyValue(path, "Kept", "Key", "value");

            // Act
            IniUtilities.WriteSectionKeyValue(path, "Doomed", key: null, value: null);

            // Assert
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => IniUtilities.GetSection(path, "Doomed"));
            Assert.NotNull(IniUtilities.GetSection(path, "Kept"));
        }

        /// <summary>
        /// Verifies that a whole section is read back with its entries in the order the file holds them,
        /// since the section is returned as an ordered dictionary and callers rely on that order.
        /// </summary>
        [Fact]
        public void GetSection_PreservesTheOrderOfTheEntries()
        {
            // Arrange
            using TempDirectory temp = new();
            string path = temp.WriteFile("ordered.ini", "[Section]\r\nThird=3\r\nFirst=1\r\nSecond=2\r\n");

            // Act
            OrderedDictionary? section = IniUtilities.GetSection(path, "Section");

            // Assert
            Assert.NotNull(section);
            Assert.Equal(["Third", "First", "Second"], section.Keys.Cast<string>(), StringComparer.Ordinal);
            Assert.Equal(["3", "1", "2"], section.Values.Cast<string>(), StringComparer.Ordinal);
        }

        /// <summary>
        /// Verifies that a key appearing twice in a section keeps its first position but takes its last
        /// value, which is how the reader resolves the duplicate.
        /// </summary>
        [Fact]
        public void GetSection_TakesTheLastValueOfADuplicatedKey()
        {
            // Arrange
            using TempDirectory temp = new();
            string path = temp.WriteFile("duplicate.ini", "[Section]\r\nKey=first\r\nOther=x\r\nKey=second\r\n");

            // Act
            OrderedDictionary? section = IniUtilities.GetSection(path, "Section");

            // Assert
            Assert.NotNull(section);
            Assert.Equal("second", section["Key"]);
            Assert.Equal(["Key", "Other"], section.Keys.Cast<string>(), StringComparer.Ordinal);
        }

        /// <summary>
        /// Verifies that an entry with no equals sign is skipped rather than becoming a key with no value.
        /// </summary>
        [Fact]
        public void GetSection_SkipsAnEntryWithNoSeparator()
        {
            // Arrange
            using TempDirectory temp = new();
            string path = temp.WriteFile("malformed.ini", "[Section]\r\nKey=value\r\nJustAName\r\nOther=second\r\n");

            // Act
            OrderedDictionary? section = IniUtilities.GetSection(path, "Section");

            // Assert
            Assert.NotNull(section);
            Assert.Equal(["Key", "Other"], section.Keys.Cast<string>(), StringComparer.Ordinal);
        }

        /// <summary>
        /// Verifies that asking for a section that is not in the file names the sections that are, since
        /// the caller cannot otherwise tell a typo from an empty file.
        /// </summary>
        [Fact]
        public void GetSection_NamesTheSectionsItDidFind()
        {
            // Arrange
            using TempDirectory temp = new();
            string path = temp.WriteFile("sections.ini", "[Alpha]\r\nKey=1\r\n[Beta]\r\nKey=2\r\n");

            // Act
            ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => IniUtilities.GetSection(path, "Gamma"));

            // Assert
            Assert.Contains("Alpha", exception.Message, StringComparison.Ordinal);
            Assert.Contains("Beta", exception.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that a section is matched without regard to case, so a caller need not know how the
        /// file spells it.
        /// </summary>
        [Fact]
        public void GetSection_MatchesTheSectionWithoutRegardToCase()
        {
            // Arrange
            using TempDirectory temp = new();
            string path = temp.WriteFile("sectioncase.ini", "[Section]\r\nKey=value\r\n");

            // Act & Assert
            Assert.NotNull(IniUtilities.GetSection(path, "SECTION"));
            Assert.NotNull(IniUtilities.GetSection(path, "section"));
        }

        /// <summary>
        /// Verifies that a blank section name is rejected as a bad argument.
        /// </summary>
        /// <param name="section">The blank section name to reject.</param>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void GetSection_RejectsABlankSectionName(string section)
        {
            // Arrange
            using TempDirectory temp = new();
            string path = temp.WriteFile("blank.ini", "[Section]\r\nKey=value\r\n");

            // Act & Assert
            _ = Assert.Throws<ArgumentException>(() => IniUtilities.GetSection(path, section));
        }

        /// <summary>
        /// Verifies that a file with no sections at all is reported as bad data rather than as an empty
        /// result, since the reader has nothing to search.
        /// </summary>
        [Fact]
        public void GetSection_ReportsAFileWithNoSections()
        {
            // Arrange
            using TempDirectory temp = new();
            string path = temp.WriteFile("nosections.ini", "not an ini file\r\n");

            // Act & Assert
            _ = Assert.Throws<InvalidDataException>(() => IniUtilities.GetSection(path, "Section"));
        }

        /// <summary>
        /// Verifies that a whole section is written and read back with its entries intact.
        /// </summary>
        [Fact]
        public void WriteSection_RoundTripsAWholeSection()
        {
            // Arrange
            using TempDirectory temp = new();
            string path = temp.GetPath("wholesection.ini");
            OrderedDictionary content = new() { { "First", "1" }, { "Second", "two" }, { "Third", @"C:\path with spaces" } };

            // Act
            IniUtilities.WriteSection(path, "Section", content);

            // Assert
            OrderedDictionary? section = IniUtilities.GetSection(path, "Section");
            Assert.NotNull(section);
            Assert.Equal(["First", "Second", "Third"], section.Keys.Cast<string>(), StringComparer.Ordinal);
            Assert.Equal(["1", "two", @"C:\path with spaces"], section.Values.Cast<string>(), StringComparer.Ordinal);
        }

        /// <summary>
        /// Verifies that numeric and boolean values are accepted and written as their text, since a
        /// caller supplying a hashtable from PowerShell will not have converted them.
        /// </summary>
        [Fact]
        public void WriteSection_AcceptsValueTypesAndWritesThemAsText()
        {
            // Arrange
            using TempDirectory temp = new();
            string path = temp.GetPath("valuetypes.ini");
            OrderedDictionary content = new() { { "Number", 42 }, { "Flag", true }, { "Decimal", 1.5 } };

            // Act
            IniUtilities.WriteSection(path, "Section", content);

            // Assert
            OrderedDictionary? section = IniUtilities.GetSection(path, "Section");
            Assert.NotNull(section);
            Assert.Equal("42", section["Number"]);
            Assert.Equal("True", section["Flag"]);
        }

        /// <summary>
        /// Verifies that writing no content removes the section outright.
        /// </summary>
        [Fact]
        public void WriteSection_RemovesTheSectionWhenGivenNoContent()
        {
            // Arrange
            using TempDirectory temp = new();
            string path = temp.GetPath("delete.ini");
            IniUtilities.WriteSection(path, "Doomed", new OrderedDictionary { { "Key", "value" } });
            IniUtilities.WriteSection(path, "Kept", new OrderedDictionary { { "Key", "value" } });

            // Act
            IniUtilities.WriteSection(path, "Doomed", content: null);

            // Assert
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => IniUtilities.GetSection(path, "Doomed"));
            Assert.NotNull(IniUtilities.GetSection(path, "Kept"));
        }

        /// <summary>
        /// Verifies that a key of an unusable type is rejected, naming the type so the caller can see what
        /// it passed.
        /// </summary>
        [Fact]
        public void WriteSection_RejectsAKeyThatIsNotAStringOrValueType()
        {
            // Arrange
            using TempDirectory temp = new();
            string path = temp.GetPath("badkey.ini");
            OrderedDictionary content = new() { { new object(), "value" } };

            // Act & Assert
            _ = Assert.Throws<ArgumentException>(() => IniUtilities.WriteSection(path, "Section", content));
        }

        /// <summary>
        /// Verifies that a value of an unusable type is rejected rather than written as whatever its type
        /// name happens to be.
        /// </summary>
        [Fact]
        public void WriteSection_RejectsAValueThatIsNotAStringOrValueType()
        {
            // Arrange
            using TempDirectory temp = new();
            string path = temp.GetPath("badvalue.ini");
            OrderedDictionary content = new() { { "Key", new object() } };

            // Act & Assert
            _ = Assert.Throws<ArgumentException>(() => IniUtilities.WriteSection(path, "Section", content));
        }

        /// <summary>
        /// Verifies that a key with no content is rejected, since an entry with no name cannot be read
        /// back.
        /// </summary>
        /// <param name="key">The unusable key.</param>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void WriteSection_RejectsABlankKey(string key)
        {
            // Arrange
            using TempDirectory temp = new();
            string path = temp.GetPath("blankkey.ini");
            OrderedDictionary content = new() { { key, "value" } };

            // Act & Assert
            _ = Assert.Throws<ArgumentException>(() => IniUtilities.WriteSection(path, "Section", content));
        }

        /// <summary>
        /// Verifies that the verified write reports a directory that does not exist, rather than silently
        /// writing nothing as the profile API would.
        /// </summary>
        [Fact]
        public void WriteSectionKeyUnverifiedValue_ReportsAMissingDirectory()
        {
            // Arrange
            using TempDirectory temp = new();
            string path = Path.Join(temp.FullName, "absent", "file.ini");

            // Act & Assert
            _ = Assert.Throws<DirectoryNotFoundException>(() => IniUtilities.WriteSectionKeyUnverifiedValue(path, "Section", "Key", "value"));
        }

        /// <summary>
        /// Verifies that the verified write rejects a blank section name.
        /// </summary>
        /// <param name="section">The blank section name to reject.</param>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void WriteSectionKeyUnverifiedValue_RejectsABlankSectionName(string section)
        {
            // Arrange
            using TempDirectory temp = new();
            string path = temp.GetPath("unverified.ini");

            // Act & Assert
            _ = Assert.Throws<ArgumentException>(() => IniUtilities.WriteSectionKeyUnverifiedValue(path, section, "Key", "value"));
        }

        /// <summary>
        /// Verifies that the verified write puts a value where the ordinary reader can find it, so the two
        /// halves of the class agree.
        /// </summary>
        [Fact]
        public void WriteSectionKeyUnverifiedValue_WritesAValueTheReaderCanFind()
        {
            // Arrange
            using TempDirectory temp = new();
            string path = temp.WriteFile("unverified.ini", string.Empty);

            // Act
            IniUtilities.WriteSectionKeyUnverifiedValue(path, "Section", "Key", "value");

            // Assert
            Assert.Equal("value", IniUtilities.GetSectionKeyValue(path, "Section", "Key"));
        }
    }
}
