using System;
using System.Collections;
using System.Collections.Generic;

namespace PSADT.Collections
{
    /// <summary>
    /// Compares two values the way the collections in this namespace need them compared.
    /// </summary>
    /// <remarks>An array compares by reference, which would leave a collection of arrays no better off than the
    /// collection this namespace was built to replace - so an array is compared by its contents instead. The choice
    /// has to be made on what the declared type could <i>hold</i> at run time rather than on what it is, since a byte
    /// array declared as <see cref="object"/> or as an interface is still a byte array. <para> That is a closed set:
    /// an array's only base types are <see cref="Array"/> and <see cref="object"/>, so the only declared types that
    /// can ever hold one are an array itself, an interface, <see cref="object"/>, and <see cref="Array"/>. Everything
    /// else keeps the framework's comparison, which spares the boxing on a value type and takes each type's own
    /// <see cref="IEquatable{T}"/> where it offers one. </para><para> Deciding it the other way round - structural
    /// for anything not sealed - reads as the safer default and is not: the structural comparison falls back to the
    /// virtual <see cref="object.Equals(object)"/>, so an unsealed type that implements <see cref="IEquatable{T}"/>
    /// without overriding that would quietly fall back to comparing references, while still hashing by its contents.
    /// </para><para> Held here rather than in each collection so that <see cref="EquatableList{T}"/> and <see
    /// cref="EquatableDictionary{TKey, TValue}"/> agree on what two values being the same means. </para></remarks>
    /// <typeparam name="T">The type of the values to compare.</typeparam>
    internal static class ElementEqualityComparer<T>
    {
        /// <summary>
        /// The comparer to use for values of type <typeparamref name="T"/>.
        /// </summary>
        internal static readonly IEqualityComparer<T> Default = typeof(T).IsArray || typeof(T).IsInterface || typeof(T) == typeof(object) || typeof(T) == typeof(Array) ? new StructuralComparer() : EqualityComparer<T>.Default;

        /// <summary>
        /// Compares values by their structure, for values that are themselves collections.
        /// </summary>
        /// <remarks>Only a value that is structural at run time is treated as one: the framework's comparer asks for
        /// <see cref="IStructuralEquatable"/> and falls back to the value's own <see cref="object.Equals(object)"/>
        /// otherwise, so a type that already compares itself properly is left to do so.</remarks>
        private sealed class StructuralComparer : IEqualityComparer<T>
        {
            /// <inheritdoc/>
            public bool Equals(T? x, T? y)
            {
                return StructuralComparisons.StructuralEqualityComparer.Equals(x, y);
            }

            /// <inheritdoc/>
            public int GetHashCode(T obj)
            {
                return obj is not null ? StructuralComparisons.StructuralEqualityComparer.GetHashCode(obj) : 0;
            }
        }
    }
}
