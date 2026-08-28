using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Management.Automation;
using System.Security.Principal;
using PSAppDeployToolkit.Utilities;

namespace PSAppDeployToolkit.Attributes
{
    /// <summary>
    /// Provides a base attribute for validating that an argument is not null, empty, or composed solely of white-space
    /// characters. Supports validation for strings, collections, and other types, and can be configured to allow or
    /// disallow null and empty values.
    /// </summary>
    /// <remarks>This attribute is intended for use in scenarios where it is important to ensure that input
    /// arguments meet specific non-null/non-empty/non-whitespace criteria, such as in command or parameter validation.
    /// It supports a variety of types, including strings, collections, and PowerShell-specific objects.</remarks>
    /// <param name="allowNull">Indicates whether null values are permitted. If set to <see langword="true"/>, null arguments will not trigger
    /// validation errors.</param>
    /// <param name="allowEmpty">Indicates whether empty values (empty strings, empty collections) are permitted. If set to <see langword="true"/>,
    /// empty values will not trigger validation errors, but whitespace-only strings will still be rejected.</param>
    public abstract class BaseValidateNotEmptyOrWhiteSpaceAttribute(bool allowNull, bool allowEmpty = false) : ValidateArgumentsAttribute
    {
        /// <summary>
        /// Validates that the argument is not empty or consists only of white-space characters.
        /// For collections, validates that the collection is not empty and that each element passes validation.
        /// </summary>
        /// <param name="arguments">The argument value to validate.</param>
        /// <param name="engineIntrinsics">Provides access to the PowerShell engine APIs.</param>
        /// <exception cref="ArgumentNullException">Thrown when the argument is null and allowNull is <see langword="false"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when the argument is empty or consists only of white-space characters and allowEmpty is <see langword="false"/>.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "MA0015:Specify the parameter name in ArgumentException", Justification = "We don't want a paramter name on these exceptions.")]
        protected override void Validate(object? arguments, EngineIntrinsics engineIntrinsics)
        {
            // Handle null based on configuration.
            if (!PowerShellUtilities.TryGetBaseObject(arguments, out arguments))
            {
                if (allowNull)
                {
                    return;
                }
                throw new ArgumentNullException(paramName: null, "The argument is null. Provide a valid value for the argument, and then try running the command again.");
            }

            // Handle varying type checks.
            if (TryGetText(arguments, out string? text))
            {
                if (allowEmpty ? IsWhiteSpaceOnly(text) : string.IsNullOrWhiteSpace(text))
                {
                    throw new ArgumentException(allowEmpty
                        ? "The argument is white space. Provide an argument that is not white space, and then try running the command again."
                        : "The argument is empty or white space. Provide an argument that is not empty or white space, and then try running the command again.");
                }
            }
            else if (arguments is IDictionary dict)
            {
                if (dict.Count is 0)
                {
                    throw new ArgumentException("The argument is an empty collection. Provide an argument that is not an empty collection, and then try running the command again.");
                }
                ValidateDictionaryValues(dict.Values);
            }
            else if (IsReadOnlyDictionary(arguments, out int count, out IEnumerable? readOnlyValues))
            {
                if (count is 0)
                {
                    throw new ArgumentException("The argument is an empty collection. Provide an argument that is not an empty collection, and then try running the command again.");
                }
                ValidateDictionaryValues(readOnlyValues);
            }
            else if (IsCollection(arguments.GetType(), out bool isElementValueType))
            {
                bool isEmpty = true;
                if (LanguagePrimitives.GetEnumerator(arguments) is IEnumerator enumerator && enumerator.MoveNext())
                {
                    // If elements are non-nullable value types, skip null/whitespace checks (they can't be null).
                    isEmpty = false;
                    if (!isElementValueType)
                    {
                        do
                        {
                            if (!PowerShellUtilities.TryGetBaseObject(enumerator.Current, out object? element))
                            {
                                throw new ArgumentException("The argument collection contains a null element. Provide a collection that does not contain null elements, and then try running the command again.");
                            }
                            if (TryGetText(element, out string? elementText) && (allowEmpty ? IsWhiteSpaceOnly(elementText) : string.IsNullOrWhiteSpace(elementText)))
                            {
                                throw new ArgumentException(allowEmpty
                                    ? "The argument collection contains an element that is white space. Provide a collection that does not contain white space elements, and then try running the command again."
                                    : "The argument collection contains an element that is empty or white space. Provide a collection that does not contain empty or white space elements, and then try running the command again.");
                            }
                        }
                        while (enumerator.MoveNext());
                    }
                }
                if (isEmpty)
                {
                    throw new ArgumentException("The argument is an empty collection. Provide an argument that is not an empty collection, and then try running the command again.");
                }
            }
        }


        /// <summary>
        /// Validates a dictionary's values by the same rules as a collection's elements.
        /// </summary>
        /// <param name="entries">The values to validate.</param>
        /// <exception cref="ArgumentException">Thrown when a value is null, empty or white space.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "MA0015:Specify the parameter name in ArgumentException", Justification = "We don't want a paramter name on these exceptions.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0038:Make method static (deprecated, use CA1822 instead)", Justification = "Reads allowEmpty, which this rule does not recognise as instance state on a primary constructor parameter.")]
        private void ValidateDictionaryValues(IEnumerable? entries)
        {
            if (entries is null)
            {
                return;
            }
            foreach (object? entry in entries)
            {
                if (!PowerShellUtilities.TryGetBaseObject(entry, out object? element))
                {
                    throw new ArgumentException("The argument dictionary contains a null value. Provide a dictionary that does not contain null values, and then try running the command again.");
                }
                if (TryGetText(element, out string? elementText) && (allowEmpty ? IsWhiteSpaceOnly(elementText) : string.IsNullOrWhiteSpace(elementText)))
                {
                    throw new ArgumentException(allowEmpty
                        ? "The argument dictionary contains a value that is white space. Provide a dictionary that does not contain white space values, and then try running the command again."
                        : "The argument dictionary contains a value that is empty or white space. Provide a dictionary that does not contain empty or white space values, and then try running the command again.");
                }
            }
        }

        /// <summary>
        /// Gets the text an argument carries, where it carries any.
        /// </summary>
        /// <remarks>Declared once and used at all three levels - the argument itself, a collection's elements and a
        /// dictionary's values - so that the shapes judged for content cannot drift apart. The element scan previously
        /// recognised strings alone, so an array of empty script blocks passed where a single empty one was refused.</remarks>
        /// <param name="value">The value to read.</param>
        /// <param name="text">When this method returns, contains the text the value carries, or <see langword="null"/> where it carries none.</param>
        /// <returns><see langword="true"/> if the value carries text; otherwise, <see langword="false"/>.</returns>
        private static bool TryGetText(object? value, [NotNullWhen(true)] out string? text)
        {
            text = value switch
            {
                string str => str,
                ScriptBlock script => script.ToString(),
                NTAccount ntAccount => ntAccount.Value,
                _ => null,
            };
            return text is not null;
        }

        /// <summary>
        /// Determines whether the specified string consists only of white-space characters (but is not empty).
        /// </summary>
        /// <param name="value">The string to check.</param>
        /// <returns><see langword="true"/> if the string is non-empty and consists only of white-space characters; otherwise, <see langword="false"/>.</returns>
        private static bool IsWhiteSpaceOnly(string value)
        {
            return value.Length > 0 && string.IsNullOrWhiteSpace(value);
        }

        /// <summary>
        /// Determines whether the specified type represents a collection that should be validated element-by-element.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <param name="isElementValueType">When this method returns, indicates whether the collection's element type is a non-nullable value type.</param>
        /// <returns><see langword="true"/> if the type is a collection (array or implements <see cref="IEnumerable"/>); otherwise, <see langword="false"/>.</returns>
        private static bool IsCollection(Type type, out bool isElementValueType)
        {
            if (type.IsArray)
            {
                isElementValueType = IsNonNullableValueType(type.GetElementType());
                return true;
            }
            if (typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string))
            {
                // Try to get the element type from generic IEnumerable<T>
                if (type.GetInterfaces().FirstOrDefault(static iface => iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>)) is Type iface)
                {
                    isElementValueType = IsNonNullableValueType(iface.GetGenericArguments()[0]);
                    return true;
                }
                isElementValueType = false;
                return true;
            }
            isElementValueType = false;
            return false;
        }

        /// <summary>
        /// Determines whether the specified type is a non-nullable value type.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns><see langword="true"/> if the type is a value type that is not <see cref="Nullable{T}"/>; otherwise, <see langword="false"/>.</returns>
        private static bool IsNonNullableValueType(Type? type)
        {
            return (type?.IsValueType) is true && Nullable.GetUnderlyingType(type) is null;
        }

        /// <summary>
        /// Determines whether the specified object implements <see cref="IReadOnlyDictionary{TKey, TValue}"/>
        /// and retrieves its count.
        /// </summary>
        /// <param name="value">The object to check.</param>
        /// <param name="count">When this method returns, contains the count of elements if the object is a read-only dictionary; otherwise, 0.</param>
        /// <param name="entries">When this method returns, contains the dictionary's values if the object is a read-only dictionary; otherwise, <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if the object implements <see cref="IReadOnlyDictionary{TKey, TValue}"/>; otherwise, <see langword="false"/>.</returns>
        private static bool IsReadOnlyDictionary(object value, out int count, out IEnumerable? entries)
        {
            if (value.GetType().GetInterfaces().FirstOrDefault(static iface => iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>)) is Type iface)
            {
                // Count is declared on IReadOnlyCollection<T>, not on IReadOnlyDictionary<TKey, TValue>, and
                // GetProperty does not search base interfaces - so asking the dictionary interface for it always
                // returned null and every read-only dictionary counted as empty.
                count = typeof(IReadOnlyCollection<>)
                    .MakeGenericType(typeof(KeyValuePair<,>).MakeGenericType(iface.GetGenericArguments()))
                    .GetProperty("Count")?.GetValue(value) as int? ?? 0;
                entries = iface.GetProperty("Values")?.GetValue(value) as IEnumerable;
                return true;
            }
            count = 0;
            entries = null;
            return false;
        }
    }
}
