using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using PSADT.Extensions;
using PSADT.FileSystem;

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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Enforcing this rule just makes a mess.")]
        public static IReadOnlyList<string> CommandLineToArgumentList(string commandLine, bool strict = false)
        {
            if (string.IsNullOrWhiteSpace(commandLine))
            {
                throw new ArgumentNullException(nameof(commandLine));
            }
            if (strict)
            {
                return CommandLineToArgumentListStrict(commandLine.TrimRemoveNull().AsSpan());
            }
            return CommandLineToArgumentListEnhanced(commandLine.TrimRemoveNull().AsSpan());
        }

        /// <summary>
        /// Converts an array of arguments back into a properly escaped Windows command line string.
        /// </summary>
        /// <param name="argv">The array of arguments to convert.</param>
        /// <param name="strict">If true, use strict escaping rules. If false, use compatible escaping rules.</param>
        /// <returns>A properly escaped command line string.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="argv"/> is null.</exception>
        /// <remarks>
        /// This method ensures that the resulting command line, when parsed back through 
        /// <see cref="CommandLineToArgumentList(string, bool)"/>, will yield the original arguments.
        /// Special characters are properly escaped according to Windows conventions.
        /// </remarks>
        public static string ArgumentListToCommandLine(IReadOnlyList<string> argv, bool strict = false)
        {
            // Consider a null or empty argument list as an error.
            if (!(argv?.Count > 0))
            {
                throw new ArgumentNullException("The specified enumerable is null or empty.", (Exception?)null);
            }

            // Construct and return the command line string.
            StringBuilder sb = new();
            if (!strict)
            {
                foreach (string arg in argv)
                {
                    if (string.IsNullOrWhiteSpace(arg))
                    {
                        throw new ArgumentNullException("The specified enumerable contains null or empty arguments.", (Exception?)null);
                    }
                    _ = sb.Append(EscapeArgumentCompatible(arg));
                    _ = sb.Append(' ');
                }
            }
            else
            {
                foreach (string arg in argv)
                {
                    if (string.IsNullOrWhiteSpace(arg))
                    {
                        throw new ArgumentNullException("The specified enumerable contains null or empty arguments.", (Exception?)null);
                    }
                    _ = sb.Append(EscapeArgumentStrict(arg));
                    _ = sb.Append(' ');
                }
            }
            return sb.ToString().TrimRemoveNull();
        }

        /// <summary>
        /// Internal unified command line parser that implements all Windows parsing rules.
        /// </summary>
        /// <param name="commandLine">The command line span to parse.</param>
        /// <returns>A list of parsed arguments.</returns>
        private static ReadOnlyCollection<string> CommandLineToArgumentListStrict(ReadOnlySpan<char> commandLine)
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
        private static ReadOnlyCollection<string> CommandLineToArgumentListEnhanced(ReadOnlySpan<char> commandLine)
        {
            // Build the argument list from the command line span and return it.
            List<string> arguments = []; int position = 0;
            while (position < commandLine.Length)
            {
                // Check for key=value patterns first - these should be parsed with special handling.
                // Following that, check for flag+quoted-path patterns, then unquoted paths,
                // otherwise just parse the argument.
                SkipWhitespace(commandLine, ref position);
                if (position >= commandLine.Length)
                {
                    break;
                }
                if (IsKeyValueArgument(commandLine, position))
                {
                    arguments.Add(ParseKeyValueArgument(commandLine, ref position));
                }
                else if (IsFlagWithQuotedPath(commandLine, position))
                {
                    arguments.Add(ParseFlagWithQuotedPath(commandLine, ref position));
                }
                else if (IsAtStartOfUnquotedPath(commandLine, position))
                {
                    arguments.Add(ParseUnquotedPath(commandLine, ref position));
                }
                else
                {
                    arguments.Add(ConvertPosixPathToWindows(ParseSingleArgument(commandLine, ref position)));
                }
            }
            return arguments.AsReadOnly();
        }

        /// <summary>
        /// Determines if the current position starts a flag with a quoted path pattern (e.g., -sfx_o"C:\Path").
        /// </summary>
        /// <param name="commandLine">The command line span.</param>
        /// <param name="position">The current position.</param>
        /// <returns>True if this looks like a flag with a quoted path.</returns>
        private static bool IsFlagWithQuotedPath(ReadOnlySpan<char> commandLine, int position)
        {
            // Must start with - or /
            if (position >= commandLine.Length || (commandLine[position] != '-' && commandLine[position] != '/'))
            {
                return false;
            }

            // Look for the pattern: flag characters followed by a quote, then a path
            int i = position + 1;

            // Skip flag name characters (letters, digits, underscores, hyphens)
            while (i < commandLine.Length && (char.IsLetterOrDigit(commandLine[i]) || commandLine[i] == '_' || commandLine[i] == '-'))
            {
                i++;
            }

            // Must have at least one flag character and be followed by a quote
            if (i <= position + 1 || i >= commandLine.Length || commandLine[i] != '"')
            {
                return false;
            }

            // Check if after the quote we have a path (drive letter or UNC)
            int afterQuote = i + 1;
            if (afterQuote >= commandLine.Length)
            {
                return false;
            }

            // Check for drive letter pattern (X:)
            if (afterQuote + 1 < commandLine.Length && char.IsLetter(commandLine[afterQuote]) && commandLine[afterQuote + 1] == ':')
            {
                return true;
            }

            // Check for UNC path (\\)
            return afterQuote + 1 < commandLine.Length && commandLine[afterQuote] == '\\' && commandLine[afterQuote + 1] == '\\';
        }

        /// <summary>
        /// Parses a flag with a quoted path pattern, preserving the quotes (e.g., -sfx_o"C:\Path" remains as-is).
        /// </summary>
        /// <param name="commandLine">The command line span.</param>
        /// <param name="position">The current position (updated as parsing progresses).</param>
        /// <returns>The parsed argument with quotes preserved only if needed.</returns>
        private static string ParseFlagWithQuotedPath(ReadOnlySpan<char> commandLine, ref int position)
        {
            // Parse the flag prefix (- or / followed by flag name)
            StringBuilder flagPart = new(); StringBuilder pathPart = new();
            while (position < commandLine.Length && commandLine[position] != '"')
            {
                _ = flagPart.Append(commandLine[position]);
                position++;
            }

            // Skip the opening quote
            if (position < commandLine.Length && commandLine[position] == '"')
            {
                position++;
            }

            // Parse until we find the closing quote or end of argument
            while (position < commandLine.Length)
            {
                char c = commandLine[position];
                if (c == '"')
                {
                    // Check for escaped quote ("")
                    if (position + 1 < commandLine.Length && commandLine[position + 1] == '"')
                    {
                        _ = pathPart.Append('"');
                        position += 2;
                        continue;
                    }

                    // Closing quote - skip it and finish
                    position++;
                    break;
                }
                else if (c == '\\')
                {
                    // Handle backslash sequences
                    int backslashStart = position;
                    while (position < commandLine.Length && commandLine[position] == '\\')
                    {
                        position++;
                    }
                    int backslashCount = position - backslashStart;

                    // Backslashes are preserved as-is regardless of what follows
                    _ = pathPart.Append('\\', backslashCount);
                }
                else
                {
                    _ = pathPart.Append(c);
                    position++;
                }
            }

            // Check if the path actually needs quoting (contains spaces, tabs, or quotes)
            string path = pathPart.ToString();
            bool needsQuotes = path.Any(static c => IsWhitespace(c) || c == '"');
            return needsQuotes ? $"{flagPart}\"{path}\"" : $"{flagPart}{path}";
        }

        /// <summary>
        /// Determines if the current position starts a key=value argument pattern.
        /// </summary>
        /// <param name="commandLine">The command line span.</param>
        /// <param name="position">The current position.</param>
        /// <returns>True if this looks like a key=value argument.</returns>
        private static bool IsKeyValueArgument(ReadOnlySpan<char> commandLine, int position)
        {
            // If the argument starts with a quote, it should be parsed as a single argument,
            // not as a key=value pair. The quotes wrap the entire argument.
            if (position < commandLine.Length && commandLine[position] == '"')
            {
                return false;
            }

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
                _ = result.Append(commandLine[position]);
                position++;
            }

            // Add the = sign.
            if (position < commandLine.Length)
            {
                _ = result.Append(commandLine[position]);
                position++;
            }

            // Now parse the value part - this might be a path with spaces.
            if (position < commandLine.Length)
            {
                // Check if the value starts with a quote.
                if (commandLine[position] == '"')
                {
                    // Remember where the quoted value starts
                    int valueStartPosition = position;

                    // Parse the quoted value to get its content.
                    int tempPosition = position;
                    string quotedValue = ParseSingleArgument(commandLine, ref tempPosition);
                    string convertedValue = ConvertPosixPathToWindows(quotedValue);

                    // Check if the value actually needs quoting (contains spaces, tabs, or quotes)
                    bool needsQuotes = convertedValue.Any(static c => IsWhitespace(c) || c == '"');
                    if (needsQuotes)
                    {
                        // Value needs quotes - check if POSIX conversion happened
                        if (convertedValue != quotedValue)
                        {
                            // The value was converted from POSIX, so we need to rebuild the quoted portion.
                            _ = result.Append('"').Append(convertedValue).Append('"');
                        }
                        else
                        {
                            // Preserve the original raw format to maintain exact semantics
                            string quotedPath = commandLine.Slice(valueStartPosition, tempPosition - valueStartPosition).ToString();
                            _ = result.Append(quotedPath);
                        }
                    }
                    else
                    {
                        // Value doesn't need quotes - use it without quotes
                        _ = result.Append(convertedValue);
                    }

                    // Update the main position to continue parsing after this key-value pair.
                    position = tempPosition;
                }
                else
                {
                    // Parse unquoted value - might be a path with spaces.
                    string value = ConvertPosixPathToWindows(ParseUnquotedValueForKeyValue(commandLine, ref position));
                    _ = value.Contains(" ") && !value.StartsWith("\"") ? result.Append('"').Append(value).Append('"') : result.Append(value);
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
                    _ = value.Append(commandLine[position]);
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
        /// <returns>True if at the start of a unquoted DOS drive path, UNC path, or POSIX path.</returns>
        private static bool IsAtStartOfUnquotedPath(ReadOnlySpan<char> commandLine, int position)
        {
            return FileSystemUtilities.IsValidFilePath(commandLine, position);
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
                    _ = tokenBuilder.Append(commandLine[position]);
                    position++;
                }
                if (tokenBuilder.Length > 0)
                {
                    tokens.Add(tokenBuilder.ToString());
                }
            }

            // Find the optimal breakpoint for the path. If we found a breakpoint, adjust the position to point to the start of the next argument.
            (string Path, int TokenCount) = FindOptimalPathFromTokens(tokens);
            if (TokenCount < tokens.Count)
            {
                position = tokenPositions[TokenCount];
            }
            else if (Path.EndsWith("\\") && position < commandLine.Length)
            {
                // If the parsed path ends with a backslash, it's likely a directory.
                // The original logic might have consumed a following argument.
                // Let's check if what follows the path is a new argument.
                int potentialNextArgPos = initialPosition + Path.Length;
                SkipWhitespace(commandLine, ref potentialNextArgPos);
                if (potentialNextArgPos < commandLine.Length && IsStartOfNewArgument(commandLine, potentialNextArgPos))
                {
                    // The path seems to be followed by a new argument, so don't include it.
                    position = initialPosition + Path.TrimEnd().Length;
                }
            }
            return ConvertPosixPathToWindows(Path);
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
            string[] executableExtensions = [".exe", ".msi", ".bat", ".cmd", ".com", ".scr"];
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
                    if (lastChar is ';' or '|' or '&' or '<' or '>' or '^')
                    {
                        // This token contains a command separator, so the path ends here.
                        return (string.Join(" ", tokens.Take(i + 1)), i + 1);
                    }
                }
            }

            // PRIORITY 3: For UNC paths without executable extensions, apply conservative rules.
            string combinedPath = string.Join(" ", tokens);
            if (combinedPath.StartsWith("\\\\", StringComparison.OrdinalIgnoreCase))
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

                        if (token.Contains(";") || token.Contains("|") || token.Contains("&") ||
                            token.Contains("<") || token.Contains(">") || token.Contains("^"))
                        {
                            return (string.Join(" ", tokens.Take(i)), i);
                        }
                    }

                    // Only apply the "penultimate token" rule if there are no obvious arguments.
                    // Check if the last token could reasonably be part of a path.
                    string lastToken = tokens[tokens.Count - 1];
                    if (!lastToken.StartsWith("/") && !lastToken.StartsWith("-") && !lastToken.Contains("=") && !lastToken.StartsWith("{"))
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
            if (ch is '/' or '-')
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
            return part.StartsWith("{") && part.EndsWith("}");
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
                        _ = argument.Append('\\', backslashCount / 2);
                        if (backslashCount % 2 == 1)
                        {
                            _ = argument.Append('"'); // Escaped quote.
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
                        _ = argument.Append('\\', backslashCount);
                    }
                }
                else if (c == '"')
                {
                    // Check for MSVCRT's "" escape sequence inside a quoted argument.
                    if (inQuote && position + 1 < commandLine.Length && commandLine[position + 1] == '"')
                    {
                        _ = argument.Append('"');
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
                    _ = argument.Append(c);
                    position++;
                }
            }
            return argument.ToString();
        }

        /// <summary>
        /// Converts a POSIX-like path (e.g., /C/Users/...) to a Windows path (C:\Users\...).
        /// </summary>
        /// <param name="path">The path to convert.</param>
        /// <returns>The converted Windows path, or the original path if it doesn't match the pattern.</returns>
        private static string ConvertPosixPathToWindows(string path)
        {
            if (path.Length >= 3 && path[0] == '/' && char.IsLetter(path[1]) && path[2] == '/')
            {
                // This looks like a POSIX-style path, e.g., /C/Program Files/app.exe
                // Convert it to a Windows-style path, e.g., C:\Program Files\app.exe
                return $"{path[1]}:\\{path.Substring(3).Replace('/', '\\')}";
            }
            return path;
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
            return c is ' ' or '\t';
        }

        /// <summary>
        /// Escapes a command-line argument to ensure compatibility with a parser that supports key-value pairs and
        /// quoted values.
        /// </summary>
        /// <remarks>This method ensures compatibility with parsers that handle quoted values in key-value
        /// pairs. If the value in a key-value pair is already quoted, it is assumed to be correctly formatted and
        /// returned as-is. For all other cases, strict escaping is applied to ensure the argument is properly
        /// formatted.</remarks>
        /// <param name="argument">The command-line argument to escape. Can be a key-value pair (e.g., "key=value") or a single value.</param>
        /// <returns>A string representing the escaped argument. If the argument is <see langword="null"/>, returns an empty
        /// quoted string (<c>""</c>). If the argument is a key-value pair with a quoted value, the original argument is
        /// returned unchanged. Otherwise, the argument is escaped using strict escaping rules.</returns>
        private static string EscapeArgumentCompatible(string argument)
        {
            // Return empty quotes for a null argument.
            if (argument is null)
            {
                return "\"\"";
            }

            // Check if the argument is a key-value pair where the value is already quoted.
            int equalsPos = argument.IndexOf("=");
            if (equalsPos > 0 && equalsPos < argument.Length - 1)
            {
                string key = argument.Substring(0, equalsPos);
                string value = argument.Substring(equalsPos + 1);
                if (value.StartsWith("\"") && value.EndsWith("\"") && value.Length >= 2)
                {
                    // Extract the actual value content (without the outer quotes)
                    string valueContent = value.Substring(1, value.Length - 2);

                    // Check if the value actually needs quoting (contains spaces, tabs, or quotes)
                    bool needsQuotes = valueContent.Any(static c => IsWhitespace(c) || c == '"');
                    if (needsQuotes)
                    {
                        // Value needs quotes - return as-is
                        return argument;
                    }
                    else
                    {
                        // Value doesn't need quotes - strip them
                        return $"{key}={valueContent}";
                    }
                }
            }

            // Check for flag+path pattern (e.g., -sfx_oC:\Path\To\Output).
            // This handles cases like 7-Zip's -sfx_o"C:\Path" where the path is attached to the flag.
            if (TryEscapeFlagWithAttachedPath(argument, out string escaped))
            {
                return escaped;
            }

            // For all other cases, use the standard strict escaping.
            return EscapeArgumentStrict(argument);
        }

        /// <summary>
        /// Attempts to escape an argument that follows the pattern of a flag with an attached path value.
        /// </summary>
        /// <param name="argument">The argument to check and potentially escape.</param>
        /// <param name="escaped">The escaped argument if the pattern matches, otherwise empty.</param>
        /// <returns>True if the argument matched the flag+path pattern and was escaped, false otherwise.</returns>
        /// <remarks>
        /// This handles scenarios like 7-Zip's -sfx_o parameter where the value is attached directly
        /// to the flag without a space, e.g., -sfx_oC:\Program Files\Output should become
        /// -sfx_o"C:\Program Files\Output" rather than "-sfx_oC:\Program Files\Output".
        /// </remarks>
        private static bool TryEscapeFlagWithAttachedPath(string argument, out string escaped)
        {
            // Must start with - or /.
            escaped = string.Empty;
            if (argument.Length < 3 || (argument[0] != '-' && argument[0] != '/'))
            {
                return false;
            }

            // Find where the flag name ends and a path value begins.
            // Look for a drive letter pattern (X:) or UNC path (\\).
            // Skip if we encounter an = sign before the path (that's a key=value pattern).
            int valueStart = -1;
            for (int i = 1; i < argument.Length - 1; i++)
            {
                // If we find an equals sign before a path, this is a key=value pattern, not flag+path.
                if (argument[i] == '=')
                {
                    return false;
                }

                // Check for drive letter pattern: letter followed by colon.
                if (char.IsLetter(argument[i]) && argument[i + 1] == ':')
                {
                    valueStart = i;
                    break;
                }

                // Check for UNC path start.
                if (argument[i] == '\\' && i + 1 < argument.Length && argument[i + 1] == '\\')
                {
                    valueStart = i;
                    break;
                }
            }

            // Need at least one character for the flag name.
            if (valueStart <= 1)
            {
                return false;
            }

            // If the path portion is already quoted (flag ends with " and value ends with "),
            // check if the quotes are actually needed.
            string flagPart = argument.Substring(0, valueStart);
            string valuePart = argument.Substring(valueStart);
            if (flagPart.EndsWith("\"") && valuePart.EndsWith("\""))
            {
                // Extract the actual flag (without trailing quote) and path (without trailing quote)
                string actualFlag = flagPart.Substring(0, flagPart.Length - 1);
                string actualPath = valuePart.Substring(0, valuePart.Length - 1);

                // Check if the path needs quoting (contains spaces, tabs, or quotes)
                bool needsQuotes = actualPath.Any(static c => IsWhitespace(c) || c == '"');

                escaped = needsQuotes ? $"{actualFlag}\"{actualPath}\"" : $"{actualFlag}{actualPath}";
                return true;
            }

            // Only apply special handling if the value needs quoting (contains spaces or quotes).
            if (!valuePart.Any(static c => IsWhitespace(c) || c == '"'))
            {
                return false;
            }

            // Escape only the value part and combine with the flag.
            escaped = flagPart + EscapeArgumentStrict(valuePart);
            return true;
        }

        /// <summary>
        /// Escapes an argument string for safe inclusion in a Windows command line.
        /// </summary>
        /// <param name="argument">The argument to escape.</param>
        /// <returns>The escaped argument string.</returns>
        private static string EscapeArgumentStrict(string argument)
        {
            // Return empty quotes for a null argument.
            if (argument is null)
            {
                return "\"\"";
            }

            // The argument must be quoted if it contains a space, tab, a quote, or is empty.
            bool needsQuoting = argument.Length == 0 || argument.Any(static c => IsWhitespace(c) || c == '"');
            if (!needsQuoting)
            {
                return argument;
            }

            // Escape the argument by doubling backslashes and escaping quotes.
            StringBuilder sb = new(); _ = sb.Append('"');
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
                    _ = sb.Append('\\', backslashes * 2);
                    break;
                }
                else if (argument[i] == '"')
                {
                    // Backslashes preceding a quote are doubled, and the quote is escaped.
                    _ = sb.Append('\\', (backslashes * 2) + 1);
                    _ = sb.Append('"');
                }
                else
                {
                    // Backslashes not followed by a quote are literal.
                    _ = sb.Append('\\', backslashes);
                    _ = sb.Append(argument[i]);
                }
            }
            _ = sb.Append('"'); return sb.ToString();
        }
    }
}
