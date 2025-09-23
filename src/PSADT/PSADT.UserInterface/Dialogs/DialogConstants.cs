using System.Text.RegularExpressions;

namespace PSADT.UserInterface.Dialogs
{
    /// <summary>
    /// Provides utility methods and constants for working with dialog-related functionality,  such as managing button
    /// text and parsing custom text formatting tags.
    /// </summary>
    /// <remarks>This class includes predefined constants for dialog button text and internal utilities for
    /// handling custom text formatting tags. It is designed to support dialog-related operations in
    /// applications.</remarks>
    public static class DialogConstants
    {
        /// <summary>
        /// Gets the text for the button used to block execution in a dialog.
        /// </summary>
        public const string BlockExecutionButtonText = "OK";

        /// <summary>
        /// Represents a compiled regular expression used to parse and identify custom text formatting tags such as
        /// [url], [accent], [bold], and [italic].
        /// </summary>
        /// <remarks>This regular expression matches the following custom formatting tags: <list
        /// type="bullet"> <item> <description><c>[url]</c>: Matches URL links in two formats: <c>[url]URL[/url]</c> for
        /// simple links, and <c>[url=URL]Description[/url]</c> for descriptive links.</description> </item> <item>
        /// <description><c>[accent]</c>, <c>[bold]</c>, <c>[italic]</c>: Matches opening and closing tags for accent,
        /// bold, and italic formatting. These tags can be nested and combined for cumulative formatting effects.</description>
        /// </item> </list> The regular expression is compiled for improved performance during repeated use and supports
        /// nested tag combinations.</remarks>
        internal static readonly Regex TextFormattingRegex = new(
            @"(?<UrlLinkSimple>\[url\](?<UrlLinkSimpleContent>.+?)\[/url\])" + @"|" +
            @"(?<UrlLinkDescriptive>\[url=(?<UrlLinkUrl>[^\]]+)\](?<UrlLinkDescription>.+?)\[/url\])" + @"|" +
            @"(?<OpenAccent>\[accent\])" + @"|" +
            @"(?<CloseAccent>\[/accent\])" + @"|" +
            @"(?<OpenBold>\[bold\])" + @"|" +
            @"(?<CloseBold>\[/bold\])" + @"|" +
            @"(?<OpenItalic>\[italic\])" + @"|" +
            @"(?<CloseItalic>\[/italic\])",
            RegexOptions.Compiled);
    }
}
