using System;
using System.Collections;
using System.Collections.Generic;

namespace PSADT.Collections
{
    /// <summary>
    /// Compares two values the way the collections in this namespace need them compared.
    /// </summary>
    /// <remarks>An array compares by reference, which would leave a collection of arrays no better off than the
    /// collection this namespace was built to replace - so an array is compared by its contents instead. Whether a
    /// value has contents to compare is asked of the value itself rather than of <typeparamref name="T"/>, since a
    /// byte array declared as <see cref="object"/> or as an interface is still a byte array. Anything that has none
    /// is handed to the framework's comparison, which keeps each type's own <see cref="IEquatable{T}"/> in play.
    /// <para> The test on
    /// <typeparamref name="T"/> sitting in front of that is only an optimisation. An array's base types are just
    /// <see cref="Array"/> and <see cref="object"/>, so a <typeparamref name="T"/> that is none of an array, an
    /// interface, <see cref="object"/> or <see cref="Array"/> can never be holding one and skips the check
    /// altogether - which also spares a value type being boxed to ask. Narrowing it too far costs a redundant type
    /// check rather than a wrong answer, which is the whole reason for asking the value. </para><para> Held here
    /// rather than in each collection so that <see cref="EquatableList{T}"/> and <see cref="EquatableDictionary{TKey,
    /// TValue}"/> agree on what two values being the same means. </para></remarks>
    /// <typeparam name="T">The type of the values to compare.</typeparam>
    internal static class ElementEqualityComparer<T>
    {
        /// <summary>
        /// The comparer to use for values of type <typeparamref name="T"/>.
        /// </summary>
        internal static readonly IEqualityComparer<T> Default = typeof(T).IsArray || typeof(T).IsInterface || typeof(T) == typeof(object) || typeof(T) == typeof(Array) ? new StructuralComparer() : EqualityComparer<T>.Default;

        /// <summary>
        /// Compares by contents the values that turn out to have any, and leaves the rest to the framework.
        /// </summary>
        /// <remarks>Handing everything to <see cref="StructuralComparisons.StructuralEqualityComparer"/> instead
        /// would be wrong for the rest: it asks a value for <see cref="IStructuralEquatable"/> and falls back to the
        /// virtual <see cref="object.Equals(object)"/> otherwise, which is not where a type has put its comparison
        /// when it implements <see cref="IEquatable{T}"/> and leaves <see cref="object.Equals(object)"/> alone. Such
        /// a type would compare by reference while still hashing by its contents, so two equal values would reach
        /// the right bucket and be turned away on the comparison. <para> The question asked of a value is whether it
        /// is <see cref="IStructuralEquatable"/> rather than whether it is an <see cref="Array"/>, which is the wider
        /// of the two and costs the same to ask. A tuple is structural without being an array, and comparing one
        /// through the framework would compare its own elements with their own default comparers - so a tuple holding
        /// an array would be back to comparing that array by reference, one level further down than where this
        /// started. </para></remarks>
        private sealed class StructuralComparer : IEqualityComparer<T>
        {
            /// <inheritdoc/>
            public bool Equals(T? x, T? y)
            {
                // Answered here rather than left to either comparer, so that what is handed on is known to be
                // there: net472 declares both of them as taking a plain T, and will not accept a maybe-null one.
                return x is null || y is null ? x is null && y is null
                    : x is IStructuralEquatable && y is IStructuralEquatable ? StructuralComparisons.StructuralEqualityComparer.Equals(x, y)
                    : EqualityComparer<T>.Default.Equals(x, y);
            }

            /// <inheritdoc/>
            public int GetHashCode(T obj)
            {
                return obj is not null ? obj is IStructuralEquatable
                    ? StructuralComparisons.StructuralEqualityComparer.GetHashCode(obj)
                    : EqualityComparer<T>.Default.GetHashCode(obj)
                    : 0;
            }
        }
    }
}
