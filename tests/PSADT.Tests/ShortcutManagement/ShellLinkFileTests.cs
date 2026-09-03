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
        /// Only these three do. <c language="csharp">IShellLink::SetShowCmd</c> accepts a normal window, a maximized one
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
        /// Verifies that clearing an icon location is carried into the saved file, and not just into the
        /// link that is open. Saving and loading is the whole point of the test: clearing it reads back as
        /// gone either way, and it is only the file that tells you whether the clearing survived.
        /// </summary>
        [Fact]
        public void IconLocation_StaysClearedAcrossASave()
        {
            // Arrange
            using TempDirectory temp = new();
            string linkPath = temp.GetPath("clearedicon.lnk");
            using (ShellLinkFile created = ShellLinkFile.Create(TargetPath))
            {
                created.IconLocation = new FileInfo(TargetPath);
                created.IconIndex = 2;
                created.Save(linkPath);
            }

            // Act
            using (ShellLinkFile opened = ShellLinkFile.Load(linkPath, Interop.STGM.STGM_READWRITE))
            {
                opened.IconLocation = null;
                opened.IconIndex = null;
                opened.Save();
            }

            // Assert
            using ShellLinkFile loaded = ShellLinkFile.Load(linkPath);
            Assert.False(loaded.HasIconLocation);
            Assert.Null(loaded.IconLocation);
            Assert.Null(loaded.IconIndex);
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
        /// Verifies that two snapshots of one unchanged shortcut compare equal, which is what the type
        /// being a record promises a caller.
        /// </summary>
        /// <remarks>
        /// Worth its own test because it used not to hold. The paths a snapshot carries are exposed as
        /// <see cref="FileInfo"/>, which does not override equality, so holding them directly made the
        /// generated comparison a reference comparison and no two snapshots ever matched - while the
        /// generated <c language="csharp">ToString</c> rendered them identically, which made it thoroughly confusing to run
        /// into. They are recorded as paths and rebuilt on read instead.
        /// </remarks>
        [Fact]
        public void ShellLinkInfo_ComparesByValue()
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

            // Assert: identical content, identical when printed, and equal
            Assert.Equal(first.ToString(), second.ToString());
            Assert.Equal(first, second);
            Assert.Equal(first.GetHashCode(), second.GetHashCode());
        }

        /// <summary>
        /// Verifies that every link flag a caller can set survives a save and a load.
        /// </summary>
        /// <remarks>
        /// These are single bits in one word rather than separate values, so the failure worth guarding
        /// against is a mask that overlaps another - setting one and finding a second has come on with
        /// it, or setting several and finding the last write cleared the earlier ones. Asserting them
        /// together is what catches that; asserting them one at a time would not.
        /// <para>
        /// The flags are set in one pass and read back after a round trip, so both the writing side and
        /// the shell's own persistence of the word are covered.
        /// </para>
        /// </remarks>
        [Fact]
        public void Save_RoundTripsEverySettableLinkFlag()
        {
            // Arrange
            using TempDirectory temp = new();
            string linkPath = temp.GetPath("flags.lnk");

            // Act
            using (ShellLinkFile created = ShellLinkFile.Create(TargetPath))
            {
                created.ForceNoLinkInfo = true;
                created.RunInSeparate = true;
                created.RunAsAdmin = true;
                created.NoPidlAlias = true;
                created.ForceUncName = true;
                created.RunWithShimLayer = true;
                created.ForceNoLinkTrack = true;
                created.EnableTargetMetadata = true;
                created.DisableLinkPathTracking = true;
                created.DisableKnownFolderRelativeTracking = true;
                created.NoKnownFolderAlias = true;
                created.AllowLinkToLink = true;
                created.UnaliasOnSave = true;
                created.PreferEnvironmentPath = true;
                created.KeepLocalIdListForUncTarget = true;
                created.Save(linkPath);
            }

            // Assert
            using ShellLinkFile loaded = ShellLinkFile.Load(linkPath);
            Assert.True(loaded.ForceNoLinkInfo);
            Assert.True(loaded.RunInSeparate);
            Assert.True(loaded.RunAsAdmin);
            Assert.True(loaded.NoPidlAlias);
            Assert.True(loaded.ForceUncName);
            Assert.True(loaded.RunWithShimLayer);
            Assert.True(loaded.ForceNoLinkTrack);
            Assert.True(loaded.EnableTargetMetadata);
            Assert.True(loaded.DisableLinkPathTracking);
            Assert.True(loaded.DisableKnownFolderRelativeTracking);
            Assert.True(loaded.NoKnownFolderAlias);
            Assert.True(loaded.AllowLinkToLink);
            Assert.True(loaded.UnaliasOnSave);
            Assert.True(loaded.PreferEnvironmentPath);
            Assert.True(loaded.KeepLocalIdListForUncTarget);
        }

        /// <summary>
        /// Verifies that a link with none of those flags set reports none of them, so the reading side
        /// is not simply answering yes.
        /// </summary>
        [Fact]
        public void Save_LeavesEverySettableLinkFlagOffWhenItWasNotSet()
        {
            // Arrange
            using TempDirectory temp = new();
            string linkPath = temp.GetPath("noflags.lnk");

            // Act
            using (ShellLinkFile created = ShellLinkFile.Create(TargetPath))
            {
                created.Save(linkPath);
            }

            // Assert
            using ShellLinkFile loaded = ShellLinkFile.Load(linkPath);
            Assert.False(loaded.ForceNoLinkInfo);
            Assert.False(loaded.RunInSeparate);
            Assert.False(loaded.RunAsAdmin);
            Assert.False(loaded.NoPidlAlias);
            Assert.False(loaded.ForceUncName);
            Assert.False(loaded.RunWithShimLayer);
            Assert.False(loaded.ForceNoLinkTrack);
            Assert.False(loaded.EnableTargetMetadata);
            Assert.False(loaded.DisableLinkPathTracking);
            Assert.False(loaded.DisableKnownFolderRelativeTracking);
            Assert.False(loaded.NoKnownFolderAlias);
            Assert.False(loaded.AllowLinkToLink);
            Assert.False(loaded.UnaliasOnSave);
            Assert.False(loaded.PreferEnvironmentPath);
            Assert.False(loaded.KeepLocalIdListForUncTarget);
        }

        /// <summary>
        /// Verifies that setting one flag does not set any of the others, which is what a mistaken mask
        /// would look like.
        /// </summary>
        [Fact]
        public void LinkFlags_AreIndependentOfOneAnother()
        {
            // Act
            using ShellLinkFile link = ShellLinkFile.Create(TargetPath);
            link.RunAsAdmin = true;

            // Assert
            Assert.True(link.RunAsAdmin);
            Assert.False(link.RunInSeparate);
            Assert.False(link.ForceNoLinkInfo);
            Assert.False(link.NoPidlAlias);
            Assert.False(link.PreferEnvironmentPath);

            // Assert: and clearing it leaves the others where they were
            link.RunInSeparate = true;
            link.RunAsAdmin = false;
            Assert.False(link.RunAsAdmin);
            Assert.True(link.RunInSeparate);
        }

        /// <summary>
        /// Verifies that the flags describing what a link carries agree with what it reports, since a
        /// caller deciding whether to read a value consults them first.
        /// </summary>
        [Fact]
        public void ContentFlags_AgreeWithTheValuesTheyDescribe()
        {
            // Arrange
            using TempDirectory temp = new();
            string linkPath = temp.GetPath("content.lnk");
            using (ShellLinkFile created = ShellLinkFile.Create(TargetPath))
            {
                created.Arguments = "/a /b";
                created.WorkingDirectory = WorkingDirectory;
                created.Save(linkPath);
            }

            // Act
            using ShellLinkFile loaded = ShellLinkFile.Load(linkPath);

            // Assert: what was written is reported as present
            Assert.True(loaded.HasArguments);
            Assert.True(loaded.HasWorkingDirectory);

            // Assert: and the remaining descriptions agree with the values beside them
            Assert.Equal(loaded.HasIconLocation, loaded.IconLocation is not null);
            Assert.Equal(loaded.HasName, !string.IsNullOrWhiteSpace(loaded.Description));

            // Assert: a link the shell wrote is a Unicode one and carries an identifier list for its target
            Assert.True(loaded.IsUnicode);
            Assert.True(loaded.HasIdList);

            // Assert: nothing here asked for any of these, so none of them should be reported
            Assert.False(loaded.HasDarwinId);
            Assert.False(loaded.HasExpandedIconSize);
            Assert.False(loaded.HasExpandableStrings);
        }

        /// <summary>
        /// Verifies that the application identity properties round-trip, since these are what place a
        /// shortcut correctly on the start menu and in a jump list.
        /// </summary>
        /// <remarks>
        /// Read back on the instance that set them rather than after a save. They live in the link's
        /// property store rather than in the link structure itself, and the store is what is being
        /// exercised - a value that goes in and comes out again has been through both halves of it.
        /// </remarks>
        [Fact]
        public void AppUserModelProperties_RoundTripThroughThePropertyStore()
        {
            // Arrange
            Guid toastActivator = new(0x6C4B96B4, 0x4C88, 0x4B9B, 0x9F, 0x1C, 0x5B, 0x9E, 0x1E, 0x5B, 0x9E, 0x1C);

            // Act
            using ShellLinkFile link = ShellLinkFile.Create(TargetPath);
            link.AppUserModelId = "Contoso.App";
            link.AppUserModelExcludeFromShowInNewInstall = true;
            link.AppUserModelIsDestListSeparator = true;
            link.AppUserModelIsDualMode = true;
            link.AppUserModelPreventPinning = true;
            link.AppUserModelRelaunchCommand = @"C:\Program Files\App\app.exe /relaunch";
            link.AppUserModelRelaunchDisplayNameResource = "Contoso Application";
            link.AppUserModelRelaunchIconResource = @"C:\Program Files\App\app.exe,0";
            link.AppUserModelStartPinOption = 1;
            link.AppUserModelToastActivatorClsid = toastActivator;

            // Assert
            Assert.Equal("Contoso.App", link.AppUserModelId);
            Assert.True(link.AppUserModelExcludeFromShowInNewInstall);
            Assert.True(link.AppUserModelIsDestListSeparator);
            Assert.True(link.AppUserModelIsDualMode);
            Assert.True(link.AppUserModelPreventPinning);
            Assert.Equal(@"C:\Program Files\App\app.exe /relaunch", link.AppUserModelRelaunchCommand);
            Assert.Equal("Contoso Application", link.AppUserModelRelaunchDisplayNameResource);
            Assert.Equal(@"C:\Program Files\App\app.exe,0", link.AppUserModelRelaunchIconResource);
            Assert.Equal(1u, link.AppUserModelStartPinOption);
            Assert.Equal(toastActivator, link.AppUserModelToastActivatorClsid);
        }

        /// <summary>
        /// Verifies that a link carrying no application identity reports none, rather than empty values
        /// a caller would take for a configured one.
        /// </summary>
        [Fact]
        public void AppUserModelProperties_AreAbsentWhenNotSet()
        {
            // Act
            using ShellLinkFile link = ShellLinkFile.Create(TargetPath);

            // Assert
            Assert.Null(link.AppUserModelId);
            Assert.Null(link.AppUserModelRelaunchCommand);
            Assert.Null(link.AppUserModelRelaunchDisplayNameResource);
            Assert.Null(link.AppUserModelRelaunchIconResource);
        }

        /// <summary>
        /// Verifies that a relative path can be recorded, which is what lets a shortcut on removable
        /// media find its target after the drive letter has changed.
        /// </summary>
        [Fact]
        public void SetRelativePath_IsRecordedOnTheLink()
        {
            // Arrange
            using TempDirectory temp = new();
            string linkPath = temp.GetPath("relative.lnk");

            // Act
            using (ShellLinkFile created = ShellLinkFile.Create(TargetPath))
            {
                created.SetRelativePath(TargetPath);
                created.Save(linkPath);
            }

            // Assert
            using ShellLinkFile loaded = ShellLinkFile.Load(linkPath);
            Assert.True(loaded.HasRelativePath);
        }

        /// <summary>
        /// Verifies that resolving a link whose target is where it was left changes nothing about it.
        /// </summary>
        /// <remarks>
        /// Asked for without any interface and without searching, so the shell answers from what the
        /// link already holds rather than going looking - which is what a deployment wants when it is
        /// reading a shortcut rather than repairing one.
        /// </remarks>
        [Fact]
        public void Resolve_LeavesAnIntactLinkAlone()
        {
            // Arrange
            using TempDirectory temp = new();
            string linkPath = temp.GetPath("resolve.lnk");
            using (ShellLinkFile created = ShellLinkFile.Create(TargetPath))
            {
                created.Save(linkPath);
            }

            // Act
            using ShellLinkFile loaded = ShellLinkFile.Load(linkPath);
            loaded.Resolve();

            // Assert
            Assert.Equal(TargetPath, loaded.TargetPath, ignoreCase: true);
        }

        /// <summary>
        /// Verifies that a link opened read-only says it cannot be saved, and one opened for writing
        /// says it can.
        /// </summary>
        [Fact]
        public void CanSave_ReflectsTheStorageModeItWasOpenedWith()
        {
            // Arrange
            using TempDirectory temp = new();
            string linkPath = temp.GetPath("cansave.lnk");
            using (ShellLinkFile created = ShellLinkFile.Create(TargetPath))
            {
                created.Save(linkPath);
            }

            // Act & Assert
            using (ShellLinkFile readOnly = ShellLinkFile.Load(linkPath))
            {
                Assert.False(readOnly.CanSave);
            }
            using ShellLinkFile writable = ShellLinkFile.Load(linkPath, Interop.STGM.STGM_READWRITE);
            Assert.True(writable.CanSave);
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
