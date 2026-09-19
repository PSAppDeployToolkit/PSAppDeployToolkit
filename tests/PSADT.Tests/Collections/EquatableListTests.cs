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
    /// by their references - which is the case a naive implementation gets wrong. The round trip is in
    /// here too, since the attribute that makes it work is the one part of the type that looks removable.
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
        /// Verifies that an array element is compared by its contents even where the element type does not say
        /// it is an array.
        /// </summary>
        /// <remarks>
        /// The comparison has to be chosen from what the declared type could hold rather than from what it is: a
        /// byte array is still a byte array when it is declared as <see cref="object"/>, and comparing it by
        /// reference there would be the same fault as comparing it by reference anywhere else. What made this
        /// worth pinning is that it used to fail inconsistently - an element typed as <c language="csharp">object[]</c> was
        /// compared by its contents, because the outer array is structural and recurses, while the same array
        /// typed as <c language="csharp">object</c> was not.
        /// </remarks>
        [Fact]
        public void Equals_ComparesArrayElementsHeldUnderAnotherType()
        {
            // Arrange: equal contents, different arrays, declared as something other than an array
            EquatableList<object> first = new([new byte[] { 1, 2, 3 }]);
            EquatableList<object> second = new([new byte[] { 1, 2, 3 }]);

            // Assert
            Assert.Equal(first, second);
            Assert.Equal(first.GetHashCode(), second.GetHashCode());
            Assert.NotEqual(first, new EquatableList<object>([new byte[] { 1, 2, 4 }]));
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
        /// Verifies that the operators compare by the elements, and that nothing at all on either side is
        /// answered rather than thrown on.
        /// </summary>
        /// <remarks>
        /// Nothing in the library reaches for them - a record compares its fields through <see
        /// cref="EqualityComparer{T}"/>, which calls <c language="csharp">Equals</c> - so this is the only thing holding the
        /// operator and the method in step. Left undefined, the operator would compare references, which is
        /// the fault this whole type exists to prevent.
        /// </remarks>
        [Fact]
        public void Operators_CompareByTheElements()
        {
            // Arrange
            EquatableList<string> list = new(["alpha", "bravo"]);
            EquatableList<string> same = new(["alpha", "bravo"]);
            EquatableList<string> different = new(["alpha", "charlie"]);

            // Assert
            AssertOperators(list, same, equal: true);
            AssertOperators(list, different, equal: false);
            AssertOperators(list, right: null, equal: false);
            AssertOperators(left: null, list, equal: false);
            AssertOperators(left: null, right: null, equal: true);
        }

        /// <summary>
        /// Asserts what both operators make of a pair of lists.
        /// </summary>
        /// <remarks>
        /// The pair is taken as parameters rather than compared where it is built, so that the operands are not
        /// values the analysis can work out for itself. A comparison it can answer without running reads to it as
        /// dead code, and the pairs worth asserting most here - a list against nothing at all, and nothing against
        /// nothing - are exactly the ones it can answer.
        /// </remarks>
        /// <param name="left">The first list, which may be nothing at all.</param>
        /// <param name="right">The second list, which may be nothing at all.</param>
        /// <param name="equal">Whether the two are expected to compare equal.</param>
        private static void AssertOperators(EquatableList<string>? left, EquatableList<string>? right, bool equal)
        {
            Assert.Equal(equal, left == right);
            Assert.Equal(!equal, left != right);
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
        /// Verifies that a list survives a data contract round trip, which is the only reason the type
        /// carries <see cref="DataContractAttribute"/> at all.
        /// </summary>
        /// <remarks>
        /// The serializer takes anything implementing <see cref="IEnumerable{T}"/> for a collection and
        /// refuses one offering no <c language="csharp">Add</c> for it to fill. The attribute sends it down the ordinary
        /// class path instead, where the single field is written and read straight back. Asserted because
        /// the attribute reads as redundant on a type that declares no other contract member, and taking
        /// it off turns every payload carrying a list into an <see cref="InvalidDataContractException"/>.
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
        /// Asserted on every surface rather than the public one, since the serializer no longer needs an
        /// <c language="csharp">Add</c> to reach: a private one reappearing would be the type going back to being filled
        /// after it was built. Implementing <see cref="ICollection{T}"/> would put one within a caller's
        /// reach as well.
        /// </remarks>
        [Fact]
        public void EquatableList_OffersNoMutableSurface()
        {
            // Arrange
            const BindingFlags surface = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            // Assert
            Assert.DoesNotContain(typeof(ICollection<string>), typeof(EquatableList<string>).GetInterfaces());
            Assert.DoesNotContain(typeof(IList<string>), typeof(EquatableList<string>).GetInterfaces());
            Assert.Null(typeof(EquatableList<string>).GetMethod("Add", surface));
            Assert.Null(typeof(EquatableList<string>).GetMethod("Insert", surface));
            Assert.Null(typeof(EquatableList<string>).GetMethod("Remove", surface));
            Assert.Null(typeof(EquatableList<string>).GetMethod("Clear", surface));
            Assert.Null(typeof(EquatableList<string>).GetConstructor(surface, binder: null, Type.EmptyTypes, modifiers: null));
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
