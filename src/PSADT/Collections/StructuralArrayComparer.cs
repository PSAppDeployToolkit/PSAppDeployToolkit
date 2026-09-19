using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace PSADT.Collections
{
    /// <summary>
    /// Compares two arrays by their contents, each element the way its own type calls for.
    /// </summary>
    /// <remarks>This is what the framework's own structural comparison is not. That one reaches every element
    /// through <see cref="object.Equals(object)"/>, so an element putting its comparison on <see cref="IEquatable{T}"/>
    /// and leaving that override alone is compared by reference - the very fault <see cref="ElementEqualityComparer{T}"/>
    /// exists to prevent, reintroduced one level down inside every array. Reading the element type off the array and
    /// asking <see cref="ElementEqualityComparer{T}"/> for it again is what carries the comparison down instead, and
    /// down again through an array of arrays. <para> The element type is read off the value rather than off the
    /// declared type, which is how an <see cref="object"/> holding an array still reaches the comparison that array's
    /// elements call for. The comparer worked out for each array type is kept, so that is one lookup on first use and
    /// none after. </para><para> It also hashes every element, where the framework's structural hash reads only the
    /// last eight. Two long arrays differing early otherwise hash alike, which is legal but leaves a set or a
    /// dictionary keyed on them with every entry in one bucket. </para></remarks>
    internal static class StructuralArrayComparer
    {
        /// <summary>
        /// Determines whether two arrays hold the same contents.
        /// </summary>
        /// <remarks>Two arrays of different types are never equal, so that the answer cannot depend on which of the
        /// two was asked.</remarks>
        /// <param name="left">The first array.</param>
        /// <param name="right">The second array.</param>
        /// <returns><see langword="true"/> if they hold the same contents; otherwise, <see langword="false"/>.</returns>
        internal static bool AreEqual(Array left, Array right)
        {
            return ReferenceEquals(left, right) || (left.GetType() == right.GetType() && left.Length == right.Length && HaveSameShape(left, right) && ComparerFor(left.GetType()).ElementsAreEqual(left, right));
        }

        /// <summary>
        /// Determines whether two arrays of one type are laid out the same way.
        /// </summary>
        /// <remarks>One type and one total length still leave room to differ: a two by three and a three by two are
        /// both six of the same thing, and their elements come back in the same order. So every dimension has to
        /// agree before the elements are walked at all.</remarks>
        /// <param name="left">The first array.</param>
        /// <param name="right">The second array.</param>
        /// <returns><see langword="true"/> if every dimension is the same length; otherwise, <see langword="false"/>.</returns>
        private static bool HaveSameShape(Array left, Array right)
        {
            for (int dimension = 0; dimension < left.Rank; dimension++)
            {
                if (left.GetLength(dimension) != right.GetLength(dimension))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Returns the hash code of an array's contents, worked out the way <see cref="AreEqual"/> compares them.
        /// </summary>
        /// <param name="array">The array to hash.</param>
        /// <returns>The hash code of its contents.</returns>
        internal static int Hash(Array array)
        {
            return ComparerFor(array.GetType()).HashElements(array);
        }

        /// <summary>
        /// Returns the comparer for arrays of the supplied type, building it on first use and keeping it thereafter.
        /// </summary>
        /// <param name="arrayType">The array type.</param>
        /// <returns>The comparer for arrays of that type.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="arrayType"/> is not an array type.</exception>
        private static IArrayComparer ComparerFor(Type arrayType)
        {
            return Comparers.GetOrAdd(arrayType, static type =>
            {
                Type elementType = type.GetElementType() ?? throw new ArgumentException($"The type '{type}' is not an array type.", nameof(type));
                return (IArrayComparer)(Activator.CreateInstance(typeof(ArrayComparer<>).MakeGenericType(elementType)) ?? throw new InvalidOperationException($"A comparer for '{type}' could not be created."));
            });
        }

        /// <summary>
        /// The comparer worked out for each array type.
        /// </summary>
        private static readonly ConcurrentDictionary<Type, IArrayComparer> Comparers = new();

        /// <summary>
        /// Compares two arrays without naming their element type, so that an array reached as <see cref="Array"/> can
        /// still be compared through it.
        /// </summary>
        private interface IArrayComparer
        {
            /// <summary>
            /// Determines whether two arrays hold the same contents.
            /// </summary>
            /// <param name="left">The first array.</param>
            /// <param name="right">The second array.</param>
            /// <returns><see langword="true"/> if they hold the same contents; otherwise, <see langword="false"/>.</returns>
            bool ElementsAreEqual(Array left, Array right);

            /// <summary>
            /// Returns the hash code of an array's contents.
            /// </summary>
            /// <param name="array">The array to hash.</param>
            /// <returns>The hash code of its contents.</returns>
            int HashElements(Array array);
        }

        /// <summary>
        /// Compares two arrays of <typeparamref name="TElement"/>, each element through the comparer that type calls
        /// for, which is what carries the comparison down through an array of arrays.
        /// </summary>
        /// <remarks>Only an array of one dimension can be read back as <typeparamref name="TElement"/>[], and reading
        /// it that way is the point: walking an array through <see cref="IEnumerator"/> instead hands back every
        /// element as an <see cref="object"/>, which boxes each one of a value type. An array of any other rank is
        /// walked that way regardless, since there is no other way to reach its elements - but it is walked with
        /// this same comparer, so the comparison still carries down. Handing those to the framework instead would
        /// carry the element fault described on the enclosing type, and would throw besides: its structural
        /// comparison reads an array by a single index and refuses anything of a higher rank.</remarks>
        /// <typeparam name="TElement">The element type of the arrays compared.</typeparam>
        [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Built reflectively for the element type read off the array, which the compiler cannot see.")]
        private sealed class ArrayComparer<TElement> : IArrayComparer
        {
            /// <inheritdoc/>
            public bool ElementsAreEqual(Array left, Array right)
            {
                IEqualityComparer<TElement> comparer = ElementEqualityComparer<TElement>.Default;
                if (left is not TElement[] first || right is not TElement[] second)
                {
                    return HigherRankAreEqual(left, right, comparer);
                }
                for (int index = 0; index < first.Length; index++)
                {
                    if (!comparer.Equals(first[index], second[index]))
                    {
                        return false;
                    }
                }
                return true;
            }

            /// <inheritdoc/>
            public int HashElements(Array array)
            {
                IEqualityComparer<TElement> comparer = ElementEqualityComparer<TElement>.Default;
                HashCode hashCode = new();
                if (array is not TElement[] elements)
                {
                    foreach (object element in array)
                    {
                        hashCode.Add((TElement)element, comparer);
                    }
                    return hashCode.ToHashCode();
                }
                foreach (TElement element in elements)
                {
                    hashCode.Add(element, comparer);
                }
                return hashCode.ToHashCode();
            }

            /// <summary>
            /// Determines whether two arrays of a rank that cannot be read back as one dimension hold the same
            /// contents.
            /// </summary>
            /// <remarks>The shape is settled before this is reached, so the two walks run out together.</remarks>
            /// <param name="left">The first array.</param>
            /// <param name="right">The second array.</param>
            /// <param name="comparer">The comparer for the element type.</param>
            /// <returns><see langword="true"/> if they hold the same contents; otherwise, <see langword="false"/>.</returns>
            private static bool HigherRankAreEqual(Array left, Array right, IEqualityComparer<TElement> comparer)
            {
                IEnumerator leftElements = left.GetEnumerator();
                IEnumerator rightElements = right.GetEnumerator();
                while (leftElements.MoveNext() && rightElements.MoveNext())
                {
                    if (!comparer.Equals((TElement)leftElements.Current, (TElement)rightElements.Current))
                    {
                        return false;
                    }
                }
                return true;
            }
        }
    }
}
