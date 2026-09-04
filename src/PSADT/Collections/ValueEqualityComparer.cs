using System.Collections;
using System.Collections.Generic;

namespace PSADT.Collections
{
    /// <summary>
    /// Compares two values the way the collections in this namespace need them compared.
    /// </summary>
    /// <remarks>For most types this is the framework's own comparison. The exception is a value that is itself an
    /// array, which compares by reference and would leave a collection of arrays no better off than the collection it
    /// was built to replace - so those are compared by their contents instead. <para> Held here rather than in each
    /// collection so that <see cref="ValueList{T}"/> and <see cref="ValueDictionary{TKey, TValue}"/> agree on what two
    /// values being the same means. </para></remarks>
    /// <typeparam name="T">The type of the values to compare.</typeparam>
    internal static class ValueEqualityComparer<T>
    {
        /// <summary>
        /// The comparer to use for values of type <typeparamref name="T"/>.
        /// </summary>
        internal static readonly IEqualityComparer<T> Default = typeof(T).IsArray ? new StructuralComparer() : EqualityComparer<T>.Default;

        /// <summary>
        /// Compares values by their structure, for values that are themselves collections.
        /// </summary>
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
