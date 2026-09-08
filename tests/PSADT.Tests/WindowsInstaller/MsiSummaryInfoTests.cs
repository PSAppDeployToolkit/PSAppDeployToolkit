using System;
using System.IO;
using PSADT.Tests.TestHelpers;
using PSADT.WindowsInstaller;
using Xunit;

namespace PSADT.Tests.WindowsInstaller
{
    /// <summary>
    /// Tests reading the summary information stream of an installer package.
    /// </summary>
    /// <remarks>
    /// The fixture is the installer committed for the tests, opened read-only. Its contents are known, which
    /// is what lets the field numbers be asserted as numbers rather than only as shapes: the stream is a
    /// numbered property set read out of one call, so a number off by one reads a neighbouring field and
    /// reports it under the wrong name - which no assertion about "a non-blank string" can catch.
    /// <para>
    /// Reading the stream needs the host to be able to produce code page 1252, which .NET does not carry.
    /// <c language="csharp">LegacyCodePages</c> registers a provider for it the way PowerShell does, so these
    /// run rather than skip.
    /// </para>
    /// </remarks>
    public sealed class MsiSummaryInfoTests
    {
        /// <summary>
        /// Verifies that the summary information of a real package is readable and carries the fields
        /// every package has.
        /// </summary>
        [Fact]
        public void MsiSummaryInfo_ReadsARealPackage()
        {
            // Arrange
            FileInfo package = TestEnvironment.TestMsiPackage;

            // Act
            MsiSummaryInfo summary = MsiSummaryInfo.Get(package.FullName);

            // Assert: the template and revision number are required of every installer package
            Assert.False(string.IsNullOrWhiteSpace(summary.Template));
            Assert.False(string.IsNullOrWhiteSpace(summary.RevisionNumber));
            _ = Assert.NotNull(summary.PageCount);
        }

        /// <summary>
        /// Verifies that the revision number is the package code, since that is the one summary field
        /// callers match packages on.
        /// </summary>
        [Fact]
        public void MsiSummaryInfo_ReportsThePackageCodeAsTheRevisionNumber()
        {
            // Arrange
            FileInfo package = TestEnvironment.TestMsiPackage;

            // Act
            MsiSummaryInfo summary = MsiSummaryInfo.Get(package.FullName);

            // Assert
            Assert.NotNull(summary.RevisionNumber);
            Assert.True(Guid.TryParse(summary.RevisionNumber, out _), $"Expected a package code, got '{summary.RevisionNumber}'.");
        }

        /// <summary>
        /// Verifies that every field of the summary stream is read under the name it belongs to.
        /// </summary>
        /// <remarks>
        /// Each field is asserted against the value the fixture actually carries, since a transposition is
        /// what this is looking for and an assertion about a field's type cannot see one - two neighbouring
        /// strings read under each other's names are both still strings. The three times are the exception
        /// and are asserted by shape: the value they should hold depends on the machine's time zone.
        /// </remarks>
        [Fact]
        public void MsiSummaryInfo_ReadsEveryFieldOfTheStream()
        {
            // Arrange
            FileInfo package = TestEnvironment.TestMsiPackage;

            // Act
            MsiSummaryInfo summary = MsiSummaryInfo.Get(package.FullName);

            // Assert: the fields the fixture carries, each against its own number
            Assert.Equal("Intel;0", summary.Template);
            Assert.Equal("{F509716E-DCB0-4037-A1CF-DC1300651714}", summary.RevisionNumber);
            Assert.Equal("tomsk", summary.LastSavedBy);
            Assert.Equal("Master Packager 26.3.9686", summary.CreatingApplication);
            Assert.Equal(500, summary.PageCount);
            Assert.Equal(2, summary.WordCount);

            // Assert: and the ones it leaves out come back absent rather than blank or zero
            Assert.Null(summary.Title);
            Assert.Null(summary.Subject);
            Assert.Null(summary.Author);
            Assert.Null(summary.Keywords);
            Assert.Null(summary.Comments);
            Assert.Null(summary.CharacterCount);
            Assert.Null(summary.Security);

            // Assert: the times are present, in the past, and in the order they have to be
            _ = Assert.NotNull(summary.CreateTimeDate);
            _ = Assert.NotNull(summary.LastSaveTimeDate);
            _ = Assert.NotNull(summary.LastPrinted);
            Assert.True(summary.LastSaveTimeDate >= summary.CreateTimeDate, "The package reports being saved before it was created.");
            Assert.True(summary.LastSaveTimeDate <= DateTime.Now, "The package reports being saved in the future.");
        }

        /// <summary>
        /// Verifies that the code page the strings were read with is reported, since it is what makes
        /// them readable at all.
        /// </summary>
        /// <remarks>
        /// Worth asserting separately because it is the field every other one in the stream depends on: an
        /// installer authored before Unicode records its strings in a legacy code page, and a string read
        /// with the wrong one is wrong rather than absent.
        /// </remarks>
        [Fact]
        public void MsiSummaryInfo_ReportsTheCodePageItsStringsWereReadWith()
        {
            // Arrange
            FileInfo package = TestEnvironment.TestMsiPackage;

            // Act
            MsiSummaryInfo summary = MsiSummaryInfo.Get(package.FullName);

            // Assert
            Assert.NotNull(summary.CodePage);
            Assert.True(summary.CodePage.CodePage > 0, "The package reports a code page of zero.");
        }

        /// <summary>
        /// Verifies that a file that is not a database is reported rather than read as one.
        /// </summary>
        [Fact]
        public void MsiSummaryInfo_RejectsAFileThatIsNotADatabase()
        {
            // Arrange
            using TempDirectory temp = new();
            string path = temp.WriteFile("notanmsi.msi", "this is not a database");

            // Act & Assert
            _ = Assert.ThrowsAny<Exception>(() => MsiSummaryInfo.Get(path));
        }

    }
}
