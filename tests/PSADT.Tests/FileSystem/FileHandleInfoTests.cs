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
    /// Tests the description of a single open handle to a file.
    /// </summary>
    /// <remarks>
    /// This is what a caller receives for each process holding a file open, and what it uses to decide
    /// whether to ask somebody to close something. The handle it describes is one this test holds open
    /// itself, so the process named must be this one and the path named must be the file it opened -
    /// which is the strongest statement that can be made about a description read off the machine.
    /// <para>
    /// The file is created in a temporary directory and removed with it, and the only handle asserted
    /// about is one this test opened and closes again.
    /// </para>
    /// </remarks>
    public sealed class FileHandleInfoTests
    {
        /// <summary>
        /// Verifies that a handle this process is holding is described as belonging to this process, and
        /// as pointing at the file that was opened.
        /// </summary>
        [Fact]
        public void FileHandleInfo_DescribesAHandleThisProcessHolds()
        {
            // Arrange
            using TempDirectory temp = new();
            string path = temp.WriteFile("held.txt", "contents");
            using Process current = Process.GetCurrentProcess();
            using FileStream held = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            // Act
            IReadOnlyList<FileHandleInfo> handles = FileHandleManager.GetOpenHandles(path);

            // Assert
            FileHandleInfo? ours = handles.FirstOrDefault(handle => handle.HandleInfo.UniqueProcessId == (nuint)current.Id);
            Assert.NotNull(ours);
            Assert.Equal(current.ProcessName, ours.ProcessName, StringComparer.OrdinalIgnoreCase);
            Assert.Equal(path, ours.FilePath, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Verifies that every description is complete, since each one may be shown to a person and each
        /// part of it is read: the process to name, the path to name it against, and the kind of object
        /// so that a handle to something that is not a file is not offered as one.
        /// </summary>
        [Fact]
        public void FileHandleInfo_DescribesEveryHandleCompletely()
        {
            // Arrange
            using TempDirectory temp = new();
            string path = temp.WriteFile("held.txt", "contents");
            using FileStream held = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            // Act & Assert
            Assert.All(FileHandleManager.GetOpenHandles(path), static handle =>
            {
                Assert.False(string.IsNullOrWhiteSpace(handle.ProcessName));
                Assert.False(string.IsNullOrWhiteSpace(handle.FilePath));
                Assert.False(string.IsNullOrWhiteSpace(handle.NtPath));
                Assert.Equal("File", handle.HandleType, StringComparer.Ordinal);
            });
        }

        /// <summary>
        /// Verifies that the native path a handle was found under resolves to the path reported, since
        /// the two are the same object named in the kernel's terms and in the caller's.
        /// </summary>
        [Fact]
        public void NtPath_NamesTheSameObjectAsTheFilePath()
        {
            // Arrange
            using TempDirectory temp = new();
            string path = temp.WriteFile("held.txt", "contents");
            using FileStream held = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            // Act & Assert
            Assert.All(FileHandleManager.GetOpenHandles(path), static handle =>
                Assert.EndsWith(Path.GetFileName(handle.FilePath), handle.NtPath, StringComparison.OrdinalIgnoreCase));
        }
    }
}
