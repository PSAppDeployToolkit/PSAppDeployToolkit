using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;

namespace PSAppDeployToolkit.Attributes
{
    /// <summary>
    /// Specifies that a collection parameter or property must contain only unique elements.
    /// </summary>
    /// <remarks>
    /// For string elements, uniqueness is evaluated using the configured <see cref="StringComparison"/> value.
    /// For non-string elements, uniqueness is evaluated using the type's equality implementation.
    /// Non-collection values are treated as valid.
    /// </remarks>
    public sealed class ValidateUniqueAttribute() : ValidateArgumentsAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ValidateUniqueAttribute"/> class.
        /// </summary>
        /// <param name="stringComparison">The string comparison mode used when evaluating string elements.</param>
        public ValidateUniqueAttribute(StringComparison stringComparison) : this()
        {
            StringComparison = stringComparison;
        }

        /// <summary>
        /// Gets the string comparison mode used when evaluating string elements.
        /// </summary>
        public StringComparison StringComparison { get; } = StringComparison.OrdinalIgnoreCase;

        /// <summary>
        /// Validates that the specified argument does not contain duplicate elements.
        /// </summary>
        /// <param name="arguments">The argument value to validate.</param>
        /// <param name="engineIntrinsics">Provides access to the PowerShell engine APIs.</param>
        /// <exception cref="ValidationMetadataException">
        /// Thrown when <paramref name="arguments"/> is a collection that contains duplicate elements.
        /// </exception>
        protected override void Validate(object arguments, EngineIntrinsics engineIntrinsics)
        {
            // Verify the provided input before proceeding.
            while (arguments is PSObject psObject)
            {
                arguments = psObject.BaseObject;
            }
            if (arguments is string || LanguagePrimitives.GetEnumerator(arguments) is not IEnumerator enumerator)
            {
                return;
            }
            if (!enumerator.MoveNext())
            {
                return;
            }

            // Determine the type of the first non-null element to select an appropriate equality comparer.
            object? firstValue = GetBaseObject(enumerator.Current);
            List<object?> bufferedValues = [firstValue];
            Type? inferredType = firstValue?.GetType();
            while (inferredType is null && enumerator.MoveNext())
            {
                object? value = GetBaseObject(enumerator.Current);
                bufferedValues.Add(value);
                inferredType = value?.GetType();
            }
            IEqualityComparer<object?> comparer = inferredType == typeof(string)
                ? new TypedDefaultEqualityComparer<string>(GetStringComparer(StringComparison))
                : inferredType is not null
                ? Activator.CreateInstance(typeof(TypedDefaultEqualityComparer<>).MakeGenericType(inferredType)) as IEqualityComparer<object?> ?? throw new InvalidOperationException($"Unable to create a typed equality comparer for type '{inferredType.FullName}'.")
                : EqualityComparer<object?>.Default;

            // Use a HashSet to track seen elements and detect duplicates efficiently.
            HashSet<object?> seen = new(comparer) { firstValue };
            for (int i = 1; i < bufferedValues.Count; i++)
            {
                if (!seen.Add(bufferedValues[i]))
                {
                    throw new ArgumentException("The argument collection contains duplicate elements. Provide a collection in which each element is unique, and then try running the command again.");
                }
            }
            while (enumerator.MoveNext())
            {
                if (!seen.Add(GetBaseObject(enumerator.Current)))
                {
                    throw new ArgumentException("The argument collection contains duplicate elements. Provide a collection in which each element is unique, and then try running the command again.");
                }
            }
        }

        /// <summary>
        /// Returns the underlying base object by recursively unwrapping any enclosing PSObject instances.
        /// </summary>
        /// <remarks>This method is useful when working with objects that may be wrapped in one or more
        /// layers of PSObject, such as those returned from PowerShell pipelines. If the input is not a PSObject, it is
        /// returned unchanged.</remarks>
        /// <param name="value">The object to unwrap. May be a PSObject or any other type; can be null.</param>
        /// <returns>The innermost object contained within the input, or null if the input is null.</returns>
        private static object? GetBaseObject(object? value)
        {
            while (value is PSObject psObject)
            {
                value = psObject.BaseObject;
            }
            return value;
        }

        /// <summary>
        /// Returns a StringComparer instance that corresponds to the specified StringComparison value.
        /// </summary>
        /// <param name="stringComparison">The type of string comparison to use when selecting the StringComparer.</param>
        /// <returns>A StringComparer that implements the specified string comparison behavior.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if stringComparison is not a valid StringComparison value.</exception>
        private static StringComparer GetStringComparer(StringComparison stringComparison)
        {
            return stringComparison switch
            {
                StringComparison.CurrentCulture => StringComparer.CurrentCulture,
                StringComparison.CurrentCultureIgnoreCase => StringComparer.CurrentCultureIgnoreCase,
                StringComparison.InvariantCulture => StringComparer.InvariantCulture,
                StringComparison.InvariantCultureIgnoreCase => StringComparer.InvariantCultureIgnoreCase,
                StringComparison.Ordinal => StringComparer.Ordinal,
                StringComparison.OrdinalIgnoreCase => StringComparer.OrdinalIgnoreCase,
                _ => throw new ArgumentOutOfRangeException(nameof(stringComparison), stringComparison, "Unsupported string comparison type."),
            };
        }

        /// <summary>
        /// Provides an equality comparer for objects that compares values of type T using the default equality comparer
        /// for T, and falls back to object equality for other types.
        /// </summary>
        /// <remarks>This comparer is useful when working with collections or APIs that operate on objects
        /// but require type-specific equality logic for a particular type T. If both objects are of type T, their
        /// equality and hash code are determined using EqualityComparer&lt;T&gt;.Default; otherwise, object equality is
        /// used.</remarks>
        /// <typeparam name="T">The type to compare when evaluating equality and hash codes.</typeparam>
        private sealed class TypedDefaultEqualityComparer<T> : IEqualityComparer<object?>
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="TypedDefaultEqualityComparer{T}"/> class.
            /// </summary>
            public TypedDefaultEqualityComparer()
            {
                Comparer = EqualityComparer<T>.Default;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="TypedDefaultEqualityComparer{T}"/> class with the specified comparer.
            /// </summary>
            /// <param name="comparer">The comparer to use. If null, <see cref="EqualityComparer{T}.Default"/> is used.</param>
            public TypedDefaultEqualityComparer(IEqualityComparer<T>? comparer)
            {
                Comparer = comparer ?? EqualityComparer<T>.Default;
            }

            /// <summary>
            /// Gets the equality comparer used to determine whether objects of type T are equal.
            /// </summary>
            /// <remarks>If no comparer is provided, the default equality comparer for the type is
            /// used.</remarks>
            private readonly IEqualityComparer<T> Comparer;

            /// <summary>
            /// Determines whether the specified objects are equal according to the equality logic for type T, or by
            /// default object equality if the objects are not of type T.
            /// </summary>
            /// <remarks>If both objects are of type T, the comparison uses the default equality
            /// comparer for T. If either object is not of type T, the comparison falls back to the default object
            /// equality comparer.</remarks>
            /// <param name="x">The first object to compare.</param>
            /// <param name="y">The second object to compare.</param>
            /// <returns>true if the specified objects are considered equal; otherwise, false.</returns>
            bool IEqualityComparer<object?>.Equals(object? x, object? y)
            {
                return x is T typedX && y is T typedY
                    ? Comparer.Equals(typedX, typedY)
                    : EqualityComparer<object?>.Default.Equals(x, y);
            }

            /// <summary>
            /// Returns a hash code for the specified object.
            /// </summary>
            /// <remarks>If the object is of type T, the default equality comparer for T is used to
            /// compute the hash code. Otherwise, the object's own GetHashCode method is used.</remarks>
            /// <param name="obj">The object for which to get a hash code. Can be null.</param>
            /// <returns>A hash code for the specified object. Returns 0 if the object is null.</returns>
            public int GetHashCode(object? obj)
            {
                return obj is T typedValue
                    ? Comparer.GetHashCode(typedValue)
                    : obj?.GetHashCode() ?? 0;
            }
        }
    }
}
