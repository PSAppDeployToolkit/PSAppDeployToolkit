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
    }
}
