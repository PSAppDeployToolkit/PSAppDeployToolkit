using System.Text.RegularExpressions;
using Xunit;

namespace PSADT.UserInterface.Tests
{
    /// <summary>
    /// Tests the pattern that finds the markup a dialog's message text may contain.
    /// </summary>
    /// <remarks>
    /// The toolkit lets a deployment put a small set of BBCode-style tags in its message strings, which
    /// the dialogs turn into runs of formatted text and clickable links. The pattern here is the whole
    /// definition of that syntax: a group that fails to match leaves the tag visible to the user as
    /// literal text, and a group that matches too much swallows the message around it.
    /// </remarks>
    public sealed class DialogTextTests
    {
        /// <summary>
        /// Verifies that a bare URL tag is matched and its address captured.
        /// </summary>
        [Fact]
        public void FormattingRegex_MatchesASimpleUrl()
        {
            // Act
            Match match = DialogText.FormattingRegex.Match("see [url]https://example.test/help[/url] for details");

            // Assert
            Assert.True(match.Success);
            Assert.True(match.Groups["UrlLinkSimple"].Success);
            Assert.Equal("https://example.test/help", match.Groups["UrlLinkSimpleContent"].Value);
        }

        /// <summary>
        /// Verifies that a URL tag with its own link text captures the address and the text separately.
        /// </summary>
        [Fact]
        public void FormattingRegex_MatchesADescriptiveUrl()
        {
            // Act
            Match match = DialogText.FormattingRegex.Match("see [url=https://example.test/help]the help page[/url]");

            // Assert
            Assert.True(match.Groups["UrlLinkDescriptive"].Success);
            Assert.Equal("https://example.test/help", match.Groups["UrlLinkUrl"].Value);
            Assert.Equal("the help page", match.Groups["UrlLinkDescription"].Value);
        }

        /// <summary>
        /// Verifies that a descriptive URL is not mistaken for a simple one.
        /// </summary>
        /// <remarks>
        /// The two URL forms are alternatives in one pattern and the simple one is written first, so it
        /// gets first refusal on any <c>[url</c>. It does not match a descriptive tag only because it
        /// requires the closing bracket immediately - which is the kind of thing that holds until someone
        /// makes the pattern more permissive.
        /// </remarks>
        [Fact]
        public void FormattingRegex_DoesNotReadADescriptiveUrlAsASimpleOne()
        {
            // Act
            Match match = DialogText.FormattingRegex.Match("[url=https://example.test]text[/url]");

            // Assert
            Assert.False(match.Groups["UrlLinkSimple"].Success);
            Assert.True(match.Groups["UrlLinkDescriptive"].Success);
        }

        /// <summary>
        /// Verifies that each formatting tag is recognised as its own group.
        /// </summary>
        /// <param name="text">The tag to match.</param>
        /// <param name="group">The group expected to capture it.</param>
        [Theory]
        [InlineData("[accent]", "OpenAccent")]
        [InlineData("[/accent]", "CloseAccent")]
        [InlineData("[bold]", "OpenBold")]
        [InlineData("[/bold]", "CloseBold")]
        [InlineData("[italic]", "OpenItalic")]
        [InlineData("[/italic]", "CloseItalic")]
        public void FormattingRegex_RecognisesEachFormattingTag(string text, string group)
        {
            // Act
            Match match = DialogText.FormattingRegex.Match($"before {text} after");

            // Assert
            Assert.True(match.Success);
            Assert.True(match.Groups[group].Success);
            Assert.Equal(text, match.Value);
        }

        /// <summary>
        /// Verifies that a closing tag is not read as an opening one.
        /// </summary>
        /// <remarks>
        /// The opening tags are written before the closing ones in the alternation, so <c>[/bold]</c> is
        /// only safe from matching <c>[bold]</c> because of the slash. Worth its own case, since the
        /// consequence of getting it wrong is text that turns bold and never turns back.
        /// </remarks>
        [Fact]
        public void FormattingRegex_DoesNotReadAClosingTagAsAnOpeningOne()
        {
            // Act
            Match match = DialogText.FormattingRegex.Match("[/bold]");

            // Assert
            Assert.False(match.Groups["OpenBold"].Success);
            Assert.True(match.Groups["CloseBold"].Success);
        }

        /// <summary>
        /// Verifies that every tag in a message is found, not just the first.
        /// </summary>
        [Fact]
        public void FormattingRegex_FindsEveryTagInAMessage()
        {
            // Act
            MatchCollection matches = DialogText.FormattingRegex.Matches("[bold]Warning[/bold]: see [url]https://example.test[/url] or [italic]ask[/italic]");

            // Assert
            Assert.Equal(5, matches.Count);
        }

        /// <summary>
        /// Verifies that the shortest possible link is taken rather than the longest.
        /// </summary>
        /// <remarks>
        /// The content groups are lazy. Were they greedy, two links in one message would come back as a
        /// single match running from the first opening tag to the last closing one, taking the text
        /// between them along with it.
        /// </remarks>
        [Fact]
        public void FormattingRegex_DoesNotRunTwoLinksTogether()
        {
            // Act
            MatchCollection matches = DialogText.FormattingRegex.Matches("[url]https://one.test[/url] and [url]https://two.test[/url]");

            // Assert
            Assert.Equal(2, matches.Count);
            Assert.Equal("https://one.test", matches[0].Groups["UrlLinkSimpleContent"].Value);
            Assert.Equal("https://two.test", matches[1].Groups["UrlLinkSimpleContent"].Value);
        }

        /// <summary>
        /// Verifies that text with no markup in it matches nothing.
        /// </summary>
        /// <remarks>
        /// Most dialog messages are ordinary sentences, so this is the common case rather than an edge
        /// one - and square brackets appear in ordinary text often enough to be worth including.
        /// </remarks>
        /// <param name="text">The unmarked text.</param>
        [Theory]
        [InlineData("Please close Microsoft Word before continuing.")]
        [InlineData("The file [Setup.exe] could not be found.")]
        [InlineData("[not a tag]")]
        [InlineData("[url]")]
        [InlineData("[bold")]
        public void FormattingRegex_MatchesNothingInTextWithoutMarkup(string text)
        {
            Assert.DoesNotMatch(DialogText.FormattingRegex, text);
        }

        /// <summary>
        /// Verifies that an unclosed link tag is left alone rather than partly matched.
        /// </summary>
        [Fact]
        public void FormattingRegex_LeavesAnUnclosedLinkAlone()
        {
            Assert.DoesNotMatch(DialogText.FormattingRegex, "[url]https://example.test");
        }

        /// <summary>
        /// Verifies that a link's text may itself carry formatting tags.
        /// </summary>
        /// <remarks>
        /// The description group accepts any characters, so tags inside it are captured as part of the
        /// description rather than as matches of their own. Whether the dialog then renders them is its
        /// business; what this records is that the pattern hands the whole run over intact rather than
        /// cutting the link short at the first inner tag.
        /// </remarks>
        [Fact]
        public void FormattingRegex_KeepsFormattingInsideALinkWithTheLink()
        {
            // Act
            Match match = DialogText.FormattingRegex.Match("[url=https://example.test][bold]click here[/bold][/url]");

            // Assert
            Assert.True(match.Groups["UrlLinkDescriptive"].Success);
            Assert.Equal("[bold]click here[/bold]", match.Groups["UrlLinkDescription"].Value);
        }

        /// <summary>
        /// Verifies that the pattern is compiled.
        /// </summary>
        /// <remarks>
        /// It is applied to every message a dialog renders, on the thread drawing that dialog. The option
        /// is on the declaration and easy to drop while editing the pattern itself, and dropping it costs
        /// interpretation on each use rather than failing.
        /// </remarks>
        [Fact]
        public void FormattingRegex_IsCompiled()
        {
            Assert.True(DialogText.FormattingRegex.Options.HasFlag(RegexOptions.Compiled));
        }
    }
}
