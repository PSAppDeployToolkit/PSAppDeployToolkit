using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace PSADT.UserInterface.Dialogs
{
    internal static class DialogTools
    {
        /// <summary>
        /// Reboots the computer and terminates this process.
        /// </summary>
        internal static void RestartComputer()
        {
            // Reboot the system and hard-exit this process.
            using (var process = new Process())
            {
                process.StartInfo.FileName = "shutdown.exe";
                process.StartInfo.Arguments = "/r /f /t 0";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                process.WaitForExit();
            }
            Environment.Exit(0);
        }

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
