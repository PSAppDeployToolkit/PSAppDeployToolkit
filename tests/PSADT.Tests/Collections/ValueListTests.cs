using System;
using System.Collections.Generic;
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
    public sealed class ValueListTests
    {
        /// <summary>
        /// Verifies that two lists holding the same elements are equal and hash alike.
        /// </summary>
        [Fact]
        public void Equals_IsByTheElements()
        {
            // Arrange
            ValueList<string> first = new(["alpha", "bravo", "charlie"]);
            ValueList<string> second = new(["alpha", "bravo", "charlie"]);

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
            Assert.NotEqual(new ValueList<string>(["alpha", "bravo"]), new ValueList<string>(["bravo", "alpha"]));
        }

        /// <summary>
        /// Verifies that lists of different lengths are not equal, including where one is a prefix of the
        /// other.
        /// </summary>
        [Fact]
        public void Equals_TakesLengthIntoAccount()
        {
            Assert.NotEqual(new ValueList<string>(["alpha"]), new ValueList<string>(["alpha", "bravo"]));
            Assert.NotEqual(new ValueList<string>([]), new ValueList<string>(["alpha"]));
        }

        /// <summary>
        /// Verifies that two empty lists are equal, since a record built with no arguments and another
        /// built the same way describe the same thing.
        /// </summary>
        [Fact]
        public void Equals_TreatsTwoEmptyListsAsEqual()
        {
            Assert.Equal(new ValueList<string>([]), new ValueList<string>([]));
            Assert.Equal(new ValueList<string>([]).GetHashCode(), new ValueList<string>([]).GetHashCode());
        }

        /// <summary>
        /// Verifies that nothing at all is not equal to a list, and that comparing against it does not
        /// fail.
        /// </summary>
        [Fact]
        public void Equals_IsNotEqualToNothing()
        {
            Assert.False(new ValueList<string>([]).Equals(other: null));
            Assert.False(new ValueList<string>(["alpha"]).Equals(obj: null));
            Assert.False(new ValueList<string>(["alpha"]).Equals(obj: "alpha"));
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
            ValueList<byte[]> first = new([[1, 2, 3], [4, 5, 6]]);
            ValueList<byte[]> second = new([[1, 2, 3], [4, 5, 6]]);

            // Assert
            Assert.Equal(first, second);
            Assert.Equal(first.GetHashCode(), second.GetHashCode());
            Assert.NotEqual(first, new ValueList<byte[]>([[1, 2, 3], [4, 5, 7]]));
        }

        /// <summary>
        /// Verifies that a null element is held and compared rather than failing.
        /// </summary>
        [Fact]
        public void Equals_HandlesNullElements()
        {
            // Arrange
            ValueList<string?> first = new(["alpha", null, "charlie"]);
            ValueList<string?> second = new(["alpha", null, "charlie"]);

            // Assert
            Assert.Equal(first, second);
            Assert.Equal(first.GetHashCode(), second.GetHashCode());
            Assert.NotEqual(first, new ValueList<string?>(["alpha", "bravo", "charlie"]));
        }

        /// <summary>
        /// Verifies that the elements handed in are the elements read back, in order.
        /// </summary>
        [Fact]
        public void ValueList_HoldsWhatItIsGiven()
        {
            // Arrange
            ValueList<string> list = new(["alpha", "bravo", "charlie"]);

            // Assert
            Assert.Equal(3, list.Count);
            Assert.Equal("bravo", list[1]);
            Assert.Equal(["alpha", "bravo", "charlie"], list);
        }

        /// <summary>
        /// Verifies that the elements are copied rather than referenced, so a caller that goes on using
        /// its own list does not alter one that has already been built - which would change that list's
        /// comparison, and with it the comparison of whatever record is holding it.
        /// </summary>
        [Fact]
        public void ValueList_CopiesWhatItIsGiven()
        {
            // Arrange
            List<string> source = ["alpha", "bravo"];
            ValueList<string> list = [.. source];

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
        /// Appending is only meant to happen while the serializer is rebuilding a list, before anything
        /// has asked it for anything. Asserted anyway: the cost of getting the cache wrong is a list that
        /// cannot be found in the dictionary it was put into, which is the kind of fault that surfaces a
        /// long way from its cause.
        /// </remarks>
        [Fact]
        public void Add_IsReflectedInTheComparison()
        {
            // Arrange
            ValueList<string> list = new(["alpha"]);
            int before = list.GetHashCode();

            // Act
            list.Add("bravo");

            // Assert
            Assert.Equal(new ValueList<string>(["alpha", "bravo"]), list);
            Assert.Equal(new ValueList<string>(["alpha", "bravo"]).GetHashCode(), list.GetHashCode());
            Assert.NotEqual(before, list.GetHashCode());
        }

        /// <summary>
        /// Verifies that nothing at all is refused, since a list built from nothing is a caller's mistake
        /// rather than an empty list.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "This is deliberate as part of unit testing.")]
        [Fact]
        public void ValueList_RefusesNothingAtAll()
        {
            _ = Assert.Throws<ArgumentNullException>(static () => new ValueList<string>(null!));
        }
    }
}
