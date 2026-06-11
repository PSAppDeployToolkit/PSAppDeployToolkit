using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text.RegularExpressions;

namespace PSAppDeployToolkit.Utilities
{
    /// <summary>
    /// A collection of utility methods for use in the PSADT module.
    /// </summary>
    public static class PowerShellUtilities
    {
        /// <summary>
        /// Converts a list of remaining arguments to a dictionary of key-value pairs.
        /// This MUST NOT return a ReadOnlyDictionary! The API must match $PSBoundParameters.
        /// </summary>
        /// <param name="remainingArguments">A list of remaining arguments to convert.</param>
        /// <returns>A dictionary of key-value pairs representing the remaining arguments.</returns>
        /// <exception cref="FormatException">Thrown when the parser is unable to process the provided arguments.</exception>
        public static IReadOnlyDictionary<string, object> ConvertValuesFromRemainingArguments(IReadOnlyList<object> remainingArguments)
        {
            if (!(remainingArguments?.Count > 0))
            {
                return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            }
            Dictionary<string, object> values = new(StringComparer.OrdinalIgnoreCase);
            try
            {
                string currentKey = string.Empty;
                foreach (object argument in remainingArguments)
                {
                    if (argument is null)
                    {
                        continue;
                    }
                    if ((argument is string str) && PowerShellParameterRegex.IsMatch(str))
                    {
                        currentKey = PowerShellParamTokenRegex.Replace(str, string.Empty);
                        values.Add(currentKey, new SwitchParameter(isPresent: true));
                    }
                    else if (!string.IsNullOrWhiteSpace(currentKey))
                    {
                        values[currentKey] = !string.IsNullOrWhiteSpace((string)((PSObject)ScriptBlock.Create("Out-String -InputObject $args[0]").InvokeReturnAsIs(argument)).BaseObject) ? argument : null!;
                        currentKey = string.Empty;
                    }
                }
            }
            catch (Exception ex) when (ex.Message is not null)
            {
                throw new FormatException("The parser was unable to process the provided arguments.", ex);
            }
            return values;
        }

        /// <summary>
        /// Converts a dictionary of key-value pairs to a string of PowerShell arguments.
        /// </summary>
        /// <param name="dict">A dictionary of key-value pairs to convert.</param>
        /// <param name="exclusions">An array of keys to exclude from the conversion.</param>
        /// <returns>A string of PowerShell arguments representing the dictionary.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the provided dictionary contains a null key or a null value.</exception>
        internal static string ConvertDictToPowerShellArgs(IReadOnlyDictionary<string, object> dict, IReadOnlyList<string>? exclusions = null)
        {
            // Internal iterator function to yield each argument.
            static IEnumerable<string> ConvertDictToPowerShellArgsImpl(IReadOnlyDictionary<string, object> dict, IReadOnlyList<string>? exclusions = null)
            {
                // Iterate through each key-value pair in the dictionary.
                foreach (KeyValuePair<string, object> entry in dict)
                {
                    // Skip anything null or excluded.
                    string key = entry.Key ?? throw new InvalidOperationException("The provided dictionary contains a null key.");
                    string? val = null;
                    if (entry.Value is null)
                    {
                        continue;
                    }
                    if (exclusions?.Contains(entry.Key, StringComparer.OrdinalIgnoreCase) == true)
                    {
                        continue;
                    }

                    // Handle nested dictionaries.
                    if (entry.Value is IDictionary dictionary)
                    {
                        yield return ConvertDictToPowerShellArgs(dictionary.Cast<DictionaryEntry>().ToDictionary(static entry => (string)entry.Key, static entry => entry.Value ?? throw new InvalidOperationException($"The value for '{entry.Key} is null."), StringComparer.OrdinalIgnoreCase), exclusions);
                        continue;
                    }

                    // Handle all other values.
                    if (entry.Value is string str)
                    {
                        val = $"'{SingleQuoteRegex.Replace(str, "''")}'";
                    }
                    else if (entry.Value is List<object> list)
                    {
                        val = ConvertDictToPowerShellArgs(ConvertValuesFromRemainingArguments(list), exclusions);
                    }
                    else if (entry.Value is IEnumerable enumerable)
                    {
                        val = enumerable.OfType<string>().ToArray() is string[] strings ? $"'{string.Join("','", strings.Select(static s => SingleQuoteRegex.Replace(s, "''")))}'" : string.Join(',', enumerable);
                    }
                    else if (entry.Value is not SwitchParameter)
                    {
                        val = entry.Value.ToString();
                    }
                    yield return !string.IsNullOrWhiteSpace(val) ? $"-{key}:{val}" : $"-{key}";
                }
            }
            return string.Join(' ', ConvertDictToPowerShellArgsImpl(dict, exclusions));
        }

        /// <summary>
        /// A regular expression to match valid PowerShell parameter names, which start with a hyphen and are followed by alphanumeric characters or hyphens, and may optionally end with a colon.
        /// </summary>
        private static readonly Regex PowerShellParameterRegex = new(@"^-[\w\d][\w\d-]+:?$", RegexOptions.Compiled);

        /// <summary>
        /// A regular expression to match the leading hyphen and optional trailing colon in PowerShell parameter tokens, used for extracting the parameter name from the token.
        /// </summary>
        private static readonly Regex PowerShellParamTokenRegex = new("(^-|:$)", RegexOptions.Compiled | RegexOptions.ExplicitCapture);

        /// <summary>
        /// A regular expression to match single quotes that are not part of a pair of single quotes, used for escaping single quotes in PowerShell string literals by doubling them. This regex uses negative lookbehind and negative lookahead assertions to ensure that it only matches single quotes that are not preceded or followed by another single quote, allowing for proper handling of escaped single quotes in PowerShell strings.
        /// </summary>
        private static readonly Regex SingleQuoteRegex = new("(?<!')'(?!')", RegexOptions.Compiled);
    }
}
