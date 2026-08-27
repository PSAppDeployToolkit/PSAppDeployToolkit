using System;
using System.IO;
using Xunit;

namespace PSADT.Interop.Tests.Extensions
{
    /// <summary>
    /// Tests the path validation guards. Each returns the path it was given so it can be used inline in
    /// an expression, which is the property that makes them worth having over a bare if statement.
    /// </summary>
    /// <remarks>
    /// These query the filesystem but never modify it. The paths used are the test assembly's own file
    /// and directory, so nothing has to be created or cleaned up.
    /// </remarks>
    public sealed class StringExtensionsTests
    {
        /// <summary>
        /// The test assembly's own file, which is guaranteed to exist while the tests are running.
        /// </summary>
        private static readonly string ExistingFile = typeof(StringExtensionsTests).Assembly.Location;

        /// <summary>
        /// The directory holding the test assembly, which is guaranteed to exist.
        /// </summary>
        private static readonly string ExistingDirectory = AppContext.BaseDirectory;

        /// <summary>
        /// A directory that does not exist, formed by appending a name no real directory would carry.
        /// </summary>
        private static readonly string MissingDirectory = Path.Join(ExistingDirectory, "a-directory-that-does-not-exist");

        /// <summary>
        /// Verifies that an existing directory is accepted and handed straight back, so the guard can sit
        /// inline in an assignment.
        /// </summary>
        [Fact]
        public void ThrowIfDirectoryDoesNotExist_ReturnsTheDirectoryItWasGiven()
        {
            // Act
            string result = ExistingDirectory.ThrowIfDirectoryDoesNotExist();

            // Assert
            Assert.Same(ExistingDirectory, result);
        }

        /// <summary>
        /// Verifies that a missing directory is reported as such rather than as a generic failure.
        /// </summary>
        [Fact]
        public void ThrowIfDirectoryDoesNotExist_ThrowsForAMissingDirectory()
        {
            // Act & Assert
            DirectoryNotFoundException exception = Assert.Throws<DirectoryNotFoundException>(static () => MissingDirectory.ThrowIfDirectoryDoesNotExist());
            Assert.Contains(MissingDirectory, exception.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that an existing file is accepted and handed straight back.
        /// </summary>
        [Fact]
        public void ThrowIfFileDoesNotExist_ReturnsTheFileItWasGiven()
        {
            // Act
            string result = ExistingFile.ThrowIfFileDoesNotExist();

            // Assert
            Assert.Same(ExistingFile, result);
        }

        /// <summary>
        /// Verifies that a missing file is reported as a file problem and names the file, which is what a
        /// caller needs to act on it.
        /// </summary>
        [Fact]
        public void ThrowIfFileDoesNotExist_ThrowsForAMissingFileAndNamesIt()
        {
            // Arrange
            string missingFile = Path.Join(ExistingDirectory, "a-file-that-does-not-exist.txt");

            // Act & Assert
            FileNotFoundException exception = Assert.Throws<FileNotFoundException>(missingFile.ThrowIfFileDoesNotExist);
            Assert.Equal(missingFile, exception.FileName);
        }

        /// <summary>
        /// Verifies that the directory guard accepts a path whose file does not exist yet, as long as the
        /// directory does. That is the case a caller about to create a file needs.
        /// </summary>
        [Fact]
        public void ThrowIfFileDirectoryDoesNotExist_AcceptsAPathWhoseDirectoryExists()
        {
            // Arrange
            string plannedFile = Path.Join(ExistingDirectory, "a-file-not-yet-written.txt");

            // Act & Assert
            Assert.Same(plannedFile, plannedFile.ThrowIfFileDirectoryDoesNotExist());
            Assert.Same(ExistingFile, ExistingFile.ThrowIfFileDirectoryDoesNotExist());
        }

        /// <summary>
        /// Verifies that a path whose directory is missing is rejected, since writing to it could not
        /// succeed.
        /// </summary>
        [Fact]
        public void ThrowIfFileDirectoryDoesNotExist_ThrowsWhenTheDirectoryIsMissing()
        {
            // Arrange
            string unreachableFile = Path.Join(MissingDirectory, "file.txt");

            // Act & Assert
            _ = Assert.Throws<DirectoryNotFoundException>(unreachableFile.ThrowIfFileDirectoryDoesNotExist);
        }

        /// <summary>
        /// Verifies that a fully qualified path is accepted and handed back.
        /// </summary>
        /// <param name="path">The path expected to be accepted.</param>
        [Theory]
        [InlineData(@"C:\dir\file.txt")]
        [InlineData(@"\\server\share\file.txt")]
        [InlineData(@"\\?\C:\dir")]
        public void ThrowIfPathIsNotFullyQualified_ReturnsAQualifiedPath(string path)
        {
            // Act & Assert
            Assert.Same(path, path.ThrowIfPathIsNotFullyQualified());
        }

        /// <summary>
        /// Verifies that a relative or drive-relative path is rejected as a drive problem, which is the
        /// exception type the guard chose to signal "this path has no root I can resolve".
        /// </summary>
        /// <param name="path">The path expected to be rejected.</param>
        [Theory]
        [InlineData("file.txt")]
        [InlineData(@"dir\file.txt")]
        [InlineData(@"\dir\file.txt")]
        [InlineData("C:dir")]
        [InlineData(".")]
        public void ThrowIfPathIsNotFullyQualified_ThrowsForAnUnqualifiedPath(string path)
        {
            // Act & Assert
            DriveNotFoundException exception = Assert.Throws<DriveNotFoundException>(path.ThrowIfPathIsNotFullyQualified);
            Assert.Contains(path, exception.Message, StringComparison.Ordinal);
        }
    }
}
