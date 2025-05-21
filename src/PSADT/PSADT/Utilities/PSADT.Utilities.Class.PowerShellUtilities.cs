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
        /// </summary>
        /// <param name="remainingArguments">A list of remaining arguments to convert.</param>
        /// <returns>A dictionary of key-value pairs representing the remaining arguments.</returns>
        public static Dictionary<string, object?> ConvertValuesFromRemainingArguments(List<object> remainingArguments)
        {
            Dictionary<string, object?> values = [];
            string currentKey = string.Empty;
            if ((null == remainingArguments) || (remainingArguments.Count == 0))
            {
                return values;
            }
            try
            {
                foreach (object argument in remainingArguments)
                {
                    if (null == argument)
                    {
                        continue;
                    }
                    if ((argument is string str) && Regex.IsMatch(str, "^-"))
                    {
                        currentKey = Regex.Replace(str, "(^-|:$)", string.Empty);
                        values.Add(currentKey, new SwitchParameter(true));
                    }
                    else if (!string.IsNullOrWhiteSpace(currentKey))
                    {
                        values.Add(currentKey, !string.IsNullOrWhiteSpace((string)((PSObject)ScriptBlock.Create("Out-String -InputObject $args[0]").InvokeReturnAsIs(argument)).BaseObject) ? argument : null);
                        currentKey = string.Empty;
                    }
                }
            }
            catch (Exception ex)
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
        public static string ConvertDictToPowerShellArgs(IDictionary dict, string[]? exclusions = null)
        {
            List<string> args = [];
            foreach (DictionaryEntry entry in dict)
            {
                string key = entry.Key.ToString()!;
                string val = string.Empty;

                // Skip anything null or excluded.
                if (null == entry.Value)
                {
                    continue;
                }
                if ((null != exclusions) && exclusions.Contains(entry.Key.ToString()))
                {
                    continue;
                }

                // Handle nested dictionaries.
                if (entry.Value is IDictionary dictionary)
                {
                    args.Add(ConvertDictToPowerShellArgs(dictionary, exclusions));
                    continue;
                }

                // Handle all over values.
                if (entry.Value is string str)
                {
                    val = $"'{str.Replace("'", "''")}'";
                }
                else if (entry.Value is List<object> list)
                {
                    val = ConvertDictToPowerShellArgs(ConvertValuesFromRemainingArguments(list), exclusions);
                }
                else if (entry.Value is IEnumerable enumerable)
                {
                    if (enumerable.OfType<string>().ToArray() is string[] strings)
                    {
                        val = $"'{string.Join("','", strings.Select(s => s.Replace("'", "''")))}'";
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
