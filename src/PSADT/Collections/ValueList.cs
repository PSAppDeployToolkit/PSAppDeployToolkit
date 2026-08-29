using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using PSADT.Utilities;

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
    /// </para><para> It is filled once and then left alone, and its hash code is worked out on first use and kept.
    /// The parameterless constructor and <see cref="Add"/> exist for the data contract serializer, which builds a
    /// collection by constructing an empty one and adding to it, and which refuses a collection type offering no way
    /// to do that. Nothing else should call them: the point of the type is to stand in for a value, and a list that
    /// changed after the record holding it was built would change that record's hash code underneath whatever was
    /// holding it. </para></remarks>
    /// <typeparam name="T">The type of the elements.</typeparam>
    internal sealed class ValueList<T> : IReadOnlyList<T>, IEquatable<ValueList<T>>
    {
        /// <summary>
        /// Initializes a new, empty instance of the <see cref="ValueList{T}"/> class.
        /// </summary>
        /// <remarks>For the data contract serializer, which fills it through <see cref="Add"/>.</remarks>
        public ValueList()
        {
            _items = [];
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueList{T}"/> class holding the specified elements.
        /// </summary>
        /// <param name="items">The elements to hold. They are copied, so the caller may go on using its own list.</param>
        internal ValueList(IEnumerable<T> items)
        {
            ArgumentNullException.ThrowIfNull(items);
            _items = [.. items];
        }

        /// <summary>
        /// Appends an element.
        /// </summary>
        /// <remarks>For the data contract serializer. See the remarks on the type.</remarks>
        /// <param name="item">The element to append.</param>
        public void Add(T item)
        {
            _items.Add(item);
            _hashCode = null;
        }

        /// <summary>
        /// Determines whether this list holds the same elements, in the same order, as another.
        /// </summary>
        /// <param name="other">The list to compare against.</param>
        /// <returns><see langword="true"/> if the two hold the same elements; otherwise, <see langword="false"/>.</returns>
        public bool Equals([NotNullWhen(true)] ValueList<T>? other)
        {
            return ReferenceEquals(this, other) || (other is not null && _items.Count == other._items.Count && _items.SequenceEqual(other._items, ElementComparer));
        }

        /// <inheritdoc/>
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return Equals(obj as ValueList<T>);
        }

        /// <inheritdoc/>
        /// <remarks>Worked out once and kept, since a record holding this asks for it every time it is put in a
        /// dictionary or a set and the list itself does not change after it has been built.</remarks>
        [SuppressMessage("Major Code Smell", "S2328:GetHashCode should not reference mutable fields", Justification = "The list is filled once and then left alone, which is what the remarks on the type describe; the cache is cleared if anything does append.")]
        public override int GetHashCode()
        {
            // Combined through the shared helper rather than here, so that every hash this library produces
            // from a sequence of values is produced the same way. The comparer is handed over with it because
            // an element that is an array has to be hashed by its contents rather than by its reference.
            return _hashCode ??= CryptographicUtilities.GenerateHashCode(_items, ElementComparer);
        }

        /// <inheritdoc/>
        public IEnumerator<T> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        /// <inheritdoc/>
        public T this[int index] => _items[index];

        /// <inheritdoc/>
        public int Count => _items.Count;

        /// <summary>
        /// The elements held.
        /// </summary>
        private readonly List<T> _items;

        /// <summary>
        /// The hash code of the elements, worked out on first use.
        /// </summary>
        private int? _hashCode;

        /// <summary>
        /// Compares two elements.
        /// </summary>
        /// <remarks>An element that is itself an array is compared by its contents, since an array compares by
        /// reference and a list of arrays would otherwise be no better off than the list this type replaces.</remarks>
        private static readonly IEqualityComparer<T> ElementComparer = ValueEqualityComparer<T>.Default;
    }
}
