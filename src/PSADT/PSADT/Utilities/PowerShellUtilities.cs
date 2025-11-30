using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using System.Text.RegularExpressions;

namespace PSADT.Utilities
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
        public static Dictionary<string, object> ConvertValuesFromRemainingArguments(IReadOnlyList<object> remainingArguments)
        {
            Dictionary<string, object> values = [];
            if (remainingArguments?.Count > 0)
            {
                try
                {
                    string currentKey = string.Empty;
                    foreach (object argument in remainingArguments)
                    {
                        if (argument is null)
                        {
                            continue;
                        }
                        if ((argument is string str) && Regex.IsMatch(str, @"^-[\w\d][\w\d-]+:?$"))
                        {
                            currentKey = Regex.Replace(str, "(^-|:$)", string.Empty);
                            values.Add(currentKey, new SwitchParameter(true));
                        }
                        else if (!string.IsNullOrWhiteSpace(currentKey))
                        {
                            values[currentKey] = !string.IsNullOrWhiteSpace((string)((PSObject)ScriptBlock.Create("Out-String -InputObject $args[0]").InvokeReturnAsIs(argument)).BaseObject) ? argument : null!;
                            currentKey = string.Empty;
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new FormatException("The parser was unable to process the provided arguments.", ex);
                }
            }
            return values;
        }

        /// <summary>
        /// Converts a dictionary of key-value pairs to a string of PowerShell arguments.
        /// </summary>
        /// <param name="dict">A dictionary of key-value pairs to convert.</param>
        /// <param name="exclusions">An array of keys to exclude from the conversion.</param>
        /// <returns>A string of PowerShell arguments representing the dictionary.</returns>
        internal static string ConvertDictToPowerShellArgs(IReadOnlyDictionary<string, object> dict, IReadOnlyList<string>? exclusions = null)
        {
            List<string> args = [];
            foreach (var entry in dict)
            {
                string key = entry.Key.ToString()!;
                string val = string.Empty;

                // Skip anything null or excluded.
                if (entry.Value is null)
                {
                    continue;
                }
                if ((exclusions is not null) && exclusions.Contains(entry.Key.ToString()))
                {
                    continue;
                }

                // Handle nested dictionaries.
                if (entry.Value is IDictionary dictionary)
                {
                    args.Add(ConvertDictToPowerShellArgs(dictionary.Cast<DictionaryEntry>().ToDictionary(static entry => (string)entry.Key, static entry => entry.Value!), exclusions));
                    continue;
                }

                // Handle all over values.
                if (entry.Value is string str)
                {
                    val = $"'{Regex.Replace(str, @"(?<!')'(?!')", "''")}'";
                }
                else if (entry.Value is List<object> list)
                {
                    val = ConvertDictToPowerShellArgs(ConvertValuesFromRemainingArguments(list), exclusions);
                }
                else if (entry.Value is IEnumerable enumerable)
                {
                    if (enumerable.OfType<string>().ToArray() is string[] strings)
                    {
                        val = $"'{string.Join("','", strings.Select(s => Regex.Replace(s, @"(?<!')'(?!')", "''")))}'";
                    }
                    else
                    {
                        val = string.Join(",", enumerable);
                    }
                }
                else if (entry.Value is not SwitchParameter)
                {
                    val = entry.Value.ToString()!;
                }

                // Add the key-value pair to the list.
                if (!string.IsNullOrWhiteSpace(val))
                {
                    args.Add($"-{key}:{val}");
                }
                else
                {
                    args.Add($"-{key}");
                }
            }
            return string.Join(" ", args);
        }
    }
}
