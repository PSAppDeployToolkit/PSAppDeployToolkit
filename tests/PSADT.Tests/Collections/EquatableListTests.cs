using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using PSADT.Collections;
using Xunit;

namespace PSADT.Tests.Collections
{
    /// <summary>
    /// Tests the list that records hold in place of a collection so that they compare by value.
    /// </summary>
    /// <remarks>
    /// The whole reason this type exists is that every collection the framework offers compares by
    /// reference, which quietly breaks the equality a record advertises. So the tests are about the
    /// comparison rather than about the list: that two holding the same elements match, that two holding
    /// different ones do not, and that a list of arrays is compared by the arrays' contents rather than
    /// by their references - which is the case a naive implementation gets wrong.
    /// </remarks>
    public sealed class EquatableListTests
    {
        /// <summary>
        /// Verifies that two lists holding the same elements are equal and hash alike.
        /// </summary>
        [Fact]
        public void Equals_IsByTheElements()
        {
            // Arrange
            EquatableList<string> first = new(["alpha", "bravo", "charlie"]);
            EquatableList<string> second = new(["alpha", "bravo", "charlie"]);

            // Assert
            Assert.Equal(first, second);
            Assert.Equal(first.GetHashCode(), second.GetHashCode());
        }

        /// <summary>
        /// Verifies that order is part of the comparison, since a list of arguments means something
        /// different in a different order.
        /// </summary>
        [Fact]
        public void Equals_TakesOrderIntoAccount()
        {
            Assert.NotEqual(new EquatableList<string>(["alpha", "bravo"]), new EquatableList<string>(["bravo", "alpha"]));
        }

        /// <summary>
        /// Verifies that lists of different lengths are not equal, including where one is a prefix of the
        /// other.
        /// </summary>
        [Fact]
        public void Equals_TakesLengthIntoAccount()
        {
            Assert.NotEqual(new EquatableList<string>(["alpha"]), new EquatableList<string>(["alpha", "bravo"]));
            Assert.NotEqual(new EquatableList<string>([]), new EquatableList<string>(["alpha"]));
        }

        /// <summary>
        /// Verifies that two empty lists are equal, since a record built with no arguments and another
        /// built the same way describe the same thing.
        /// </summary>
        [Fact]
        public void Equals_TreatsTwoEmptyListsAsEqual()
        {
            Assert.Equal(new EquatableList<string>([]), new EquatableList<string>([]));
            Assert.Equal(new EquatableList<string>([]).GetHashCode(), new EquatableList<string>([]).GetHashCode());
        }

        /// <summary>
        /// Verifies that nothing at all is not equal to a list, and that comparing against it does not
        /// fail.
        /// </summary>
        [Fact]
        public void Equals_IsNotEqualToNothing()
        {
            Assert.False(new EquatableList<string>([]).Equals(other: null));
            Assert.False(new EquatableList<string>(["alpha"]).Equals(obj: null));
            Assert.False(new EquatableList<string>(["alpha"]).Equals(obj: "alpha"));
        }

        /// <summary>
        /// Verifies that elements that are themselves arrays are compared by their contents.
        /// </summary>
        /// <remarks>
        /// This is the case a straightforward implementation gets wrong. An array compares by reference,
        /// so a list of arrays compared with the default comparer is no better off than the collection
        /// this type replaces - and the firmware tables are read as exactly that.
        /// </remarks>
        [Fact]
        public void Equals_ComparesArrayElementsByTheirContents()
        {
            // Arrange: equal contents, different arrays
            EquatableList<byte[]> first = new([[1, 2, 3], [4, 5, 6]]);
            EquatableList<byte[]> second = new([[1, 2, 3], [4, 5, 6]]);

            // Assert
            Assert.Equal(first, second);
            Assert.Equal(first.GetHashCode(), second.GetHashCode());
            Assert.NotEqual(first, new EquatableList<byte[]>([[1, 2, 3], [4, 5, 7]]));
        }

        /// <summary>
        /// Verifies that a null element is held and compared rather than failing.
        /// </summary>
        [Fact]
        public void Equals_HandlesNullElements()
        {
            // Arrange
            EquatableList<string?> first = new(["alpha", null, "charlie"]);
            EquatableList<string?> second = new(["alpha", null, "charlie"]);

            // Assert
            Assert.Equal(first, second);
            Assert.Equal(first.GetHashCode(), second.GetHashCode());
            Assert.NotEqual(first, new EquatableList<string?>(["alpha", "bravo", "charlie"]));
        }

        /// <summary>
        /// Verifies that the elements handed in are the elements read back, in order.
        /// </summary>
        [Fact]
        public void EquatableList_HoldsWhatItIsGiven()
        {
            // Arrange
            EquatableList<string> list = new(["alpha", "bravo", "charlie"]);

            // Assert
            Assert.Equal(3, list.Count);
            Assert.Equal("bravo", list[1]);
            Assert.Equal(["alpha", "bravo", "charlie"], list, StringComparer.Ordinal);
        }

        /// <summary>
        /// Verifies that the elements are copied rather than referenced, so a caller that goes on using
        /// its own list does not alter one that has already been built - which would change that list's
        /// comparison, and with it the comparison of whatever record is holding it.
        /// </summary>
        [Fact]
        public void EquatableList_CopiesWhatItIsGiven()
        {
            // Arrange
            List<string> source = ["alpha", "bravo"];
            EquatableList<string> list = new(source);

            // Act
            source.Add("charlie");

            // Assert
            Assert.Equal(2, list.Count);
        }

        /// <summary>
        /// Verifies that appending changes the comparison, since the hash is worked out once and kept and
        /// a stale one would leave the list findable under the wrong key.
        /// </summary>
        /// <remarks>
        /// Reached by reflection because that is the only way it is reached at all: the member is private
        /// so that nothing but the serializer can call it, and the serializer calls it reflectively. The
        /// cost of getting the cache wrong is a list that cannot be found in the dictionary it was put
        /// into, which is the kind of fault that surfaces a long way from its cause.
        /// </remarks>
        [Fact]
        public void Add_IsReflectedInTheComparison()
        {
            // Arrange
            EquatableList<string> list = new(["alpha"]);
            int before = list.GetHashCode();
            MethodInfo? add = typeof(EquatableList<string>).GetMethod("Add", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(add);

            // Act
            _ = add.Invoke(list, ["bravo"]);

            // Assert
            Assert.Equal(new EquatableList<string>(["alpha", "bravo"]), list);
            Assert.Equal(new EquatableList<string>(["alpha", "bravo"]).GetHashCode(), list.GetHashCode());
            Assert.NotEqual(before, list.GetHashCode());
        }

        /// <summary>
        /// Verifies that a list survives a data contract round trip, which is the only reason the
        /// parameterless constructor and <c language="csharp">Add</c> exist at all.
        /// </summary>
        /// <remarks>
        /// The serializer rebuilds a collection by constructing an empty one and adding to it, reaches
        /// both by reflection, and refuses a collection type offering no way to do it. Asserted because
        /// both members are private, so nothing the compiler can see would notice them going missing.
        /// </remarks>
        [Fact]
        public void Serialization_RoundTripsEveryElement()
        {
            // Arrange
            EquatableList<string> original = new(["alpha", "bravo"]);
            DataContractSerializer serializer = new(typeof(EquatableList<string>));

            // Act
            using MemoryStream stream = new();
            serializer.WriteObject(stream, original);
            stream.Position = 0;
            object? deserialized = serializer.ReadObject(stream);
            Assert.NotNull(deserialized);
            EquatableList<string> restored = (EquatableList<string>)deserialized;

            // Assert
            Assert.Equal(["alpha", "bravo"], restored, StringComparer.Ordinal);
            Assert.Equal(original, restored);
            Assert.Equal(original.GetHashCode(), restored.GetHashCode());
        }

        /// <summary>
        /// Verifies that no mutable surface is offered, since the type stands in for a value.
        /// </summary>
        /// <remarks>
        /// Implementing <see cref="ICollection{T}"/> would put an <c language="csharp">Add</c> on the type that callers could
        /// reach, and would also stop the serializer working: it binds to that interface's member in
        /// preference to the private one, and a read-only implementation of it throws.
        /// </remarks>
        [Fact]
        public void EquatableList_OffersNoMutableSurface()
        {
            // Assert
            Assert.DoesNotContain(typeof(ICollection<string>), typeof(EquatableList<string>).GetInterfaces());
            Assert.DoesNotContain(typeof(IList<string>), typeof(EquatableList<string>).GetInterfaces());
            Assert.Null(typeof(EquatableList<string>).GetMethod("Add", BindingFlags.Instance | BindingFlags.Public));
            Assert.Null(typeof(EquatableList<string>).GetMethod("Insert", BindingFlags.Instance | BindingFlags.Public));
            Assert.Null(typeof(EquatableList<string>).GetMethod("Remove", BindingFlags.Instance | BindingFlags.Public));
            Assert.Null(typeof(EquatableList<string>).GetMethod("Clear", BindingFlags.Instance | BindingFlags.Public));
        }

        /// <summary>
        /// Verifies that nothing at all is refused, since a list built from nothing is a caller's mistake
        /// rather than an empty list.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "This is deliberate as part of unit testing.")]
        [Fact]
        public void EquatableList_RefusesNothingAtAll()
        {
            _ = Assert.Throws<ArgumentNullException>(static () => new EquatableList<string>(null!));
        }
    }
}
