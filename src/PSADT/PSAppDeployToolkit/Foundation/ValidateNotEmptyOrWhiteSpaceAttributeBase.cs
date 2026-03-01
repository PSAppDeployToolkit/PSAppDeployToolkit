using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Language;
using System.Reflection;
using System.Security.Principal;

namespace PSAppDeployToolkit.Foundation
{
    /// <summary>
    /// Provides a base attribute for validating that an argument is not empty or composed solely of white-space
    /// characters. Supports validation for strings, collections, and other types, and can be configured to allow or
    /// disallow null values.
    /// </summary>
    /// <remarks>This attribute is intended for use in scenarios where it is important to ensure that input
    /// arguments are neither empty nor only white space, such as in command or parameter validation. It supports a
    /// variety of types, including strings, collections, and PowerShell-specific objects. When applied, it enforces
    /// that arguments meet the specified non-empty criteria, optionally allowing nulls if configured.</remarks>
    /// <param name="allowNull">Indicates whether null values are permitted. If set to <see langword="true"/>, null arguments will not trigger
    /// validation errors.</param>
    public abstract class ValidateNotEmptyOrWhiteSpaceAttributeBase(bool allowNull) : ValidateArgumentsAttribute
    {
        /// <summary>
        /// Validates that the argument is not empty or consists only of white-space characters.
        /// For collections, validates that the collection is not empty and that each element passes validation.
        /// </summary>
        /// <param name="arguments">The argument value to validate.</param>
        /// <param name="engineIntrinsics">Provides access to the PowerShell engine APIs.</param>
        /// <exception cref="ValidationMetadataException">
        /// Thrown when <paramref name="arguments"/> fails validation based on the configured rules.
        /// </exception>
        protected override void Validate(object arguments, EngineIntrinsics engineIntrinsics)
        {
            // Unwrap PSObject to get the underlying value.
            if (arguments is PSObject pso)
            {
                arguments = pso.BaseObject;
            }

            // Handle null based on configuration.
            if (IsNull(arguments))
            {
                if (allowNull)
                {
                    return;
                }
                throw new ValidationMetadataException("The argument is null. Provide a valid value for the argument, and then try running the command again.");
            }

            // Handle varying type checks.
            if (arguments is string str)
            {
                if (string.IsNullOrWhiteSpace(str))
                {
                    throw new ValidationMetadataException("The argument is empty or white space. Provide an argument that is not empty or white space, and then try running the command again.");
                }
            }
            else if (arguments is ScriptBlock script)
            {
                if (string.IsNullOrWhiteSpace(script.ToString()))
                {
                    throw new ValidationMetadataException("The argument is empty or white space. Provide an argument that is not empty or white space, and then try running the command again.");
                }
            }
            else if (arguments is NTAccount ntAccount)
            {
                if (string.IsNullOrWhiteSpace(ntAccount.Value))
                {
                    throw new ValidationMetadataException("The argument is empty or white space. Provide an argument that is not empty or white space, and then try running the command again.");
                }
            }
            else if (arguments is IDictionary dict)
            {
                if (dict.Count == 0)
                {
                    throw new ValidationMetadataException("The argument is an empty collection. Provide an argument that is not an empty collection, and then try running the command again.");
                }
            }
            else if (IsReadOnlyDictionary(arguments, out int count))
            {
                if (count == 0)
                {
                    throw new ValidationMetadataException("The argument is an empty collection. Provide an argument that is not an empty collection, and then try running the command again.");
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
                            object element = enumerator.Current;
                            if (IsNull(element))
                            {
                                throw new ValidationMetadataException("The argument collection contains a null element. Provide a collection that does not contain null elements, and then try running the command again.");
                            }
                            if (element is string elementStr && string.IsNullOrWhiteSpace(elementStr))
                            {
                                throw new ValidationMetadataException("The argument collection contains an element that is empty or white space. Provide a collection that does not contain empty or white space elements, and then try running the command again.");
                            }
                        }
                        while (enumerator.MoveNext());
                    }
                }
                if (isEmpty)
                {
                    throw new ValidationMetadataException("The argument is an empty collection. Provide an argument that is not an empty collection, and then try running the command again.");
                }
            }
        }

        /// <summary>
        /// Determines whether the specified value is null, including PowerShell-specific null representations.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <returns><c>true</c> if the value is null or a PowerShell/database null representation; otherwise, <c>false</c>.</returns>
        private static bool IsNull(object? value)
        {
            return value is null || value is DBNull || value == AutomationNull.Value || value == NullString.Value;
        }

        /// <summary>
        /// Determines whether the specified type represents a collection that should be validated element-by-element.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <param name="isElementValueType">When this method returns, indicates whether the collection's element type is a non-nullable value type.</param>
        /// <returns><c>true</c> if the type is a collection (array or implements <see cref="IEnumerable"/>); otherwise, <c>false</c>.</returns>
        private static bool IsCollection(Type type, out bool isElementValueType)
        {
            if (type.IsArray)
            {
                Type? elementType = type.GetElementType();
                isElementValueType = IsNonNullableValueType(elementType);
                return true;
            }
            if (typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string))
            {
                // Try to get the element type from generic IEnumerable<T>
                foreach (Type iface in type.GetInterfaces())
                {
                    if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    {
                        Type elementType = iface.GetGenericArguments()[0];
                        isElementValueType = IsNonNullableValueType(elementType);
                        return true;
                    }
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
        /// <returns><c>true</c> if the type is a value type that is not <see cref="Nullable{T}"/>; otherwise, <c>false</c>.</returns>
        private static bool IsNonNullableValueType(Type? type)
        {
            return type != null && type.IsValueType && Nullable.GetUnderlyingType(type) is null;
        }

        /// <summary>
        /// Determines whether the specified object implements <see cref="IReadOnlyDictionary{TKey, TValue}"/>
        /// and retrieves its count.
        /// </summary>
        /// <param name="value">The object to check.</param>
        /// <param name="count">When this method returns, contains the count of elements if the object is a read-only dictionary; otherwise, 0.</param>
        /// <returns><c>true</c> if the object implements <see cref="IReadOnlyDictionary{TKey, TValue}"/>; otherwise, <c>false</c>.</returns>
        private static bool IsReadOnlyDictionary(object value, out int count)
        {
            foreach (Type iface in value.GetType().GetInterfaces())
            {
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>))
                {
                    // Use reflection to get the Count property.
                    count = iface.GetProperty("Count") is PropertyInfo countProperty && countProperty.GetValue(value) is int countValue ? countValue : 0;
                    return true;
                }
            }
            count = 0;
            return false;
        }
    }
}
