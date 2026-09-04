using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PSADT.UserInterface.DialogOptions;
using PSADT.UserInterface.Interfaces.Fluent;
using PSADT.UserInterface.Interfaces.Tests.TestHelpers;
using Xunit;

namespace PSADT.UserInterface.Interfaces.Tests.Fluent
{
    /// <summary>
    /// Tests the base every Fluent dialog derives from.
    /// </summary>
    /// <remarks>
    /// The base is abstract and cannot be derived from outside its own assembly - its XAML refuses to
    /// load into a type from anywhere else - so unlike the Classic base there is no probe here. What
    /// needs an instance is observed through the custom dialog, the simplest concrete dialog there is,
    /// and what does not is called directly: the message formatter and the other helpers worth testing
    /// on this type are static and need no dialog at all.
    /// <para>
    /// Most of the substance is that formatter, which turns the small markup language a deployment may
    /// put in its strings into runs of formatted text and clickable links. It needs a text block and
    /// nothing else, so those tests build one and hand it over.
    /// </para>
    /// <para>
    /// No window is ever shown. The dialog parks itself far off every monitor until its first paint and
    /// only returns on screen once content has rendered, so showing one would put a real window in front
    /// of whoever is running the suite for as long as the test took.
    /// </para>
    /// </remarks>
    public sealed class FluentDialogTests
    {
        /// <summary>
        /// Verifies that text with no markup in it comes through as one plain run.
        /// </summary>
        [Fact]
        public void FormatMessage_LeavesPlainTextAlone()
        {
            // Act
            (string Text, FontWeight Weight, FontStyle Style, bool Accented) single = Assert.Single(Runs("Installing Contoso. This will take a few minutes."));

            // Assert
            Assert.Equal("Installing Contoso. This will take a few minutes.", single.Text);
            Assert.Equal(FontWeights.Normal, single.Weight);
            Assert.Equal(FontStyles.Normal, single.Style);
        }

        /// <summary>
        /// Verifies that a message of nothing produces nothing.
        /// </summary>
        /// <param name="message">The message the deployment supplied.</param>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\r\n")]
        public void FormatMessage_ProducesNothingForAMessageOfNothing(string message)
        {
            // Act & Assert
            Assert.Equal(0, InlineCount(message));
        }

        /// <summary>
        /// Verifies that the previous message is cleared before a new one is written.
        /// </summary>
        /// <remarks>
        /// The progress dialog reuses one text block for the life of the dialog, writing a new message
        /// into it on every update. Without the clear, each update would append and the block would grow
        /// into a transcript of everything the deployment had ever reported.
        /// </remarks>
        [Fact]
        public void FormatMessage_ClearsWhatWasThereBefore()
        {
            // Act
            List<string> texts = DialogHost.Run(static () =>
            {
                TextBlock block = new();
                Format(block, "the first message");
                Format(block, "the second message");
                return block.Inlines.OfType<Run>().Select(static r => r.Text).ToList();
            });

            // Assert
            Assert.Equal("the second message", Assert.Single(texts));
        }

        /// <summary>
        /// Verifies that a message of nothing also clears what was there before.
        /// </summary>
        /// <remarks>
        /// The clear happens before the message is examined, so an update to nothing empties the block
        /// rather than leaving the previous message showing.
        /// </remarks>
        [Fact]
        public void FormatMessage_ClearsEvenWhenTheNewMessageIsNothing()
        {
            // Act
            int count = DialogHost.Run(static () =>
            {
                TextBlock block = new();
                Format(block, "the first message");
                Format(block, "   ");
                return block.Inlines.Count;
            });

            // Assert
            Assert.Equal(0, count);
        }

        /// <summary>
        /// Verifies that bold text is emboldened and the tags themselves disappear.
        /// </summary>
        [Fact]
        public void FormatMessage_EmboldensBoldText()
        {
            // Act
            List<(string Text, FontWeight Weight, FontStyle Style, bool Accented)> runs = Runs("plain [bold]loud[/bold] plain");

            // Assert
            Assert.Equal(["plain ", "loud", " plain"], runs.Select(static r => r.Text), StringComparer.Ordinal);
            Assert.Equal([FontWeights.Normal, FontWeights.Bold, FontWeights.Normal], runs.Select(static r => r.Weight));
        }

        /// <summary>
        /// Verifies that italic text is slanted.
        /// </summary>
        [Fact]
        public void FormatMessage_SlantsItalicText()
        {
            // Act
            List<(string Text, FontWeight Weight, FontStyle Style, bool Accented)> runs = Runs("plain [italic]leaning[/italic] plain");

            // Assert
            Assert.Equal([FontStyles.Normal, FontStyles.Italic, FontStyles.Normal], runs.Select(static r => r.Style));
        }

        /// <summary>
        /// Verifies that accented text is emboldened and given the theme's accent colour.
        /// </summary>
        /// <remarks>
        /// Accent is the one style that changes two things at once, and the colour is the half that
        /// cannot be a fixed value: it has to follow the theme, so it is attached as a resource
        /// reference rather than a brush. What is asserted is therefore that something was set locally
        /// on the foreground, which is what distinguishes an accented run from a merely bold one.
        /// </remarks>
        [Fact]
        public void FormatMessage_AccentsAccentedText()
        {
            // Act
            List<(string Text, FontWeight Weight, FontStyle Style, bool Accented)> runs = Runs("plain [accent]coloured[/accent] plain");

            // Assert
            Assert.Equal(FontWeights.Bold, runs[1].Weight);
            Assert.True(runs[1].Accented);
            Assert.False(runs[0].Accented);
        }

        /// <summary>
        /// Verifies that nested styles accumulate rather than replacing one another.
        /// </summary>
        /// <remarks>
        /// The formatter keeps a stack of the styles currently open, so text inside two tags carries
        /// both. An implementation that tracked only the innermost tag would lose the bold here.
        /// </remarks>
        [Fact]
        public void FormatMessage_AccumulatesNestedStyles()
        {
            // Act
            List<(string Text, FontWeight Weight, FontStyle Style, bool Accented)> runs = Runs("[bold]loud [italic]and leaning[/italic] loud[/bold]");

            // Assert
            Assert.Equal(["loud ", "and leaning", " loud"], runs.Select(static r => r.Text), StringComparer.Ordinal);
            Assert.All(runs, static r => Assert.Equal(FontWeights.Bold, r.Weight));
            Assert.Equal([FontStyles.Normal, FontStyles.Italic, FontStyles.Normal], runs.Select(static r => r.Style));
        }

        /// <summary>
        /// Verifies that a style closing restores what was open around it.
        /// </summary>
        [Fact]
        public void FormatMessage_RestoresTheEnclosingStyleOnClosing()
        {
            // Act
            List<(string Text, FontWeight Weight, FontStyle Style, bool Accented)> runs = Runs("[bold]a[/bold]b");

            // Assert
            Assert.Equal(FontWeights.Bold, runs[0].Weight);
            Assert.Equal(FontWeights.Normal, runs[1].Weight);
        }

        /// <summary>
        /// Verifies that whitespace between tags survives.
        /// </summary>
        /// <remarks>
        /// The formatter tests its text for null or empty rather than for whitespace, deliberately: the
        /// gap between two styled words, and the line break between two paragraphs, are both text a user
        /// would notice the absence of.
        /// </remarks>
        [Fact]
        public void FormatMessage_KeepsWhitespaceBetweenTags()
        {
            // Act
            List<(string Text, FontWeight Weight, FontStyle Style, bool Accented)> runs = Runs("[bold]one[/bold]\r\n[bold]two[/bold]");

            // Assert
            Assert.Equal(["one", "\r\n", "two"], runs.Select(static r => r.Text), StringComparer.Ordinal);
        }

        /// <summary>
        /// Verifies that a closing tag with nothing open is ignored rather than throwing.
        /// </summary>
        /// <remarks>
        /// A deployment can put anything in its strings, so a stray closing tag has to be survivable.
        /// The tag is consumed either way, so the user does not see it.
        /// </remarks>
        [Fact]
        public void FormatMessage_IgnoresAClosingTagThatOpenedNothing()
        {
            // Act
            List<(string Text, FontWeight Weight, FontStyle Style, bool Accented)> runs = Runs("nothing[/bold] opened");

            // Assert
            Assert.Equal("nothing opened", string.Concat(runs.Select(static r => r.Text)));
            Assert.All(runs, static r => Assert.Equal(FontWeights.Normal, r.Weight));
        }

        /// <summary>
        /// Verifies that a style left open runs to the end of the message.
        /// </summary>
        [Fact]
        public void FormatMessage_RunsAnUnclosedStyleToTheEnd()
        {
            // Act
            List<(string Text, FontWeight Weight, FontStyle Style, bool Accented)> runs = Runs("plain [bold]never closed");

            // Assert
            Assert.Equal(FontWeights.Normal, runs[0].Weight);
            Assert.Equal("never closed", runs[1].Text);
            Assert.Equal(FontWeights.Bold, runs[1].Weight);
        }

        /// <summary>
        /// Verifies that a bare link becomes a hyperlink showing its own address.
        /// </summary>
        [Fact]
        public void FormatMessage_TurnsABareLinkIntoAHyperlink()
        {
            // Act
            (string Text, string? Uri, object? ToolTip) link = Assert.Single(Links("see [url]https://example.test/help[/url] for details"));

            // Assert
            Assert.Equal("https://example.test/help", link.Uri);
            Assert.Equal("https://example.test/help", link.Text);
        }

        /// <summary>
        /// Verifies that a described link shows the description and navigates to the address.
        /// </summary>
        [Fact]
        public void FormatMessage_TurnsADescribedLinkIntoAHyperlink()
        {
            // Act
            (string Text, string? Uri, object? ToolTip) link = Assert.Single(Links("read [url=https://example.test/policy]the IT policy[/url] first"));

            // Assert
            Assert.Equal("https://example.test/policy", link.Uri);
            Assert.Equal("the IT policy", link.Text);
        }

        /// <summary>
        /// Verifies that an address written without a scheme is still navigable.
        /// </summary>
        /// <remarks>
        /// A deployment writing an address the way it would be said aloud produces something the shell
        /// cannot open, so a scheme is put in front of it. Only the two prefixes that unambiguously name
        /// a web or file address get this - anything else without a scheme is not assumed to be one.
        /// </remarks>
        /// <param name="written">The address as the deployment wrote it.</param>
        /// <param name="expected">The address the link should navigate to.</param>
        [Theory]
        [InlineData("www.example.test", "http://www.example.test/")]
        [InlineData("ftp.example.test", "http://ftp.example.test/")]
        [InlineData("WWW.EXAMPLE.TEST", "http://www.example.test/")]
        public void FormatMessage_GivesASchemelessAddressAScheme(string written, string expected)
        {
            // Act
            (string Text, string? Uri, object? ToolTip) link = Assert.Single(Links($"[url]{written}[/url]"));

            // Assert
            Assert.Equal(expected, link.Uri);
        }

        /// <summary>
        /// Verifies that the address shown to the user is the one the deployment wrote.
        /// </summary>
        /// <remarks>
        /// The scheme is added for navigation only. Showing it back would mean a user reading an address
        /// the deployment did not write, and hovering already reveals the real target.
        /// </remarks>
        [Fact]
        public void FormatMessage_ShowsTheAddressAsItWasWritten()
        {
            // Act
            (string Text, string? Uri, object? ToolTip) link = Assert.Single(Links("[url]www.example.test[/url]"));

            // Assert
            Assert.Equal("www.example.test", link.Text);
        }

        /// <summary>
        /// Verifies that a mail address is left as it was written.
        /// </summary>
        [Fact]
        public void FormatMessage_LeavesAMailAddressAlone()
        {
            // Act
            (string Text, string? Uri, object? ToolTip) link = Assert.Single(Links("[url]mailto:support@example.test[/url]"));

            // Assert
            Assert.Equal("mailto:support@example.test", link.Uri);
        }

        /// <summary>
        /// Verifies that something that is not an address at all is shown as plain text.
        /// </summary>
        /// <remarks>
        /// Nothing in the markup stops a deployment putting arbitrary text inside a link tag. Rather
        /// than producing a hyperlink that fails when clicked, the formatter shows the text and moves on.
        /// </remarks>
        [Fact]
        public void FormatMessage_ShowsSomethingThatIsNotAnAddressAsPlainText()
        {
            // Act
            List<(string Text, FontWeight Weight, FontStyle Style, bool Accented)> runs = Runs("[url]ask your IT department[/url]");

            // Assert
            Assert.Empty(Links("[url]ask your IT department[/url]"));
            Assert.Contains("ask your IT department", string.Concat(runs.Select(static r => r.Text)), StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that a link carries a tooltip naming where it goes.
        /// </summary>
        [Fact]
        public void FormatMessage_TellsTheUserWhereALinkGoes()
        {
            // Act
            (string Text, string? Uri, object? ToolTip) link = Assert.Single(Links("[url=https://example.test/policy]the IT policy[/url]"));

            // Assert
            Assert.Equal("Open link: https://example.test/policy", link.ToolTip);
        }

        /// <summary>
        /// Verifies that several links in one message each become their own hyperlink.
        /// </summary>
        [Fact]
        public void FormatMessage_HandlesSeveralLinksInOneMessage()
        {
            // Act
            List<(string Text, string? Uri, object? ToolTip)> links = Links("see [url]https://example.test/a[/url] and [url=https://example.test/b]the other one[/url]");

            // Assert
            Assert.Equal(2, links.Count);
            Assert.Equal("https://example.test/a", links[0].Uri);
            Assert.Equal("https://example.test/b", links[1].Uri);
        }

        /// <summary>
        /// Verifies that button text is shown and its accelerator marker honoured.
        /// </summary>
        /// <remarks>
        /// The text goes into an access text element rather than straight onto the button, which is what
        /// makes an underscore in the string mark the next character as the button's keyboard shortcut
        /// instead of being shown literally.
        /// </remarks>
        [Fact]
        public void SetButtonContent_PutsTheTextIntoAnAccessTextElement()
        {
            // Act
            string text = DialogHost.Run(static () =>
            {
                Fluence.Wpf.Controls.Button button = new();
                SetButtonContent(button, "_Continue");
                return ((AccessText)button.Content).Text;
            });

            // Assert
            Assert.Equal("_Continue", text);
        }

        /// <summary>
        /// Verifies that a button given nothing keeps whatever it already had.
        /// </summary>
        /// <remarks>
        /// A dialog sets the content of only the buttons it wants, so a blank string has to be ignored
        /// rather than blanking a button.
        /// </remarks>
        /// <param name="text">The blank text to set.</param>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void SetButtonContent_LeavesAButtonAloneWhenGivenNothing(string text)
        {
            // Act
            object? content = DialogHost.Run(() =>
            {
                Fluence.Wpf.Controls.Button button = new() { Content = "untouched" };
                SetButtonContent(button, text);
                return button.Content;
            });

            // Assert
            Assert.Equal("untouched", content);
        }

        /// <summary>
        /// Verifies that an accent colour is unpacked from the integer the caller supplies.
        /// </summary>
        /// <remarks>
        /// The colour arrives as a single number because that is what survives the trip from PowerShell
        /// and across the client/server boundary. Reading it back in the wrong byte order is the obvious
        /// way to get this wrong, and it would show as a dialog accented in the wrong colour rather than
        /// as any kind of error, so each channel is given a different value here.
        /// </remarks>
        /// <param name="packed">The colour as a single number.</param>
        /// <param name="alpha">The alpha channel it should unpack to.</param>
        /// <param name="red">The red channel it should unpack to.</param>
        /// <param name="green">The green channel it should unpack to.</param>
        /// <param name="blue">The blue channel it should unpack to.</param>
        [Theory]
        [InlineData(unchecked((int)0xFF102030), 0xFF, 0x10, 0x20, 0x30)]
        [InlineData(unchecked((int)0x80FF0000), 0x80, 0xFF, 0x00, 0x00)]
        [InlineData(0x0000FF00, 0x00, 0x00, 0xFF, 0x00)]
        [InlineData(unchecked((int)0xFFFFFFFF), 0xFF, 0xFF, 0xFF, 0xFF)]
        [InlineData(0, 0x00, 0x00, 0x00, 0x00)]
        public void IntToColor_UnpacksTheChannelsInOrder(int packed, byte alpha, byte red, byte green, byte blue)
        {
            // Act
            Color color = NonPublic.CallStatic<FluentDialog, Color>("IntToColor", packed);

            // Assert
            Assert.Equal(Color.FromArgb(alpha, red, green, blue), color);
        }

        /// <summary>
        /// Verifies that an icon file offering several sizes yields the largest of them.
        /// </summary>
        /// <remarks>
        /// A dialog icon is shown large, so the frame that matters is the biggest one the file carries.
        /// The fixture writes its frames smallest first, so an implementation taking the first frame
        /// rather than the largest would fail here.
        /// </remarks>
        [Fact]
        public void GetIcon_TakesTheLargestFrameOfAnIcon()
        {
            // Arrange
            using TempDirectory directory = new();
            string path = directory.WriteFile("multi.ico", TestImages.IcoBytes(16, 32, 64));

            // Act
            BitmapSource icon = GetIcon(path);

            // Assert
            Assert.Equal(64, icon.PixelWidth);
            Assert.Equal(64, icon.PixelHeight);
        }

        /// <summary>
        /// Verifies that an image which is not an icon is read as it stands.
        /// </summary>
        [Fact]
        public void GetIcon_ReadsAnImageThatIsNotAnIcon()
        {
            // Arrange
            using TempDirectory directory = new();
            string path = directory.WriteFile("logo.png", TestImages.PngBytes(48, 48));

            // Act & Assert
            Assert.Equal(48, GetIcon(path).PixelWidth);
        }

        /// <summary>
        /// Verifies that the icon is frozen so it can be shared across threads.
        /// </summary>
        /// <remarks>
        /// The cache hands the same icon to every dialog, and the dialogs run on a thread of their own.
        /// An unfrozen bitmap belongs to the thread that made it and would throw when another tried to
        /// use it, so freezing is what makes the cache safe rather than merely fast.
        /// </remarks>
        [Fact]
        public void GetIcon_FreezesWhatItReturns()
        {
            // Arrange
            using TempDirectory directory = new();
            string path = directory.WriteFile("frozen.png", TestImages.PngBytes(32, 32));

            // Act & Assert
            Assert.True(GetIcon(path).IsFrozen);
        }

        /// <summary>
        /// Verifies that the same path gives back the same icon rather than decoding it again.
        /// </summary>
        [Fact]
        public void GetIcon_GivesBackTheSameIconForTheSamePath()
        {
            // Arrange
            using TempDirectory directory = new();
            string path = directory.WriteFile("cached.png", TestImages.PngBytes(32, 32));

            // Act
            BitmapSource first = GetIcon(path);
            BitmapSource second = GetIcon(path.ToUpperInvariant());

            // Assert
            Assert.Same(first, second);
        }

        /// <summary>
        /// Verifies that the application's title and subtitle are shown.
        /// </summary>
        [Fact]
        public void Constructor_ShowsTheTitleAndSubtitle()
        {
            // Arrange
            Hashtable table = SampleOptions.CustomDialog();
            table["AppTitle"] = "Contoso Suite 4.1";
            table["Subtitle"] = "IT Services";

            // Act & Assert
            WithDialog(table, static dialog =>
            {
                Assert.Equal("Contoso Suite 4.1", dialog.Title);
                Assert.Equal("Contoso Suite 4.1", dialog.AppTitleTextBlock.Text);
                Assert.Equal("IT Services", dialog.SubtitleTextBlock.Text);
            });
        }

        /// <summary>
        /// Records that markup in the title is left in.
        /// </summary>
        /// <remarks>
        /// The Classic dialogs strip it; these do not, because a Fluent title is set straight from the
        /// options. A deployment putting markup in its application title therefore sees the tags in the
        /// Fluent header and taskbar entry and not in the Classic caption. Pinned so the difference is
        /// a known one.
        /// </remarks>
        [Fact]
        public void Constructor_DoesNotStripMarkupFromTheTitle()
        {
            // Arrange
            Hashtable table = SampleOptions.CustomDialog();
            table["AppTitle"] = "Install [bold]Contoso[/bold]";

            // Act & Assert
            WithDialog(table, static dialog => Assert.Equal("Install [bold]Contoso[/bold]", dialog.Title));
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

            // Act & Assert
            WithDialog(table, dialog => Assert.Equal(topMost, dialog.Topmost));
        }

        /// <summary>
        /// Verifies that the dialog is parked off screen until it has something to show.
        /// </summary>
        /// <remarks>
        /// A window that sized itself to its content on screen would be seen doing it. The dialog is
        /// therefore built at a coordinate no monitor covers and only moved into place once its content
        /// has rendered, so the first thing anyone sees is the finished article.
        /// </remarks>
        [Fact]
        public void Constructor_ParksTheWindowOffScreen()
        {
            // Act & Assert
            WithDialog(SampleOptions.CustomDialog(), static dialog =>
            {
                Assert.True(dialog.Left < -30000, FormattableString.Invariant($"Expected the window to be parked off screen, but Left was {dialog.Left}."));
                Assert.True(dialog.Top < -30000, FormattableString.Invariant($"Expected the window to be parked off screen, but Top was {dialog.Top}."));
                Assert.Equal(WindowStartupLocation.Manual, dialog.WindowStartupLocation);
            });
        }

        /// <summary>
        /// Verifies that the sections belonging to other dialog types start out hidden.
        /// </summary>
        /// <remarks>
        /// All the dialog types share one window, so the base hides everything and each derived dialog
        /// shows only what it needs. A section left visible by default would appear in every dialog that
        /// did not think to hide it. Checked here through the custom dialog, so the sections listed are
        /// the ones it has no business showing.
        /// </remarks>
        [Fact]
        public void Constructor_HidesTheSectionsBelongingToOtherDialogs()
        {
            // Act & Assert
            WithDialog(SampleOptions.CustomDialog(), static dialog =>
            {
                Assert.Equal(Visibility.Collapsed, dialog.CloseAppsStackPanel.Visibility);
                Assert.Equal(Visibility.Collapsed, dialog.ProgressStackPanel.Visibility);
                Assert.Equal(Visibility.Collapsed, dialog.InputBoxStackPanel.Visibility);
                Assert.Equal(Visibility.Collapsed, dialog.ListSelectionStackPanel.Visibility);
                Assert.Equal(Visibility.Collapsed, dialog.DeferRemainingStackPanel.Visibility);
                Assert.Equal(Visibility.Collapsed, dialog.DeferDeadlineStackPanel.Visibility);
            });
        }

        /// <summary>
        /// Verifies that a dialog with no custom message and no countdown shows neither.
        /// </summary>
        /// <remarks>
        /// Both are supplied by the dialogs that want them - the restart and close-applications dialogs -
        /// rather than read from the options by the base, so a custom dialog is the one that shows what
        /// the base does when neither was passed.
        /// </remarks>
        [Fact]
        public void Constructor_ShowsNoCustomMessageOrCountdownWhenGivenNeither()
        {
            // Act & Assert
            WithDialog(SampleOptions.CustomDialog(), static dialog =>
            {
                Assert.Equal(Visibility.Collapsed, dialog.CustomMessageTextBlock.Visibility);
                Assert.Equal(Visibility.Collapsed, dialog.CountdownStackPanel.Visibility);
            });
        }

        /// <summary>
        /// Verifies that a dialog refuses to close until it is told it may.
        /// </summary>
        /// <remarks>
        /// The window cancels its own closing while the guard is unset, so a user pressing Alt+F4 or
        /// closing from the taskbar cannot dismiss a dialog without answering it. Asked here by handing
        /// the dialog a closing event and reading back whether it cancelled.
        /// </remarks>
        [Fact]
        public void CloseDialog_IsWhatPermitsClosing()
        {
            // Act & Assert
            WithDialog(SampleOptions.CustomDialog(), static dialog =>
            {
                Assert.False(WouldAllowClosing(dialog));
                dialog.CloseDialog();
                Assert.True(WouldAllowClosing(dialog));
            });
        }

        /// <summary>
        /// Verifies that disposing twice is harmless.
        /// </summary>
        /// <remarks>
        /// The window disposes itself when it closes, and the caller that built it disposes it too. Both
        /// happen in the ordinary course of showing a dialog, so the second has to be a no-op rather
        /// than a second round of detaching handlers.
        /// </remarks>
        [Fact]
        public void Dispose_IsHarmlessASecondTime()
        {
            // Act & Assert
            WithDialog(SampleOptions.CustomDialog(), static dialog =>
            {
                dialog.Dispose();
                dialog.Dispose();
                Assert.True(NonPublic.Property<bool>(dialog, "Disposed"));
            });
        }

        /// <summary>
        /// Verifies that a single visible button is laid out across half the width and made the default.
        /// </summary>
        /// <remarks>
        /// A lone button stretched across the whole dialog looks like a banner rather than a control, so
        /// it goes in the right-hand half. Being the only thing to press, it is also the one the Enter
        /// key should activate and the one to carry the accent.
        /// </remarks>
        [Fact]
        public void ButtonLayout_PutsALoneButtonInTheRightHalfAndMakesItDefault()
        {
            // Act & Assert
            WithDialog(SampleOptions.CustomDialog(), static dialog =>
            {
                dialog.ButtonLeft.Visibility = Visibility.Collapsed;
                dialog.ButtonMiddle.Visibility = Visibility.Collapsed;
                dialog.ButtonRight.Visibility = Visibility.Visible;
                LayOutButtons(dialog);

                Assert.Equal(2, dialog.ActionButtons.ColumnDefinitions.Count);
                Assert.Equal(1, Grid.GetColumn(dialog.ButtonRight));
                Assert.True(dialog.ButtonRight.IsDefault);
            });
        }

        /// <summary>
        /// Verifies that several visible buttons share the width equally.
        /// </summary>
        /// <param name="count">How many buttons to show.</param>
        [Theory]
        [InlineData(2)]
        [InlineData(3)]
        public void ButtonLayout_SharesTheWidthBetweenSeveralButtons(int count)
        {
            // Act & Assert
            WithDialog(SampleOptions.CustomDialog(), dialog =>
            {
                dialog.ButtonLeft.Visibility = Visibility.Visible;
                dialog.ButtonMiddle.Visibility = count > 2 ? Visibility.Visible : Visibility.Collapsed;
                dialog.ButtonRight.Visibility = Visibility.Visible;
                LayOutButtons(dialog);

                Assert.Equal(count, dialog.ActionButtons.ColumnDefinitions.Count);
                Assert.All(dialog.ActionButtons.ColumnDefinitions, static c => Assert.Equal(GridUnitType.Star, c.Width.GridUnitType));
            });
        }

        /// <summary>
        /// Verifies that no visible buttons means no columns.
        /// </summary>
        [Fact]
        public void ButtonLayout_LeavesNoColumnsWhenNothingIsVisible()
        {
            // Act & Assert
            WithDialog(SampleOptions.CustomDialog(), static dialog =>
            {
                dialog.ButtonLeft.Visibility = Visibility.Collapsed;
                dialog.ButtonMiddle.Visibility = Visibility.Collapsed;
                dialog.ButtonRight.Visibility = Visibility.Collapsed;
                LayOutButtons(dialog);

                Assert.Empty(dialog.ActionButtons.ColumnDefinitions);
            });
        }

        /// <summary>
        /// Formats a message and describes the runs it produced.
        /// </summary>
        /// <remarks>
        /// Described as plain values rather than handed back as inlines, because an inline is a
        /// dependency object and reading one outside the apartment that made it throws. Doing the
        /// reading inside and returning what was read lets each test assert in the ordinary way.
        /// </remarks>
        /// <param name="message">The message to format.</param>
        /// <returns>One entry per plain run, in the order they appear.</returns>
        private static List<(string Text, FontWeight Weight, FontStyle Style, bool Accented)> Runs(string message)
        {
            return DialogHost.Run(() =>
            {
                List<(string, FontWeight, FontStyle, bool)> runs = [];
                foreach (Run run in Formatted(message).OfType<Run>())
                {
                    runs.Add((run.Text, run.FontWeight, run.FontStyle, !Equals(run.ReadLocalValue(TextElement.ForegroundProperty), DependencyProperty.UnsetValue)));
                }
                return runs;
            });
        }

        /// <summary>
        /// Formats a message and describes the hyperlinks it produced.
        /// </summary>
        /// <param name="message">The message to format.</param>
        /// <returns>One entry per hyperlink, in the order they appear.</returns>
        private static List<(string Text, string? Uri, object? ToolTip)> Links(string message)
        {
            return DialogHost.Run(() =>
            {
                List<(string, string?, object?)> links = [];
                foreach (Hyperlink link in Formatted(message).OfType<Hyperlink>())
                {
                    links.Add((string.Concat(link.Inlines.OfType<Run>().Select(static r => r.Text)), link.NavigateUri?.AbsoluteUri, link.ToolTip));
                }
                return links;
            });
        }

        /// <summary>
        /// Formats a message and counts what it produced.
        /// </summary>
        /// <param name="message">The message to format.</param>
        /// <returns>How many inlines the formatter added.</returns>
        private static int InlineCount(string message)
        {
            return DialogHost.Run(() => Formatted(message).Count);
        }

        /// <summary>
        /// Formats a message into a fresh text block. Must be called from within the apartment.
        /// </summary>
        /// <param name="message">The message to format.</param>
        /// <returns>The inlines the formatter added.</returns>
        private static InlineCollection Formatted(string message)
        {
            TextBlock block = new();
            Format(block, message);
            return block.Inlines;
        }

        /// <summary>
        /// Formats a message into a text block.
        /// </summary>
        /// <param name="textBlock">The block to write into.</param>
        /// <param name="message">The message to format.</param>
        private static void Format(TextBlock textBlock, string message)
        {
            NonPublic.CallStatic<FluentDialog>("FormatMessageWithHyperlinks", textBlock, message);
        }

        /// <summary>
        /// Sets a button's content, honouring an accelerator marker in the text.
        /// </summary>
        /// <param name="button">The button to set.</param>
        /// <param name="text">The text to set it to.</param>
        private static void SetButtonContent(Fluence.Wpf.Controls.Button button, string text)
        {
            NonPublic.CallStatic<FluentDialog>("SetButtonContentWithAccelerator", button, text);
        }

        /// <summary>
        /// Reads an icon through the dialog's own cache.
        /// </summary>
        /// <param name="path">The path or base64 text to read from.</param>
        /// <returns>The icon, which is frozen and so safe to read anywhere.</returns>
        private static BitmapSource GetIcon(string path)
        {
            return DialogHost.Run(() => NonPublic.CallStatic<FluentDialog, BitmapSource>("GetIcon", path));
        }

        /// <summary>
        /// Lays the action buttons out for whichever of them are currently visible.
        /// </summary>
        /// <remarks>
        /// The layout pass runs before the first paint, which these tests never reach because they never
        /// show a window, so it is asked for directly.
        /// </remarks>
        /// <param name="dialog">The dialog to lay out.</param>
        private static void LayOutButtons(FluentDialog dialog)
        {
            NonPublic.Call(dialog, "UpdateButtonLayout");
        }

        /// <summary>
        /// Asks a dialog whether it would presently allow itself to be closed.
        /// </summary>
        /// <param name="dialog">The dialog to ask.</param>
        /// <returns><see langword="true"/> if it would; otherwise, <see langword="false"/>.</returns>
        private static bool WouldAllowClosing(FluentDialog dialog)
        {
            CancelEventArgs closing = new();
            NonPublic.Call(dialog, "OnClosing", closing);
            return !closing.Cancel;
        }

        /// <summary>
        /// Builds the simplest concrete Fluent dialog, runs a body against it and disposes it.
        /// </summary>
        /// <param name="table">The options to build it from.</param>
        /// <param name="body">The assertions to run.</param>
        private static void WithDialog(Hashtable table, Action<CustomDialog> body)
        {
            DialogHost.WithDialog(() => new CustomDialog(new CustomDialogOptions(table)), body);
        }
    }
}
