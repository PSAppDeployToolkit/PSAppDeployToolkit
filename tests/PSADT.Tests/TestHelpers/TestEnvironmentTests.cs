using System;
using System.Collections.Generic;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using PSADT.FileSystem;
using Xunit;

namespace PSADT.Tests.TestHelpers
{
    /// <summary>
    /// Tests the best-effort search behind the cached patch fixture.
    /// </summary>
    public sealed class TestEnvironmentTests
    {
        /// <summary>
        /// Verifies that files the caller cannot open, whether missing or denied to it, are passed over for
        /// the first one it can.
        /// </summary>
        [Fact]
        public void FindFirstReadableFile_SkipsFilesItCannotOpen()
        {
            // Arrange
            using TempDirectory temp = new();
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();
            Assert.NotNull(identity.User);
            FileInfo missing = new(temp.GetPath("missing.msp"));
            FileInfo denied = new(temp.WriteFile("denied.msp", "denied"));
            FileInfo first = new(temp.WriteFile("first.msp", "first"));
            FileInfo second = new(temp.WriteFile("second.msp", "second"));
            FileSecurity security = FileSystemUtilities.GetAccessControl(denied, AccessControlSections.Access);
            security.AddAccessRule(new FileSystemAccessRule(identity.User, FileSystemRights.ReadData, AccessControlType.Deny));
            FileSystemUtilities.SetAccessControl(denied, security);

            // Act
            FileInfo? found = TestEnvironment.FindFirstReadableFile(() => [missing, denied, first, second]);

            // Assert
            Assert.Same(first, found);
        }

        /// <summary>
        /// Verifies that an enumeration holding nothing the caller can open produces no fixture.
        /// </summary>
        [Fact]
        public void FindFirstReadableFile_ReturnsNullWhenNoFileCanBeOpened()
        {
            // Arrange
            using TempDirectory temp = new();
            FileInfo missing = new(temp.GetPath("missing.msp"));

            // Act & Assert
            Assert.Null(TestEnvironment.FindFirstReadableFile(() => [missing]));
        }

        /// <summary>
        /// Verifies that an enumeration refused as it starts produces no fixture.
        /// </summary>
        [Fact]
        public void FindFirstReadableFile_ReturnsNullWhenEnumerationIsRefused()
        {
            Assert.Null(TestEnvironment.FindFirstReadableFile(static () => throw new UnauthorizedAccessException()));
        }

        /// <summary>
        /// Verifies that enumerating a directory that is not there produces no fixture.
        /// </summary>
        [Fact]
        public void FindFirstReadableFile_ReturnsNullForAMissingDirectory()
        {
            // Arrange
            using TempDirectory temp = new();
            DirectoryInfo missing = new(temp.GetPath("missing"));

            // Act & Assert
            Assert.Null(TestEnvironment.FindFirstReadableFile(() => missing.EnumerateFiles("*.msp", SearchOption.TopDirectoryOnly)));
        }

        /// <summary>
        /// Verifies that an enumeration failing part-way through produces no fixture rather than an exception.
        /// </summary>
        [Fact]
        public void FindFirstReadableFile_ReturnsNullWhenEnumerationFailsPartWay()
        {
            // Arrange
            using TempDirectory temp = new();
            FileInfo missing = new(temp.GetPath("missing.msp"));

            // Act & Assert
            Assert.Null(TestEnvironment.FindFirstReadableFile(() => FailAfter(missing)));
        }

        /// <summary>
        /// Verifies that a failure other than an I/O or access one is not mistaken for a missing fixture.
        /// </summary>
        [Fact]
        public void FindFirstReadableFile_LetsOtherExceptionsThrough()
        {
            _ = Assert.Throws<InvalidOperationException>(static () => TestEnvironment.FindFirstReadableFile(static () => throw new InvalidOperationException()));
        }

        /// <summary>
        /// Yields the given file, then fails the way a later read of the directory would.
        /// </summary>
        /// <param name="file">The file to yield first.</param>
        /// <returns>An enumeration that throws once the file has been consumed.</returns>
        /// <exception cref="IOException">Thrown when the enumeration moves past the file.</exception>
        private static IEnumerable<FileInfo> FailAfter(FileInfo file)
        {
            yield return file;
            throw new IOException();
        }
    }
}
