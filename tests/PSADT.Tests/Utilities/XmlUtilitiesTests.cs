using System;
using System.IO;
using System.Xml;
using PSADT.Utilities;
using Xunit;

namespace PSADT.Tests.Utilities
{
    /// <summary>
    /// Tests the hardened XML loading.
    /// </summary>
    /// <remarks>
    /// The class exists to load XML that came from somewhere untrusted - a patch's embedded metadata, for
    /// instance - without letting the document reach outside itself. Two settings do that work: document
    /// type definitions are prohibited outright, and the resolver is removed so nothing can be fetched by
    /// reference. Neither can be read back off the document, so both are demonstrated by handing the
    /// loader a document that would need them and requiring it to refuse.
    /// </remarks>
    public sealed class XmlUtilitiesTests
    {
        /// <summary>
        /// Verifies that ordinary XML loads and its content is readable.
        /// </summary>
        [Fact]
        public void SafeLoadFromText_LoadsOrdinaryXml()
        {
            // Act
            XmlDocument document = XmlUtilities.SafeLoadFromText("<root><child attribute=\"value\">text</child></root>");

            // Assert
            Assert.Equal("root", document.DocumentElement?.Name);
            Assert.Equal("text", document.DocumentElement?.FirstChild?.InnerText);
            Assert.Equal("value", document.DocumentElement?.FirstChild?.Attributes?["attribute"]?.Value);
        }

        /// <summary>
        /// Verifies that a document type definition is refused, which is what closes off entity expansion
        /// and the denial of service that comes with it.
        /// </summary>
        /// <param name="xml">The document carrying a definition.</param>
        [Theory]
        [InlineData("<!DOCTYPE root><root />")]
        [InlineData("<!DOCTYPE root [ <!ELEMENT root EMPTY> ]><root />")]
        [InlineData("<!DOCTYPE root [ <!ENTITY harmless \"text\"> ]><root>&harmless;</root>")]
        public void SafeLoadFromText_RefusesADocumentTypeDefinition(string xml)
        {
            _ = Assert.Throws<XmlException>(() => XmlUtilities.SafeLoadFromText(xml));
        }

        /// <summary>
        /// Verifies that an external entity is refused rather than fetched, which is the attack the
        /// hardening is named for.
        /// </summary>
        /// <remarks>
        /// The entity below names a file that exists on every Windows installation. A loader that resolved
        /// it would succeed and place the file's content in the document, so this test failing to throw
        /// would mean an untrusted document could read the file system.
        /// </remarks>
        [Fact]
        public void SafeLoadFromText_RefusesAnExternalEntity()
        {
            // Arrange
            const string xml = "<!DOCTYPE root [ <!ENTITY leak SYSTEM \"file:///C:/Windows/win.ini\"> ]><root>&leak;</root>";

            // Act & Assert
            _ = Assert.Throws<XmlException>(static () => XmlUtilities.SafeLoadFromText(xml));
        }

        /// <summary>
        /// Verifies that malformed XML is reported as such rather than loading partially.
        /// </summary>
        /// <param name="xml">The malformed document.</param>
        [Theory]
        [InlineData("<root>")]
        [InlineData("<root></wrong>")]
        [InlineData("not xml at all")]
        [InlineData("<root attribute=unquoted />")]
        public void SafeLoadFromText_ReportsMalformedXml(string xml)
        {
            _ = Assert.Throws<XmlException>(() => XmlUtilities.SafeLoadFromText(xml));
        }

        /// <summary>
        /// Verifies that blank input is rejected as an absent argument rather than as malformed XML, so a
        /// caller can tell a programming error from bad data.
        /// </summary>
        /// <param name="input">The blank input to reject.</param>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\r\n")]
        public void SafeLoadFromText_RejectsBlankInput(string input)
        {
            _ = Assert.Throws<ArgumentException>(() => XmlUtilities.SafeLoadFromText(input));
        }

        /// <summary>
        /// Verifies that null input is rejected as absent.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "This is deliberate as part of unit testing.")]
        [Fact]
        public void SafeLoadFromText_RejectsNull()
        {
            _ = Assert.Throws<ArgumentNullException>(static () => XmlUtilities.SafeLoadFromText(null!));
        }

        /// <summary>
        /// Verifies that a document is loaded from a file with the same hardening as from text, since the
        /// two overloads share one reader configuration and a divergence would be silent.
        /// </summary>
        [Fact]
        public void SafeLoadFromPath_LoadsAndHardensTheSameWay()
        {
            // Arrange
            using TestHelpers.TempDirectory temp = new();
            string wellFormed = temp.WriteFile("good.xml", "<root><child>text</child></root>");
            string withDefinition = temp.WriteFile("dtd.xml", "<!DOCTYPE root><root />");

            // Act
            XmlDocument document = XmlUtilities.SafeLoadFromPath(wellFormed);

            // Assert
            Assert.Equal("root", document.DocumentElement?.Name);
            Assert.Equal("text", document.DocumentElement?.FirstChild?.InnerText);
            _ = Assert.Throws<XmlException>(() => XmlUtilities.SafeLoadFromPath(withDefinition));
        }

        /// <summary>
        /// Verifies that a missing file is reported as missing rather than surfacing as a parse failure.
        /// </summary>
        [Fact]
        public void SafeLoadFromPath_ReportsAMissingFile()
        {
            // Arrange
            using TestHelpers.TempDirectory temp = new();

            // Act & Assert
            _ = Assert.Throws<FileNotFoundException>(() => XmlUtilities.SafeLoadFromPath(temp.GetPath("absent.xml")));
        }

        /// <summary>
        /// Verifies that a blank path is reported as a missing file.
        /// </summary>
        /// <remarks>
        /// Not as a bad argument, which is the asymmetry worth writing down: the text overload validates
        /// its input and reports an absent one as an argument error, while this overload passes the path
        /// to the shared existence check and a blank path simply does not exist. Harmless, but a caller
        /// distinguishing the two cases by exception type will not get what it expects.
        /// </remarks>
        /// <param name="path">The blank path to reject.</param>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void SafeLoadFromPath_ReportsABlankPathAsAMissingFile(string path)
        {
            _ = Assert.Throws<FileNotFoundException>(() => XmlUtilities.SafeLoadFromPath(path));
        }

        /// <summary>
        /// Verifies that a reader is loaded through the same configuration, since that overload is what
        /// the other two are built on and is reachable on its own.
        /// </summary>
        [Fact]
        public void SafeLoadCommon_AppliesTheSameHardening()
        {
            // Arrange
            using StringReader wellFormed = new("<root />");
            using StringReader withDefinition = new("<!DOCTYPE root><root />");

            // Act
            XmlDocument document = XmlUtilities.SafeLoadCommon(wellFormed);

            // Assert
            Assert.Equal("root", document.DocumentElement?.Name);
            _ = Assert.Throws<XmlException>(() => XmlUtilities.SafeLoadCommon(withDefinition));
        }
    }
}
