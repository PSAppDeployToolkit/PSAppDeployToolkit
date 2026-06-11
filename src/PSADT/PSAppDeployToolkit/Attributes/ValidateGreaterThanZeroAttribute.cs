using System;
using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Language;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace PSAppDeployToolkit.Attributes
{
    /// <summary>
    /// Specifies that a parameter or property must be greater than zero.
    /// </summary>
    /// <remarks>
    /// This attribute validates comparable value types against their default value (for example, numeric types against
    /// 0 and <see cref="TimeSpan"/> against <see cref="TimeSpan.Zero"/>). For non-value types, it validates values
    /// that expose a public static <c>Zero</c> property and implement <see cref="IComparable"/>. For collections, each
    /// element is validated individually.
    /// </remarks>
    public sealed class ValidateGreaterThanZeroAttribute : ValidateArgumentsAttribute
    {
        /// <summary>
        /// Validates that the specified argument is greater than zero.
        /// </summary>
        /// <param name="arguments">The argument value to validate.</param>
        /// <param name="engineIntrinsics">Provides access to the PowerShell engine APIs.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="arguments"/> is null, cannot be compared to zero, or is less than or equal to
        /// zero.
        /// </exception>
        protected override void Validate(object arguments, EngineIntrinsics engineIntrinsics)
        {
            arguments = GetBaseObject(arguments);
            if (IsNull(arguments))
            {
                throw new ArgumentNullException(paramName: null, "The argument is null. Provide an argument that is greater than zero, and then try running the command again.");
            }
            if (arguments is not string && LanguagePrimitives.GetEnumerator(arguments) is IEnumerator enumerator)
            {
                ValidateElements(enumerator);
                return;
            }
            ValidateValue(arguments);
        }

        /// <summary>
        /// Validates that each element in the specified collection is greater than zero.
        /// </summary>
        /// <param name="enumerator">The enumerator for the collection to validate.</param>
        /// <exception cref="ArgumentNullException">Thrown when any element in the collection is null.</exception>
        private static void ValidateElements(IEnumerator enumerator)
        {
            while (enumerator.MoveNext())
            {
                object element = GetBaseObject(enumerator.Current);
                if (IsNull(element))
                {
                    throw new ArgumentNullException(paramName: null, "The argument collection contains a null element. Provide a collection whose elements are greater than zero, and then try running the command again.");
                }
                ValidateValue(element);
            }
        }

        /// <summary>
        /// Validates that the specified argument is greater than zero.
        /// </summary>
        /// <param name="value">The argument value to validate.</param>
        /// <exception cref="ArgumentNullException">Thrown when the argument is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the argument type does not support greater-than-zero validation.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the argument is less than or equal to zero.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "MA0015:Specify the parameter name in ArgumentException", Justification = "We don't want a paramter name on these exceptions.")]
        private static void ValidateValue(object value)
        {
            value = GetBaseObject(value);
            if (IsNull(value))
            {
                throw new ArgumentNullException(paramName: null, "The argument is null. Provide an argument that is greater than zero, and then try running the command again.");
            }
            if (!TryIsGreaterThanZero(value, out bool isGreaterThanZero))
            {
                throw new ArgumentException($"The argument type '{value.GetType().FullName}' does not support greater-than-zero validation.");
            }
            if (!isGreaterThanZero)
            {
                throw new ArgumentOutOfRangeException(paramName: null, value, "The argument is less than or equal to zero. Provide an argument that is greater than zero, and then try running the command again.");
            }
        }

        /// <summary>
        /// Returns the underlying base object by recursively unwrapping any enclosing PSObject instances.
        /// </summary>
        /// <param name="value">The object to unwrap. May be a PSObject or any other type; can be null.</param>
        /// <returns>The innermost object contained within the input, or null if the input is null.</returns>
        private static object GetBaseObject(object value)
        {
            while (value is PSObject psObject)
            {
                value = psObject.BaseObject;
            }
            return value;
        }

        /// <summary>
        /// Attempts to determine whether the specified value is greater than zero.
        /// </summary>
        /// <remarks>This method supports both value types and reference types that implement <see
        /// cref="IComparable"/>. For reference types, a static property named "Zero" of the same type is used as the
        /// zero value for comparison.</remarks>
        /// <param name="value">The value to compare against zero. Must implement <see cref="IComparable"/>.</param>
        /// <param name="isGreaterThanZero">When this method returns, contains <see langword="true"/> if the value is greater than zero; otherwise, <see
        /// langword="false"/>. This parameter is passed uninitialized.</param>
        /// <returns>true if the comparison was successful and the result is available in <paramref name="isGreaterThanZero"/>;
        /// otherwise, false.</returns>
        /// <exception cref="InvalidOperationException">Thrown if a default value cannot be created for the type of <paramref name="value"/>.</exception>
        private static bool TryIsGreaterThanZero(object value, out bool isGreaterThanZero)
        {
            Type valueType = value.GetType(); if (valueType.IsValueType && value is IComparable comparableValue)
            {
                object defaultValue = Activator.CreateInstance(valueType) ?? throw new InvalidOperationException($"Unable to create default value for type '{valueType.FullName}'.");
                isGreaterThanZero = comparableValue.CompareTo(defaultValue) > 0;
                return true;
            }
            if (value is IComparable comparableReference && valueType.GetProperty("Zero", BindingFlags.Public | BindingFlags.Static) is PropertyInfo zeroProperty && zeroProperty.PropertyType == valueType && zeroProperty.GetValue(null) is object zeroValue)
            {
                isGreaterThanZero = comparableReference.CompareTo(zeroValue) > 0;
                return true;
            }
            isGreaterThanZero = false;
            return false;
        }

        /// <summary>
        /// Determines whether the specified object represents a null or special null-equivalent value.
        /// </summary>
        /// <remarks>This method treats standard null, database null (DBNull), and certain
        /// PowerShell-specific null representations as equivalent for the purpose of null checking.</remarks>
        /// <param name="value">The object to test for null or special null-equivalent values. This can be any object, including database or
        /// PowerShell-specific null representations.</param>
        /// <returns>true if the value is null, a database null, or a recognized special null-equivalent; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsNull(object? value)
        {
            return value is null || value is DBNull || value == AutomationNull.Value || value == NullString.Value;
        }
    }
}
