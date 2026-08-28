using System;
using PSADT.AppManagement;
using PSADT.Tests.TestHelpers;
using Xunit;

namespace PSADT.Tests.AppManagement
{
    /// <summary>
    /// Tests reading the identity out of a packaged application.
    /// </summary>
    /// <remarks>
    /// Only the refusals are covered. Reading a real package needs one to read, and there is no package
    /// this can rely on finding: the installed ones live under a directory an ordinary account cannot
    /// enumerate, and a machine may have no loose package file at all. Rather than gate a test on a
    /// fixture that will almost never be present, the parts that hold on any machine are covered here -
    /// that nothing is accepted in place of a package, and that a file which is not one is reported
    /// rather than read as though it were.
    /// </remarks>
    public sealed class AppxManifestInfoTests
    {
        /// <summary>
        /// Verifies that nothing at all is refused.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "This is deliberate as part of unit testing.")]
        [Fact]
        public void Get_RefusesANullPackage()
        {
            _ = Assert.Throws<ArgumentNullException>(static () => AppxManifestInfo.Get(null!));
        }

        /// <summary>
        /// Verifies that a package that is not there is reported rather than read as an empty one.
        /// </summary>
        [Fact]
        public void Get_ReportsAPackageThatIsNotThere()
        {
            // Arrange
            using TempDirectory temp = new();

            // Act & Assert
            Assert.NotNull(Record.Exception(() => AppxManifestInfo.Get(new Uri(temp.GetPath("absent.msix")))));
        }

        /// <summary>
        /// Verifies that a file that is not a package is reported rather than read as one, since a
        /// deployment handed the wrong file needs to be told so rather than shown an empty identity.
        /// </summary>
        [Fact]
        public void Get_ReportsAFileThatIsNotAPackage()
        {
            // Arrange
            using TempDirectory temp = new();
            string path = temp.WriteFile("notapackage.msix", "this is not a package");

            // Act & Assert
            Assert.NotNull(Record.Exception(() => AppxManifestInfo.Get(new Uri(path))));
        }
    }
}
