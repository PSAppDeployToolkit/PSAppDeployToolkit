using System;
using System.Globalization;
using System.IO;
using PSADT.ShortcutManagement;
using PSADT.Tests.TestHelpers;
using PSADT.Utilities;
using Xunit;

namespace PSADT.Tests.ShortcutManagement
{
    /// <summary>
    /// Tests reading and writing internet shortcut files.
    /// </summary>
    /// <remarks>
    /// The type wraps the shell's URL object, and unlike a shell link most of its properties go through a
    /// property storage rather than through named methods. That storage is typed, so each property has its
    /// own conversion to get wrong, which is why the round trips below cover the string, integer and URI
    /// cases separately rather than as one.
    /// <para>
    /// Every test runs through <see cref="StaThread"/>. The shell's URL object can only be reached from a
    /// single-threaded apartment, and xunit runs test bodies on multi-threaded thread pool threads, so
    /// without the hop every test here fails on interface marshalling rather than on what it asserts.
    /// </para>
    /// <para>
    /// <see cref="InternetShortcutFile.Invoke()"/> is deliberately not covered: it launches the address in
    /// the default browser, which is a side effect on the machine rather than a value to assert.
    /// </para>
    /// </remarks>
    public sealed class InternetShortcutFileTests
    {
        /// <summary>
        /// Verifies that a newly created shortcut carries the address it was created with.
        /// </summary>
        [Fact]
        public void Create_SetsTheUrl()
        {
            StaThread.Run(static () =>
            {
                using InternetShortcutFile shortcut = InternetShortcutFile.Create(Url);
                Assert.Equal(Url, shortcut.Url);
            });
        }

        /// <summary>
        /// Verifies that a null address is rejected when creating, since a shortcut with no address is not
        /// a shortcut.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "This is deliberate as part of unit testing.")]
        [Fact]
        public void Create_RejectsANullUrl()
        {
            StaThread.Run(static () => Assert.Throws<ArgumentNullException>(static () => InternetShortcutFile.Create(null!)));
        }

        /// <summary>
        /// Verifies that a shortcut saved and loaded back reports the same address, which is the round trip
        /// everything else here depends on.
        /// </summary>
        /// <param name="url">The address to write and read back.</param>
        [Theory]
        [InlineData("https://psappdeploytoolkit.com/")]
        [InlineData("https://example.com/path/to/page")]
        [InlineData("https://example.com/path?query=value&other=thing")]
        [InlineData("http://example.com/")]
        [InlineData("ftp://example.com/file.txt")]
        public void Save_RoundTripsTheUrl(string url)
        {
            StaThread.Run(() =>
            {
                // Arrange
                using TempDirectory temp = new();
                string shortcutPath = temp.GetPath("url.url");
                Uri expected = new(url);

                // Act
                using (InternetShortcutFile created = InternetShortcutFile.Create(expected))
                {
                    created.Save(shortcutPath);
                }

                // Assert
                Assert.True(File.Exists(shortcutPath));
                using InternetShortcutFile loaded = InternetShortcutFile.Load(shortcutPath);
                Assert.Equal(expected, loaded.Url);
            });
        }

        /// <summary>
        /// Verifies that the string properties held in the property storage survive a save and load.
        /// </summary>
        [Fact]
        public void Save_RoundTripsTheStringProperties()
        {
            StaThread.Run(static () =>
            {
                // Arrange
                using TempDirectory temp = new();
                string shortcutPath = temp.GetPath("strings.url");

                // Act
                using (InternetShortcutFile created = InternetShortcutFile.Create(Url))
                {
                    created.Author = "Devicie";
                    created.Description = "A test shortcut";
                    created.Comment = "A comment";
                    created.WhatsNew = "Something new";
                    created.Save(shortcutPath);
                }

                // Assert
                using InternetShortcutFile loaded = InternetShortcutFile.Load(shortcutPath);
                Assert.Equal("Devicie", loaded.Author);
                Assert.Equal("A test shortcut", loaded.Description);
                Assert.Equal("A comment", loaded.Comment);
                Assert.Equal("Something new", loaded.WhatsNew);
            });
        }

        /// <summary>
        /// Verifies that the icon file and its index survive together, since the index is only read back
        /// when a file is present.
        /// </summary>
        /// <remarks>
        /// The path form is what is asserted, in both directions. The shell stores the path as given - the
        /// saved file holds <c language="text">IconFile=C:\Windows\System32\shell32.dll</c> - but hands it back as
        /// <c language="text">file:///C:/Windows/System32/shell32.dll</c>, so the getter translates it back. Without that a
        /// caller could not pass what it read straight back in, and reading a shortcut and writing it out
        /// again would rewrite the icon as a URI.
        /// </remarks>
        [Fact]
        public void Save_RoundTripsTheIcon()
        {
            StaThread.Run(static () =>
            {
                // Arrange
                using TempDirectory temp = new();
                string shortcutPath = temp.GetPath("icon.url");

                // Act
                using (InternetShortcutFile created = InternetShortcutFile.Create(Url))
                {
                    created.IconFile = IconPath;
                    created.IconIndex = 3;
                    created.Save(shortcutPath);
                }

                // Assert
                using InternetShortcutFile loaded = InternetShortcutFile.Load(shortcutPath);
                Assert.Equal(IconPath, loaded.IconFile, ignoreCase: true);
                Assert.Equal(3, loaded.IconIndex);
            });
        }

        /// <summary>
        /// Verifies that the index is not reported when no icon file is set, so a caller cannot read an
        /// index that refers to nothing.
        /// </summary>
        [Fact]
        public void IconIndex_IsNotReportedWithoutAnIconFile()
        {
            StaThread.Run(static () =>
            {
                using InternetShortcutFile created = InternetShortcutFile.Create(Url);
                Assert.Null(created.IconFile);
                Assert.Null(created.IconIndex);
            });
        }

        /// <summary>
        /// Verifies that clearing the index while a file is still set is refused, since the pair would then
        /// be inconsistent.
        /// </summary>
        [Fact]
        public void IconIndex_RefusesToBeClearedWhileAnIconFileIsSet()
        {
            StaThread.Run(static () =>
            {
                // Arrange
                using InternetShortcutFile created = InternetShortcutFile.Create(Url);
                created.IconFile = IconPath;
                created.IconIndex = 1;

                // Act & Assert
                _ = Assert.Throws<InvalidOperationException>(() => created.IconIndex = null);
            });
        }

        /// <summary>
        /// Verifies that the setter puts the show command into the saved file, in the form the shell reads.
        /// </summary>
        /// <remarks>
        /// Asserted against the file rather than through the getter, which answers from what the instance
        /// remembers and so would pass whether or not anything was written. The file is plain text in an
        /// INI-like layout, so the stored value can be checked directly.
        /// </remarks>
        /// <param name="showCommand">The show command to write.</param>
        /// <param name="expectedValue">The number the shell should store for it.</param>
        [Theory]
        [InlineData(ShortcutWindowStyle.Normal, 1)]
        [InlineData(ShortcutWindowStyle.Maximized, 3)]
        [InlineData(ShortcutWindowStyle.MinimizedNoActivate, 7)]
        public void Save_WritesTheShowCommandToTheFile(ShortcutWindowStyle showCommand, int expectedValue)
        {
            StaThread.Run(() =>
            {
                // Arrange
                using TempDirectory temp = new();
                string shortcutPath = temp.GetPath("written.url");

                // Act
                using (InternetShortcutFile created = InternetShortcutFile.Create(Url))
                {
                    created.ShowCommand = showCommand;
                    created.Save(shortcutPath);
                }

                // Assert
                Assert.Contains(
                    $"ShowCommand={expectedValue.ToString(CultureInfo.InvariantCulture)}",
                    File.ReadAllText(shortcutPath),
                    StringComparison.Ordinal);
            });
        }

        /// <summary>
        /// Verifies that the show command survives a save and load.
        /// </summary>
        /// <remarks>
        /// The shell writes this into the file but will not hand it back through the property storage it
        /// was written to, so the value is remembered on the instance and read back out of the file for a
        /// shortcut that was loaded. Both routes are covered here: the value is read on the instance that
        /// set it, and again on one loaded from the saved file.
        /// </remarks>
        /// <param name="showCommand">The show command to write and read back.</param>
        [Theory]
        [InlineData(ShortcutWindowStyle.Normal)]
        [InlineData(ShortcutWindowStyle.Maximized)]
        [InlineData(ShortcutWindowStyle.MinimizedNoActivate)]
        public void Save_RoundTripsTheShowCommand(ShortcutWindowStyle showCommand)
        {
            StaThread.Run(() =>
            {
                // Arrange
                using TempDirectory temp = new();
                string shortcutPath = temp.GetPath("showcmd.url");

                // Act
                using (InternetShortcutFile created = InternetShortcutFile.Create(Url))
                {
                    created.ShowCommand = showCommand;

                    // Assert: readable on the instance that set it, before anything is saved
                    Assert.Equal(showCommand, created.ShowCommand);
                    created.Save(shortcutPath);
                }

                // Assert: and on one loaded back from the saved file
                using InternetShortcutFile loaded = InternetShortcutFile.Load(shortcutPath);
                Assert.Equal(showCommand, loaded.ShowCommand);
            });
        }

        /// <summary>
        /// Verifies that the working directory survives a save and load.
        /// </summary>
        /// <remarks>
        /// The shell will not hand this back through the property storage it was written to, exactly as it
        /// will not for the show command, so it is remembered on the instance and read back out of the file
        /// for a shortcut that was loaded. Both routes are covered here.
        /// </remarks>
        [Fact]
        public void Save_RoundTripsTheWorkingDirectory()
        {
            StaThread.Run(static () =>
            {
                // Arrange
                using TempDirectory temp = new();
                string shortcutPath = temp.GetPath("workingdir.url");
                string workingDirectory = Environment.SystemDirectory;

                // Act
                using (InternetShortcutFile created = InternetShortcutFile.Create(Url))
                {
                    created.WorkingDirectory = workingDirectory;

                    // Assert: readable on the instance that set it, before anything is saved
                    Assert.Equal(workingDirectory, created.WorkingDirectory);
                    created.Save(shortcutPath);
                }

                // Assert: written into the file, and readable on one loaded back from it
                Assert.Contains($"WorkingDirectory={workingDirectory}", File.ReadAllText(shortcutPath), StringComparison.Ordinal);
                using InternetShortcutFile loaded = InternetShortcutFile.Load(shortcutPath);
                Assert.Equal(workingDirectory, loaded.WorkingDirectory);
            });
        }

        /// <summary>
        /// Verifies that the show command is read out of the file once, as the shortcut is loaded, rather
        /// than on every read of the property.
        /// </summary>
        /// <remarks>
        /// Demonstrated by changing the file underneath a loaded shortcut and asking again: an instance
        /// that re-read the file would report the new value, and one that read it at load reports what it
        /// was opened with. The behaviour is the same either way for any caller doing something sensible
        /// - this pins which of the two is happening, so that reading it stops costing a call into the
        /// shell and a read of the file every time somebody asks.
        /// </remarks>
        [Fact]
        public void ShowCommand_IsReadFromTheFileOnceAtLoad()
        {
            StaThread.Run(static () =>
            {
                // Arrange: a saved shortcut holding a show command
                using TempDirectory temp = new();
                string shortcutPath = temp.GetPath("cached.url");
                using (InternetShortcutFile created = InternetShortcutFile.Create(Url))
                {
                    created.ShowCommand = ShortcutWindowStyle.Maximized;
                    created.Save(shortcutPath);
                }

                // Act
                using InternetShortcutFile loaded = InternetShortcutFile.Load(shortcutPath);
                Assert.Equal(ShortcutWindowStyle.Maximized, loaded.ShowCommand);
                IniUtilities.WriteSectionKeyValue(shortcutPath, "InternetShortcut", "ShowCommand", ((int)ShortcutWindowStyle.MinimizedNoActivate).ToString(CultureInfo.InvariantCulture));

                // Assert: the file now says otherwise, and the loaded shortcut still reports what it opened with
                Assert.Equal(
                    ((int)ShortcutWindowStyle.MinimizedNoActivate).ToString(CultureInfo.InvariantCulture),
                    IniUtilities.GetSectionKeyValue(shortcutPath, "InternetShortcut", "ShowCommand"),
                    StringComparer.Ordinal);
                Assert.Equal(ShortcutWindowStyle.Maximized, loaded.ShowCommand);
            });
        }

        /// <summary>
        /// Verifies that a shortcut opened read-only says it cannot be saved, and one opened for writing
        /// says it can.
        /// </summary>
        /// <remarks>
        /// Asked rather than attempted. Saving a shortcut opened read-only fails part way through the
        /// shell's own write, which can leave the file in a worse state than not having tried.
        /// </remarks>
        [Fact]
        public void CanSave_ReflectsTheStorageModeItWasOpenedWith()
        {
            StaThread.Run(static () =>
            {
                // Arrange
                using TempDirectory temp = new();
                string shortcutPath = temp.GetPath("cansave.url");
                using (InternetShortcutFile created = InternetShortcutFile.Create(Url))
                {
                    // Assert: one that was built rather than loaded has nothing stopping it
                    Assert.True(created.CanSave);
                    created.Save(shortcutPath);
                }

                // Assert
                using (InternetShortcutFile readOnly = InternetShortcutFile.Load(shortcutPath))
                {
                    Assert.False(readOnly.CanSave);
                }
                using InternetShortcutFile writable = InternetShortcutFile.Load(shortcutPath, Interop.STGM.STGM_READWRITE);
                Assert.True(writable.CanSave);
            });
        }

        /// <summary>
        /// Verifies that whether the shortcut has roamed between machines round-trips, since it is the
        /// one boolean the property set carries.
        /// </summary>
        [Fact]
        public void Save_RoundTripsWhetherItHasRoamed()
        {
            StaThread.Run(static () =>
            {
                // Arrange
                using TempDirectory temp = new();
                string shortcutPath = temp.GetPath("roamed.url");

                // Act
                using (InternetShortcutFile created = InternetShortcutFile.Create(Url))
                {
                    // Assert: absent until it is set, rather than reported as not having roamed
                    Assert.Null(created.Roamed);
                    created.Roamed = true;
                    Assert.True(created.Roamed);
                    created.Save(shortcutPath);
                }

                // Assert
                using InternetShortcutFile loaded = InternetShortcutFile.Load(shortcutPath);
                Assert.True(loaded.Roamed);
            });
        }

        /// <summary>
        /// Verifies that clearing the show command reports it as cleared, rather than falling back to
        /// the value still sitting in the file.
        /// </summary>
        /// <remarks>
        /// The value has to be read back out of the file for a shortcut that was loaded, because the
        /// shell will not report it. That makes "never set" and "set to nothing" look alike unless they
        /// are told apart deliberately - and getting it wrong means a caller clears the window style,
        /// reads it back, and is handed the value it just cleared.
        /// </remarks>
        [Fact]
        public void ShowCommand_ClearingItIsNotOverriddenByTheSavedFile()
        {
            StaThread.Run(static () =>
            {
                // Arrange: a saved shortcut that holds a show command
                using TempDirectory temp = new();
                string shortcutPath = temp.GetPath("cleared.url");
                using (InternetShortcutFile created = InternetShortcutFile.Create(Url))
                {
                    created.ShowCommand = ShortcutWindowStyle.Maximized;
                    created.Save(shortcutPath);
                }

                // Act
                using InternetShortcutFile loaded = InternetShortcutFile.Load(shortcutPath, Interop.STGM.STGM_READWRITE);
                Assert.Equal(ShortcutWindowStyle.Maximized, loaded.ShowCommand);
                loaded.ShowCommand = null;

                // Assert
                Assert.Null(loaded.ShowCommand);
            });
        }

        /// <summary>
        /// Verifies that the hotkey survives a save and load through its string form, which is the only
        /// form this type exposes.
        /// </summary>
        /// <param name="hotkey">The hotkey to write and read back.</param>
        [Theory]
        [InlineData("Ctrl+Shift+A")]
        [InlineData("Ctrl+Alt+F9")]
        [InlineData("Alt+Num5")]
        public void Save_RoundTripsTheHotkey(string hotkey)
        {
            StaThread.Run(() =>
            {
                // Arrange
                using TempDirectory temp = new();
                string shortcutPath = temp.GetPath("hotkey.url");

                // Act
                using (InternetShortcutFile created = InternetShortcutFile.Create(Url))
                {
                    created.Hotkey = hotkey;
                    created.Save(shortcutPath);
                }

                // Assert
                using InternetShortcutFile loaded = InternetShortcutFile.Load(shortcutPath);
                Assert.Equal(hotkey, loaded.Hotkey);
            });
        }

        /// <summary>
        /// Verifies that no hotkey reads back as absent rather than as a hotkey with no key, since the
        /// stored value for "none" is zero and has to be distinguished.
        /// </summary>
        [Fact]
        public void Hotkey_IsNotReportedWhenNoneIsSet()
        {
            StaThread.Run(static () =>
            {
                using InternetShortcutFile created = InternetShortcutFile.Create(Url);
                Assert.Null(created.Hotkey);
            });
        }

        /// <summary>
        /// Verifies that saving over a shortcut opened for reading is refused, rather than appearing to
        /// succeed and discarding the change.
        /// </summary>
        [Fact]
        public void Save_IsRefusedForAReadOnlyLoad()
        {
            StaThread.Run(static () =>
            {
                // Arrange
                using TempDirectory temp = new();
                string shortcutPath = temp.GetPath("readonly.url");
                using (InternetShortcutFile created = InternetShortcutFile.Create(Url))
                {
                    created.Save(shortcutPath);
                }

                // Act & Assert
                using InternetShortcutFile loaded = InternetShortcutFile.Load(shortcutPath);
                _ = Assert.Throws<InvalidOperationException>(loaded.Save);
                _ = Assert.Throws<InvalidOperationException>(() => loaded.Save(shortcutPath));
            });
        }

        /// <summary>
        /// Verifies that a shortcut opened for reading can still be written elsewhere, so the refusal above
        /// is about overwriting the source rather than about saving at all.
        /// </summary>
        [Fact]
        public void Save_AllowsAReadOnlyLoadToBeWrittenElsewhere()
        {
            StaThread.Run(static () =>
            {
                // Arrange
                using TempDirectory temp = new();
                string sourcePath = temp.GetPath("source.url");
                string copyPath = temp.GetPath("copy.url");
                using (InternetShortcutFile created = InternetShortcutFile.Create(Url))
                {
                    created.Save(sourcePath);
                }

                // Act
                using (InternetShortcutFile loaded = InternetShortcutFile.Load(sourcePath))
                {
                    loaded.Save(copyPath);
                }

                // Assert
                using InternetShortcutFile copy = InternetShortcutFile.Load(copyPath);
                Assert.Equal(Url, copy.Url);
            });
        }

        /// <summary>
        /// Verifies that a shortcut created in memory has no path to save itself to.
        /// </summary>
        [Fact]
        public void Save_ReportsThatANewShortcutHasNoPath()
        {
            StaThread.Run(static () =>
            {
                using InternetShortcutFile created = InternetShortcutFile.Create(Url);
                _ = Assert.Throws<InvalidOperationException>(created.Save);
            });
        }

        /// <summary>
        /// Verifies that a shortcut loaded through a relative name still reports a current file.
        /// </summary>
        /// <remarks>
        /// The shell records whatever name it was handed, so after a relative load the current file is a
        /// relative name too. That rules out deciding "has this been saved anywhere" by looking at the
        /// returned string: a genuine current file is not always fully qualified, so any such test would
        /// report no file here and make the parameterless save refuse a shortcut that has one. The
        /// distinction is carried by the result code instead, which is what the underlying wrapper reads.
        /// </remarks>
        [Fact]
        public void FilePath_IsReportedAfterLoadingThroughARelativeName()
        {
            StaThread.Run(static () =>
            {
                // Arrange
                using TempDirectory temp = new();
                using (InternetShortcutFile created = InternetShortcutFile.Create(Url))
                {
                    created.Save(temp.GetPath("relative.url"));
                }

                // Act: load by a name relative to the directory the shortcut is in
                string previous = Directory.GetCurrentDirectory();
                Directory.SetCurrentDirectory(temp.FullName);
                try
                {
                    using InternetShortcutFile loaded = InternetShortcutFile.Load("relative.url");

                    // Assert
                    Assert.NotNull(loaded.FilePath);
                    Assert.Equal("relative.url", loaded.FilePath.Name);
                }
                finally
                {
                    Directory.SetCurrentDirectory(previous);
                }
            });
        }

        /// <summary>
        /// Verifies that loading a shortcut that is not there is reported as a missing file.
        /// </summary>
        [Fact]
        public void Load_ReportsAMissingFile()
        {
            StaThread.Run(static () =>
            {
                using TempDirectory temp = new();
                _ = Assert.Throws<FileNotFoundException>(() => InternetShortcutFile.Load(temp.GetPath("absent.url")));
            });
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
            StaThread.Run(() => Assert.Throws<ArgumentException>(() => InternetShortcutFile.Load(filePath)));
        }

        /// <summary>
        /// Verifies that a disposed shortcut refuses to be used rather than calling through to a released
        /// COM object.
        /// </summary>
        [Fact]
        public void Dispose_MakesFurtherUseThrow()
        {
            StaThread.Run(static () =>
            {
                // Arrange
                using InternetShortcutFile shortcut = InternetShortcutFile.Create(Url);

                // Act
                shortcut.Dispose();

                // Assert
                _ = Assert.Throws<ObjectDisposedException>(() => _ = shortcut.Url);
                _ = Assert.Throws<ObjectDisposedException>(() => _ = shortcut.Name);
                _ = Assert.Throws<ObjectDisposedException>(() => _ = shortcut.FilePath);
                _ = Assert.Throws<ObjectDisposedException>(shortcut.GetInfoSnapshot);
                _ = Assert.Throws<ObjectDisposedException>(shortcut.Save);
            });
        }

        /// <summary>
        /// Verifies that disposing twice is harmless.
        /// </summary>
        [Fact]
        public void Dispose_IsIdempotent()
        {
            StaThread.Run(static () =>
            {
                using InternetShortcutFile shortcut = InternetShortcutFile.Create(Url);
                Assert.Null(Record.Exception(shortcut.Dispose));
                Assert.Null(Record.Exception(shortcut.Dispose));
            });
        }

        /// <summary>
        /// Verifies that the snapshot reports the same values as the shortcut it was taken from.
        /// </summary>
        [Fact]
        public void GetInfoSnapshot_MatchesTheShortcut()
        {
            StaThread.Run(static () =>
            {
                // Arrange
                using TempDirectory temp = new();
                string shortcutPath = temp.GetPath("snapshot.url");
                using (InternetShortcutFile created = InternetShortcutFile.Create(Url))
                {
                    created.Author = "Devicie";
                    created.Description = "A test shortcut";
                    created.Save(shortcutPath);
                }

                // Act
                using InternetShortcutFile loaded = InternetShortcutFile.Load(shortcutPath);
                InternetShortcutInfo snapshot = loaded.GetInfoSnapshot();

                // Assert
                Assert.Equal(loaded.FilePath?.FullName, snapshot.FilePath.FullName);
                Assert.Equal(loaded.Url, snapshot.Url);
                Assert.Equal(loaded.Author, snapshot.Author);
                Assert.Equal(loaded.Description, snapshot.Description);
            });
        }

        /// <summary>
        /// Verifies that the static accessor produces the same values as loading and snapshotting by hand,
        /// since it is the entry point the PowerShell module uses.
        /// </summary>
        /// <remarks>
        /// Compared member by member because <see cref="InternetShortcutInfo"/> carries a
        /// <see cref="FileInfo"/> and so cannot be compared with record equality, for the same reason
        /// <c language="csharp">ShellLinkInfo</c> cannot.
        /// </remarks>
        [Fact]
        public void InternetShortcutInfo_GetMatchesALoadedSnapshot()
        {
            StaThread.Run(static () =>
            {
                // Arrange
                using TempDirectory temp = new();
                string shortcutPath = temp.GetPath("accessor.url");
                using (InternetShortcutFile created = InternetShortcutFile.Create(Url))
                {
                    created.Description = "A test shortcut";
                    created.Save(shortcutPath);
                }

                // Act
                InternetShortcutInfo direct = InternetShortcutInfo.Get(shortcutPath);

                // Assert
                using InternetShortcutFile loaded = InternetShortcutFile.Load(shortcutPath);
                InternetShortcutInfo snapshot = loaded.GetInfoSnapshot();
                Assert.Equal(snapshot.FilePath.FullName, direct.FilePath.FullName);
                Assert.Equal(snapshot.Url, direct.Url);
                Assert.Equal(snapshot.Description, direct.Description);
            });
        }

        /// <summary>
        /// The address used throughout, chosen so nothing is ever resolved over the network.
        /// </summary>
        private static Uri Url { get; } = new("https://psappdeploytoolkit.com/");

        /// <summary>
        /// A file that exists on every Windows installation, used as an icon source.
        /// </summary>
        private static string IconPath { get; } = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.System), "shell32.dll");
    }
}
