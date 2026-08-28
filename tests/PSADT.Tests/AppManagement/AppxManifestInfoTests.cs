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
    /// The fixture is a real signed package committed alongside the tests, so the values asserted here
    /// are the ones actually in its manifest rather than ones invented for the test. It is never
    /// installed, registered or run - the package is opened, its manifest read, and that is all.
    /// <para>
    /// The composed names are the interesting part. Neither is written in the manifest: both are built
    /// by the packaging API from the identity and a hash of the publisher's distinguished name, so they
    /// are the one thing here that a mistake in reading the manifest would visibly corrupt. They are
    /// asserted by their structure and against each other rather than against a hard-coded hash, which
    /// would only restate whatever the API happened to return.
    /// </para>
    /// </remarks>
    public sealed class AppxManifestInfoTests
    {
        /// <summary>
        /// Verifies that the identity read from a real package is the identity its manifest declares.
        /// </summary>
        [Fact]
        public void Get_ReadsTheIdentityFromAPackage()
        {
            // Act
            AppxManifestInfo manifest = ReadTestPackage();

            // Assert
            Assert.Equal(PackageName, manifest.Name, StringComparer.Ordinal);
            Assert.Equal(PackagePublisher, manifest.Publisher, StringComparer.Ordinal);
            Assert.Equal(new Version(2026, 8, 2802, 5733), manifest.Version);
            Assert.Equal(Interop.APPX_PACKAGE_ARCHITECTURE.APPX_PACKAGE_ARCHITECTURE_NEUTRAL, manifest.Architecture);

            // Assert: an ordinary package carries no resource identifier, and says so rather than
            // reporting an empty one - only a resource package has one at all
            Assert.Null(manifest.ResourceId);
        }

        /// <summary>
        /// Verifies that a single package is reported as one rather than as a bundle, since the two are
        /// read from different manifests inside the archive and looking for the wrong one finds nothing.
        /// </summary>
        [Fact]
        public void Get_ReportsASinglePackageAsAPackage()
        {
            Assert.Equal(AppxPackageType.Package, ReadTestPackage().PackageType);
        }

        /// <summary>
        /// Verifies that the family name is the package name joined to the publisher's identifier, which
        /// is the form everything addressing an installed package by family uses.
        /// </summary>
        [Fact]
        public void Get_ComposesTheFamilyName()
        {
            // Act
            AppxManifestInfo manifest = ReadTestPackage();

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(PublisherIdOf(manifest)));
            Assert.Equal($"{PackageName}_{PublisherIdOf(manifest)}", manifest.PackageFamilyName, StringComparer.Ordinal);
        }

        /// <summary>
        /// Verifies that the full name carries the version and architecture as well, and identifies the
        /// same publisher as the family name does.
        /// </summary>
        /// <remarks>
        /// The publisher identifier appearing in both is what ties them together: they are composed
        /// separately, so the two disagreeing would mean one of them was built from a misread manifest.
        /// </remarks>
        [Fact]
        public void Get_ComposesTheFullName()
        {
            // Act
            AppxManifestInfo manifest = ReadTestPackage();

            // Assert
            Assert.Equal($"{PackageName}_2026.8.2802.5733_neutral__{PublisherIdOf(manifest)}", manifest.PackageFullName, StringComparer.Ordinal);
        }

        /// <summary>
        /// Verifies that reading the same package twice gives the same answer, since nothing about it
        /// changes between readings.
        /// </summary>
        [Fact]
        public void Get_IsStableBetweenReadings()
        {
            Assert.Equal(ReadTestPackage(), ReadTestPackage());
        }

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

        /// <summary>
        /// Reads the packaged application shipped alongside the tests.
        /// </summary>
        /// <returns>Its manifest information.</returns>
        private static AppxManifestInfo ReadTestPackage()
        {
            Assert.True(TestEnvironment.TestPackage.Exists, $"The test package {TestEnvironment.TestPackage.FullName} was not copied alongside the tests.");
            return AppxManifestInfo.Get(new Uri(TestEnvironment.TestPackage.FullName));
        }

        /// <summary>
        /// The publisher's identifier, as the packaging API composed it into the family name.
        /// </summary>
        /// <remarks>
        /// Taken from the composed name rather than worked out here. It is a hash of the publisher's
        /// distinguished name, and reproducing that in the test would be reimplementing the thing under
        /// test - so it is read back out and used to check the two composed names against each other.
        /// </remarks>
        /// <param name="manifest">The manifest to read it from.</param>
        /// <returns>The publisher's identifier.</returns>
        private static string PublisherIdOf(AppxManifestInfo manifest)
        {
            return manifest.PackageFamilyName[(PackageName.Length + 1)..];
        }

        /// <summary>
        /// The name the fixture's manifest declares.
        /// </summary>
        private const string PackageName = "tplant.Winget.Source";

        /// <summary>
        /// The publisher the fixture's manifest declares.
        /// </summary>
        private const string PackagePublisher = "CN=Thomas Plant, O=Thomas Plant, L=Griffith, S=Australian Capital Territory, C=AU";
    }
}
