using System;
using System.Collections;
using System.Collections.Generic;
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
        protected override void Validate(object arguments, EngineIntrinsics engineIntrinsics)
        {
            // Handle null based on configuration.
            arguments = PowerShellUtilities.GetBaseObject<object>(arguments);
            if (PowerShellUtilities.ObjectIsNull(arguments))
            {
                if (allowNull)
                {
                    return;
                }
                throw new ArgumentNullException(paramName: null, "The argument is null. Provide a valid value for the argument, and then try running the command again.");
            }

            // Handle varying type checks.
            if (arguments is string str)
            {
                if (allowEmpty ? IsWhiteSpaceOnly(str) : string.IsNullOrWhiteSpace(str))
                {
                    throw new ArgumentException(allowEmpty
                        ? "The argument is white space. Provide an argument that is not white space, and then try running the command again."
                        : "The argument is empty or white space. Provide an argument that is not empty or white space, and then try running the command again.");
                }
            }
            else if (arguments is ScriptBlock script)
            {
                string scriptStr = script.ToString();
                if (allowEmpty ? IsWhiteSpaceOnly(scriptStr) : string.IsNullOrWhiteSpace(scriptStr))
                {
                    throw new ArgumentException(allowEmpty
                        ? "The argument is white space. Provide an argument that is not white space, and then try running the command again."
                        : "The argument is empty or white space. Provide an argument that is not empty or white space, and then try running the command again.");
                }
            }
            else if (arguments is NTAccount ntAccount)
            {
                if (allowEmpty ? IsWhiteSpaceOnly(ntAccount.Value) : string.IsNullOrWhiteSpace(ntAccount.Value))
                {
                    throw new ArgumentException(allowEmpty
                        ? "The argument is white space. Provide an argument that is not white space, and then try running the command again."
                        : "The argument is empty or white space. Provide an argument that is not empty or white space, and then try running the command again.");
                }
            }
            else if (arguments is IDictionary dict)
            {
                if (dict.Count == 0)
                {
                    throw new ArgumentException("The argument is an empty collection. Provide an argument that is not an empty collection, and then try running the command again.");
                }
            }
            else if (IsReadOnlyDictionary(arguments, out int count))
            {
                if (count == 0)
                {
                    throw new ArgumentException("The argument is an empty collection. Provide an argument that is not an empty collection, and then try running the command again.");
                }
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
                            object element = PowerShellUtilities.GetBaseObject<object>(enumerator.Current);
                            if (PowerShellUtilities.ObjectIsNull(element))
                            {
                                throw new ArgumentException("The argument collection contains a null element. Provide a collection that does not contain null elements, and then try running the command again.");
                            }
                            if (element is string elementStr && (allowEmpty ? IsWhiteSpaceOnly(elementStr) : string.IsNullOrWhiteSpace(elementStr)))
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
            return type?.IsValueType == true && Nullable.GetUnderlyingType(type) is null;
        }

        /// <summary>
        /// Determines whether the specified object implements <see cref="IReadOnlyDictionary{TKey, TValue}"/>
        /// and retrieves its count.
        /// </summary>
        /// <param name="value">The object to check.</param>
        /// <param name="count">When this method returns, contains the count of elements if the object is a read-only dictionary; otherwise, 0.</param>
        /// <returns><see langword="true"/> if the object implements <see cref="IReadOnlyDictionary{TKey, TValue}"/>; otherwise, <see langword="false"/>.</returns>
        private static bool IsReadOnlyDictionary(object value, out int count)
        {
            if (value.GetType().GetInterfaces().FirstOrDefault(static iface => iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>)) is Type iface)
            {
                // Use reflection to get the Count property.
                count = iface.GetProperty("Count")?.GetValue(value) as int? ?? 0;
                return true;
            }
            count = 0;
            return false;
        }
    }
}
