using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

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
    /// compares them, so a dictionary of arrays compares by the arrays' contents rather than by their references. That
    /// cuts both ways for a key: a caller that holds on to an array it used as one and then writes to it changes what
    /// that key hashes to, and the entry it opened is no longer reachable. Only the entries are copied in, not the
    /// keys themselves, so a key has to be left alone once it has been handed over.
    /// </para><para> It is filled once and then left alone. Only <see cref="IReadOnlyDictionary{TKey, TValue}"/> is
    /// implemented, so there is no member on any surface that would change it. <see cref="DataContractAttribute"/> is what allows that: the data contract serializer would
    /// otherwise see <see cref="IEnumerable{T}"/>, take this for a collection of pairs, and refuse one that offers no
    /// <c language="csharp">Add</c> for it to fill. Carrying the attribute sends it down the ordinary class path instead, where it
    /// writes the one field and rebuilds the type without running a constructor. </para><para> What that path does not
    /// carry over is the comparer, and the entries are looked up through it - so <see cref="OnDeserialized"/> puts it
    /// back before anything reads them. Without it a dictionary keyed by arrays would come off the wire looking its
    /// keys up by reference, and two that had just been sent as equal would arrive unequal. </para></remarks>
    /// <typeparam name="TKey">The type of the keys.</typeparam>
    /// <typeparam name="TValue">The type of the values.</typeparam>
    [SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix", Justification = "The Dictionary suffix is the correct one and is already present.")]
    [SuppressMessage("Design", "MA0182:Avoid unused internal types", Justification = "This is used across InternalsVisibleTo boundaries, by PSADT.UserInterface and by the tests.")]
    [DataContract]
    internal sealed class EquatableDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>, IEquatable<EquatableDictionary<TKey, TValue>> where TKey : notnull
    {
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
        /// Puts the comparer back once the serializer has rebuilt the entries.
        /// </summary>
        /// <remarks>The serializer rebuilds the entries into a dictionary of its own making, which is a dictionary
        /// with the framework's comparer. Putting the comparer back here rather than at each read is what makes a
        /// dictionary off the wire the same dictionary as one that was built.</remarks>
        /// <param name="context">The deserialization context.</param>
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            _items = new(_items, KeyComparer);
        }

        /// <summary>
        /// Determines whether this dictionary holds the same entries as another.
        /// </summary>
        /// <param name="other">The dictionary to compare against.</param>
        /// <returns><see langword="true"/> if the two hold the same entries; otherwise, <see langword="false"/>.</returns>
        public bool Equals([NotNullWhen(true)] EquatableDictionary<TKey, TValue>? other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }
            if (other is null || _items.Count != other._items.Count)
            {
                return false;
            }

            // Walked rather than run through a query, which would capture the other dictionary into a closure and
            // box this one's enumerator on every comparison. The counts match, so every entry held there being held
            // here is enough - nothing there is left over.
            foreach (KeyValuePair<TKey, TValue> entry in _items)
            {
                if (!other._items.TryGetValue(entry.Key, out TValue? value) || !ValueComparer.Equals(entry.Value, value))
                {
                    return false;
                }
            }
            return true;
        }

        /// <inheritdoc/>
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return Equals(obj as EquatableDictionary<TKey, TValue>);
        }

        /// <inheritdoc/>
        /// <remarks>Worked out on every call rather than once and kept. A kept one would be worth something only
        /// where the same dictionary is hashed more than once, which nothing here does. What it would cost is a hash
        /// that stops describing what the dictionary holds: a value that is itself an array is handed out by the
        /// indexer and is writable through it, and a kept hash would go on reporting what that value used to be.
        /// Worked out on demand the two always agree, and a caller that writes to something it was handed has broken
        /// the rule every hash container has rather than found a fault in this one.</remarks>
        public override int GetHashCode()
        {
            return ComputeHashCode();
        }

        /// <summary>
        /// Determines whether two dictionaries hold the same entries under the same keys.
        /// </summary>
        /// <remarks>Defined so that the operator cannot quietly disagree with <see cref="Equals(EquatableDictionary{TKey, TValue})"/>,
        /// which it would if it were left comparing references as a reference type's operator does by default.</remarks>
        /// <param name="left">The first dictionary, which may be <see langword="null"/>.</param>
        /// <param name="right">The second dictionary, which may be <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if the two hold the same entries, or both are <see langword="null"/>; otherwise, <see langword="false"/>.</returns>
        public static bool operator ==(EquatableDictionary<TKey, TValue>? left, EquatableDictionary<TKey, TValue>? right)
        {
            return left is not null ? left.Equals(right) : right is null;
        }

        /// <summary>
        /// Determines whether two dictionaries differ in their entries or in the keys they are held under.
        /// </summary>
        /// <param name="left">The first dictionary, which may be <see langword="null"/>.</param>
        /// <param name="right">The second dictionary, which may be <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if the two differ, or one is <see langword="null"/> and the other is not; otherwise, <see langword="false"/>.</returns>
        public static bool operator !=(EquatableDictionary<TKey, TValue>? left, EquatableDictionary<TKey, TValue>? right)
        {
            return !(left == right);
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
        /// Combines the entries into a hash code.
        /// </summary>
        /// <remarks>Each entry is reduced to a hash of its key and value, and those are added together rather than
        /// folded one after another, since addition is what makes the result the same however the dictionary was
        /// filled - which the comparison requires, order not counting there either. The sum is allowed to wrap, a hash
        /// being a bit pattern rather than a number. The count seeds it so that entries hashing to zero still tell one
        /// size of dictionary from another. <para> The comparers are handed to the combiner rather than asked for a
        /// hash here, so that a null value is answered the same way the list answers a null element. </para></remarks>
        /// <returns>The hash code of the entries.</returns>
        private int ComputeHashCode()
        {
            unchecked
            {
                int hashCode = _items.Count;
                foreach (KeyValuePair<TKey, TValue> entry in _items)
                {
                    HashCode entryHashCode = new();
                    entryHashCode.Add(entry.Key, KeyComparer);
                    entryHashCode.Add(entry.Value, ValueComparer);
                    hashCode += entryHashCode.ToHashCode();
                }
                return hashCode;
            }
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
        /// <remarks>Not read-only, since <see cref="OnDeserialized"/> replaces it once to put the comparer back.</remarks>
        [DataMember]
        private Dictionary<TKey, TValue> _items;

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
