using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using PSADT.Utilities;

namespace PSADT.Collections
{
    /// <summary>
    /// A dictionary that compares by its contents rather than by reference.
    /// </summary>
    /// <remarks>The dictionary counterpart to <see cref="EquatableList{T}"/>, and it exists for the same reason: a
    /// record compares each of its fields, every dictionary the framework offers compares by reference, and a record
    /// holding one is therefore a record that never equals another describing the same thing. A record stores this
    /// privately and exposes it through a property typed as <see cref="IReadOnlyDictionary{TKey, TValue}"/>, so the
    /// generated equality picks up the entries while the type's callers see no difference.
    /// <para> Order is not part of the comparison, since two dictionaries holding the same entries describe the same
    /// thing however they were filled. Keys and values are both compared the way <see cref="ElementEqualityComparer{T}"/>
    /// compares them, so a dictionary of arrays compares by the arrays' contents rather than by their references.
    /// </para><para> It is filled once and then left alone, and its hash code is worked out on first use and kept.
    /// Only <see cref="IReadOnlyDictionary{TKey, TValue}"/> is implemented, so there is no member on any surface that
    /// would change it. The parameterless constructor and <see cref="Add"/> are private and exist solely for the data
    /// contract serializer, which builds a collection by constructing an empty one and adding to it, reaches both by
    /// reflection, and refuses a type offering no way to do it. Being private is the point: a dictionary that changed
    /// after the record holding it was built would change that record's hash code underneath whatever was holding it,
    /// so nothing outside this type can. </para><para> <see cref="Add"/> takes a <see cref="KeyValuePair{TKey,
    /// TValue}"/> rather than a key and a value because that is what the serializer looks for once a type is not an
    /// <c language="csharp">IDictionary</c>: it treats this as a collection of pairs and wants the pair. </para></remarks>
    /// <typeparam name="TKey">The type of the keys.</typeparam>
    /// <typeparam name="TValue">The type of the values.</typeparam>
    [SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix", Justification = "The Dictionary suffix is the correct one and is already present.")]
    [SuppressMessage("Design", "MA0182:Avoid unused internal types", Justification = "This is used across InternalsVisibleTo boundaries, by PSADT.UserInterface and by the tests.")]
    internal sealed class EquatableDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>, IEquatable<EquatableDictionary<TKey, TValue>> where TKey : notnull
    {
        /// <summary>
        /// Initializes a new, empty instance of the <see cref="EquatableDictionary{TKey, TValue}"/> class.
        /// </summary>
        /// <remarks>For the data contract serializer, which fills it through <see cref="Add"/>.</remarks>
        private EquatableDictionary()
        {
            _items = new(KeyComparer);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EquatableDictionary{TKey, TValue}"/> class holding the specified
        /// entries.
        /// </summary>
        /// <param name="entries">The entries to hold. They are copied, so the caller may go on using its own dictionary.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="entries"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="entries"/> holds the same key more than once.</exception>
        internal EquatableDictionary(IEnumerable<KeyValuePair<TKey, TValue>> entries)
        {
            ArgumentNullException.ThrowIfNull(entries);
            _items = new(KeyComparer);
            foreach (KeyValuePair<TKey, TValue> entry in entries)
            {
                _items.Add(entry.Key, entry.Value);
            }
        }

        /// <summary>
        /// Adds an entry.
        /// </summary>
        /// <remarks>For the data contract serializer. See the remarks on the type.</remarks>
        /// <param name="item">The entry to add.</param>
        [SuppressMessage("CodeQuality", "IDE0052:Remove unread private members", Justification = "The data contract serializer calls this by reflection, which the compiler cannot see.")]
        private void Add(KeyValuePair<TKey, TValue> item)
        {
            _items.Add(item.Key, item.Value);
            _hashCode = null;
        }

        /// <summary>
        /// Determines whether this dictionary holds the same entries as another.
        /// </summary>
        /// <param name="other">The dictionary to compare against.</param>
        /// <returns><see langword="true"/> if the two hold the same entries; otherwise, <see langword="false"/>.</returns>
        public bool Equals([NotNullWhen(true)] EquatableDictionary<TKey, TValue>? other)
        {
            return ReferenceEquals(this, other) || (other is not null && _items.Count == other._items.Count && _items.All(entry => other._items.TryGetValue(entry.Key, out TValue? value) && ValueComparer.Equals(entry.Value, value)));
        }

        /// <inheritdoc/>
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return Equals(obj as EquatableDictionary<TKey, TValue>);
        }

        /// <inheritdoc/>
        /// <remarks>Worked out once and kept, since a record holding this asks for it every time it is put in a
        /// dictionary or a set and the dictionary itself does not change after it has been built. <para> Each entry is
        /// reduced to a hash of its key and value, and those are then sorted before being combined, so that two
        /// dictionaries holding the same entries hash alike however they were filled - which is what makes this agree
        /// with the comparison above, where order does not count. </para></remarks>
        [SuppressMessage("Major Code Smell", "S2328:GetHashCode should not reference mutable fields", Justification = "The dictionary is filled once and then left alone, which is what the remarks on the type describe; the cache is cleared if the serializer does add.")]
        public override int GetHashCode()
        {
            // Combined through the shared helper rather than here, so that every hash this library produces
            // from a sequence of values is produced the same way.
            return _hashCode ??= CryptographicUtilities.GenerateHashCode(GetSortedEntryHashCodes(), EqualityComparer<int>.Default);
        }

        /// <inheritdoc/>
        public bool ContainsKey(TKey key)
        {
            return _items.ContainsKey(key);
        }

        /// <inheritdoc/>
        [SuppressMessage("Style", "IDE0370:Suppression is unnecessary", Justification = "This is needed for interop between net472 and modern targets.")]
        public bool TryGetValue(TKey key, out TValue value)
        {
            return _items.TryGetValue(key, out value!);
        }

        /// <inheritdoc/>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        /// <summary>
        /// Reduces each entry to a hash of its key and value, and sorts them.
        /// </summary>
        /// <remarks>Sorting is what makes the result independent of the order the dictionary was filled in,
        /// which the comparison requires and which the combining helper - being a running total over a sequence - does
        /// not provide on its own.</remarks>
        /// <returns>The entries' hash codes, in ascending order.</returns>
        private List<int> GetSortedEntryHashCodes()
        {
            List<int> hashCodes = new(_items.Count);
            foreach (KeyValuePair<TKey, TValue> entry in _items)
            {
                unchecked
                {
                    hashCodes.Add((KeyComparer.GetHashCode(entry.Key) * 31) + (entry.Value is not null ? ValueComparer.GetHashCode(entry.Value) : 0));
                }
            }
            hashCodes.Sort();
            return hashCodes;
        }

        /// <inheritdoc/>
        public TValue this[TKey key] => _items[key];

        /// <inheritdoc/>
        public IEnumerable<TKey> Keys => _items.Keys;

        /// <inheritdoc/>
        public IEnumerable<TValue> Values => _items.Values;

        /// <inheritdoc/>
        public int Count => _items.Count;

        /// <summary>
        /// The entries held.
        /// </summary>
        private readonly Dictionary<TKey, TValue> _items;

        /// <summary>
        /// The hash code of the entries, worked out on first use.
        /// </summary>
        private int? _hashCode;

        /// <summary>
        /// Compares two keys.
        /// </summary>
        private static readonly IEqualityComparer<TKey> KeyComparer = ElementEqualityComparer<TKey>.Default;

        /// <summary>
        /// Compares two values.
        /// </summary>
        private static readonly IEqualityComparer<TValue> ValueComparer = ElementEqualityComparer<TValue>.Default;
    }
}
