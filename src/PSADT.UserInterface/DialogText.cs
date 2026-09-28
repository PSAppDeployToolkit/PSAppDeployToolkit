using System.Text.RegularExpressions;

namespace PSADT.UserInterface
{
    /// <summary>
    /// Provides regular expression utilities for parsing and identifying custom text formatting tags such as [url],
    /// [accent], [bold], and [italic] within dialog text.
    /// </summary>
    /// <remarks>This class contains a compiled regular expression that matches custom formatting tags used in
    /// dialog text. Supported tags include [url] for hyperlinks (both simple and descriptive formats), as well as
    /// [accent], [bold], and [italic] for text styling. These tags can be nested and combined to achieve cumulative
    /// formatting effects. The regular expression is compiled to improve performance during repeated parsing
    /// operations.</remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0182: Avoid unused internal types.", Justification = "This is used across InternalsVisibleTo boundaries.")]
    internal static class DialogText
    {
        /// <summary>
        /// Represents a compiled regular expression used to parse and identify custom text formatting tags such as
        /// [url], [accent], [bold], and [italic].
        /// </summary>
        /// <remarks>This regular expression matches the following custom formatting tags: <list
        /// type="bullet"> <item> <description><c language="text">[url]</c>: Matches URL links in two formats: <c language="text">[url]URL[/url]</c> for
        /// simple links, and <c language="text">[url=URL]Description[/url]</c> for descriptive links.</description> </item> <item>
        /// <description><c language="text">[accent]</c>, <c language="text">[bold]</c>, <c language="text">[italic]</c>: Matches opening and closing tags for accent,
        /// bold, and italic formatting. These tags can be nested and combined for cumulative formatting effects.</description>
        /// </item> </list> The regular expression is compiled for improved performance during repeated use and supports
        /// nested tag combinations.</remarks>
        internal static readonly Regex FormattingRegex = new(
            @"(?<UrlLinkSimple>\[url\](?<UrlLinkSimpleContent>.+?)\[/url\])" + "|" +
            @"(?<UrlLinkDescriptive>\[url=(?<UrlLinkUrl>[^\]]+)\](?<UrlLinkDescription>.+?)\[/url\])" + "|" +
            @"(?<OpenAccent>\[accent\])" + "|" +
            @"(?<CloseAccent>\[/accent\])" + "|" +
            @"(?<OpenBold>\[bold\])" + "|" +
            @"(?<CloseBold>\[/bold\])" + "|" +
            @"(?<OpenItalic>\[italic\])" + "|" +
            @"(?<CloseItalic>\[/italic\])",
            RegexOptions.Compiled);
    }
}
