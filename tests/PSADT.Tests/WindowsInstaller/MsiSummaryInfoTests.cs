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
