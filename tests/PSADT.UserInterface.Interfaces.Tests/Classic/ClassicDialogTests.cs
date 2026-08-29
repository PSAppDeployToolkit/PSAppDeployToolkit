using System;
using System.Collections;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using PSADT.UserInterface.DialogOptions;
using PSADT.UserInterface.DialogResults;
using PSADT.UserInterface.Interfaces.Classic;
using PSADT.UserInterface.Interfaces.Tests.TestHelpers;
using Xunit;

namespace PSADT.UserInterface.Interfaces.Tests.Classic
{
    /// <summary>
    /// Tests the base every Classic dialog derives from.
    /// </summary>
    /// <remarks>
    /// The base cannot be constructed from outside its own hierarchy, so these tests derive a dialog of
    /// their own from it. That is worth the few lines: it isolates what the base does from what any real
    /// dialog adds on top, and because friend access satisfies the internal half of
    /// <c>private protected</c>, a derived type here reaches the tag stripper, the countdown formatter,
    /// the close guard and the window procedure directly rather than by reflection.
    /// <para>
    /// No dialog is ever shown. Everything asserted on is applied by the constructor or reachable by
    /// handing the dialog a message, and showing one would need a real desktop, block on a message loop,
    /// and put a window in front of whoever is running the suite.
    /// </para>
    /// </remarks>
    public sealed class ClassicDialogTests
    {
        /// <summary>
        /// Verifies that the title is taken from the options with any markup removed.
        /// </summary>
        /// <remarks>
        /// A title bar cannot render markup, so a tag left in would be shown to the user verbatim.
        /// </remarks>
        [Fact]
        public void Constructor_StripsMarkupFromTheTitle()
        {
            // Arrange
            Hashtable table = SampleOptions.CustomDialog();
            table["AppTitle"] = "Install [bold]Contoso[/bold] 1.2";

            // Act
            using ProbeDialog dialog = Build(table);

            // Assert
            Assert.Equal("Install Contoso 1.2", dialog.Text);
        }

        /// <summary>
        /// Verifies that the topmost setting is carried through.
        /// </summary>
        /// <param name="topMost">Whether the dialog should sit above other windows.</param>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Constructor_CarriesTheTopMostSetting(bool topMost)
        {
            // Arrange
            Hashtable table = SampleOptions.CustomDialog();
            table["DialogTopMost"] = topMost;

            // Act
            using ProbeDialog dialog = Build(table);

            // Assert
            Assert.Equal(topMost, dialog.TopMost);
        }

        /// <summary>
        /// Verifies that the expiry and persist timers are left dormant when no duration was given.
        /// </summary>
        /// <remarks>
        /// Both timers are created by the designer with an interval of <see cref="int.MaxValue"/>, and
        /// that value is what the load handler tests to decide whether to start them. The sentinel is
        /// load-bearing rather than decorative: a timer left at some other interval by an options type
        /// that stopped defaulting would silently start and close the dialog under the user.
        /// </remarks>
        [Fact]
        public void Constructor_LeavesTheTimersDormantWithoutDurations()
        {
            // Act
            using ProbeDialog dialog = Build(SampleOptions.CustomDialog());

            // Assert
            Assert.Equal(int.MaxValue, NonPublic.Field<Timer>(dialog, "expiryTimer").Interval);
            Assert.Equal(int.MaxValue, NonPublic.Field<Timer>(dialog, "persistTimer").Interval);
        }

        /// <summary>
        /// Verifies that a given expiry and persist duration reach their timers as milliseconds.
        /// </summary>
        [Fact]
        public void Constructor_SetsTheTimerIntervalsFromTheDurations()
        {
            // Arrange
            Hashtable table = SampleOptions.CustomDialog();
            table["DialogExpiryDuration"] = TimeSpan.FromMinutes(3);
            table["DialogPersistInterval"] = TimeSpan.FromSeconds(45);

            // Act
            using ProbeDialog dialog = Build(table);

            // Assert
            Assert.Equal(180_000, NonPublic.Field<Timer>(dialog, "expiryTimer").Interval);
            Assert.Equal(45_000, NonPublic.Field<Timer>(dialog, "persistTimer").Interval);
        }

        /// <summary>
        /// Verifies that the caption carries neither a minimize nor a maximize button by default.
        /// </summary>
        /// <remarks>
        /// A dialog the deployment wants answered must not offer the user a way to put it aside, so
        /// neither box is present unless minimizing was asked for. The close button is a separate
        /// matter: it stays in the caption and is greyed out through the system menu when the dialog
        /// loads, rather than being removed.
        /// </remarks>
        [Fact]
        public void Constructor_LeavesTheCaptionWithoutMinimizeOrMaximize()
        {
            // Act
            using ProbeDialog dialog = Build(SampleOptions.CustomDialog());

            // Assert
            Assert.False(dialog.MinimizeBox);
            Assert.False(dialog.MaximizeBox);
        }

        /// <summary>
        /// Records that a dialog appears in the taskbar whether or not minimizing was allowed.
        /// </summary>
        /// <remarks>
        /// Recorded rather than endorsed. The comment above the opt-in block describes turning the
        /// taskbar button on, and the control box with it, as part of enabling minimizing - but nothing
        /// ever turns either off, and both properties start out true on any form. So four of that
        /// block's five statements assign the value the property already held, and the only one that
        /// changes anything is the minimize box itself.
        /// <para>
        /// Whether that matters is a product question rather than a test one: a dialog that cannot be
        /// minimized arguably has no business owning a taskbar button, but having one also makes a
        /// dialog that got buried findable. This pins today's answer so that changing it is a decision
        /// somebody makes rather than something that drifts.
        /// </para>
        /// </remarks>
        /// <param name="allowMinimize">Whether minimizing was asked for.</param>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Constructor_ShowsInTheTaskbarEitherWay(bool allowMinimize)
        {
            // Arrange
            Hashtable table = SampleOptions.CustomDialog();
            table["DialogAllowMinimize"] = allowMinimize;

            // Act
            using ProbeDialog dialog = Build(table);

            // Assert
            Assert.True(dialog.ShowInTaskbar);
            Assert.True(dialog.ControlBox);
        }

        /// <summary>
        /// Verifies that opting in to minimizing turns on everything needed for it to work.
        /// </summary>
        /// <remarks>
        /// Five settings have to move together, which is why this lives on the base rather than being
        /// repeated per dialog. The border style is the non-obvious one: Windows will not draw a
        /// minimize glyph on a fixed-dialog frame however the box is set, so the frame has to change
        /// too. And without the taskbar button, a dialog the user minimized could not be got back.
        /// </remarks>
        [Fact]
        public void Constructor_TurnsOnEverythingMinimizingNeeds()
        {
            // Arrange
            Hashtable table = SampleOptions.CustomDialog();
            table["DialogAllowMinimize"] = true;

            // Act
            using ProbeDialog dialog = Build(table);

            // Assert
            Assert.True(dialog.ControlBox);
            Assert.True(dialog.MinimizeBox);
            Assert.False(dialog.MaximizeBox);
            Assert.True(dialog.ShowInTaskbar);
            Assert.Equal(FormBorderStyle.FixedSingle, dialog.FormBorderStyle);
        }

        /// <summary>
        /// Verifies that a dialog refuses to close until it is told it may.
        /// </summary>
        /// <remarks>
        /// The guard is what the closing handler reads to cancel a close the user started - from the
        /// taskbar, or with Alt+F4 - so that the only way out of the dialog is a button that records an
        /// answer.
        /// </remarks>
        [Fact]
        public void CloseDialog_IsWhatPermitsClosing()
        {
            // Arrange
            using ProbeDialog dialog = Build(SampleOptions.CustomDialog());

            // Act & Assert
            Assert.False(dialog.MayClose());
            DialogHost.Run(dialog.CloseDialog);
            Assert.True(dialog.MayClose());
        }

        /// <summary>
        /// Verifies that the dialog result starts at the value the caller supplied.
        /// </summary>
        [Fact]
        public void Constructor_StartsAtTheSuppliedResult()
        {
            // Act
            using ProbeDialog dialog = Build(SampleOptions.CustomDialog());

            // Assert
            Assert.Equal(CustomDialogResult.DefaultResult, dialog.DialogResult);
        }

        /// <summary>
        /// Verifies that a move request is swallowed when the dialog is not allowed to move.
        /// </summary>
        /// <remarks>
        /// Removing the Move item from the system menu stops the menu route, but not a drag on the
        /// caption or the keyboard shortcut, both of which arrive as this message. Swallowing it in the
        /// window procedure is what actually pins the dialog in place.
        /// </remarks>
        [Fact]
        public void WndProc_SwallowsAMoveWhenMovingIsDisallowed()
        {
            // Arrange
            Hashtable table = SampleOptions.CustomDialog();
            table["DialogAllowMove"] = false;
            using ProbeDialog dialog = Build(table);

            // Act
            dialog.SendSystemCommand(SystemCommandMove);

            // Assert
            Assert.Equal(0, dialog.SystemCommandsReachingWindows);
        }

        /// <summary>
        /// Verifies that a move request is passed on when the dialog is allowed to move.
        /// </summary>
        /// <remarks>
        /// Moving is the default, so this is the behaviour most dialogs get.
        /// </remarks>
        [Fact]
        public void WndProc_PassesAMoveOnWhenMovingIsAllowed()
        {
            // Arrange
            Hashtable table = SampleOptions.CustomDialog();
            table["DialogAllowMove"] = true;
            using ProbeDialog dialog = Build(table);

            // Act
            dialog.SendSystemCommand(SystemCommandMove);

            // Assert
            Assert.Equal(1, dialog.SystemCommandsReachingWindows);
        }

        /// <summary>
        /// Verifies that the low bits Windows uses for its own purposes do not hide a move.
        /// </summary>
        /// <remarks>
        /// The documentation for this message reserves the low four bits, so the guard masks them off
        /// before comparing. A guard that compared the raw value would let a move straight through
        /// whenever Windows used them - which it does when the command came from a mouse or an
        /// accelerator - and the dialog would be draggable after all.
        /// </remarks>
        /// <param name="lowBits">The value Windows put in the reserved low bits.</param>
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(15)]
        public void WndProc_SwallowsAMoveWhateverTheReservedBitsHold(int lowBits)
        {
            // Arrange
            Hashtable table = SampleOptions.CustomDialog();
            table["DialogAllowMove"] = false;
            using ProbeDialog dialog = Build(table);

            // Act
            dialog.SendSystemCommand(SystemCommandMove | lowBits);

            // Assert
            Assert.Equal(0, dialog.SystemCommandsReachingWindows);
        }

        /// <summary>
        /// Verifies that a system command which is not a move is passed on even when moving is
        /// disallowed.
        /// </summary>
        /// <remarks>
        /// The other half of the masking pair. A guard that masked too widely would swallow the
        /// neighbouring commands, closing among them, and the dialog would stop responding to its own
        /// system menu.
        /// </remarks>
        [Fact]
        public void WndProc_PassesOnCommandsThatAreNotMoves()
        {
            // Arrange
            Hashtable table = SampleOptions.CustomDialog();
            table["DialogAllowMove"] = false;
            using ProbeDialog dialog = Build(table);

            // Act
            dialog.SendSystemCommand(SystemCommandContextHelp);

            // Assert
            Assert.Equal(1, dialog.SystemCommandsReachingWindows);
        }

        /// <summary>
        /// Verifies that an elapsed time is rendered as hours, minutes and seconds.
        /// </summary>
        /// <remarks>
        /// Hours are the largest unit rather than the largest that fits, so a countdown spanning more
        /// than a day reads as the hours remaining rather than resetting to a small number beside a day
        /// count the label has nowhere to show.
        /// </remarks>
        /// <param name="days">Days remaining.</param>
        /// <param name="hours">Hours remaining.</param>
        /// <param name="minutes">Minutes remaining.</param>
        /// <param name="seconds">Seconds remaining.</param>
        /// <param name="expected">The text the label should carry.</param>
        [Theory]
        [InlineData(0, 0, 0, 0, "0:00:00")]
        [InlineData(0, 0, 0, 9, "0:00:09")]
        [InlineData(0, 0, 5, 30, "0:05:30")]
        [InlineData(0, 2, 5, 30, "2:05:30")]
        [InlineData(1, 0, 0, 0, "24:00:00")]
        [InlineData(3, 4, 5, 6, "76:05:06")]
        public void FormatTime_RendersHoursMinutesAndSeconds(int days, int hours, int minutes, int seconds, string expected)
        {
            // Act & Assert
            Assert.Equal(expected, ProbeDialog.Format(new TimeSpan(days, hours, minutes, seconds)));
        }

        /// <summary>
        /// Verifies that the style tags are removed and the text between them kept.
        /// </summary>
        /// <param name="text">The text the deployment supplied.</param>
        /// <param name="expected">What the user should end up seeing.</param>
        [Theory]
        [InlineData("[bold]loud[/bold]", "loud")]
        [InlineData("[italic]leaning[/italic]", "leaning")]
        [InlineData("[accent]coloured[/accent]", "coloured")]
        [InlineData("before [bold]middle[/bold] after", "before middle after")]
        [InlineData("[bold]outer [italic]inner[/italic] outer[/bold]", "outer inner outer")]
        [InlineData("[bold][accent][italic]all three[/italic][/accent][/bold]", "all three")]
        public void StripFormattingTags_RemovesStyleTags(string text, string expected)
        {
            // Act & Assert
            Assert.Equal(expected, ProbeDialog.StripTags(text));
        }

        /// <summary>
        /// Verifies that a link is reduced to the text a user would have clicked.
        /// </summary>
        /// <remarks>
        /// A Windows Forms label cannot carry a hyperlink, so the two link forms collapse differently: a
        /// bare link keeps its address, because the address was the visible text, while a described link
        /// keeps the description and drops the address entirely. The second is the one worth pinning -
        /// the user loses the address, which is the right trade for a control that could not have made
        /// it clickable anyway.
        /// </remarks>
        /// <param name="text">The text the deployment supplied.</param>
        /// <param name="expected">What the user should end up seeing.</param>
        [Theory]
        [InlineData("[url]https://example.test/help[/url]", "https://example.test/help")]
        [InlineData("see [url]https://example.test[/url] first", "see https://example.test first")]
        [InlineData("[url=https://example.test/help]the help page[/url]", "the help page")]
        [InlineData("read [url=https://example.test]this[/url] first", "read this first")]
        public void StripFormattingTags_ReducesALinkToItsVisibleText(string text, string expected)
        {
            // Act & Assert
            Assert.Equal(expected, ProbeDialog.StripTags(text));
        }

        /// <summary>
        /// Verifies that text carrying no tags is returned unchanged.
        /// </summary>
        /// <param name="text">The text the deployment supplied.</param>
        [Theory]
        [InlineData("")]
        [InlineData("nothing to do here")]
        [InlineData("[notatag]left alone[/notatag]")]
        [InlineData("100% [ of things ] are fine")]
        public void StripFormattingTags_LeavesTextWithoutTagsAlone(string text)
        {
            // Act & Assert
            Assert.Equal(text, ProbeDialog.StripTags(text));
        }

        /// <summary>
        /// Verifies that an unbalanced tag is still removed.
        /// </summary>
        /// <remarks>
        /// The opening and closing tags are matched separately rather than as a pair, so a deployment
        /// that forgot a closing tag gets its text shown without markup rather than with a stray
        /// <c>[bold]</c> in the middle of it.
        /// </remarks>
        /// <param name="text">The text the deployment supplied.</param>
        /// <param name="expected">What the user should end up seeing.</param>
        [Theory]
        [InlineData("[bold]never closed", "never closed")]
        [InlineData("never opened[/bold]", "never opened")]
        [InlineData("[bold]crossed [italic]over[/bold] here[/italic]", "crossed over here")]
        public void StripFormattingTags_RemovesAnUnbalancedTag(string text, string expected)
        {
            // Act & Assert
            Assert.Equal(expected, ProbeDialog.StripTags(text));
        }

        /// <summary>
        /// Verifies that a tag repeated in the text is removed everywhere it appears.
        /// </summary>
        /// <remarks>
        /// Each match is removed by replacing its literal text, which takes out every occurrence at once
        /// and makes the remaining matches for that tag do nothing. This is the case that would fail if
        /// the removal ever moved to using the positions the matches carry, since those describe the
        /// text as it was before any replacement had shortened it.
        /// </remarks>
        [Fact]
        public void StripFormattingTags_RemovesEveryOccurrenceOfARepeatedTag()
        {
            // Act & Assert
            Assert.Equal("one two three", ProbeDialog.StripTags("[bold]one[/bold] [bold]two[/bold] [bold]three[/bold]"));
        }

        /// <summary>
        /// Verifies that an icon is produced from a path on disk.
        /// </summary>
        [Fact]
        public void GetIcon_ReadsAnIconFromDisk()
        {
            // Arrange
            using TempDirectory directory = new();
            string path = directory.WriteFile("app.ico", TestImages.IcoBytes(32));

            // Act & Assert
            Assert.NotNull(ClassicDialog.GetIcon(path));
        }

        /// <summary>
        /// Verifies that an image which is not an icon is converted into one.
        /// </summary>
        /// <remarks>
        /// Deployments configure their logo as a PNG far more often than as an ICO, and a form's Icon
        /// property will not take a bitmap. The conversion is what makes the common case work at all.
        /// </remarks>
        [Fact]
        public void GetIcon_ConvertsAnImageThatIsNotAnIcon()
        {
            // Arrange
            using TempDirectory directory = new();
            string path = directory.WriteFile("logo.png", TestImages.PngBytes(64, 64));

            // Act & Assert
            Assert.NotNull(ClassicDialog.GetIcon(path));
        }

        /// <summary>
        /// Verifies that an icon can come from base64 text rather than a file.
        /// </summary>
        /// <remarks>
        /// This is the form the client/server payload carries, because the dialog is shown in the user's
        /// session by a process that may not be able to read the deployment's directory.
        /// </remarks>
        [Fact]
        public void GetIcon_ReadsAnIconFromBase64()
        {
            // Act & Assert
            Assert.NotNull(ClassicDialog.GetIcon(TestImages.PngBase64(48, 48)));
        }

        /// <summary>
        /// Verifies that the same path gives back the same icon rather than loading it again.
        /// </summary>
        /// <remarks>
        /// Every dialog in a deployment asks for the same icon, so without the cache the image would be
        /// decoded once per dialog. Reference equality is the assertion because that is what the cache
        /// promises - and it is also why a caller must never dispose what it gets back.
        /// </remarks>
        [Fact]
        public void GetIcon_GivesBackTheSameIconForTheSamePath()
        {
            // Arrange
            using TempDirectory directory = new();
            string path = directory.WriteFile("cached.png", TestImages.PngBytes(32, 32));

            // Act
            Icon first = ClassicDialog.GetIcon(path);
            Icon second = ClassicDialog.GetIcon(path);

            // Assert
            Assert.Same(first, second);
        }

        /// <summary>
        /// Verifies that the cache ignores case in the path.
        /// </summary>
        /// <remarks>
        /// Windows paths are case insensitive, so two spellings of one file are one file and should not
        /// be decoded twice.
        /// </remarks>
        [Fact]
        public void GetIcon_TreatsPathCaseAsInsignificant()
        {
            // Arrange
            using TempDirectory directory = new();
            string path = directory.WriteFile("MixedCase.png", TestImages.PngBytes(32, 32));

            // Act
            Icon first = ClassicDialog.GetIcon(path);
            Icon second = ClassicDialog.GetIcon(path.ToUpperInvariant());

            // Assert
            Assert.Same(first, second);
        }

        /// <summary>
        /// Verifies that a missing file is reported rather than swallowed.
        /// </summary>
        [Fact]
        public void GetIcon_ReportsAMissingFile()
        {
            // Arrange
            using TempDirectory directory = new();
            string path = directory.GetPath("absent.png");

            // Act & Assert
            _ = Assert.Throws<FileNotFoundException>(() => ClassicDialog.GetIcon(path));
        }

        /// <summary>
        /// Verifies that a banner is produced from a path on disk at its original size.
        /// </summary>
        [Fact]
        public void GetBanner_ReadsABitmapFromDisk()
        {
            // Arrange
            using TempDirectory directory = new();
            string path = directory.WriteFile("banner.png", TestImages.PngBytes(450, 100));

            // Act
            Bitmap banner = ClassicDialog.GetBanner(path);

            // Assert
            Assert.Equal(450, banner.Width);
            Assert.Equal(100, banner.Height);
        }

        /// <summary>
        /// Verifies that a banner can come from base64 text rather than a file.
        /// </summary>
        [Fact]
        public void GetBanner_ReadsABitmapFromBase64()
        {
            // Act
            Bitmap banner = ClassicDialog.GetBanner(TestImages.PngBase64(300, 60));

            // Assert
            Assert.Equal(300, banner.Width);
            Assert.Equal(60, banner.Height);
        }

        /// <summary>
        /// Verifies that the same path gives back the same banner rather than loading it again.
        /// </summary>
        [Fact]
        public void GetBanner_GivesBackTheSameBitmapForTheSamePath()
        {
            // Arrange
            using TempDirectory directory = new();
            string path = directory.WriteFile("cached-banner.png", TestImages.PngBytes(200, 50));

            // Act
            Bitmap first = ClassicDialog.GetBanner(path);
            Bitmap second = ClassicDialog.GetBanner(path);

            // Assert
            Assert.Same(first, second);
        }

        /// <summary>
        /// Verifies that bytes which are not an image are reported rather than swallowed.
        /// </summary>
        [Fact]
        public void GetBanner_ReportsSomethingThatIsNotAnImage()
        {
            // Arrange
            using TempDirectory directory = new();
            string path = directory.WriteFile("prose.png", TestImages.NotAnImage());

            // Act & Assert
            _ = Assert.Throws<ArgumentException>(() => ClassicDialog.GetBanner(path));
        }

        /// <summary>
        /// The system command asking a window to move, as it arrives in the window procedure.
        /// </summary>
        private const int SystemCommandMove = 0xF010;

        /// <summary>
        /// A system command that is not a move, used to show the guard does not swallow its neighbours.
        /// </summary>
        private const int SystemCommandContextHelp = 0xF180;

        /// <summary>
        /// Builds a probe dialog on the shared apartment from the given options.
        /// </summary>
        /// <param name="table">The options to build it from.</param>
        /// <returns>The dialog, which the caller owns.</returns>
        private static ProbeDialog Build(Hashtable table)
        {
            return DialogHost.Run(() => new ProbeDialog(new CustomDialogOptions(table)));
        }

        /// <summary>
        /// A dialog that adds nothing to the base except a way to reach it.
        /// </summary>
        /// <remarks>
        /// Deriving is what makes the base's <c>private protected</c> members reachable without
        /// reflection. It also isolates the base: a real dialog would bring its own controls and its own
        /// constructor logic, and an assertion here would no longer be about the base alone.
        /// <para>
        /// The default window procedure is intercepted rather than merely counted. A system command that
        /// reached the real one would be acted on - and a move command in particular puts Windows into a
        /// modal drag loop that waits for input nobody is going to give it, hanging the run. Counting and
        /// dropping gives the same signal with none of that risk, and it is enough: whether the base
        /// procedure was reached at all is exactly what the guard decides.
        /// </para>
        /// </remarks>
        private sealed class ProbeDialog : ClassicDialog
        {
            /// <summary>
            /// Initializes a probe over the given options.
            /// </summary>
            /// <param name="options">The options to apply.</param>
            internal ProbeDialog(BaseDialogOptions options) : base(options, CustomDialogResult.DefaultResult)
            {
            }

            /// <summary>
            /// How many system commands got past the dialog's window procedure.
            /// </summary>
            public int SystemCommandsReachingWindows { get; private set; }

            /// <summary>
            /// Strips the formatting tags from a string.
            /// </summary>
            /// <param name="text">The text to strip.</param>
            /// <returns>The stripped text.</returns>
            public static string StripTags(string text)
            {
                return StripFormattingTags(text);
            }

            /// <summary>
            /// Formats an elapsed time the way a countdown label wants it.
            /// </summary>
            /// <param name="elapsed">The time to format.</param>
            /// <returns>The formatted text.</returns>
            public static string Format(TimeSpan elapsed)
            {
                return FormatTime(elapsed);
            }

            /// <summary>
            /// Whether the dialog would presently allow itself to be closed.
            /// </summary>
            /// <returns><see langword="true"/> if it would; otherwise, <see langword="false"/>.</returns>
            public bool MayClose()
            {
                return CanClose();
            }

            /// <summary>
            /// Hands the dialog a system command as though Windows had sent it.
            /// </summary>
            /// <param name="command">The command to send.</param>
            public void SendSystemCommand(int command)
            {
                DialogHost.Run(() =>
                {
                    Message message = Message.Create(Handle, WindowMessageSysCommand, (IntPtr)command, IntPtr.Zero);
                    WndProc(ref message);
                });
            }

            /// <inheritdoc />
            protected override void DefWndProc(ref Message m)
            {
                // Count the system commands that got this far and drop them. Everything else - including
                // the messages sent while the handle is being created - has to go through untouched or
                // the window would not work at all.
                if (m.Msg == WindowMessageSysCommand)
                {
                    SystemCommandsReachingWindows++;
                    return;
                }
                base.DefWndProc(ref m);
            }

            /// <summary>
            /// The window message carrying a system command.
            /// </summary>
            private const int WindowMessageSysCommand = 0x0112;
        }
    }
}
