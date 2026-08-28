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
    /// <remarks>The dictionary counterpart to <see cref="ValueList{T}"/>, and it exists for the same reason: a
    /// record compares each of its fields, every dictionary the framework offers compares by reference, and a record
    /// holding one is therefore a record that never equals another describing the same thing. A record stores this
    /// privately and exposes it wrapped in a <see cref="System.Collections.ObjectModel.ReadOnlyDictionary{TKey,
    /// TValue}"/>, so the generated equality picks up the entries while the type's callers see a concrete dictionary.
    /// <para> Order is not part of the comparison, since two dictionaries holding the same entries describe the same
    /// thing however they were filled. Keys and values are both compared the way <see cref="ValueEqualityComparer{T}"/>
    /// compares them, so a dictionary of arrays compares by the arrays' contents rather than by their references.
    /// </para><para> It is filled once and then left alone. Its hash code is worked out on first use and kept, and the
    /// members that would change it after the fact - <see cref="IDictionary{TKey, TValue}.Remove(TKey)"/>, <see
    /// cref="ICollection{T}.Clear"/> and the assigning indexer - throw rather than allow it. <see cref="Add"/> and the
    /// parameterless constructor are the exception, and they exist for the data contract serializer, which builds a
    /// dictionary by constructing an empty one and adding to it and which refuses a type offering no way to do that.
    /// Nothing else should call them: a dictionary that changed after the record holding it was built would change that
    /// record's hash code underneath whatever was holding it. </para><para> <see cref="IDictionary{TKey, TValue}"/> is
    /// implemented, despite the type being a value, because it is what both the serializer and <see
    /// cref="System.Collections.ObjectModel.ReadOnlyDictionary{TKey, TValue}"/> require. The members of it that do not
    /// belong here are implemented explicitly so that they stay off this type's own surface. </para></remarks>
    /// <typeparam name="TKey">The type of the keys.</typeparam>
    /// <typeparam name="TValue">The type of the values.</typeparam>
    [SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix", Justification = "The Dictionary suffix is the correct one and is already present.")]
    [SuppressMessage("Design", "MA0182:Avoid unused internal types", Justification = "This is used across InternalsVisibleTo boundaries, by PSADT.UserInterface and by the tests.")]
    internal sealed class ValueDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>, IEquatable<ValueDictionary<TKey, TValue>> where TKey : notnull
    {
        /// <summary>
        /// Initializes a new, empty instance of the <see cref="ValueDictionary{TKey, TValue}"/> class.
        /// </summary>
        /// <remarks>For the data contract serializer, which fills it through <see cref="Add"/>.</remarks>
        public ValueDictionary()
        {
            _items = new(KeyComparer);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueDictionary{TKey, TValue}"/> class holding the specified
        /// entries.
        /// </summary>
        /// <param name="entries">The entries to hold. They are copied, so the caller may go on using its own dictionary.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="entries"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="entries"/> holds the same key more than once.</exception>
        internal ValueDictionary(IEnumerable<KeyValuePair<TKey, TValue>> entries)
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
        /// <param name="key">The key to add.</param>
        /// <param name="value">The value to add against it.</param>
        public void Add(TKey key, TValue value)
        {
            _items.Add(key, value);
            _hashCode = null;
        }

        /// <summary>
        /// Determines whether this dictionary holds the same entries as another.
        /// </summary>
        /// <param name="other">The dictionary to compare against.</param>
        /// <returns><see langword="true"/> if the two hold the same entries; otherwise, <see langword="false"/>.</returns>
        public bool Equals([NotNullWhen(true)] ValueDictionary<TKey, TValue>? other)
        {
            return ReferenceEquals(this, other) || (other is not null && _items.Count == other._items.Count && _items.All(entry => other._items.TryGetValue(entry.Key, out TValue? value) && ValueComparer.Equals(entry.Value, value)));
        }

        /// <inheritdoc/>
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return Equals(obj as ValueDictionary<TKey, TValue>);
        }

        /// <inheritdoc/>
        /// <remarks>Worked out once and kept, since a record holding this asks for it every time it is put in a
        /// dictionary or a set and the dictionary itself does not change after it has been built. <para> Each entry is
        /// reduced to a hash of its key and value, and those are then sorted before being combined, so that two
        /// dictionaries holding the same entries hash alike however they were filled - which is what makes this agree
        /// with the comparison above, where order does not count. </para></remarks>
        [SuppressMessage("Major Code Smell", "S2328:GetHashCode should not reference mutable fields", Justification = "The dictionary is filled once and then left alone, which is what the remarks on the type describe; the cache is cleared if anything does add.")]
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
        /// <remarks>The <c>MaybeNullWhen</c> annotation the framework puts on this parameter is deliberately not
        /// repeated here. The .NET Framework reference assemblies carry no nullable annotations at all, so annotating
        /// the implementation more richly than the member it implements is an error there; leaving it off says only
        /// that the value is set, which is true on both. <para> The lookup goes through the indexer rather than the
        /// underlying dictionary's own <c>TryGetValue</c> for the same reason: what that reports about the value it
        /// hands back differs between the two frameworks, so a single spelling of it cannot compile clean on both.
        /// </para></remarks>
        [SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "An unconstrained TValue has no other way to spell the value handed back when the key is absent, which the return value already tells the caller to ignore.")]
        public bool TryGetValue(TKey key, out TValue value)
        {
            bool found = _items.ContainsKey(key);
            value = found ? _items[key] : default!;
            return found;
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

        /// <inheritdoc/>
        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        {
            return _items.TryGetValue(item.Key, out TValue? value) && ValueComparer.Equals(item.Value, value);
        }

        /// <inheritdoc/>
        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<TKey, TValue>>)_items).CopyTo(array, arrayIndex);
        }

        /// <inheritdoc/>
        /// <remarks>Not supported. See the remarks on the type.</remarks>
        /// <exception cref="NotSupportedException">Always thrown.</exception>
        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        {
            throw new NotSupportedException("A ValueDictionary is filled through Add(TKey, TValue) and not changed afterwards.");
        }

        /// <inheritdoc/>
        /// <remarks>Not supported. See the remarks on the type.</remarks>
        /// <exception cref="NotSupportedException">Always thrown.</exception>
        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            throw new NotSupportedException("A ValueDictionary is filled through Add(TKey, TValue) and not changed afterwards.");
        }

        /// <inheritdoc/>
        /// <remarks>Not supported. See the remarks on the type.</remarks>
        /// <exception cref="NotSupportedException">Always thrown.</exception>
        bool IDictionary<TKey, TValue>.Remove(TKey key)
        {
            throw new NotSupportedException("A ValueDictionary is filled through Add(TKey, TValue) and not changed afterwards.");
        }

        /// <inheritdoc/>
        /// <remarks>Not supported. See the remarks on the type.</remarks>
        /// <exception cref="NotSupportedException">Always thrown.</exception>
        void ICollection<KeyValuePair<TKey, TValue>>.Clear()
        {
            throw new NotSupportedException("A ValueDictionary is filled through Add(TKey, TValue) and not changed afterwards.");
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

        /// <inheritdoc cref="IReadOnlyDictionary{TKey, TValue}.this" />
        public TValue this[TKey key] => _items[key];

        /// <inheritdoc/>
        /// <remarks>Reading is supported; assigning is not. See the remarks on the type.</remarks>
        /// <exception cref="NotSupportedException">Thrown when assigning.</exception>
        TValue IDictionary<TKey, TValue>.this[TKey key]
        {
            get => _items[key];
            set => throw new NotSupportedException("A ValueDictionary is filled through Add(TKey, TValue) and not changed afterwards.");
        }

        /// <inheritdoc cref="IDictionary{TKey, TValue}.Keys" />
        public ICollection<TKey> Keys => _items.Keys;

        /// <inheritdoc/>
        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => _items.Keys;

        /// <inheritdoc cref="IDictionary{TKey, TValue}.Values" />
        public ICollection<TValue> Values => _items.Values;

        /// <inheritdoc/>
        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => _items.Values;

        /// <inheritdoc/>
        public int Count => _items.Count;

        /// <inheritdoc/>
        /// <remarks>Reported as false because the serializer does fill this through <see cref="Add"/>. Every other
        /// way of changing it throws; see the remarks on the type.</remarks>
        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;

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
        private static readonly IEqualityComparer<TKey> KeyComparer = ValueEqualityComparer<TKey>.Default;

        /// <summary>
        /// Compares two values.
        /// </summary>
        private static readonly IEqualityComparer<TValue> ValueComparer = ValueEqualityComparer<TValue>.Default;
    }
}
