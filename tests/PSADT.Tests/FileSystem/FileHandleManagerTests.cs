using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using PSADT.FileSystem;
using PSADT.Tests.TestHelpers;
using Xunit;

namespace PSADT.Tests.FileSystem
{
    /// <summary>
    /// Tests the system-wide open handle enumeration.
    /// </summary>
    /// <remarks>
    /// This is what answers "which process is holding this file open", so what matters is that a handle
    /// the test itself is holding is found, described correctly, and filtered on properly. The enumeration
    /// walks every handle on the machine and resolves each name by injecting a thread into the owning
    /// process, so it also has to cope with the processes an ordinary caller cannot open at all; that it
    /// returns rather than throwing is asserted here too, since an unelevated run is the normal case.
    /// <para>
    /// Closing handles is deliberately not covered: it reaches into other processes and takes something
    /// away from them.
    /// </para>
    /// </remarks>
    public sealed class FileHandleManagerTests
    {
        /// <summary>
        /// Verifies that a file this process is holding open is found, and attributed to this process.
        /// </summary>
        [Fact]
        public void GetOpenHandles_FindsAFileHeldByThisProcess()
        {
            // Arrange
            using TempDirectory temp = new();
            string path = temp.WriteFile("held.txt", "content");
            using Process current = Process.GetCurrentProcess();

            // Act
            using FileStream held = new(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            IReadOnlyList<FileHandleInfo> handles = FileHandleManager.GetOpenHandles(path);

            // Assert
            FileHandleInfo? found = handles.FirstOrDefault(h => h.FilePath.Equals(path, StringComparison.OrdinalIgnoreCase));
            Assert.NotNull(found);
            Assert.Equal(current.ProcessName, found.ProcessName, ignoreCase: true);
            Assert.Equal("File", found.HandleType);
            Assert.StartsWith(@"\Device\", found.NtPath, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Verifies that the same file is no longer reported once the handle is released, so the result
        /// reflects the machine now rather than a cached view.
        /// </summary>
        [Fact]
        public void GetOpenHandles_StopsReportingAFileOnceItIsReleased()
        {
            // Arrange
            using TempDirectory temp = new();
            string path = temp.WriteFile("released.txt", "content");

            // Act: held
            using (FileStream held = new(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                Assert.NotEmpty(FileHandleManager.GetOpenHandles(path));
            }

            // Assert: released
            Assert.Empty(FileHandleManager.GetOpenHandles(path));
        }

        /// <summary>
        /// Verifies that filtering by path returns only that path, since the unfiltered enumeration covers
        /// the whole machine and a caller asking about one file should not have to sift it.
        /// </summary>
        [Fact]
        public void GetOpenHandles_FiltersToTheRequestedPath()
        {
            // Arrange
            using TempDirectory temp = new();
            string wanted = temp.WriteFile("wanted.txt", "content");
            string other = temp.WriteFile("other.txt", "content");

            // Act
            using FileStream heldWanted = new(wanted, FileMode.Open, FileAccess.Read, FileShare.Read);
            using FileStream heldOther = new(other, FileMode.Open, FileAccess.Read, FileShare.Read);
            IReadOnlyList<FileHandleInfo> handles = FileHandleManager.GetOpenHandles(wanted);

            // Assert
            Assert.NotEmpty(handles);
            Assert.All(handles, h => Assert.Equal(wanted, h.FilePath, ignoreCase: true));
        }

        /// <summary>
        /// Verifies that a file nothing is holding open is reported as such rather than as an error.
        /// </summary>
        [Fact]
        public void GetOpenHandles_ReturnsNothingForAFileNobodyHolds()
        {
            // Arrange
            using TempDirectory temp = new();
            string path = temp.WriteFile("unheld.txt", "content");

            // Act & Assert
            Assert.Empty(FileHandleManager.GetOpenHandles(path));
        }

        /// <summary>
        /// Verifies that a path that does not exist is reported as holding nothing, rather than failing.
        /// </summary>
        [Fact]
        public void GetOpenHandles_ReturnsNothingForAPathThatDoesNotExist()
        {
            // Arrange
            using TempDirectory temp = new();

            // Act & Assert
            Assert.Empty(FileHandleManager.GetOpenHandles(temp.GetPath("absent.txt")));
        }

        /// <summary>
        /// Verifies that a blank path is rejected as an absent argument rather than treated as "no filter",
        /// which would silently enumerate the whole machine.
        /// </summary>
        /// <param name="path">The blank path to reject.</param>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void GetOpenHandles_RejectsABlankPath(string path)
        {
            _ = Assert.Throws<ArgumentException>(() => FileHandleManager.GetOpenHandles(path));
        }

        /// <summary>
        /// Verifies that the unfiltered enumeration completes and describes every handle it reports, which
        /// is the path that has to survive the processes this caller cannot open.
        /// </summary>
        /// <remarks>
        /// Run unelevated, most of the machine's processes refuse to be opened for handle duplication. The
        /// enumeration is expected to skip those rather than fail, so reaching the assertions at all is
        /// most of what this test checks.
        /// </remarks>
        [Fact]
        public void GetOpenHandles_EnumeratesTheWholeMachineWithoutFailing()
        {
            // Arrange
            using TempDirectory temp = new();
            string path = temp.WriteFile("amongst-many.txt", "content");

            // Act
            using FileStream held = new(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            IReadOnlyList<FileHandleInfo> handles = FileHandleManager.GetOpenHandles();

            // Assert: the machine has handles, ours is among them, and each is fully described
            Assert.NotEmpty(handles);
            Assert.Contains(handles, h => h.FilePath.Equals(path, StringComparison.OrdinalIgnoreCase));
            Assert.All(handles, static h =>
            {
                Assert.False(string.IsNullOrWhiteSpace(h.FilePath));
                Assert.False(string.IsNullOrWhiteSpace(h.NtPath));
                Assert.False(string.IsNullOrWhiteSpace(h.ProcessName));
                Assert.True(
                    string.Equals(h.HandleType, "File", StringComparison.Ordinal) || string.Equals(h.HandleType, "Directory", StringComparison.Ordinal),
                    $"Unexpected handle type '{h.HandleType}'.");
            });
        }
    }
}
