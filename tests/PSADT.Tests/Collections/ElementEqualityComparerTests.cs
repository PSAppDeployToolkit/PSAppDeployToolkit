using System;
using System.Collections.Generic;
using PSADT.Collections;
using Xunit;

namespace PSADT.Tests.Collections
{
    /// <summary>
    /// Tests the comparer the collections in this namespace take their element comparison from.
    /// </summary>
    /// <remarks>
    /// The comparison has to be chosen from what the declared type could hold at run time rather than from
    /// what it is, because a byte array declared as <see cref="object"/> is still a byte array and comparing
    /// it by reference there is the same fault the collections exist to prevent. What can hold an array is a
    /// closed set - an array's only base types are <see cref="Array"/> and <see cref="object"/> - so the
    /// tests are in two halves: which comparer each declared type is given, and what that comparer then does.
    /// <para>
    /// The first half is worth asserting on its own because the predicate reads as over-specified and invites
    /// being tidied into something shorter. Two shorter forms are wrong in opposite directions: dispatching on
    /// <c language="csharp">IsArray</c> alone misses an array held under any other declared type, and dispatching structurally
    /// on anything unsealed drops a type that implements <see cref="IEquatable{T}"/> without overriding
    /// <see cref="object.Equals(object)"/> back to comparing references, since the structural comparison falls
    /// back to the virtual one.
    /// </para>
    /// </remarks>
    public sealed class ElementEqualityComparerTests
    {
        /// <summary>
        /// Verifies that a type that cannot hold an array is left with the framework's own comparison.
        /// </summary>
        /// <remarks>
        /// Which is what takes each type's <see cref="IEquatable{T}"/> where it offers one, and what spares a
        /// value type being boxed for every comparison.
        /// </remarks>
        [Fact]
        public void Default_LeavesATypeThatCannotHoldAnArrayWithTheFrameworkComparison()
        {
            AssertFrameworkComparison<string>();
            AssertFrameworkComparison<long>();
            AssertFrameworkComparison<uint>();
            AssertFrameworkComparison<Version>();
        }

        /// <summary>
        /// Verifies that every type that can hold an array is given the structural comparison instead.
        /// </summary>
        [Fact]
        public void Default_GivesATypeThatCanHoldAnArrayTheStructuralComparison()
        {
            AssertStructuralComparison<byte[]>();
            AssertStructuralComparison<object>();
            AssertStructuralComparison<Array>();
            AssertStructuralComparison<IReadOnlyList<byte>>();
        }

        /// <summary>
        /// Asserts that the supplied type is left with the framework's own comparison.
        /// </summary>
        /// <remarks>
        /// Taken as a type parameter rather than written out at each call because naming <see cref="string"/> as
        /// the type argument of <see cref="EqualityComparer{T}"/> in source reads as a string comparison made
        /// without settling on a <see cref="StringComparer"/>, which is not what this is: the assertion is about
        /// which comparer is handed back, and never compares a string at all.
        /// </remarks>
        /// <typeparam name="T">The type whose comparer to check.</typeparam>
        private static void AssertFrameworkComparison<T>()
        {
            Assert.Same(EqualityComparer<T>.Default, ElementEqualityComparer<T>.Default);
        }

        /// <summary>
        /// Asserts that the supplied type is given the structural comparison instead.
        /// </summary>
        /// <typeparam name="T">The type whose comparer to check.</typeparam>
        private static void AssertStructuralComparison<T>()
        {
            Assert.NotSame(EqualityComparer<T>.Default, ElementEqualityComparer<T>.Default);
        }

        /// <summary>
        /// Verifies that an array is compared and hashed by its contents however it happens to be declared.
        /// </summary>
        [Fact]
        public void Default_ComparesAnArrayByItsContentsHoweverItIsDeclared()
        {
            // Arrange: equal contents, different arrays
            byte[] first = [1, 2, 3];
            byte[] second = [1, 2, 3];
            byte[] other = [1, 2, 4];

            // Assert
            Assert.True(ElementEqualityComparer<byte[]>.Default.Equals(first, second));
            Assert.True(ElementEqualityComparer<object>.Default.Equals(first, second));
            Assert.True(ElementEqualityComparer<IReadOnlyList<byte>>.Default.Equals(first, second));
            Assert.Equal(ElementEqualityComparer<object>.Default.GetHashCode(first), ElementEqualityComparer<object>.Default.GetHashCode(second));
            Assert.False(ElementEqualityComparer<object>.Default.Equals(first, other));
        }

        /// <summary>
        /// Verifies that a value that is not an array is still compared the way it compares itself, whether it
        /// is declared as itself or as something that could have held an array.
        /// </summary>
        /// <remarks>
        /// The structural comparison asks for <c language="csharp">IStructuralEquatable</c> and falls back to the value's own
        /// comparison otherwise, so putting a type on that path does not take its comparison away from it.
        /// </remarks>
        [Fact]
        public void Default_KeepsAValuesOwnComparisonWhereItIsNotAnArray()
        {
            Assert.True(ElementEqualityComparer<string>.Default.Equals("alpha", "alpha"));
            Assert.False(ElementEqualityComparer<string>.Default.Equals("alpha", "bravo"));
            Assert.True(ElementEqualityComparer<object>.Default.Equals("alpha", "alpha"));
            Assert.False(ElementEqualityComparer<object>.Default.Equals("alpha", "bravo"));
            Assert.True(ElementEqualityComparer<object>.Default.Equals(new Version(1, 2), new Version(1, 2)));
            Assert.Equal(
                ElementEqualityComparer<object>.Default.GetHashCode(new Version(1, 2)),
                ElementEqualityComparer<object>.Default.GetHashCode(new Version(1, 2)));
        }

        /// <summary>
        /// Verifies that an array of more than one dimension is compared by its contents and by its shape.
        /// </summary>
        /// <remarks>
        /// Worth pinning because the obvious way to handle a rank this comparer cannot index - handing it to the
        /// framework's structural comparison - does not merely answer wrongly: that one reads an array by a single
        /// index and throws on anything of a higher rank, so a collection holding one would fail rather than
        /// compare. The shape is asserted alongside because one type and one total length do not settle it: the two
        /// arrays below are both four <see cref="int"/> laid out in the same order, and are not the same array.
        /// </remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1814:Prefer jagged arrays over multidimensional", Justification = "A multidimensional array is the shape under test.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3967:Multidimensional arrays should not be used", Justification = "A multidimensional array is the shape under test.")]
        [Fact]
        public void Default_ComparesAnArrayOfMoreThanOneDimension()
        {
            // Arrange
            int[,] first = { { 1, 2 }, { 3, 4 } };
            int[,] second = { { 1, 2 }, { 3, 4 } };
            int[,] other = { { 1, 2 }, { 3, 5 } };
            int[,] reshaped = { { 1 }, { 2 }, { 3 }, { 4 } };

            // Assert
            Assert.True(ElementEqualityComparer<object>.Default.Equals(first, second));
            Assert.Equal(
                ElementEqualityComparer<object>.Default.GetHashCode(first),
                ElementEqualityComparer<object>.Default.GetHashCode(second));
            Assert.False(ElementEqualityComparer<object>.Default.Equals(first, other));
            Assert.False(ElementEqualityComparer<object>.Default.Equals(first, reshaped));
        }

        /// <summary>
        /// Verifies that an array nested inside a value that is itself structural is still compared by its
        /// contents.
        /// </summary>
        /// <remarks>
        /// A tuple has contents without being an array, so asking a value whether it is an <see cref="Array"/>
        /// would send this one to the framework's comparison - which compares a tuple's elements with their own
        /// default comparers, putting the array back to comparing by reference one level further down than where
        /// this started. Asking whether it is <see cref="System.Collections.IStructuralEquatable"/> instead costs the same and takes
        /// the tuple down the structural path, where the comparison recurses and reaches the array.
        /// </remarks>
        [Fact]
        public void Default_ComparesAnArrayNestedInsideAStructuralValue()
        {
            // Arrange: equal contents, different arrays, wrapped in tuples and held as object
            object first = Tuple.Create(1, new byte[] { 1, 2, 3 });
            object second = Tuple.Create(1, new byte[] { 1, 2, 3 });
            object other = Tuple.Create(1, new byte[] { 1, 2, 4 });

            // Assert
            Assert.True(ElementEqualityComparer<object>.Default.Equals(first, second));
            Assert.Equal(
                ElementEqualityComparer<object>.Default.GetHashCode(first),
                ElementEqualityComparer<object>.Default.GetHashCode(second));
            Assert.False(ElementEqualityComparer<object>.Default.Equals(first, other));
        }

        /// <summary>
        /// Verifies that an array of a type whose comparison lives on <see cref="IEquatable{T}"/> is compared by
        /// its elements rather than by their references.
        /// </summary>
        /// <remarks>
        /// The same fault the direct case pins, one level down. The framework's structural comparison reaches an
        /// array's elements through the virtual <see cref="object.Equals(object)"/>, so handing an array to it puts
        /// back exactly what asking the value rather than the declared type was meant to fix. What makes it worth
        /// pinning separately is the shape of the failure: the elements hash by value while comparing by reference,
        /// so two equal arrays land in one bucket and are turned away on the comparison.
        /// </remarks>
        [Fact]
        public void Default_ComparesAnArrayOfAnIEquatableTypeByItsElements()
        {
            // Arrange: equal by the interface's own comparison, different instances, different arrays
            IValued[] first = [new Valued(1), new Valued(2)];
            IValued[] second = [new Valued(1), new Valued(2)];
            IValued[] other = [new Valued(1), new Valued(3)];

            // Assert
            Assert.True(ElementEqualityComparer<IValued[]>.Default.Equals(first, second));
            Assert.True(ElementEqualityComparer<object>.Default.Equals(first, second));
            Assert.Equal(
                ElementEqualityComparer<object>.Default.GetHashCode(first),
                ElementEqualityComparer<object>.Default.GetHashCode(second));
            Assert.False(ElementEqualityComparer<object>.Default.Equals(first, other));
        }

        /// <summary>
        /// Verifies that every element of an array takes part in its hash code, not merely the last of them.
        /// </summary>
        /// <remarks>
        /// The framework's structural hash reads only the last eight elements, which is legal - two values that
        /// are not equal may hash alike - but leaves a set or a dictionary keyed on long arrays with every entry
        /// in one bucket, and those are the arrays the cost of hashing them was supposed to buy something for.
        /// </remarks>
        [Fact]
        public void Default_HashesEveryElementOfAnArray()
        {
            // Arrange: long enough that only the tail would otherwise be read, differing at the very front
            byte[] first = new byte[64];
            byte[] second = new byte[64];
            first[0] = 1;

            // Assert
            Assert.False(ElementEqualityComparer<byte[]>.Default.Equals(first, second));
            Assert.NotEqual(
                ElementEqualityComparer<byte[]>.Default.GetHashCode(first),
                ElementEqualityComparer<byte[]>.Default.GetHashCode(second));
        }

        /// <summary>
        /// Verifies that a type keeps a comparison it has put on <see cref="IEquatable{T}"/> alone, even when it
        /// is held under an interface and therefore on the structural path.
        /// </summary>
        /// <remarks>
        /// This is the case that separates asking the value whether it is an array from asking the declared type.
        /// An interface has to be on the structural path because it could be holding an array, but treating
        /// everything on that path as structural is wrong for the rest of it: the framework's structural comparer
        /// falls back to the virtual <see cref="object.Equals(object)"/>, which a type implementing
        /// <see cref="IEquatable{T}"/> and nothing else has not overridden. Left that way these two values compare
        /// unequal while hashing alike, so they reach the right bucket in a dictionary and are turned away on the
        /// comparison - the same fault, in the same shape, that the collections in this namespace exist to prevent.
        /// </remarks>
        [Fact]
        public void Default_KeepsAnIEquatableComparisonHeldUnderAnInterface()
        {
            // Arrange: equal by the interface's own comparison, different instances
            IValued first = new Valued(1);
            IValued second = new Valued(1);

            // Assert
            Assert.True(ElementEqualityComparer<IValued>.Default.Equals(first, second));
            Assert.Equal(
                ElementEqualityComparer<IValued>.Default.GetHashCode(first),
                ElementEqualityComparer<IValued>.Default.GetHashCode(second));
            Assert.False(ElementEqualityComparer<IValued>.Default.Equals(first, new Valued(2)));
        }

        /// <summary>
        /// Verifies that nothing at all is compared and hashed rather than thrown on, since a collection is
        /// free to hold a null element.
        /// </summary>
        /// <remarks>
        /// The nulls are forced through rather than written plainly because the two targets disagree about them.
        /// The modern <see cref="IEqualityComparer{T}"/> declares <c language="csharp">Equals(T? x, T? y)</c>, where net472's
        /// predates that annotation and declares <c language="csharp">Equals(T x, T y)</c> - and a bare unconstrained type
        /// parameter will not take a null literal. So the operator is necessary on one target and redundant on the
        /// other, which is the same split that has <c language="csharp">EquatableDictionary.TryGetValue</c> forcing its out
        /// parameter. Handing a null over is the case under test on both.
        /// </remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Redundancy", "RCS1249:Unnecessary null-forgiving operator", Justification = "It is necessary on net472 and only unnecessary on the modern target, which is the interop this solution multi-targets for.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "This is deliberate as part of unit testing.")]
        [Fact]
        public void Default_TreatsNothingAtAllAsHashingToZero()
        {
            Assert.True(ElementEqualityComparer<object>.Default.Equals(null!, null!));
            Assert.False(ElementEqualityComparer<object>.Default.Equals(null!, "alpha"));
            Assert.Equal(0, ElementEqualityComparer<object>.Default.GetHashCode(null!));
        }

        /// <summary>
        /// An interface that carries its own comparison, which <see cref="IEquatable{T}"/> allows an interface to do.
        /// </summary>
        private interface IValued : IEquatable<IValued>
        {
            /// <summary>
            /// Gets the value that decides equality.
            /// </summary>
            int Value { get; }
        }

        /// <summary>
        /// An implementation that leaves its comparison where the interface put it.
        /// </summary>
        /// <remarks>
        /// <see cref="object.Equals(object)"/> is deliberately left alone, which is what makes this worth having: a
        /// type that overrode it would compare the same whichever way the comparer dispatched, and so would not tell
        /// the two apart.
        /// </remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1067:Override Object.Equals(object) when implementing IEquatable<T>", Justification = "Not overriding it is the condition under test; see the remarks on the type.")]
        private sealed class Valued : IValued
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="Valued"/> class.
            /// </summary>
            /// <param name="value">The value that decides equality.</param>
            internal Valued(int value)
            {
                Value = value;
            }

            /// <inheritdoc/>
            public int Value { get; }

            /// <inheritdoc/>
            public bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] IValued? other)
            {
                return other is not null && other.Value == Value;
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                return Value;
            }
        }
    }
}
