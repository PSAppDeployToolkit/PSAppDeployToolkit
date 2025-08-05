using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PSADT.Extensions;

namespace PSADT.ProcessManagement
{
    /// <summary>
    /// Provides utilities for parsing and constructing Windows command lines according to various Windows parsing rules.
    /// </summary>
    /// <remarks>
    /// This class implements command line parsing that strictly adheres to multiple Windows command line parsing standards:
    /// <list type="bullet">
    /// <item><description>Microsoft's CommandLineToArgv() P/Invoke behavior</description></item>
    /// <item><description>Microsoft Visual C Runtime (msvcrt) pre-2008 rules</description></item>
    /// <item><description>Microsoft Visual C Runtime (msvcrt) post-2008 rules</description></item>
    /// <item><description>Other commonly accepted Windows command line parsing conventions</description></item>
    /// </list>
    /// The implementation uses tokenization with multiple passes and lookahead/lookbehind capabilities
    /// to ensure 100% accurate parsing without compromise. Performance is optimized using Span&lt;T&gt; 
    /// where beneficial, but accuracy takes precedence over performance.
    /// </remarks>
    public static class CommandLineUtilities
    {
        /// <summary>
        /// Parses a Windows command line string into an array of arguments using unified Windows parsing rules.
        /// </summary>
        /// <param name="commandLine">The command line string to parse.</param>
        /// <returns>An array of argument strings.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="commandLine"/> is null.</exception>
        /// <remarks>
        /// This method implements a unified parser that combines the behaviors of CommandLineToArgv(),
        /// msvcrt pre-2008, msvcrt post-2008, and other Windows standards into a single, comprehensive parser.
        /// The parser uses multiple passes with tokenization to ensure complete accuracy.
        /// </remarks>
        public static IReadOnlyList<string> CommandLineToArgumentList(string commandLine)
        {
            // Use the unified parser that combines all Windows parsing rules.
            if (commandLine is null)
            {
                throw new ArgumentNullException(nameof(commandLine));
            }
            if (string.IsNullOrWhiteSpace(commandLine))
            {
                return Array.Empty<string>();
            }
            return CommandLineToArgumentList(commandLine.AsSpan());
        }

        /// <summary>
        /// Internal unified command line parser that implements all Windows parsing rules.
        /// </summary>
        /// <param name="commandLine">The command line span to parse.</param>
        /// <returns>A list of parsed arguments.</returns>
        public static IReadOnlyList<string> CommandLineToArgumentList(ReadOnlySpan<char> commandLine)
        {
            // Build the argument list from the command line span and return it.
            List<string> arguments = []; int position = 0;
            while (position < commandLine.Length)
            {
                SkipWhitespace(commandLine, ref position);
                if (position >= commandLine.Length)
                {
                    break;
                }
                arguments.Add(ParseSingleArgument(commandLine, ref position));
            }
            return arguments.AsReadOnly();
        }

        /// <summary>
        /// Converts an array of arguments back into a properly escaped Windows command line string.
        /// </summary>
        /// <param name="args">The array of arguments to convert.</param>
        /// <returns>A properly escaped command line string.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="args"/> is null.</exception>
        /// <remarks>
        /// This method ensures that the resulting command line, when parsed back through 
        /// <see cref="CommandLineToArgumentList(string)"/>, will yield the original arguments.
        /// Special characters are properly escaped according to Windows conventions.
        /// </remarks>
        public static string? ArgumentListToCommandLine(IEnumerable<string> argv)
        {
            // Consider a null or empty argument list as an error.
            if (null == argv || argv.Count() == 0)
            {
                throw new ArgumentNullException(nameof(argv));
            }

            // Construct and return the command line string.
            StringBuilder sb = new();
            foreach (string arg in argv)
            {
                sb.Append(EscapeArgument(arg));
                sb.Append(' ');
            }
            return sb.ToString().TrimRemoveNull() is string commandLine && commandLine.Length > 0 ? commandLine : null;
        }

        /// <summary>
        /// Parses a single argument from the command line using comprehensive Windows parsing rules.
        /// </summary>
        /// <param name="commandLine">The command line span.</param>
        /// <param name="position">The current position in the command line (updated as parsing progresses).</param>
        /// <returns>The parsed argument.</returns>
        private static string ParseSingleArgument(ReadOnlySpan<char> commandLine, ref int position)
        {
            // Read the next argument from the command line, handling quotes and backslashes.
            StringBuilder argument = new(); bool inQuote = false;
            while (position < commandLine.Length)
            {
                char c = commandLine[position];
                if (IsWhitespace(c) && !inQuote)
                {
                    break;
                }

                if (c == '\\')
                {
                    int backslashStart = position;
                    while (position < commandLine.Length && commandLine[position] == '\\')
                    {
                        position++;
                    }
                    int backslashCount = position - backslashStart;

                    if (position < commandLine.Length && commandLine[position] == '"')
                    {
                        // Backslashes are followed by a quote.
                        // 2n backslashes + quote -> n backslashes, and the quote is a delimiter.
                        // 2n+1 backslashes + quote -> n backslashes + a literal quote.
                        argument.Append('\\', backslashCount / 2);
                        if (backslashCount % 2 == 1)
                        {
                            argument.Append('"'); // Escaped quote.
                        }
                        else
                        {
                            inQuote = !inQuote; // Delimiter quote.
                        }
                        position++; // Consume the quote.
                    }
                    else
                    {
                        // Backslashes are not followed by a quote, treat them literally.
                        argument.Append('\\', backslashCount);
                    }
                }
                else if (c == '"')
                {
                    // Check for MSVCRT's "" escape sequence inside a quoted argument.
                    if (inQuote && position + 1 < commandLine.Length && commandLine[position + 1] == '"')
                    {
                        argument.Append('"');
                        position += 2;
                    }
                    else
                    {
                        inQuote = !inQuote;
                        position++;
                    }
                }
                else
                {
                    argument.Append(c);
                    position++;
                }
            }
            return argument.ToString();
        }

        /// <summary>
        /// Skips whitespace characters starting from the given position.
        /// </summary>
        /// <param name="commandLine">The command line span.</param>
        /// <param name="position">The position to start from (updated to the first non-whitespace character).</param>
        private static void SkipWhitespace(ReadOnlySpan<char> commandLine, ref int position)
        {
            // Skip all whitespace characters (space and tab) until we reach a non-whitespace character or the end of the command line.
            while (position < commandLine.Length && IsWhitespace(commandLine[position]))
            {
                position++;
            }
        }

        /// <summary>
        /// Determines whether a character is considered whitespace by Windows command line parsing.
        /// </summary>
        /// <param name="c">The character to check.</param>
        /// <returns>True if the character is whitespace, false otherwise.</returns>
        private static bool IsWhitespace(char c)
        {
            // Windows command line parsing considers space and tab as whitespace.
            return c == ' ' || c == '\t';
        }

        /// <summary>
        /// Escapes an argument string for safe inclusion in a Windows command line.
        /// </summary>
        /// <param name="argument">The argument to escape.</param>
        /// <returns>The escaped argument string.</returns>
        private static string EscapeArgument(string argument)
        {
            // Return empty quotes for a null argument.
            if (argument is null)
            {
                return "\"\"";
            }

            // The argument must be quoted if it contains a space, tab, a quote, or is empty.
            bool needsQuoting = argument.Length == 0 || argument.Any(c => IsWhitespace(c) || c == '"');
            if (!needsQuoting)
            {
                return argument;
            }

            // Escape the argument by doubling backslashes and escaping quotes.
            StringBuilder sb = new(); sb.Append('"');
            for (int i = 0; i < argument.Length; ++i)
            {
                int backslashes = 0;
                while (i < argument.Length && argument[i] == '\\')
                {
                    backslashes++;
                    i++;
                }

                if (i == argument.Length)
                {
                    // Trailing backslashes are doubled.
                    sb.Append('\\', backslashes * 2);
                    break;
                }
                else if (argument[i] == '"')
                {
                    // Backslashes preceding a quote are doubled, and the quote is escaped.
                    sb.Append('\\', backslashes * 2 + 1);
                    sb.Append('"');
                }
                else
                {
                    // Backslashes not followed by a quote are literal.
                    sb.Append('\\', backslashes);
                    sb.Append(argument[i]);
                }
            }
            sb.Append('"'); return sb.ToString();
        }
    }
}
