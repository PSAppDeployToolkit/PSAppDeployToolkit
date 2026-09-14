using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace PSADT.Collections
{
    /// <summary>
    /// A list that compares by its contents rather than by reference.
    /// </summary>
    /// <remarks>A record compares each of its fields, and every collection the framework offers compares by
    /// reference - so a record holding one is a record that never equals another describing the same thing, while its
    /// generated <c language="csharp">ToString</c> renders the two identically. This exists to be held in place of one: a record stores
    /// it privately and exposes it through a property typed as <see cref="IReadOnlyList{T}"/>, so the generated
    /// equality picks up the contents while the type's callers see no difference. <para> Making it the declared type
    /// of the field, rather than writing a comparison by hand on each record, is the point: equality then stays
    /// correct when a member is added later, which a hand-written one would not. </para><para> Elements are compared
    /// the same way, so a list of arrays compares by the arrays' contents rather than by their references.
    /// </para><para> The elements are held in an array that is set once and never replaced, so nothing on any surface
    /// can change what this compares as. <see cref="DataContractAttribute"/> is what allows that: the data contract
    /// serializer would otherwise see <see cref="IEnumerable{T}"/>, take this for a collection, and refuse one that
    /// offers no <c language="csharp">Add</c> for it to fill. Carrying the attribute sends it down the ordinary class path instead,
    /// where it writes the one field and rebuilds the type without running a constructor. </para></remarks>
    /// <typeparam name="T">The type of the elements.</typeparam>
    [DataContract]
    internal sealed class EquatableList<T> : IReadOnlyList<T>, IEquatable<EquatableList<T>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EquatableList{T}"/> class holding the specified elements.
        /// </summary>
        /// <param name="items">The elements to hold. They are copied, so the caller may go on using its own list.</param>
        internal EquatableList(IEnumerable<T> items)
        {
            ArgumentNullException.ThrowIfNull(items);
            _items = [.. items];
        }

        /// <summary>
        /// Determines whether this list holds the same elements, in the same order, as another.
        /// </summary>
        /// <param name="other">The list to compare against.</param>
        /// <returns><see langword="true"/> if the two hold the same elements; otherwise, <see langword="false"/>.</returns>
        public bool Equals([NotNullWhen(true)] EquatableList<T>? other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }
            if (other is null || _items.Length != other._items.Length)
            {
                return false;
            }

            // Walked rather than run through a query, so that comparing two lists allocates nothing: a query asks
            // each array for an enumerator, and an array hands back an object to do it.
            for (int index = 0; index < _items.Length; index++)
            {
                if (!ElementComparer.Equals(_items[index], other._items[index]))
                {
                    return false;
                }
            }
            return true;
        }

        /// <inheritdoc/>
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return Equals(obj as EquatableList<T>);
        }

        /// <inheritdoc/>
        /// <remarks>Worked out on every call rather than once and kept. A kept one would be worth something only
        /// where the same list is hashed more than once, which nothing here does. What it would cost is a hash that
        /// stops describing what the list holds: an element that is itself an array is handed out by the indexer and
        /// is writable through it, and a kept hash would go on reporting what that element used to be. Worked out on
        /// demand the two always agree, and a caller that writes to something it was handed has broken the rule every
        /// hash container has rather than found a fault in this one.</remarks>
        public override int GetHashCode()
        {
            return ComputeHashCode();
        }

        /// <summary>
        /// Determines whether two lists hold the same elements in the same order.
        /// </summary>
        /// <remarks>Defined so that the operator cannot quietly disagree with <see cref="Equals(EquatableList{T})"/>,
        /// which it would if it were left comparing references as a reference type's operator does by default.</remarks>
        /// <param name="left">The first list, which may be <see langword="null"/>.</param>
        /// <param name="right">The second list, which may be <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if the two hold the same elements, or both are <see langword="null"/>; otherwise, <see langword="false"/>.</returns>
        public static bool operator ==(EquatableList<T>? left, EquatableList<T>? right)
        {
            return left is not null ? left.Equals(right) : right is null;
        }

        /// <summary>
        /// Determines whether two lists differ in their elements or in the order of them.
        /// </summary>
        /// <param name="left">The first list, which may be <see langword="null"/>.</param>
        /// <param name="right">The second list, which may be <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if the two differ, or one is <see langword="null"/> and the other is not; otherwise, <see langword="false"/>.</returns>
        public static bool operator !=(EquatableList<T>? left, EquatableList<T>? right)
        {
            return !(left == right);
        }

        /// <inheritdoc/>
        public IEnumerator<T> GetEnumerator()
        {
            return ((IEnumerable<T>)_items).GetEnumerator();
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        /// <summary>
        /// Combines the elements into a hash code.
        /// </summary>
        /// <remarks>The comparer is handed to each element rather than left to the combiner's own, because an
        /// element that is an array has to be hashed by its contents rather than by its reference.</remarks>
        /// <returns>The hash code of the elements.</returns>
        private int ComputeHashCode()
        {
            HashCode hashCode = new();
            foreach (T item in _items)
            {
                hashCode.Add(item, ElementComparer);
            }
            return hashCode.ToHashCode();
        }

        /// <inheritdoc/>
        public T this[int index] => _items[index];

        /// <inheritdoc/>
        public int Count => _items.Length;

        /// <summary>
        /// An empty list.
        /// </summary>
        /// <remarks>Shared, since the type cannot be changed once it is built. Held so that a record with nothing
        /// to report hands back an empty list rather than nothing at all, which a caller piping it would get an
        /// iteration out of.</remarks>
        internal static readonly EquatableList<T> Empty = new([]);

        /// <summary>
        /// The elements held.
        /// </summary>
        [DataMember]
        private readonly T[] _items;

        /// <summary>
        /// Compares two elements.
        /// </summary>
        /// <remarks>An element that is itself an array is compared by its contents, since an array compares by
        /// reference and a list of arrays would otherwise be no better off than the list this type replaces.</remarks>
        private static readonly IEqualityComparer<T> ElementComparer = ElementEqualityComparer<T>.Default;
    }
}
