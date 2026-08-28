using System;
using System.IO;
using System.Linq;
using PSADT.Tests.TestHelpers;
using PSADT.WindowsInstaller;
using Xunit;

namespace PSADT.Tests.WindowsInstaller
{
    /// <summary>
    /// Tests reading the summary information stream of an installer package.
    /// </summary>
    /// <remarks>
    /// The fixture comes from the installer cache under the Windows directory, which holds a copy of every
    /// package installed through Windows Installer. Using one of those means the tests read a database a
    /// real product shipped rather than one authored here, at the cost of not knowing its contents in
    /// advance - so the assertions are about the fields every package must carry, never about a particular
    /// product's values. The package is opened read-only.
    /// <para>
    /// Reading the stream also needs the host to be able to produce code page 1252, which the .NET runtime
    /// does not register by default. PowerShell registers it, so the module is unaffected; a bare test host
    /// is not, and skips rather than fails.
    /// </para>
    /// </remarks>
    public sealed class MsiSummaryInfoTests
    {
        /// <summary>
        /// Verifies that the summary information of a real package is readable and carries the fields
        /// every package has.
        /// </summary>
        [Fact(Skip = "Needs a readable installer in the Windows Installer cache and a host that can produce code page 1252.", SkipUnless = nameof(TestEnvironment.CanReadMsiSummaryInfo), SkipType = typeof(TestEnvironment))]
        public void MsiSummaryInfo_ReadsARealPackage()
        {
            // Arrange
            FileInfo? package = TestEnvironment.CachedMsiPackage;
            Assert.NotNull(package);

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
        [Fact(Skip = "Needs a readable installer in the Windows Installer cache and a host that can produce code page 1252.", SkipUnless = nameof(TestEnvironment.CanReadMsiSummaryInfo), SkipType = typeof(TestEnvironment))]
        public void MsiSummaryInfo_ReportsThePackageCodeAsTheRevisionNumber()
        {
            // Arrange
            FileInfo? package = TestEnvironment.CachedMsiPackage;
            Assert.NotNull(package);

            // Act
            MsiSummaryInfo summary = MsiSummaryInfo.Get(package.FullName);

            // Assert
            Assert.NotNull(summary.RevisionNumber);
            Assert.True(Guid.TryParse(summary.RevisionNumber, out _), $"Expected a package code, got '{summary.RevisionNumber}'.");
        }

        /// <summary>
        /// Verifies that every field of the summary stream is read, and that each is either a real value
        /// or absent rather than a blank one.
        /// </summary>
        /// <remarks>
        /// The summary stream is a numbered property set, so every field here is read by its number out
        /// of one call - which means a number off by one reads a neighbouring field and reports it under
        /// the wrong name. Nothing can assert a particular product's values, but the types can be checked
        /// against the numbers they are meant to be: the three counts are numbers, the three times are
        /// times, and the strings are either something or nothing.
        /// </remarks>
        [Fact(Skip = "Needs a readable installer in the Windows Installer cache and a host that can produce code page 1252.", SkipUnless = nameof(TestEnvironment.CanReadMsiSummaryInfo), SkipType = typeof(TestEnvironment))]
        public void MsiSummaryInfo_ReadsEveryFieldOfTheStream()
        {
            // Arrange
            FileInfo? package = TestEnvironment.CachedMsiPackage;
            Assert.NotNull(package);

            // Act
            MsiSummaryInfo summary = MsiSummaryInfo.Get(package.FullName);

            // Assert: the strings are either a value or nothing, never blank
            Assert.All(
                [summary.Title, summary.Subject, summary.Author, summary.Keywords, summary.Comments, summary.LastSavedBy, summary.CreatingApplication],
                static value => Assert.True(value is null || !string.IsNullOrWhiteSpace(value), "A summary field came back blank rather than absent."));

            // Assert: the counts, where present, are counts
            Assert.All(
                new int?[] { summary.PageCount, summary.WordCount, summary.CharacterCount, summary.Security }.AsEnumerable(),
                static value => Assert.True(value is null or >= 0, "A summary count came back negative."));

            // Assert: the times, where present, are in the past - a package cannot have been authored ahead of now
            Assert.All(
                new DateTime?[] { summary.CreateTimeDate, summary.LastSaveTimeDate, summary.LastPrinted }.AsEnumerable(),
                static value => Assert.True(value is null || value <= DateTime.Now, "A summary time is in the future."));

            // Assert: a package that was saved was created first
            if (summary.CreateTimeDate is DateTime created && summary.LastSaveTimeDate is DateTime saved)
            {
                Assert.True(saved >= created, "The package reports being saved before it was created.");
            }

            // Assert: and the application that wrote it named itself, which every authoring tool does
            Assert.NotNull(summary.CreatingApplication);
        }

        /// <summary>
        /// Verifies that the code page the strings were read with is reported, since it is what makes
        /// them readable at all.
        /// </summary>
        /// <remarks>
        /// Worth asserting separately because it is the field this whole file is gated on. An installer
        /// authored before Unicode records its strings in a legacy code page, and the .NET runtime does
        /// not register those by default - PowerShell does, which is why the module is unaffected and a
        /// bare test host is not.
        /// </remarks>
        [Fact(Skip = "Needs a readable installer in the Windows Installer cache and a host that can produce code page 1252.", SkipUnless = nameof(TestEnvironment.CanReadMsiSummaryInfo), SkipType = typeof(TestEnvironment))]
        public void MsiSummaryInfo_ReportsTheCodePageItsStringsWereReadWith()
        {
            // Arrange
            FileInfo? package = TestEnvironment.CachedMsiPackage;
            Assert.NotNull(package);

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
