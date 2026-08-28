using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.Serialization;
using PSADT.Collections;
using Xunit;

namespace PSADT.Tests.Collections
{
    /// <summary>
    /// Tests the dictionary that records hold in place of a framework one so that they compare by value.
    /// </summary>
    /// <remarks>
    /// The counterpart to <see cref="ValueListTests"/>, and it exists for the same reason: every dictionary
    /// the framework offers compares by reference, which quietly breaks the equality a record advertises.
    /// So the tests are about the comparison rather than about the dictionary.
    /// <para>
    /// What is specific to this type, and therefore gets the most attention here, is that the order the
    /// entries were added in must not count - two dictionaries describing the same mapping are the same
    /// mapping however they were filled - and that the hash has to agree with that, which is the part a
    /// running total over a sequence does not give for free.
    /// </para>
    /// </remarks>
    public sealed class ValueDictionaryTests
    {
        /// <summary>
        /// Verifies that two dictionaries holding the same entries are equal and hash alike.
        /// </summary>
        [Fact]
        public void Equals_IsByTheEntries()
        {
            // Arrange
            ValueDictionary<string, string> first = new([new("alpha", "one"), new("bravo", "two")]);
            ValueDictionary<string, string> second = new([new("alpha", "one"), new("bravo", "two")]);

            // Assert
            Assert.Equal(first, second);
            Assert.Equal(first.GetHashCode(), second.GetHashCode());
        }

        /// <summary>
        /// Verifies that the order the entries were added in is not part of the comparison, since a mapping
        /// means the same thing however it was built.
        /// </summary>
        /// <remarks>
        /// The hash is asserted alongside the comparison rather than separately. A comparison that ignores
        /// order and a hash that does not would put two equal dictionaries in different buckets, which is
        /// the fault that surfaces a long way from its cause.
        /// </remarks>
        [Fact]
        public void Equals_DoesNotTakeOrderIntoAccount()
        {
            // Arrange
            ValueDictionary<string, string> first = new([new("alpha", "one"), new("bravo", "two"), new("charlie", "three")]);
            ValueDictionary<string, string> second = new([new("charlie", "three"), new("alpha", "one"), new("bravo", "two")]);

            // Assert
            Assert.Equal(first, second);
            Assert.Equal(first.GetHashCode(), second.GetHashCode());
        }

        /// <summary>
        /// Verifies that the values are part of the comparison, not just the keys.
        /// </summary>
        [Fact]
        public void Equals_TakesTheValuesIntoAccount()
        {
            Assert.NotEqual(
                new ValueDictionary<string, string>([new("alpha", "one")]),
                new ValueDictionary<string, string>([new("alpha", "two")]));
        }

        /// <summary>
        /// Verifies that the keys are part of the comparison, not just the values.
        /// </summary>
        [Fact]
        public void Equals_TakesTheKeysIntoAccount()
        {
            Assert.NotEqual(
                new ValueDictionary<string, string>([new("alpha", "one")]),
                new ValueDictionary<string, string>([new("bravo", "one")]));
        }

        /// <summary>
        /// Verifies that dictionaries of different sizes are not equal, including where one is a subset of
        /// the other.
        /// </summary>
        [Fact]
        public void Equals_TakesSizeIntoAccount()
        {
            Assert.NotEqual(
                new ValueDictionary<string, string>([new("alpha", "one")]),
                new ValueDictionary<string, string>([new("alpha", "one"), new("bravo", "two")]));
            Assert.NotEqual(
                new ValueDictionary<string, string>([]),
                new ValueDictionary<string, string>([new("alpha", "one")]));
        }

        /// <summary>
        /// Verifies that two empty dictionaries are equal, since a record built with no entries and another
        /// built the same way describe the same thing.
        /// </summary>
        [Fact]
        public void Equals_TreatsTwoEmptyDictionariesAsEqual()
        {
            Assert.Equal(new ValueDictionary<string, string>([]), new ValueDictionary<string, string>([]));
            Assert.Equal(new ValueDictionary<string, string>([]).GetHashCode(), new ValueDictionary<string, string>([]).GetHashCode());
        }

        /// <summary>
        /// Verifies that nothing at all is not equal to a dictionary, and that comparing against it does not
        /// fail.
        /// </summary>
        [Fact]
        public void Equals_IsNotEqualToNothing()
        {
            Assert.False(new ValueDictionary<string, string>([]).Equals(other: null));
            Assert.False(new ValueDictionary<string, string>([new("alpha", "one")]).Equals(obj: null));
            Assert.False(new ValueDictionary<string, string>([new("alpha", "one")]).Equals(obj: "alpha"));
        }

        /// <summary>
        /// Verifies that values that are themselves arrays are compared by their contents.
        /// </summary>
        /// <remarks>
        /// This is the case a straightforward implementation gets wrong, and it is why the comparison is
        /// taken from a shared helper rather than from the default comparer.
        /// </remarks>
        [Fact]
        public void Equals_ComparesArrayValuesByTheirContents()
        {
            // Arrange: equal contents, different arrays
            ValueDictionary<string, byte[]> first = new([new("alpha", [1, 2, 3])]);
            ValueDictionary<string, byte[]> second = new([new("alpha", [1, 2, 3])]);

            // Assert
            Assert.Equal(first, second);
            Assert.Equal(first.GetHashCode(), second.GetHashCode());
            Assert.NotEqual(first, new ValueDictionary<string, byte[]>([new("alpha", [1, 2, 4])]));
        }

        /// <summary>
        /// Verifies that keys that are themselves arrays are compared by their contents, so that an entry
        /// put in under one array can be found under another holding the same bytes.
        /// </summary>
        [Fact]
        public void Equals_ComparesArrayKeysByTheirContents()
        {
            // Arrange: equal contents, different arrays
            ValueDictionary<byte[], string> first = new([new([1, 2, 3], "alpha")]);
            ValueDictionary<byte[], string> second = new([new([1, 2, 3], "alpha")]);

            // Assert
            Assert.Equal(first, second);
            Assert.Equal(first.GetHashCode(), second.GetHashCode());
            Assert.True(first.ContainsKey([1, 2, 3]));
            Assert.NotEqual(first, new ValueDictionary<byte[], string>([new([1, 2, 4], "alpha")]));
        }

        /// <summary>
        /// Verifies that a null value is held and compared rather than failing.
        /// </summary>
        [Fact]
        public void Equals_HandlesNullValues()
        {
            // Arrange
            ValueDictionary<string, string?> first = new([new("alpha", value: null), new("bravo", "two")]);
            ValueDictionary<string, string?> second = new([new("alpha", value: null), new("bravo", "two")]);

            // Assert
            Assert.Equal(first, second);
            Assert.Equal(first.GetHashCode(), second.GetHashCode());
            Assert.NotEqual(first, new ValueDictionary<string, string?>([new("alpha", "one"), new("bravo", "two")]));
        }

        /// <summary>
        /// Verifies that a dictionary whose values are themselves dictionaries compares all the way down.
        /// </summary>
        /// <remarks>
        /// This is the shape the help console's module map is held in, and the reason the type had to exist
        /// at all: the outer mapping compares by its entries only if the inner ones do too, which they do
        /// only because they are this type rather than a framework dictionary.
        /// </remarks>
        [Fact]
        public void Equals_ComparesNestedDictionariesByTheirEntries()
        {
            // Arrange
            ValueDictionary<string, ValueDictionary<string, string>> first = new([new("module", new([new("topic", "help")]))]);
            ValueDictionary<string, ValueDictionary<string, string>> second = new([new("module", new([new("topic", "help")]))]);

            // Assert
            Assert.Equal(first, second);
            Assert.Equal(first.GetHashCode(), second.GetHashCode());
            Assert.NotEqual(first, new ValueDictionary<string, ValueDictionary<string, string>>([new("module", new([new("topic", "other")]))]));
        }

        /// <summary>
        /// Verifies that the entries handed in are the entries read back, through every member a caller has
        /// to read them with.
        /// </summary>
        [Fact]
        public void ValueDictionary_HoldsWhatItIsGiven()
        {
            // Arrange
            ValueDictionary<string, string> dictionary = new([new("alpha", "one"), new("bravo", "two")]);

            // Assert
            Assert.Equal(2, dictionary.Count);
            Assert.Equal("two", dictionary["bravo"]);
            Assert.True(dictionary.ContainsKey("alpha"));
            Assert.False(dictionary.ContainsKey("charlie"));
            Assert.Equal(["alpha", "bravo"], dictionary.Keys);
            Assert.Equal(["one", "two"], dictionary.Values);
        }

        /// <summary>
        /// Verifies that a lookup reports whether it found anything, and hands back the value only when it
        /// did.
        /// </summary>
        /// <remarks>
        /// Worth its own test because the lookup is not a straight delegation to the underlying dictionary:
        /// the two target frameworks describe what that hands back differently, so it is written a way that
        /// compiles clean on both and therefore has to be confirmed to still behave.
        /// </remarks>
        [Fact]
        public void TryGetValue_ReportsWhetherTheKeyIsThere()
        {
            // Arrange
            ValueDictionary<string, string> dictionary = new([new("alpha", "one")]);

            // Assert
            Assert.True(dictionary.TryGetValue("alpha", out string? found));
            Assert.Equal("one", found);
            Assert.False(dictionary.TryGetValue("bravo", out string? missing));
            Assert.Null(missing);
        }

        /// <summary>
        /// Verifies that the entries are copied rather than referenced, so a caller that goes on using its
        /// own dictionary does not alter one that has already been built - which would change its
        /// comparison, and with it the comparison of whatever record is holding it.
        /// </summary>
        [Fact]
        public void ValueDictionary_CopiesWhatItIsGiven()
        {
            // Arrange
            Dictionary<string, string> source = new(StringComparer.Ordinal) { ["alpha"] = "one" };
            ValueDictionary<string, string> dictionary = new(source);

            // Act
            source["bravo"] = "two";

            // Assert
            _ = Assert.Single(dictionary);
        }

        /// <summary>
        /// Verifies that adding changes the comparison, since the hash is worked out once and kept and a
        /// stale one would leave the dictionary findable under the wrong key.
        /// </summary>
        /// <remarks>
        /// Adding is only meant to happen while the serializer is rebuilding a dictionary, before anything
        /// has asked it for anything. Asserted anyway, for the same reason the list asserts it.
        /// </remarks>
        [Fact]
        public void Add_IsReflectedInTheComparison()
        {
            // Arrange
            ValueDictionary<string, string> dictionary = new([new("alpha", "one")]);
            int before = dictionary.GetHashCode();

            // Act
            dictionary.Add("bravo", "two");

            // Assert
            Assert.Equal(new ValueDictionary<string, string>([new("alpha", "one"), new("bravo", "two")]), dictionary);
            Assert.Equal(new ValueDictionary<string, string>([new("alpha", "one"), new("bravo", "two")]).GetHashCode(), dictionary.GetHashCode());
            Assert.NotEqual(before, dictionary.GetHashCode());
        }

        /// <summary>
        /// Verifies that every way of changing a dictionary other than the one the serializer needs is
        /// refused.
        /// </summary>
        /// <remarks>
        /// The type is meant to stand in for a value, so a dictionary that could be emptied or rewritten
        /// after the record holding it was built would change that record's hash underneath whatever was
        /// holding it. These members exist only because the interface the serializer and the read-only
        /// wrapper both require declares them.
        /// </remarks>
        [Fact]
        public void ValueDictionary_RefusesToBeChangedAfterTheFact()
        {
            // Arrange
            IDictionary<string, string> dictionary = new ValueDictionary<string, string>([new("alpha", "one")]);

            // Assert
            _ = Assert.Throws<NotSupportedException>(() => dictionary.Remove("alpha"));
            _ = Assert.Throws<NotSupportedException>(() => dictionary.Remove(new KeyValuePair<string, string>("alpha", "one")));
            _ = Assert.Throws<NotSupportedException>(dictionary.Clear);
            _ = Assert.Throws<NotSupportedException>(() => dictionary.Add(new KeyValuePair<string, string>("bravo", "two")));
            _ = Assert.Throws<NotSupportedException>(() => dictionary["alpha"] = "two");
            _ = Assert.Single(dictionary);
        }

        /// <summary>
        /// Verifies that the members carried only to satisfy the collection interface report what they are
        /// supposed to, since nothing in the library calls them and a mistake would go unnoticed.
        /// </summary>
        [Fact]
        public void ValueDictionary_SatisfiesTheCollectionInterface()
        {
            // Arrange
            ICollection<KeyValuePair<string, string>> collection = new ValueDictionary<string, string>([new("alpha", "one")]);
            KeyValuePair<string, string>[] target = new KeyValuePair<string, string>[1];

            // Act
            collection.CopyTo(target, 0);

            // Assert
            Assert.True(collection.Contains(new KeyValuePair<string, string>("alpha", "one")));
            Assert.False(collection.Contains(new KeyValuePair<string, string>("alpha", "two")));
            Assert.False(collection.Contains(new KeyValuePair<string, string>("bravo", "one")));
            Assert.Equal([new("alpha", "one")], target);
        }

        /// <summary>
        /// Verifies that a dictionary survives a data contract round trip, which is the only reason the
        /// parameterless constructor and <c>Add</c> are public at all.
        /// </summary>
        /// <remarks>
        /// The serializer rebuilds a collection by constructing an empty one and adding to it, and refuses
        /// a type offering no way to do that. Nesting one inside another is asserted because that is the
        /// shape the help console's module map takes over the wire.
        /// </remarks>
        [Fact]
        public void Serialization_RoundTripsEveryEntry()
        {
            // Arrange
            ValueDictionary<string, ValueDictionary<string, string>> original = new([new("module", new([new("topic", "help"), new("other", "text")]))]);
            DataContractSerializer serializer = new(typeof(ValueDictionary<string, ValueDictionary<string, string>>));

            // Act
            using MemoryStream stream = new();
            serializer.WriteObject(stream, original);
            stream.Position = 0;

            // Assigned through a local rather than cast inline: the two target frameworks disagree on
            // whether ReadObject's return is nullable, so a null-forgiving operator is necessary on one
            // and flagged as redundant on the other.
            object? deserialized = serializer.ReadObject(stream);
            Assert.NotNull(deserialized);
            ValueDictionary<string, ValueDictionary<string, string>> restored = (ValueDictionary<string, ValueDictionary<string, string>>)deserialized;

            // Assert
            Assert.Equal(original, restored);
            Assert.Equal(original.GetHashCode(), restored.GetHashCode());
            Assert.Equal("help", restored["module"]["topic"]);
        }

        /// <summary>
        /// Verifies that the type can be handed straight to the read-only wrapper, which is how a record
        /// exposes one without letting an interface reach PowerShell.
        /// </summary>
        /// <remarks>
        /// The wrapper takes a mutable dictionary, which is the whole reason this type implements one
        /// despite standing in for a value. Reading through it is asserted because the wrapper holds the
        /// dictionary rather than copying it.
        /// </remarks>
        [Fact]
        public void ValueDictionary_CanBeWrappedForCallers()
        {
            // Arrange
            ValueDictionary<string, string> dictionary = new([new("alpha", "one"), new("bravo", "two")]);

            // Act
            ReadOnlyDictionary<string, string> wrapped = new(dictionary);

            // Assert
            Assert.Equal(2, wrapped.Count);
            Assert.Equal("one", wrapped["alpha"]);
            Assert.True(wrapped.TryGetValue("bravo", out string? found));
            Assert.Equal("two", found);
        }

        /// <summary>
        /// Verifies that nothing at all is refused, since a dictionary built from nothing is a caller's
        /// mistake rather than an empty dictionary.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "This is deliberate as part of unit testing.")]
        [Fact]
        public void ValueDictionary_RefusesNothingAtAll()
        {
            _ = Assert.Throws<ArgumentNullException>(static () => new ValueDictionary<string, string>(null!));
        }

        /// <summary>
        /// Verifies that the same key twice is refused rather than quietly keeping one of them, since a
        /// caller handing over two values for one key has not decided what it means.
        /// </summary>
        [Fact]
        public void ValueDictionary_RefusesADuplicateKey()
        {
            _ = Assert.Throws<ArgumentException>(static () => new ValueDictionary<string, string>([new("alpha", "one"), new("alpha", "two")]));
        }
    }
}
