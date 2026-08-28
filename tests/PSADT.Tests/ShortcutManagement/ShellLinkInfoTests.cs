using System;
using System.IO;
using PSADT.ShortcutManagement;
using PSADT.Tests.TestHelpers;
using Xunit;

namespace PSADT.Tests.ShortcutManagement
{
    /// <summary>
    /// Tests the read-only description of a shell shortcut.
    /// </summary>
    /// <remarks>
    /// This is what a caller receives when it asks about a shortcut rather than opening one to edit, so
    /// the questions worth asking are whether it reports everything the shortcut holds and whether the
    /// flags describing what the shortcut carries agree with the values themselves. A flag saying there
    /// are arguments while the arguments are absent, or the reverse, would leave a caller deciding on
    /// one and reading the other.
    /// <para>
    /// Every shortcut is written into a temporary directory and removed with it, so nothing on the
    /// machine is altered.
    /// </para>
    /// </remarks>
    public sealed class ShellLinkInfoTests
    {
        /// <summary>
        /// Verifies that a shortcut's description reports back what was saved into it.
        /// </summary>
        [Fact]
        public void Get_ReportsWhatWasSaved()
        {
            // Arrange
            using TempDirectory temp = new();
            string linkPath = temp.GetPath("described.lnk");
            using (ShellLinkFile created = ShellLinkFile.Create(TargetPath))
            {
                created.Arguments = "/c exit 0";
                created.Description = "A description";
                created.WorkingDirectory = Environment.SystemDirectory;
                created.WindowStyle = ShortcutWindowStyle.Maximized;
                created.Save(linkPath);
            }

            // Act
            ShellLinkInfo info = ShellLinkInfo.Get(linkPath);

            // Assert
            Assert.Equal(linkPath, info.FilePath.FullName, StringComparer.OrdinalIgnoreCase);
            Assert.Equal(TargetPath, info.TargetPath, StringComparer.OrdinalIgnoreCase);
            Assert.Equal("/c exit 0", info.Arguments);
            Assert.Equal("A description", info.Description);
            Assert.Equal(Environment.SystemDirectory, info.WorkingDirectory, StringComparer.OrdinalIgnoreCase);
            Assert.Equal(ShortcutWindowStyle.Maximized, info.WindowStyle);
        }

        /// <summary>
        /// Verifies that the flags describing what a shortcut carries agree with what it reports, so a
        /// caller cannot be told a value is present and then handed nothing.
        /// </summary>
        [Fact]
        public void Get_FlagsAgreeWithTheValuesTheyDescribe()
        {
            // Arrange
            using TempDirectory temp = new();
            string linkPath = temp.GetPath("flags.lnk");
            using (ShellLinkFile created = ShellLinkFile.Create(TargetPath))
            {
                created.Arguments = "/c exit 0";
                created.WorkingDirectory = Environment.SystemDirectory;
                created.Save(linkPath);
            }

            // Act
            ShellLinkInfo info = ShellLinkInfo.Get(linkPath);

            // Assert
            Assert.True(info.HasArguments);
            Assert.False(string.IsNullOrWhiteSpace(info.Arguments));
            Assert.True(info.HasWorkingDirectory);
            Assert.False(string.IsNullOrWhiteSpace(info.WorkingDirectory));
            Assert.Equal(info.HasIconLocation, info.IconLocation is not null);
        }

        /// <summary>
        /// Verifies that a shortcut carrying nothing optional reports nothing rather than empty values,
        /// so a caller can tell a value that was never set from one set to nothing.
        /// </summary>
        [Fact]
        public void Get_ReportsAbsentValuesAsAbsent()
        {
            // Arrange
            using TempDirectory temp = new();
            string linkPath = temp.GetPath("bare.lnk");
            using (ShellLinkFile created = ShellLinkFile.Create(TargetPath))
            {
                created.Save(linkPath);
            }

            // Act
            ShellLinkInfo info = ShellLinkInfo.Get(linkPath);

            // Assert
            Assert.Null(info.Arguments);
            Assert.Null(info.Description);
            Assert.False(info.HasArguments);
            Assert.Null(info.AppUserModelId);
        }

        /// <summary>
        /// Verifies that the description agrees with reading the same shortcut through the editable form,
        /// since the two are different views of one file and a caller may use either.
        /// </summary>
        [Fact]
        public void Get_AgreesWithTheEditableForm()
        {
            // Arrange
            using TempDirectory temp = new();
            string linkPath = temp.GetPath("agrees.lnk");
            using (ShellLinkFile created = ShellLinkFile.Create(TargetPath))
            {
                created.Description = "A description";
                created.Save(linkPath);
            }

            // Act
            ShellLinkInfo info = ShellLinkInfo.Get(linkPath);
            using ShellLinkFile loaded = ShellLinkFile.Load(linkPath);

            // Assert
            Assert.Equal(loaded.TargetPath, info.TargetPath, StringComparer.OrdinalIgnoreCase);
            Assert.Equal(loaded.Description, info.Description);
            Assert.Equal(loaded.WindowStyle, info.WindowStyle);
        }

        /// <summary>
        /// Verifies that a shortcut that is not there is reported rather than described as an empty one.
        /// </summary>
        [Fact]
        public void Get_ReportsAShortcutThatIsNotThere()
        {
            using TempDirectory temp = new();
            Assert.NotNull(Record.Exception(() => ShellLinkInfo.Get(temp.GetPath("absent.lnk"))));
        }

        /// <summary>
        /// Verifies that a path to nothing at all is refused.
        /// </summary>
        /// <param name="filePath">The blank path to refuse.</param>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Get_RefusesABlankPath(string filePath)
        {
            _ = Assert.Throws<ArgumentException>(() => ShellLinkInfo.Get(filePath));
        }

        /// <summary>
        /// A target that is certain to exist on any machine the tests land on.
        /// </summary>
        private static readonly string TargetPath = Path.Join(Environment.SystemDirectory, "cmd.exe");
    }
}
