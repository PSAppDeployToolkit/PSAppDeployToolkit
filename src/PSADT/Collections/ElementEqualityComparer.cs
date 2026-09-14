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
        /// the right bucket and be turned away on the comparison. <para> One operand being structural is enough to
        /// take the pair down that route, rather than both, so that the two values of a comparison are never reached
        /// by routes the single value of a hash could not have been. Structural against not-structural is answered by
        /// the structural one, which refuses anything that is not its own kind - where leaving that pair to the
        /// framework would answer it by a comparison the hash of either side never sees. </para><para> An array goes to
        /// <see cref="StructuralArrayComparer"/> rather than to the framework, because the framework's structural
        /// comparison reaches an array's elements through that same fallback and so puts the fault back one level
        /// down inside every array. One of the pair being an array is enough to send it there, and both being one is
        /// what the answer then turns on: an array against something else is no, whichever of the two was asked first,
        /// rather than the other value deciding it with its own comparison whenever the array happens to be second.
        /// </para><para> Anything else structural - a tuple - still goes to the framework, which compares a tuple
        /// holding an array by that array's contents. The seam left there is an array of a type that compares only
        /// through <see cref="IEquatable{T}"/> held inside a tuple, which nothing here is shaped like.
        /// </para></remarks>
        private sealed class StructuralComparer : IEqualityComparer<T>
        {
            /// <inheritdoc/>
            public bool Equals(T? x, T? y)
            {
                // Answered here rather than left to either comparer, so that what is handed on is known to be
                // there: net472 declares both of them as taking a plain T, and will not accept a maybe-null one.
                return x is null || y is null ? x is null && y is null
                    : x is Array || y is Array ? x is Array left && y is Array right && StructuralArrayComparer.AreEqual(left, right)
                    : x is IStructuralEquatable || y is IStructuralEquatable ? StructuralComparisons.StructuralEqualityComparer.Equals(x, y)
                    : EqualityComparer<T>.Default.Equals(x, y);
            }

            /// <inheritdoc/>
            public int GetHashCode(T obj)
            {
                return obj is Array array ? StructuralArrayComparer.Hash(array)
                    : obj is IStructuralEquatable ? StructuralComparisons.StructuralEqualityComparer.GetHashCode(obj)
                    : obj is not null ? EqualityComparer<T>.Default.GetHashCode(obj)
                    : 0;
            }
        }
    }
}
