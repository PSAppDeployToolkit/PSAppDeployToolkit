using System;
using System.IO;
using PSADT.ShortcutManagement;
using PSADT.Tests.TestHelpers;
using Xunit;

namespace PSADT.Tests.ShortcutManagement
{
    /// <summary>
    /// Tests reading and writing shell link files.
    /// </summary>
    /// <remarks>
    /// The type is a wrapper over the shell's own link object, so what is worth testing is not the format
    /// but the boundary: that every property set before a save comes back after a load, that the
    /// read-only access mode is actually enforced rather than merely recorded, and that a disposed
    /// instance refuses to be used. Every file written goes into a temporary directory that is removed
    /// with the test.
    /// </remarks>
    public sealed class ShellLinkFileTests
    {
        /// <summary>
        /// Verifies that a newly created link carries the target it was created with, before anything has
        /// been written to disk.
        /// </summary>
        [Fact]
        public void Create_SetsTheTargetPath()
        {
            // Act
            using ShellLinkFile link = ShellLinkFile.Create(TargetPath);

            // Assert
            Assert.Equal(TargetPath, link.TargetPath, ignoreCase: true);
        }

        /// <summary>
        /// Verifies that a link saved to a path and loaded back reports the same target, which is the
        /// round trip everything else in this file depends on.
        /// </summary>
        [Fact]
        public void Save_RoundTripsTheTargetPath()
        {
            // Arrange
            using TempDirectory temp = new();
            string linkPath = temp.GetPath("target.lnk");

            // Act
            using (ShellLinkFile created = ShellLinkFile.Create(TargetPath))
            {
                created.Save(linkPath);
            }

            // Assert
            Assert.True(File.Exists(linkPath));
            using ShellLinkFile loaded = ShellLinkFile.Load(linkPath);
            Assert.Equal(TargetPath, loaded.TargetPath, ignoreCase: true);
        }

        /// <summary>
        /// Verifies that the descriptive string properties survive a save and load, since these are what
        /// a shortcut on a desktop or start menu actually shows.
        /// </summary>
        [Fact]
        public void Save_RoundTripsTheStringProperties()
        {
            // Arrange
            using TempDirectory temp = new();
            string linkPath = temp.GetPath("strings.lnk");

            // Act
            using (ShellLinkFile created = ShellLinkFile.Create(TargetPath))
            {
                created.Description = "A test shortcut";
                created.Arguments = "/first /second";
                created.WorkingDirectory = WorkingDirectory;
                created.Save(linkPath);
            }

            // Assert
            using ShellLinkFile loaded = ShellLinkFile.Load(linkPath);
            Assert.Equal("A test shortcut", loaded.Description);
            Assert.Equal("/first /second", loaded.Arguments);
            Assert.Equal(WorkingDirectory, loaded.WorkingDirectory, ignoreCase: true);
        }

        /// <summary>
        /// Verifies that the three window styles the shell honours survive a save and load.
        /// </summary>
        /// <remarks>
        /// Only these three do. <c>IShellLink::SetShowCmd</c> accepts a normal window, a maximized one
        /// and a minimized one that does not activate, and nothing else, so the other nine members of
        /// <see cref="ShortcutWindowStyle"/> cannot be stored in a shell link even though the type offers
        /// them. The test below states what becomes of those instead.
        /// </remarks>
        /// <param name="windowStyle">The window style to write and read back.</param>
        [Theory]
        [InlineData(ShortcutWindowStyle.Normal)]
        [InlineData(ShortcutWindowStyle.Maximized)]
        [InlineData(ShortcutWindowStyle.MinimizedNoActivate)]
        public void Save_RoundTripsTheWindowStylesTheShellHonours(ShortcutWindowStyle windowStyle)
        {
            // Arrange
            using TempDirectory temp = new();
            string linkPath = temp.GetPath("style.lnk");

            // Act
            using (ShellLinkFile created = ShellLinkFile.Create(TargetPath))
            {
                created.WindowStyle = windowStyle;
                created.Save(linkPath);
            }

            // Assert
            using ShellLinkFile loaded = ShellLinkFile.Load(linkPath);
            Assert.Equal(windowStyle, loaded.WindowStyle);
        }

        /// <summary>
        /// Verifies that every other window style is silently reduced to a normal window.
        /// </summary>
        /// <remarks>
        /// A trap in the surface rather than a defect in this code: the enumeration faithfully mirrors the
        /// SW_ constants, but a shell link can carry only three of them, so a caller asking for a hidden
        /// or minimized shortcut gets a normal one and no error. Pinned so the loss is written down
        /// somewhere a reader will find it.
        /// </remarks>
        /// <param name="windowStyle">The window style the shell will not store.</param>
        [Theory]
        [InlineData(ShortcutWindowStyle.Hidden)]
        [InlineData(ShortcutWindowStyle.Minimized)]
        [InlineData(ShortcutWindowStyle.NormalNoActivate)]
        [InlineData(ShortcutWindowStyle.NormalNoRestore)]
        [InlineData(ShortcutWindowStyle.MinimizedActivateRecent)]
        [InlineData(ShortcutWindowStyle.NormalNoRestoreNoActivate)]
        [InlineData(ShortcutWindowStyle.Restore)]
        [InlineData(ShortcutWindowStyle.ProcessDefault)]
        [InlineData(ShortcutWindowStyle.ForceMinimized)]
        public void Save_ReducesAnUnsupportedWindowStyleToNormal(ShortcutWindowStyle windowStyle)
        {
            // Arrange
            using TempDirectory temp = new();
            string linkPath = temp.GetPath("unsupported.lnk");

            // Act
            using (ShellLinkFile created = ShellLinkFile.Create(TargetPath))
            {
                created.WindowStyle = windowStyle;
                created.Save(linkPath);
            }

            // Assert
            using ShellLinkFile loaded = ShellLinkFile.Load(linkPath);
            Assert.Equal(ShortcutWindowStyle.Normal, loaded.WindowStyle);
        }

        /// <summary>
        /// Verifies that the icon location and index survive together, since the index is only meaningful
        /// alongside a file and the two are stored as one value by the shell.
        /// </summary>
        [Fact]
        public void Save_RoundTripsTheIconLocation()
        {
            // Arrange
            using TempDirectory temp = new();
            string linkPath = temp.GetPath("icon.lnk");

            // Act
            using (ShellLinkFile created = ShellLinkFile.Create(TargetPath))
            {
                created.IconLocation = new FileInfo(TargetPath);
                created.IconIndex = 2;
                created.Save(linkPath);
            }

            // Assert
            using ShellLinkFile loaded = ShellLinkFile.Load(linkPath);
            Assert.Equal(TargetPath, loaded.IconLocation?.FullName, ignoreCase: true);
            Assert.Equal(2, loaded.IconIndex);
        }

        /// <summary>
        /// Verifies that the hotkey survives a save and load through its string form, which is the only
        /// way this type exposes it and therefore the only way it can be set.
        /// </summary>
        /// <param name="hotkey">The hotkey to write and read back.</param>
        [Theory]
        [InlineData("Ctrl+Shift+A")]
        [InlineData("Ctrl+Alt+F5")]
        [InlineData("Alt+Num5")]
        [InlineData("Ctrl+Shift++")]
        public void Save_RoundTripsTheHotkey(string hotkey)
        {
            // Arrange
            using TempDirectory temp = new();
            string linkPath = temp.GetPath("hotkey.lnk");

            // Act
            using (ShellLinkFile created = ShellLinkFile.Create(TargetPath))
            {
                created.Hotkey = hotkey;
                created.Save(linkPath);
            }

            // Assert
            using ShellLinkFile loaded = ShellLinkFile.Load(linkPath);
            Assert.Equal(hotkey, loaded.Hotkey);
        }

        /// <summary>
        /// Verifies that the run-as-administrator flag survives a save and load, which is the flag most
        /// often set on a deployed shortcut and the one whose loss would be silent.
        /// </summary>
        [Fact]
        public void Save_RoundTripsTheRunAsAdminFlag()
        {
            // Arrange
            using TempDirectory temp = new();
            string linkPath = temp.GetPath("elevated.lnk");

            // Act
            using (ShellLinkFile created = ShellLinkFile.Create(TargetPath))
            {
                created.RunAsAdmin = true;
                created.Save(linkPath);
            }

            // Assert
            using ShellLinkFile loaded = ShellLinkFile.Load(linkPath);
            Assert.True(loaded.RunAsAdmin);
        }

        /// <summary>
        /// Verifies that saving over a link opened for reading is refused, rather than appearing to
        /// succeed and silently discarding the change.
        /// </summary>
        [Fact]
        public void Save_IsRefusedForAReadOnlyLoad()
        {
            // Arrange
            using TempDirectory temp = new();
            string linkPath = temp.GetPath("readonly.lnk");
            using (ShellLinkFile created = ShellLinkFile.Create(TargetPath))
            {
                created.Save(linkPath);
            }

            // Act & Assert
            using ShellLinkFile loaded = ShellLinkFile.Load(linkPath);
            _ = Assert.Throws<InvalidOperationException>(loaded.Save);
            _ = Assert.Throws<InvalidOperationException>(() => loaded.Save(linkPath));
        }

        /// <summary>
        /// Verifies that a link opened for reading can still be written somewhere else, so the refusal
        /// above is about overwriting the source rather than about saving at all.
        /// </summary>
        [Fact]
        public void Save_AllowsAReadOnlyLoadToBeWrittenElsewhere()
        {
            // Arrange
            using TempDirectory temp = new();
            string sourcePath = temp.GetPath("source.lnk");
            string copyPath = temp.GetPath("copy.lnk");
            using (ShellLinkFile created = ShellLinkFile.Create(TargetPath))
            {
                created.Save(sourcePath);
            }

            // Act
            using (ShellLinkFile loaded = ShellLinkFile.Load(sourcePath))
            {
                loaded.Save(copyPath);
            }

            // Assert
            Assert.True(File.Exists(copyPath));
            using ShellLinkFile copy = ShellLinkFile.Load(copyPath);
            Assert.Equal(TargetPath, copy.TargetPath, ignoreCase: true);
        }

        /// <summary>
        /// Verifies that a link created in memory has no path to save itself to, so the parameterless
        /// save reports that rather than writing somewhere arbitrary.
        /// </summary>
        [Fact]
        public void Save_ReportsThatANewLinkHasNoPath()
        {
            // Act & Assert
            using ShellLinkFile created = ShellLinkFile.Create(TargetPath);
            _ = Assert.Throws<InvalidOperationException>(created.Save);
        }

        /// <summary>
        /// Verifies that loading a shortcut that is not there is reported as a missing file rather than
        /// surfacing whatever the shell would have said.
        /// </summary>
        [Fact]
        public void Load_ReportsAMissingFile()
        {
            // Arrange
            using TempDirectory temp = new();

            // Act & Assert
            _ = Assert.Throws<FileNotFoundException>(() => ShellLinkFile.Load(temp.GetPath("absent.lnk")));
        }

        /// <summary>
        /// Verifies that a blank path is rejected as a bad argument rather than as a missing file.
        /// </summary>
        /// <param name="filePath">The blank path to reject.</param>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Load_RejectsABlankPath(string filePath)
        {
            _ = Assert.Throws<ArgumentException>(() => ShellLinkFile.Load(filePath));
        }

        /// <summary>
        /// Verifies that a disposed link refuses to be used, rather than calling through to a released
        /// COM object.
        /// </summary>
        [Fact]
        public void Dispose_MakesFurtherUseThrow()
        {
            // Arrange
            using ShellLinkFile link = ShellLinkFile.Create(TargetPath);

            // Act
            link.Dispose();

            // Assert
            _ = Assert.Throws<ObjectDisposedException>(() => _ = link.TargetPath);
            _ = Assert.Throws<ObjectDisposedException>(() => _ = link.Description);
            _ = Assert.Throws<ObjectDisposedException>(() => _ = link.FilePath);
            _ = Assert.Throws<ObjectDisposedException>(link.GetInfoSnapshot);
            _ = Assert.Throws<ObjectDisposedException>(link.Save);
        }

        /// <summary>
        /// Verifies that disposing twice is harmless, since the type is used with a using statement that
        /// may follow an explicit dispose.
        /// </summary>
        [Fact]
        public void Dispose_IsIdempotent()
        {
            // Arrange
            using ShellLinkFile link = ShellLinkFile.Create(TargetPath);

            // Act & Assert
            Assert.Null(Record.Exception(link.Dispose));
            Assert.Null(Record.Exception(link.Dispose));
        }

        /// <summary>
        /// Verifies that the snapshot reports the same values as the link it was taken from, since that
        /// is the form callers outside this assembly receive.
        /// </summary>
        [Fact]
        public void GetInfoSnapshot_MatchesTheLink()
        {
            // Arrange
            using TempDirectory temp = new();
            string linkPath = temp.GetPath("snapshot.lnk");
            using (ShellLinkFile created = ShellLinkFile.Create(TargetPath))
            {
                created.Description = "A test shortcut";
                created.Arguments = "/quiet";
                created.WorkingDirectory = WorkingDirectory;
                created.RunAsAdmin = true;
                created.Save(linkPath);
            }

            // Act
            using ShellLinkFile loaded = ShellLinkFile.Load(linkPath);
            ShellLinkInfo snapshot = loaded.GetInfoSnapshot();

            // Assert
            Assert.Equal(loaded.FilePath?.FullName, snapshot.FilePath.FullName);
            Assert.Equal(loaded.TargetPath, snapshot.TargetPath);
            Assert.Equal(loaded.Description, snapshot.Description);
            Assert.Equal(loaded.Arguments, snapshot.Arguments);
            Assert.Equal(loaded.WorkingDirectory, snapshot.WorkingDirectory);
            Assert.Equal(loaded.RunAsAdmin, snapshot.RunAsAdmin);
            Assert.Equal(loaded.WindowStyle, snapshot.WindowStyle);
        }

        /// <summary>
        /// Verifies that the static accessor produces the same snapshot as loading and snapshotting by
        /// hand, since it is the entry point the PowerShell module uses.
        /// </summary>
        [Fact]
        public void ShellLinkInfo_GetMatchesALoadedSnapshot()
        {
            // Arrange
            using TempDirectory temp = new();
            string linkPath = temp.GetPath("accessor.lnk");
            using (ShellLinkFile created = ShellLinkFile.Create(TargetPath))
            {
                created.Description = "A test shortcut";
                created.Save(linkPath);
            }

            // Act
            ShellLinkInfo direct = ShellLinkInfo.Get(linkPath);

            // Assert: compared member by member rather than with record equality, which cannot work here
            // for the reason the next test sets out
            using ShellLinkFile loaded = ShellLinkFile.Load(linkPath);
            ShellLinkInfo snapshot = loaded.GetInfoSnapshot();
            Assert.Equal(snapshot.FilePath.FullName, direct.FilePath.FullName);
            Assert.Equal(snapshot.TargetPath, direct.TargetPath);
            Assert.Equal(snapshot.Description, direct.Description);
            Assert.Equal(snapshot.WindowStyle, direct.WindowStyle);
            Assert.Equal(snapshot.RunAsAdmin, direct.RunAsAdmin);
        }

        /// <summary>
        /// Verifies that two snapshots of one unchanged shortcut do not compare equal, despite the type
        /// being a record.
        /// </summary>
        /// <remarks>
        /// <see cref="ShellLinkInfo"/> is a record, so two snapshots of the same shortcut ought to be
        /// equal. They are not. The generated comparison includes the <see cref="FileInfo"/> members, and
        /// <see cref="FileInfo"/> does not override equality, so those compare by reference and no two
        /// snapshots ever match. The record's generated <c>ToString</c> renders the two identically, which
        /// makes the inequality especially confusing to run into.
        /// <para>
        /// Pinned rather than repaired. Fixing it means overriding equality or storing the paths as
        /// strings, and <c>InternetShortcutInfo</c> and <c>UserProfileInfo</c> have the same shape, so it
        /// is a decision about several public records rather than a local change.
        /// </para>
        /// </remarks>
        [Fact]
        public void ShellLinkInfo_RecordEqualityDoesNotCompareByValue()
        {
            // Arrange
            using TempDirectory temp = new();
            string linkPath = temp.GetPath("equality.lnk");
            using (ShellLinkFile created = ShellLinkFile.Create(TargetPath))
            {
                created.Save(linkPath);
            }

            // Act
            ShellLinkInfo first = ShellLinkInfo.Get(linkPath);
            ShellLinkInfo second = ShellLinkInfo.Get(linkPath);

            // Assert: identical content, identical when printed, and still not equal
            Assert.Equal(first.ToString(), second.ToString());
            Assert.NotEqual(first, second);
        }

        /// <summary>
        /// An executable that exists on every Windows installation, used as a link target so the shell
        /// has something real to resolve.
        /// </summary>
        private static string TargetPath { get; } = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.System), "notepad.exe");

        /// <summary>
        /// A directory that exists on every Windows installation, used as a working directory.
        /// </summary>
        private static string WorkingDirectory { get; } = Environment.GetFolderPath(Environment.SpecialFolder.System);
    }
}
