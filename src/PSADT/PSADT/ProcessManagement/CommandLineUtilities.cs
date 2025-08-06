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
        /// Parses a Windows command line string into an array of arguments using unified Windows parsing rules
        /// with optional path detection for unquoted file paths containing spaces.
        /// </summary>
        /// <param name="commandLine">The command line string to parse.</param>
        /// <param name="strict">If true, use strict escaping rules. If false, use compatible escaping rules.</param>
        /// <returns>An array of argument strings.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="commandLine"/> is null or whitespace.</exception>
        /// <remarks>
        /// This method implements a unified parser that combines the behaviors of CommandLineToArgv(),
        /// msvcrt pre-2008, msvcrt post-2008, and other Windows standards into a single, comprehensive parser.
        /// When <paramref name="strict"/> is false, the parser will attempt to detect unquoted
        /// DOS drive paths (like C:\Program Files\app.exe) and UNC paths (like \\server\share\file.exe)
        /// that contain spaces and group them as single arguments.
        /// </remarks>
        public static IReadOnlyList<string> CommandLineToArgumentList(string commandLine, bool strict = false)
        {
            if (commandLine is null)
            {
                throw new ArgumentNullException(nameof(commandLine));
            }
            if (string.IsNullOrWhiteSpace(commandLine))
            {
                return Array.Empty<string>();
            }
            if (strict)
            {
                return CommandLineToArgumentListStrict(commandLine.AsSpan());
            }
            return CommandLineToArgumentListEnhanced(commandLine.AsSpan());
        }

        /// <summary>
        /// Internal unified command line parser that implements all Windows parsing rules.
        /// </summary>
        /// <param name="commandLine">The command line span to parse.</param>
        /// <returns>A list of parsed arguments.</returns>
        private static IReadOnlyList<string> CommandLineToArgumentListStrict(ReadOnlySpan<char> commandLine)
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
        /// Internal unified command line parser that implements all Windows parsing rules with path detection.
        /// </summary>
        /// <param name="commandLine">The command line span to parse.</param>
        /// <returns>A list of parsed arguments.</returns>
        private static IReadOnlyList<string> CommandLineToArgumentListEnhanced(ReadOnlySpan<char> commandLine)
        {
            // Build the argument list from the command line span and return it.
            List<string> arguments = []; int position = 0;
            while (position < commandLine.Length)
            {
                // Check for key=value patterns first - these should be parsed with special handling.
                // Following that, check for unquoted paths, otherwise just parse the argument.
                SkipWhitespace(commandLine, ref position);
                if (position >= commandLine.Length)
                {
                    break;
                }
                if (IsKeyValueArgument(commandLine, position))
                {
                    arguments.Add(ParseKeyValueArgument(commandLine, ref position));
                }
                else if (IsAtStartOfUnquotedPath(commandLine, position))
                {
                    arguments.Add(ParseUnquotedPath(commandLine, ref position));
                }
                else
                {
                    arguments.Add(ParseSingleArgument(commandLine, ref position));
                }
            }
            return arguments.AsReadOnly();
        }

        /// <summary>
        /// Determines if the current position starts a key=value argument pattern.
        /// </summary>
        /// <param name="commandLine">The command line span.</param>
        /// <param name="position">The current position.</param>
        /// <returns>True if this looks like a key=value argument.</returns>
        private static bool IsKeyValueArgument(ReadOnlySpan<char> commandLine, int position)
        {
            // Look for a pattern like: word=value where word doesn't contain spaces.
            int equalPos = position;
            bool foundKey = false;

            // Look for the key part (no spaces allowed).
            while (equalPos < commandLine.Length && !IsWhitespace(commandLine[equalPos]))
            {
                if (commandLine[equalPos] == '=')
                {
                    // We need at least one character for the key.
                    foundKey = equalPos > position;
                    break;
                }
                equalPos++;
            }
            return foundKey;
        }

        /// <summary>
        /// Parses a key=value argument that may have an unquoted path as the value.
        /// </summary>
        /// <param name="commandLine">The command line span.</param>
        /// <param name="position">The current position (updated as parsing progresses).</param>
        /// <returns>The parsed key=value argument.</returns>
        private static string ParseKeyValueArgument(ReadOnlySpan<char> commandLine, ref int position)
        {
            // Parse the key part (up to the =).
            StringBuilder result = new();
            while (position < commandLine.Length && commandLine[position] != '=')
            {
                result.Append(commandLine[position]);
                position++;
            }

            // Add the = sign.
            if (position < commandLine.Length)
            {
                result.Append(commandLine[position]);
                position++;
            }

            // Now parse the value part - this might be a path with spaces.
            if (position < commandLine.Length)
            {
                // Check if the value starts with a quote - if so, use standard parsing.
                if (commandLine[position] == '"')
                {
                    // Use standard argument parsing for quoted values.
                    string quotedValue = ParseSingleArgument(commandLine, ref position);
                    result.Append(quotedValue);
                }
                else
                {
                    // Parse unquoted value - might be a path with spaces.
                    string value = ParseUnquotedValueForKeyValue(commandLine, ref position);
                    result.Append(value);
                }
            }
            return result.ToString();
        }

        /// <summary>
        /// Parses the value part of a key=value argument, handling paths with spaces.
        /// </summary>
        /// <param name="commandLine">The command line span.</param>
        /// <param name="position">The current position (updated as parsing progresses).</param>
        /// <returns>The parsed value.</returns>
        private static string ParseUnquotedValueForKeyValue(ReadOnlySpan<char> commandLine, ref int position)
        {
            // Check if the value looks like a path.
            if (!IsAtStartOfUnquotedPath(commandLine, position))
            {
                // Parse as a regular value (stops at whitespace).
                StringBuilder value = new();
                while (position < commandLine.Length && !IsWhitespace(commandLine[position]))
                {
                    value.Append(commandLine[position]);
                    position++;
                }
                return value.ToString();
            }
            else
            {
                // Parse as a path that might have spaces.
                return ParseUnquotedPath(commandLine, ref position);
            }
        }

        /// <summary>
        /// Determines if the current position is at the start of an unquoted file path.
        /// </summary>
        /// <param name="commandLine">The command line span.</param>
        /// <param name="position">The current position.</param>
        /// <returns>True if at the start of a unquoted DOS drive path or UNC path.</returns>
        private static bool IsAtStartOfUnquotedPath(ReadOnlySpan<char> commandLine, int position)
        {
            // We're at the start of nothing if we're at the end of the command line.
            if (position >= commandLine.Length)
            {
                return false;
            }

            // Check for UNC path (starts with \\).
            if (position + 1 < commandLine.Length && commandLine[position] == '\\' && commandLine[position + 1] == '\\')
            {
                // If the characters following the initial \\ are more backslashes followed by a quote,
                // it's likely an escaped argument, not a UNC path. Let ParseSingleArgument handle it.
                int p = position + 2;
                while (p < commandLine.Length && commandLine[p] == '\\')
                {
                    p++;
                }
                if (p < commandLine.Length && commandLine[p] == '"')
                {
                    return false;
                }
                return true;
            }

            // Check for DOS drive path (starts with letter:\ or letter:/).
            if (position + 2 < commandLine.Length && char.IsLetter(commandLine[position]) && commandLine[position + 1] == ':' && (commandLine[position + 2] == '\\' || commandLine[position + 2] == '/'))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Parses an unquoted file path that may contain spaces.
        /// </summary>
        /// <param name="commandLine">The command line span.</param>
        /// <param name="position">The current position (updated as parsing progresses).</param>
        /// <returns>The parsed path argument.</returns>
        private static string ParseUnquotedPath(ReadOnlySpan<char> commandLine, ref int position)
        {
            // Parse tokens until we hit the end or find something that looks like a new argument.
            List<string> tokens = []; List<int> tokenPositions = [];
            int initialPosition = position;
            while (position < commandLine.Length)
            {
                // Skip any leading whitespace.
                int beforeWhitespace = position;
                SkipWhitespace(commandLine, ref position);
                if (position >= commandLine.Length)
                {
                    break;
                }
                
                // Check if this position starts a new argument (but not for the first token).
                if (tokens.Count > 0 && IsStartOfNewArgument(commandLine, position))
                {
                    // Reset position to before the whitespace so the next parser can handle it.
                    position = beforeWhitespace;
                    break;
                }
                
                // Record the start position of this token.
                tokenPositions.Add(position);
                
                // Parse the current token (non-whitespace characters).
                StringBuilder tokenBuilder = new();
                while (position < commandLine.Length && !IsWhitespace(commandLine[position]))
                {
                    // Check for characters that should break path parsing AFTER being included
                    // in the current token. This way "C:\Windows;" becomes one token, not two.
                    char ch = commandLine[position];
                    bool shouldBreakAfterChar = (ch == ';' || ch == '|' || ch == '&' || ch == '<' || ch == '>' || ch == '^');
                    tokenBuilder.Append(ch);
                    position++;
                    
                    // If this character should break path parsing, do so after including it.
                    if (shouldBreakAfterChar)
                    {
                        break;
                    }
                }
                
                if (tokenBuilder.Length > 0)
                {
                    // If the last character we added was a special character, break out of path parsing.
                    tokens.Add(tokenBuilder.ToString()); string currentToken = tokenBuilder.ToString();
                    if (currentToken.Length > 0)
                    {
                        char lastChar = currentToken[currentToken.Length - 1];
                        if (lastChar == ';' || lastChar == '|' || lastChar == '&' || lastChar == '<' || lastChar == '>' || lastChar == '^')
                        {
                            break;
                        }
                    }
                }
            }

            // Find the optimal breakpoint for the path. If we found a breakpoint, adjust the position to point to the start of the next argument.
            var pathInfo = FindOptimalPathFromTokens(tokens);
            if (pathInfo.TokenCount < tokens.Count)
            {
                position = tokenPositions[pathInfo.TokenCount];
            }
            else if (pathInfo.Path.EndsWith("\\") && position < commandLine.Length)
            {
                // If the parsed path ends with a backslash, it's likely a directory.
                // The original logic might have consumed a following argument.
                // Let's check if what follows the path is a new argument.
                int potentialNextArgPos = initialPosition + pathInfo.Path.Length;
                SkipWhitespace(commandLine, ref potentialNextArgPos);
                if (potentialNextArgPos < commandLine.Length && IsStartOfNewArgument(commandLine, potentialNextArgPos))
                {
                    // The path seems to be followed by a new argument, so don't include it.
                    position = initialPosition + pathInfo.Path.TrimEnd().Length;
                }
            }
            return pathInfo.Path;
        }

        /// <summary>
        /// Determines the optimal path from a list of tokens by looking for executable extensions
        /// and argument patterns.
        /// </summary>
        /// <param name="tokens">The list of tokens to analyze.</param>
        /// <returns>Information about the optimal path and how many tokens it consumes.</returns>
        private static (string Path, int TokenCount) FindOptimalPathFromTokens(List<string> tokens)
        {
            // Verify the supplied tokens before proceeding.
            if (tokens.Count == 0)
            {
                return (string.Empty, 0);
            }
            if (tokens.Count == 1)
            {
                return (tokens[0], 1);
            }

            // Try to find where the executable path ends by looking for executable extensions.
            string[] executableExtensions = { ".exe", ".msi", ".bat", ".cmd", ".com", ".scr" };
            for (int i = 0; i < tokens.Count; i++)
            {
                // Check if this part ends with an executable extension.
                string currentPath = string.Join(" ", tokens.Take(i + 1));
                foreach (string ext in executableExtensions)
                {
                    if (currentPath.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                    {
                        return (currentPath, i + 1);
                    }
                }
            }

            // PRIORITY 1: Look for argument-like patterns to stop at. This should take precedence over any path-specific logic.
            for (int i = 1; i < tokens.Count; i++)
            {
                if (IsArgumentLike(tokens[i]))
                {
                    return (string.Join(" ", tokens.Take(i)), i);
                }
            }

            // PRIORITY 2: Check for tokens that end with special characters that indicate command separation.
            for (int i = 0; i < tokens.Count; i++)
            {
                string token = tokens[i];
                if (token.Length > 0)
                {
                    char lastChar = token[token.Length - 1];
                    if (lastChar == ';' || lastChar == '|' || lastChar == '&' || 
                        lastChar == '<' || lastChar == '>' || lastChar == '^')
                    {
                        // This token contains a command separator, so the path ends here.
                        return (string.Join(" ", tokens.Take(i + 1)), i + 1);
                    }
                }
            }

            // PRIORITY 3: For UNC paths without executable extensions, apply conservative rules.
            string combinedPath = string.Join(" ", tokens);
            if (combinedPath.StartsWith("\\\\"))
            {
                // If a token ends with a backslash, it's likely a directory. The path ends here.
                for (int i = 0; i < tokens.Count - 1; i++)
                {
                    if (tokens[i].EndsWith("\\"))
                    {
                        return (string.Join(" ", tokens.Take(i + 1)), i + 1);
                    }
                }

                // For UNC paths, if we have more than 4 tokens, be conservative but don't override argument detection.
                if (tokens.Count > 4)
                {
                    // Look for patterns that suggest we've gone too far (Tokens containing special shell characters).
                    // Allow \\server\share\folder before being strict.
                    for (int i = 3; i < tokens.Count; i++)
                    {
                        string token = tokens[i];
                        
                        if (token.Contains(';') || token.Contains('|') || token.Contains('&') ||
                            token.Contains('<') || token.Contains('>') || token.Contains('^'))
                        {
                            return (string.Join(" ", tokens.Take(i)), i);
                        }
                    }
                    
                    // Only apply the "penultimate token" rule if there are no obvious arguments.
                    // Check if the last token could reasonably be part of a path.
                    string lastToken = tokens[tokens.Count - 1];
                    if (!lastToken.StartsWith("/") && !lastToken.StartsWith("-") && 
                        !lastToken.Contains("=") && !lastToken.StartsWith("{"))
                    {
                        return (string.Join(" ", tokens.Take(tokens.Count - 1)), tokens.Count - 1);
                    }
                }
            }

            // PRIORITY 4: For regular drive paths without extensions, be conservative too.
            if (tokens.Count > 3 && combinedPath.Length > 2 && char.IsLetter(combinedPath[0]) && combinedPath[1] == ':')
            {
                // Check if the last token looks like an argument
                string lastToken = tokens[tokens.Count - 1];
                if (IsArgumentLike(lastToken))
                {
                    return (string.Join(" ", tokens.Take(tokens.Count - 1)), tokens.Count - 1);
                }
            }

            // Default to combining all tokens as a single path.
            return (string.Join(" ", tokens), tokens.Count);
        }

        /// <summary>
        /// Determines if the current position starts a new argument (like a flag or option).
        /// </summary>
        /// <param name="commandLine">The command line span.</param>
        /// <param name="position">The current position.</param>
        /// <returns>True if this looks like the start of a new argument.</returns>
        private static bool IsStartOfNewArgument(ReadOnlySpan<char> commandLine, int position)
        {
            // There's no new arguments if we're at the end of the command line.
            if (position >= commandLine.Length)
            {
                return false;
            }

            // Check for common argument patterns.
            char ch = commandLine[position];
            if (ch == '/' || ch == '-')
            {
                return true;
            }

            // Check for quoted arguments.
            if (ch == '"')
            {
                return true;
            }

            // Check for GUID-like patterns (often used as arguments in installers).
            if (ch == '{')
            {
                return true;
            }

            // Check for key=value patterns.
            int equalPos = position;
            while (equalPos < commandLine.Length && !IsWhitespace(commandLine[equalPos]))
            {
                if (commandLine[equalPos] == '=')
                {
                    return true;
                }
                equalPos++;
            }

            // If none of the above patterns match, it might be a continuation of the path.
            return false;
        }

        /// <summary>
        /// Determines if a string looks like a command line argument rather than part of a path.
        /// </summary>
        /// <param name="part">The string part to check.</param>
        /// <returns>True if it looks like an argument.</returns>
        private static bool IsArgumentLike(string part)
        {
            // Empty strings aren't argument-like.
            if (string.IsNullOrWhiteSpace(part))
            {
                return false;
            }

            // Check for flag patterns.
            if (part.StartsWith("/") || part.StartsWith("-"))
            {
                return true;
            }

            // Check for key=value patterns.
            if (part.Contains("="))
            {
                return true;
            }

            // Check for GUID patterns.
            if (part.StartsWith("{") && part.EndsWith("}"))
            {
                return true;
            }
            return false;
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
                // If we're not in quotes and we hit whitespace, we're done with this argument.
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
