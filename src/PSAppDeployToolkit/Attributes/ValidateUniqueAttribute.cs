using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using PSAppDeployToolkit.Utilities;

namespace PSAppDeployToolkit.Attributes
{
    /// <summary>
    /// Specifies that a collection parameter or property must contain only unique elements.
    /// </summary>
    /// <remarks>
    /// For string elements, uniqueness is evaluated using the configured <see cref="StringComparison"/> value.
    /// For non-string elements, uniqueness is evaluated using the type's equality implementation.
    /// Null elements are not valid. Non-collection values are treated as valid.
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S3253:Constructor and destructor declarations should not be redundant", Justification = "This primary constructor is required for PowerShell.")]
    public sealed class ValidateUniqueAttribute() : ValidateArgumentsAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ValidateUniqueAttribute"/> class.
        /// </summary>
        /// <param name="stringComparison">The string comparison mode used when evaluating string elements.</param>
        public ValidateUniqueAttribute(StringComparison stringComparison) : this()
        {
            StringComparison = stringComparison;
        }

        /// <summary>
        /// Gets the string comparison mode used when evaluating string elements.
        /// </summary>
        public StringComparison StringComparison { get; } = StringComparison.OrdinalIgnoreCase;

        /// <summary>
        /// Validates that the specified argument does not contain duplicate elements.
        /// </summary>
        /// <param name="arguments">The argument value to validate.</param>
        /// <param name="engineIntrinsics">Provides access to the PowerShell engine APIs.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the method is called from outside the PSAppDeployToolkit module context.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="arguments"/> is a collection that contains null or duplicate elements.
        /// </exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "MA0015:Specify the parameter name in ArgumentException", Justification = "We don't want a paramter name on these exceptions.")]
        protected override void Validate(object arguments, EngineIntrinsics engineIntrinsics)
        {
            // Verify the provided input before proceeding.
            arguments = PowerShellUtilities.GetBaseObject<object>(arguments);
            if (arguments is string || LanguagePrimitives.GetEnumerator(arguments) is not IEnumerator enumerator)
            {
                return;
            }
            if (!enumerator.MoveNext())
            {
                return;
            }

            // Check the first element before anything else, since a collection carrying nothing cannot be a
            // collection of unique things.
            if (!PowerShellUtilities.TryGetBaseObject(enumerator.Current, out object? firstValue))
            {
                throw new ArgumentException("The argument collection contains null elements. Provide a collection in which each element has a value, and then try running the command again.");
            }

            // Use a HashSet to track seen elements and detect duplicates efficiently.
            HashSet<object?> seen = new(new ElementEqualityComparer(StringComparison)) { firstValue };
            while (enumerator.MoveNext())
            {
                if (!PowerShellUtilities.TryGetBaseObject(enumerator.Current, out object? value))
                {
                    throw new ArgumentException("The argument collection contains null elements. Provide a collection in which each element has a value, and then try running the command again.");
                }
                if (!seen.Add(value))
                {
                    throw new ArgumentException("The argument collection contains duplicate elements. Provide a collection in which each element is unique, and then try running the command again.");
                }
            }
        }

        /// <summary>
        /// Compares two elements, applying the configured <see cref="StringComparison"/> where both are strings and
        /// each type's own equality otherwise.
        /// </summary>
        /// <remarks>The comparison is decided per pair rather than from the type of the collection's first element.
        /// Taking it from the first element made the answer depend on ordering: two strings differing only in case were
        /// duplicates on their own but distinct when an integer preceded them, because the comparer had been typed for
        /// an integer and the strings then fell back to case-sensitive object equality.</remarks>
        /// <param name="stringComparison">The comparison to apply to strings.</param>
        private sealed class ElementEqualityComparer(StringComparison stringComparison) : IEqualityComparer<object?>
        {
            /// <summary>
            /// Determines whether two elements are the same element.
            /// </summary>
            /// <param name="x">The first element to compare.</param>
            /// <param name="y">The second element to compare.</param>
            /// <returns><see langword="true"/> if they are the same; otherwise, <see langword="false"/>.</returns>
            public new bool Equals(object? x, object? y)
            {
                return x is string first && y is string second
                    ? string.Equals(first, second, stringComparison)
                    : EqualityComparer<object?>.Default.Equals(x, y);
            }

            /// <summary>
            /// Returns a hash code for an element, consistent with how it is compared.
            /// </summary>
            /// <param name="obj">The element to hash.</param>
            /// <returns>Its hash code, or zero where it has none.</returns>
            public int GetHashCode(object? obj)
            {
                return obj is string value ? _stringComparer.GetHashCode(value) : obj?.GetHashCode() ?? 0;
            }

            /// <summary>
            /// The comparer matching the configured comparison, resolved once so an unrecognised comparison is refused
            /// when validation starts rather than at the first hash.
            /// </summary>
            private readonly StringComparer _stringComparer = StringComparer.FromComparison(stringComparison);
        }
    }
}
