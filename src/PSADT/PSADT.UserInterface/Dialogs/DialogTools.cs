using System.Text.RegularExpressions;

namespace PSADT.UserInterface.Dialogs
{
    /// <summary>
    /// Provides utility methods and constants for working with dialog-related functionality,  such as managing button
    /// text and parsing custom text formatting tags.
    /// </summary>
    /// <remarks>This class includes predefined constants for dialog button text and internal utilities  for
    /// handling custom text formatting tags. It is designed to support dialog-related  operations in
    /// applications.</remarks>
    public static class DialogTools
    {
        /// <summary>
        /// Gets the text for the button used to block execution in a dialog.
        /// </summary>
        public static string BlockExecutionButtonText = "OK";

        /// <summary>
        /// Represents a compiled regular expression used to parse and identify custom text formatting tags such as
        /// [url], [accent], [bold], and [italic].
        /// </summary>
        /// <remarks>This regular expression matches the following custom formatting tags: <list
        /// type="bullet"> <item> <description><c>[url]</c>: Matches a URL link enclosed in <c>[url]</c> and
        /// <c>[/url]</c> tags.</description> </item> <item> <description><c>[accent]</c>: Matches text enclosed in
        /// <c>[accent]</c> and <c>[/accent]</c> tags.</description> </item> <item> <description><c>[bold]</c>: Matches
        /// text enclosed in <c>[bold]</c> and <c>[/bold]</c> tags.</description> </item> <item>
        /// <description><c>[italic]</c>: Matches text enclosed in <c>[italic]</c> and <c>[/italic]</c>
        /// tags.</description> </item> </list> The regular expression is compiled for improved performance during
        /// repeated use.</remarks>
        internal static readonly Regex TextFormattingRegex = new(
            @"(?<UrlLink>\[url\](?<UrlLinkContent>.+?)\[/url\])" + @"|" +
            @"(?<Accent>\[accent\](?<AccentText>.+?)\[/accent\])" + @"|" +
            @"(?<Bold>\[bold\](?<BoldText>.+?)\[/bold\])" + @"|" +
            @"(?<Italic>\[italic\](?<ItalicText>.+?)\[/italic\])",
            RegexOptions.Compiled);
    }
}
