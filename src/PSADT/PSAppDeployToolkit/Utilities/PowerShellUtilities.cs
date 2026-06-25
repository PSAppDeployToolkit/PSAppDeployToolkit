using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Language;
using System.Text.RegularExpressions;

namespace PSAppDeployToolkit.Utilities
{
    /// <summary>
    /// A collection of utility methods for use in the PSADT module.
    /// </summary>
    public static class PowerShellUtilities
    {
        /// <summary>
        /// Gets the base object of a PSObject, unwrapping any nested PSObjects to retrieve the underlying object of type T.
        /// </summary>
        /// <typeparam name="T">The type of the underlying object to retrieve.</typeparam>
        /// <param name="obj">The object to unwrap.</param>
        /// <returns>The underlying object of type T if the input is a PSObject; otherwise, the input object itself cast to type T.</returns>
        public static T GetBaseObject<T>(object obj)
        {
            while (obj is PSObject psObj)
            {
                obj = psObj.BaseObject;
            }
            return (T)obj;
        }

        /// <summary>
        /// Attempts to get the base object of a PSObject, unwrapping any nested PSObjects to retrieve the underlying object of type T. Returns true if successful, false otherwise.
        /// </summary>
        /// <typeparam name="T">The type of the underlying object to retrieve.</typeparam>
        /// <param name="obj">The object to unwrap.</param>
        /// <param name="baseObject">When this method returns, contains the underlying object of type T if the operation succeeded, or the default value of T if the operation failed.</param>
        /// <returns>true if the underlying object of type T was successfully retrieved; otherwise, false.</returns>
        public static bool TryGetBaseObject<T>(object? obj, [NotNullWhen(true)] out T? baseObject) where T : notnull
        {
            while (obj is PSObject psObj)
            {
                obj = psObj.BaseObject;
            }
            if (ObjectIsNull(obj) || obj is not T t)
            {
                baseObject = default;
                return false;
            }
            baseObject = t;
            return true;
        }

        /// <summary>
        /// Determines whether the specified object is considered null in the context of PowerShell, including checks for DBNull, AutomationNull, and NullString.
        /// </summary>
        /// <param name="obj">The object to check for null.</param>
        /// <returns>true if the object is considered null; otherwise, false.</returns>
        public static bool ObjectIsNull(object? obj)
        {
            return obj is null || obj is DBNull || obj == AutomationNull.Value || obj == NullString.Value;
        }

        /// <summary>
        /// Determines whether the specified object has no displayable content when converted to a string, using PowerShell's Out-String cmdlet to evaluate the object's string representation.
        /// </summary>
        /// <param name="obj">The object to check for displayable content.</param>
        /// <returns>true if the object has no displayable content; otherwise, false.</returns>
        public static bool ObjectRendersAsEmpty(object? obj)
        {
            return string.IsNullOrWhiteSpace(GetBaseObject<string>(ScriptBlock.Create("Out-String -InputObject $args[0]").InvokeReturnAsIs(obj)));
        }

        /// <summary>
        /// Converts a list of remaining arguments to a dictionary of key-value pairs.
        /// This MUST NOT return a ReadOnlyDictionary! The API must match $PSBoundParameters.
        /// </summary>
        /// <param name="remainingArguments">A list of remaining arguments to convert.</param>
        /// <returns>A dictionary of key-value pairs representing the remaining arguments.</returns>
        /// <exception cref="FormatException">Thrown when the parser is unable to process the provided arguments.</exception>
        public static IDictionary<string, object> ConvertValuesFromRemainingArguments(IEnumerable<object> remainingArguments)
        {
            Dictionary<string, object> values = new(StringComparer.OrdinalIgnoreCase);
            try
            {
                string currentKey = string.Empty;
                foreach (object argument in remainingArguments)
                {
                    if (argument is string str && PowerShellParameterRegex.IsMatch(str))
                    {
                        currentKey = PowerShellParamTokenRegex.Replace(str, string.Empty);
                        values.Add(currentKey, new SwitchParameter(isPresent: true));
                    }
                    else if (!string.IsNullOrWhiteSpace(currentKey))
                    {
                        if (!ObjectRendersAsEmpty(argument))
                        {
                            values[currentKey] = argument;
                        }
                        else
                        {
                            _ = values.Remove(currentKey);
                        }
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
        /// <param name="boundParameters">A dictionary of key-value pairs to convert.</param>
        /// <returns>A string of PowerShell arguments representing the dictionary.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the provided dictionary contains a null key or a null value.</exception>
        public static IReadOnlyList<string> ConvertBoundParametersToArgumentList(IEnumerable<KeyValuePair<string, object>> boundParameters)
        {
            // Iterate through each key-value pair in the dictionary.
            List<string> argumentList = []; foreach (KeyValuePair<string, object> boundParameter in boundParameters)
            {
                // Ensure the shape of the incoming data is correct.
                if (boundParameter.Key is null)
                {
                    throw new InvalidOperationException("The provided dictionary contains a null key. All keys must be non-null strings.");
                }

                // Handle the value with specific type handling.
                if (boundParameter.Value is IEnumerable<object> remainingArguments && remainingArguments.Any(static v => v is string k && PowerShellParameterRegex.IsMatch(k)))
                {
                    argumentList.AddRange(ConvertBoundParametersToArgumentList(ConvertValuesFromRemainingArguments(remainingArguments)));
                    continue;
                }
                if (boundParameter.Value is SwitchParameter switchParameter)
                {
                    if (switchParameter.IsPresent)
                    {
                        argumentList.Add($"-{boundParameter.Key}");
                    }
                    continue;
                }
                if (!ObjectRendersAsEmpty(boundParameter.Value))
                {
                    argumentList.Add($"-{boundParameter.Key}:{boundParameter.Value}");
                }
            }
            return argumentList.AsReadOnly();
        }

        /// <summary>
        /// A regular expression to match valid PowerShell parameter names, which start with a hyphen and are followed by alphanumeric characters or hyphens, and may optionally end with a colon.
        /// </summary>
        private static readonly Regex PowerShellParameterRegex = new(@"^-[\w\d][\w\d-]+:?$", RegexOptions.Compiled);

        /// <summary>
        /// A regular expression to match the leading hyphen and optional trailing colon in PowerShell parameter tokens, used for extracting the parameter name from the token.
        /// </summary>
        private static readonly Regex PowerShellParamTokenRegex = new("(^-|:$)", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
    }
}
