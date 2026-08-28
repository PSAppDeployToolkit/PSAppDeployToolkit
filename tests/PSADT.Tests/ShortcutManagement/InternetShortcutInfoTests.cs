using System;
using PSADT.ShortcutManagement;
using PSADT.Tests.TestHelpers;
using Xunit;

namespace PSADT.Tests.ShortcutManagement
{
    /// <summary>
    /// Tests the read-only description of an internet shortcut.
    /// </summary>
    /// <remarks>
    /// Every shortcut is written into a temporary directory and removed with it, so nothing on the
    /// machine is altered.
    /// <para>
    /// Anything that creates or loads one has to run on a single-threaded apartment: the shell object
    /// underneath is registered only for that apartment model, and asking for it from anywhere else
    /// comes back as the interface not being registered at all rather than as a threading complaint.
    /// </para>
    /// <para>
    /// Two of the values a shortcut can hold are not asserted here, and deliberately so: the icon comes
    /// back as a file URI where it went in as a path, and the show command comes back as nothing at all
    /// whatever was written. Both are recorded against the editable form of this type, where the
    /// behaviour originates.
    /// </para>
    /// </remarks>
    public sealed class InternetShortcutInfoTests
    {
        /// <summary>
        /// Verifies that a shortcut's description reports back what was saved into it.
        /// </summary>
        [Fact]
        public void Get_ReportsWhatWasSaved()
        {
            StaThread.Run(static () =>
            {
                // Arrange
                using TempDirectory temp = new();
                string linkPath = temp.GetPath("described.url");
                using (InternetShortcutFile created = InternetShortcutFile.Create(Url))
                {
                    created.Save(linkPath);
                }

                // Act
                InternetShortcutInfo info = InternetShortcutInfo.Get(linkPath);

                // Assert
                Assert.Equal(linkPath, info.FilePath.FullName, StringComparer.OrdinalIgnoreCase);
                Assert.Equal(Url, info.Url);
            });
        }

        /// <summary>
        /// Verifies that the description agrees with reading the same shortcut through the editable form,
        /// since the two are different views of one file and a caller may use either.
        /// </summary>
        [Fact]
        public void Get_AgreesWithTheEditableForm()
        {
            StaThread.Run(static () =>
            {
                // Arrange
                using TempDirectory temp = new();
                string linkPath = temp.GetPath("agrees.url");
                using (InternetShortcutFile created = InternetShortcutFile.Create(Url))
                {
                    created.Save(linkPath);
                }

                // Act
                InternetShortcutInfo info = InternetShortcutInfo.Get(linkPath);
                using InternetShortcutFile loaded = InternetShortcutFile.Load(linkPath);

                // Assert
                Assert.Equal(loaded.Url, info.Url);
            });
        }

        /// <summary>
        /// Verifies that a shortcut carrying nothing optional reports nothing rather than empty values.
        /// </summary>
        [Fact]
        public void Get_ReportsAbsentValuesAsAbsent()
        {
            StaThread.Run(static () =>
            {
                // Arrange
                using TempDirectory temp = new();
                string linkPath = temp.GetPath("bare.url");
                using (InternetShortcutFile created = InternetShortcutFile.Create(Url))
                {
                    created.Save(linkPath);
                }

                // Act
                InternetShortcutInfo info = InternetShortcutInfo.Get(linkPath);

                // Assert
                Assert.Null(info.WhatsNew);
                Assert.Null(info.Author);
                Assert.Null(info.Comment);
            });
        }

        /// <summary>
        /// Verifies that a shortcut that is not there is reported rather than described as an empty one.
        /// </summary>
        [Fact]
        public void Get_ReportsAShortcutThatIsNotThere()
        {
            StaThread.Run(static () =>
            {
                using TempDirectory temp = new();
                Assert.NotNull(Record.Exception(() => InternetShortcutInfo.Get(temp.GetPath("absent.url"))));
            });
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
            StaThread.Run(() => Assert.Throws<ArgumentException>(() => InternetShortcutInfo.Get(filePath)));
        }

        /// <summary>
        /// A destination for the shortcuts, which is never actually reached.
        /// </summary>
        private static readonly Uri Url = new("https://psappdeploytoolkit.com/");
    }
}
