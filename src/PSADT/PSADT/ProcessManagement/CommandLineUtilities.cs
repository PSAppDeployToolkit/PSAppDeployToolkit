using System;
using System.Collections.Generic;
using System.Text;
using PSADT.Extensions;

namespace PSADT.ProcessManagement
{
    /// <summary>
    /// Provides utility methods for parsing and constructing command-line arguments.
    /// </summary>
    /// <remarks>The <see cref="CommandLineUtilities"/> class includes methods for handling common scenarios
    /// involving command-line arguments, such as parsing a command-line string into individual arguments and
    /// constructing a command-line string from an array of arguments. These methods handle edge cases like quoted
    /// strings, whitespace, and special characters to ensure compatibility with typical command-line parsing
    /// behavior.</remarks>
    public static class CommandLineUtilities
    {
        /// <summary>
        /// Parses a command-line string into an array of arguments, handling quoted strings and whitespace.
        /// </summary>
        /// <remarks>This method processes the command-line string character by character, respecting
        /// quoted substrings and handling special cases such as lone quotes and unquoted paths. It ensures that
        /// arguments are correctly split while preserving quoted content as single arguments. Whitespace outside of
        /// quotes is treated as a delimiter.</remarks>
        /// <param name="commandLine">The command-line string to parse. This string must not be null, empty, or consist only of whitespace.</param>
        /// <returns>An array of strings, where each element represents an individual argument parsed from the command-line
        /// string. Quoted strings are treated as single arguments, and whitespace outside of quotes is used to separate
        /// arguments.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="commandLine"/> is null, empty, or consists only of whitespace.</exception>
        public static IReadOnlyList<string> CommandLineToArgumentList(string commandLine)
        {
            // Validate input before continuing.
            if (string.IsNullOrWhiteSpace(commandLine))
            {
                throw new ArgumentNullException(nameof(commandLine));
            }

            // Set up the state for parsing.
            List<string> args = [];
            StringBuilder current = new();
            char groupingOpener = '\0';
            bool inQuotes = false;
            bool keepThisQuote = false;
            bool pathMode = false;

            // Parse the command line character by character.
            commandLine = commandLine.TrimRemoveNull();
            int len = commandLine.Length;
            for (int i = 0; i < len; i++)
            {
                // Store off the current character as we can't use a foreach loop here.
                char c = commandLine[i];

                // Specifically handle escaped backslashes.
                if (inQuotes && c == '\\' && i + 1 < len && commandLine[i + 1] == '\\' && current.Length > 0 && current[current.Length - 1] != '"')
                {
                    // Handle escaped backslash inside quotes (don't remove valid trailing slashes).
                    current.Append('\\'); i++;
                    continue;
                }

                // Handle all the special cases we require.
                bool isSlashQuote = c == '\\' && i + 1 < len && commandLine[i + 1] == '"';
                bool isPlainQuote = c == '"'  && !isSlashQuote;
                if (!inQuotes && current.Length == 0 && isSlashQuote)
                {
                    // Handle stand-alone slash-quote (e.g., /Delimiter \") as literal quote.
                    args.Add("\""); i++;
                }
                else if (isSlashQuote || isPlainQuote)
                {
                    // If we're *not* already in a quoted segment, have no accumulated text, and the next
                    // char is either whitespace or end-of-line, treat this as a standalone quote-arg.
                    if (isPlainQuote && !inQuotes && current.Length == 0 && (i + 1 >= len || char.IsWhiteSpace(commandLine[i + 1])))
                    {
                        args.Add("\"");
                        continue;
                    }

                    // Otherwise, this is the start/end of a grouping quote.
                    if (!inQuotes)
                    {
                        groupingOpener = isSlashQuote ? '\\' : '"';
                        keepThisQuote = current.Length > 0;
                        inQuotes = true;
                        if (keepThisQuote)
                        {
                            current.Append('"');
                        }
                    }
                    else if ((groupingOpener == '\\' && isSlashQuote) || (groupingOpener == '"'  && isPlainQuote))
                    {
                        if (keepThisQuote)
                        {
                            current.Append('"');
                        }
                        inQuotes = false;
                        groupingOpener = '\0';
                    }
                    else
                    {
                        // Mismatched quote (e.g. \" inside a "…"), treat as literal.
                        current.Append('"');
                    }

                    // If we consumed a slash+quote, skip both.
                    if (isSlashQuote)
                    {
                        i++;
                    }
                }
                else if (char.IsWhiteSpace(c) && !inQuotes)
                {
                    // We're processing unquoted whitespace.
                    if (pathMode)
                    {
                        // Peek ahead to see if next chunk is a new arg.
                        int j = i + 1; while (j < len && char.IsWhiteSpace(commandLine[j]))
                        {
                            j++;
                        }

                        bool nextIsNewArg;
                        if (j < len)
                        {
                            char nc = commandLine[j];
                            if (nc != '/' && nc != '-' && nc != '"')
                            {
                                int k = j; while (k < len && !char.IsWhiteSpace(commandLine[k]))
                                {
                                    k++;
                                }
                                string tok = commandLine.Substring(j, k - j);
                                nextIsNewArg = GetFirstPathIndex(tok, 0) >= 0;
                            }
                            else
                            {
                                nextIsNewArg = true;
                            }
                        }
                        else
                        {
                            nextIsNewArg = true;
                        }

                        if (nextIsNewArg)
                        {
                            FlushArg(args, current);
                            pathMode = false;
                        }
                        else
                        {
                            current.Append(' ');
                        }
                    }
                    else
                    {
                        if (current.Length > 0)
                        {
                            FlushArg(args, current);
                        }
                    }
                }
                else
                {
                    // This is for anything else, so just append it.
                    current.Append(c);

                    // Detect unquoted path start.
                    if (!inQuotes && !pathMode)
                    {
                        int start = GetFirstPathIndex(commandLine, i);
                        if (start == i)
                        {
                            pathMode = true;
                        }
                    }
                }
            }

            // Final flush before returning.
            if (current.Length > 0)
            {
                FlushArg(args, current);
            }
            return args.AsReadOnly();
        }

        /// <summary>
        /// Converts an array of command-line arguments into a single command-line string.
        /// </summary>
        /// <remarks>This method ensures that each argument is properly escaped and quoted as needed to
        /// handle special characters, whitespace, or paths. Arguments containing quotes are processed to escape them
        /// correctly, and paths are handled verbatim if they are enclosed in quotes. The resulting string is suitable
        /// for  use in command-line execution scenarios.</remarks>
        /// <param name="args">An array of strings representing the individual command-line arguments. Each argument will be processed and
        /// formatted appropriately for inclusion in a command-line string.</param>
        /// <returns>A single string representing the command-line arguments, formatted and escaped as necessary.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="args"/> is <see langword="null"/> or empty.</exception>
        public static string? ArgumentListToCommandLine(IEnumerable<string> args)
        {
            // Validate input before continuing.
            if (null == args)
            {
                throw new ArgumentNullException(nameof(args));
            }

            // Build the command line from the arguments and return it.
            StringBuilder sb = new(); bool first = true;
            foreach (var arg in args)
            {
                // Handle the first argument separately to avoid leading space.
                if (!first)
                {
                    sb.Append(' ');
                }
                first = false;

                // Ensure there's no whitespace before that first quote.
                int firstQ = arg.IndexOf('"'); int lastQ = arg.LastIndexOf('"');
                bool noPreWhitespace = true;
                for (int x = 0; x < firstQ; x++)
                {
                    if (char.IsWhiteSpace(arg[x]))
                    {
                        noPreWhitespace = false;
                        break;
                    }
                }

                // Handle special case of quotes within quoted arguments.
                if (firstQ > 0 && lastQ == arg.Length - 1 && noPreWhitespace)
                {
                    // Re-escape any literal " inside inner and continue.
                    string prefix = arg.Substring(0, firstQ + 1);
                    string inner = arg.Substring(firstQ + 1, lastQ - firstQ - 1);
                    string suffix = "\"";
                    sb.Append(prefix).Append(EscapeQuotes(inner)).Append(suffix);
                    continue;
                }

                // Perform remaining checks for whitespace and paths.
                if (arg.IndexOf(' ') >= 0 || arg.IndexOf('\t') >= 0 || IsPath(arg))
                {
                    // This is a path or has whitespace, fully quote+escape.
                    sb.Append(QuoteArgument(arg));
                }
                else if (arg.IndexOf('"') >= 0)
                {
                    // This contains a '"' (but isn't case #1), just escape the quotes.
                    sb.Append(EscapeQuotes(arg));
                }
                else
                {
                    // Otherwise, just append it raw.
                    sb.Append(arg);
                }
            }
            return sb.ToString().TrimRemoveNull() is string commandLine && commandLine.Length > 0 ? commandLine + '\0' : null;
        }

        /// <summary>
        /// Determines whether the specified string represents a valid file or network path.
        /// </summary>
        /// <remarks>A valid file path must start with a drive letter followed by a colon and a backslash
        /// or forward slash (e.g., "C:\"). A valid network path must start with two backslashes (e.g.,
        /// "\\server").</remarks>
        /// <param name="s">The string to evaluate as a potential path.</param>
        /// <returns><see langword="true"/> if the string represents a valid file path (e.g., "C:\path") or network path (e.g.,
        /// "\\server\share"); otherwise, <see langword="false"/>.</returns>
        private static bool IsPath(string s)
        {
            if (s.Length >= 3 && char.IsLetter(s[0]) && s[1] == ':' && (s[2] == '\\' || s[2] == '/'))
            {
                // We've got a drive letter path.
                return true;
            }
            if (s.Length >= 2 && s[0] == '\\' && s[1] == '\\')
            {
                // We've got a UNC path.
                return true;
            }
            return false;
        }

        /// <summary>
        /// Determines the starting index of the first valid path segment in the specified string.
        /// </summary>
        /// <remarks>A valid path segment is defined as either: <list type="bullet"> <item>A drive letter
        /// path in the format "X:\\" or "X:/", where X is a letter.</item> <item>A UNC path starting with "\\".</item>
        /// </list></remarks>
        /// <param name="s">The string to search for a valid path segment. Cannot be <see langword="null"/>.</param>
        /// <param name="offset">The zero-based index at which to begin the search. Must be greater than or equal to 0 and less than the
        /// length of <paramref name="s"/>.</param>
        /// <returns>The zero-based index of the first valid path segment if found; otherwise, -1.</returns>
        private static int GetFirstPathIndex(string s, int offset = 0)
        {
            // Handle drive letter paths.
            for (int i = offset; i + 2 < s.Length; i++)
            {
                if (char.IsLetter(s[i]) && s[i + 1] == ':' && (s[i + 2] == '\\' || s[i + 2] == '/'))
                {
                    return i;
                }
            }

            // Handle UNC paths.
            for (int i = offset; i + 1 < s.Length; i++)
            {
                if (s[i] == '\\' && s[i + 1] == '\\')
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Processes the current token in the buffer and appends it to the list of arguments.
        /// </summary>
        /// <remarks>The method handles the token in the following ways: <list type="bullet"> <item>
        /// <description>If the token contains any double quotes (<c>"</c>), it is added to the list
        /// as-is.</description> </item> <item> <description>If a path is detected within the token, the path portion is
        /// quoted and appended to the list.</description> </item> <item> <description>If no path is detected, the token
        /// is added to the list in its raw form.</description> </item> </list></remarks>
        /// <param name="args">The list of arguments to which the processed token will be added.</param>
        /// <param name="buf">The buffer containing the current token to process.</param>
        private static void FlushArg(List<string> args, StringBuilder buf)
        {
            args.Add(buf.ToString());
            buf.Clear();
        }

        /// <summary>
        /// Quotes a string for use as a command-line argument, ensuring proper escaping of special characters.
        /// </summary>
        /// <remarks>This method ensures that the returned string is properly formatted for scenarios
        /// where command-line arguments require quoting and escaping, such as when passing arguments to  external
        /// processes. Double quotes within the input string are escaped, and trailing backslashes are handled
        /// correctly to prevent misinterpretation.</remarks>
        /// <param name="s">The string to be quoted and escaped.</param>
        /// <returns>A quoted string that is safe to use as a command-line argument. Special characters, such as double quotes
        /// and backslashes, are properly escaped.</returns>
        private static string QuoteArgument(string s)
        {
            // Process the string to escape quotes and handle backslashes.
            StringBuilder sb = new("\""); int backslashes = 0;
            foreach (char c in s)
            {
                if (c == '\\')
                {
                    // Count backslashes and append them.
                    backslashes++;
                    sb.Append(c);
                }
                else if (c == '"')
                {
                    // Escape any run of backslashes before a quote.
                    sb.Append(new string('\\', backslashes)); backslashes = 0;
                    sb.Append("\\\"");
                }
                else
                {
                    // For any other character, reset backslashes and append the character.
                    backslashes = 0;
                    sb.Append(c);
                }
            }

            // Escape trailing backslashes.
            if (backslashes > 0)
            {
                sb.Append(new string('\\', backslashes));
            }

            // Close the quote and return the result.
            sb.Append('"');
            return sb.ToString();
        }

        /// <summary>
        /// Escapes double-quote characters in the specified string by prefixing them with a backslash.
        /// </summary>
        /// <param name="s">The input string to process. Cannot be <see langword="null"/>.</param>
        /// <returns>A new string where each double-quote character (<c>"</c>) is replaced with an escaped sequence (<c>\"</c>),
        /// and any preceding backslashes are preserved.</returns>
        private static string EscapeQuotes(string s)
        {
            // Process the string to escape quotes and handle backslashes.
            StringBuilder sb = new(); int backslashes = 0;
            foreach (char c in s)
            {
                if (c == '\\')
                {
                    // Count backslashes and append them.
                    backslashes++;
                    sb.Append(c);
                }
                else if (c == '"')
                {
                    // Escape any run of backslashes before a quote.
                    sb.Append(new string('\\', backslashes)); backslashes = 0;
                    sb.Append("\\\"");
                }
                else
                {
                    // For any other character, reset backslashes and append the character.
                    backslashes = 0;
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }
    }
}
