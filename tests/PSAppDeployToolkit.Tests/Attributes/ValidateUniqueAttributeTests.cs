using System;
using System.Collections.Generic;
using System.Management.Automation;
using PSAppDeployToolkit.Attributes;
using PSAppDeployToolkit.Tests.TestHelpers;
using Xunit;

namespace PSAppDeployToolkit.Tests.Attributes
{
    /// <summary>
    /// Tests the validator that insists a collection holds no duplicates.
    /// </summary>
    /// <remarks>
    /// How elements are compared is decided from the type of the <em>first</em> one, which is the part worth pinning:
    /// strings go through the configured <see cref="StringComparison"/> and everything else through its own equality,
    /// with a fallback for a collection whose elements are not all alike.
    /// </remarks>
    public sealed class ValidateUniqueAttributeTests
    {
        /// <summary>
        /// Verifies that a collection of distinct elements is accepted.
        /// </summary>
        [Fact]
        public void Validate_AcceptsDistinctElements()
        {
            ArgumentAttributes.Validate(new ValidateUniqueAttribute(), uniqueStringArguments);
            ArgumentAttributes.Validate(new ValidateUniqueAttribute(), uniqueIntegerArguments);
        }

        /// <summary>
        /// Verifies that a repeated element is refused.
        /// </summary>
        [Fact]
        public void Validate_RefusesADuplicate()
        {
            Assert.Contains(
                "contains duplicate elements",
                Assert.Throws<ArgumentException>(static () => ArgumentAttributes.Validate(new ValidateUniqueAttribute(), duplicatedStringArguments)).Message,
                StringComparison.Ordinal);
            _ = Assert.Throws<ArgumentException>(static () => ArgumentAttributes.Validate(new ValidateUniqueAttribute(), duplicatedIntegerArguments));
        }

        /// <summary>
        /// Verifies that strings are compared without regard to case by default.
        /// </summary>
        /// <remarks>
        /// The default suits what these parameters name - processes, file extensions, account names - where two
        /// spellings differing only in case mean one thing.
        /// </remarks>
        [Fact]
        public void Validate_IgnoresCaseByDefault()
        {
            Assert.Equal(StringComparison.OrdinalIgnoreCase, new ValidateUniqueAttribute().StringComparison);
            _ = Assert.Throws<ArgumentException>(static () => ArgumentAttributes.Validate(new ValidateUniqueAttribute(), differingCaseArguments));
        }

        /// <summary>
        /// Verifies that an explicit comparison is honoured.
        /// </summary>
        [Fact]
        public void Validate_HonoursAnExplicitStringComparison()
        {
            // Arrange
            ValidateUniqueAttribute ordinal = new(StringComparison.Ordinal);

            // Assert
            Assert.Equal(StringComparison.Ordinal, ordinal.StringComparison);
            ArgumentAttributes.Validate(ordinal, differingCaseArguments);
            _ = Assert.Throws<ArgumentException>(() => ArgumentAttributes.Validate(ordinal, sameCaseArguments));
        }

        /// <summary>
        /// Verifies that anything which is not a collection is accepted.
        /// </summary>
        /// <remarks>
        /// Including nothing at all. Uniqueness is meaningless for a single value, so the validator declines to have an
        /// opinion rather than refusing - which means it can sit on a parameter that accepts either one value or many.
        /// </remarks>
        [Fact]
        public void Validate_AcceptsAnythingThatIsNotACollection()
        {
            ArgumentAttributes.Validate(new ValidateUniqueAttribute(), 42);
            ArgumentAttributes.Validate(new ValidateUniqueAttribute(), "a string is not a collection here");
            ArgumentAttributes.Validate(new ValidateUniqueAttribute(), arguments: null);
        }

        /// <summary>
        /// Verifies that an empty collection is accepted.
        /// </summary>
        /// <remarks>
        /// Unlike the not-empty validators, this one has nothing to say about emptiness - an empty collection trivially
        /// holds no duplicates. Worth stating because the two families sit side by side on the same parameters.
        /// </remarks>
        [Fact]
        public void Validate_AcceptsAnEmptyCollection()
        {
            ArgumentAttributes.Validate(new ValidateUniqueAttribute(), Array.Empty<string>());
            ArgumentAttributes.Validate(new ValidateUniqueAttribute(), new List<int>());
        }

        /// <summary>
        /// Verifies that a collection carrying nothing at all is refused.
        /// </summary>
        /// <remarks>
        /// Both positions matter: the first element decides the comparer, so it is checked before one is built, and
        /// every later element is checked as it is read.
        /// </remarks>
        [Fact]
        public void Validate_RefusesACollectionCarryingNothing()
        {
            Assert.Contains(
                "contains null elements",
                Assert.Throws<ArgumentException>(static () => ArgumentAttributes.Validate(new ValidateUniqueAttribute(), new object?[] { null, "bravo" })).Message,
                StringComparison.Ordinal);
            Assert.Contains(
                "contains null elements",
                Assert.Throws<ArgumentException>(static () => ArgumentAttributes.Validate(new ValidateUniqueAttribute(), new object?[] { "alpha", null })).Message,
                StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that elements of a type other than string are compared by their own equality.
        /// </summary>
        /// <remarks>
        /// Exercised with a record, since that is what these collections actually hold - a list of process definitions,
        /// for instance - and a record compares by its contents, so two built alike are duplicates.
        /// </remarks>
        [Fact]
        public void Validate_ComparesNonStringElementsByTheirOwnEquality()
        {
            ArgumentAttributes.Validate(new ValidateUniqueAttribute(), new[] { new Named("alpha"), new Named("bravo") });
            _ = Assert.Throws<ArgumentException>(static () => ArgumentAttributes.Validate(new ValidateUniqueAttribute(), new[] { new Named("alpha"), new Named("alpha") }));
        }

        /// <summary>
        /// Verifies that elements of unlike types are never duplicates of one another.
        /// </summary>
        /// <remarks>
        /// A string and a number that render alike are still different values, so <c language="text">1</c> and <c language="text">"1"</c> coexist. What
        /// matters is that this holds in either order.
        /// </remarks>
        [Fact]
        public void Validate_FallsBackToObjectEqualityForMixedTypes()
        {
            ArgumentAttributes.Validate(new ValidateUniqueAttribute(), new object[] { 1, "1" });
            ArgumentAttributes.Validate(new ValidateUniqueAttribute(), new object[] { "1", 1 });
            ArgumentAttributes.Validate(new ValidateUniqueAttribute(), new object[] { 1, "alpha", 2 });
            _ = Assert.Throws<ArgumentException>(static () => ArgumentAttributes.Validate(new ValidateUniqueAttribute(), new object[] { 1, "alpha", 1 }));
        }

        /// <summary>
        /// Verifies that the string comparison applies wherever two strings meet, whatever preceded them.
        /// </summary>
        /// <remarks>
        /// Previously the comparer was typed from the collection's first element, so these two answers disagreed: the
        /// pair on its own was a duplicate, but the same pair behind an integer was not, because the comparer had been
        /// typed for an integer and the strings fell back to case-sensitive equality. The comparison is now decided per
        /// pair, so ordering cannot change the answer.
        /// </remarks>
        [Fact]
        public void Validate_AppliesTheStringComparisonWhateverTheElementOrder()
        {
            _ = Assert.Throws<ArgumentException>(static () => ArgumentAttributes.Validate(new ValidateUniqueAttribute(), new object[] { "alpha", "ALPHA" }));
            _ = Assert.Throws<ArgumentException>(static () => ArgumentAttributes.Validate(new ValidateUniqueAttribute(), new object[] { 1, "alpha", "ALPHA" }));
            _ = Assert.Throws<ArgumentException>(static () => ArgumentAttributes.Validate(new ValidateUniqueAttribute(), new object[] { new Named("x"), "alpha", "ALPHA" }));
            ArgumentAttributes.Validate(new ValidateUniqueAttribute(StringComparison.Ordinal), new object[] { 1, "alpha", "ALPHA" });
        }

        /// <summary>
        /// Verifies that wrapped elements are unwrapped before being compared.
        /// </summary>
        [Fact]
        public void Validate_UnwrapsElements()
        {
            _ = Assert.Throws<ArgumentException>(static () => ArgumentAttributes.Validate(
                new ValidateUniqueAttribute(),
                new object[] { PSObject.AsPSObject("alpha"), PSObject.AsPSObject("alpha") }));
        }

        /// <summary>
        /// An array of unique strings.
        /// </summary>
        private static readonly string[] uniqueStringArguments = ["alpha", "bravo", "charlie"];

        /// <summary>
        /// An array of unique integers.
        /// </summary>
        private static readonly int[] uniqueIntegerArguments = [1, 2, 3];

        /// <summary>
        /// An array of strings that contains a duplicate.
        /// </summary>
        private static readonly string[] duplicatedStringArguments = ["alpha", "bravo", "alpha"];

        /// <summary>
        /// An array of integers that contains a duplicate.
        /// </summary>
        private static readonly int[] duplicatedIntegerArguments = [1, 2, 1];

        /// <summary>
        /// An array of strings that differ only in case.
        /// </summary>
        private static readonly string[] differingCaseArguments = ["alpha", "ALPHA"];

        /// <summary>
        /// An array of strings that contains a duplicate.
        /// </summary>
        private static readonly string[] sameCaseArguments = ["alpha", "alpha"];

        /// <summary>
        /// A record standing in for the kind of element these collections hold.
        /// </summary>
        /// <param name="Name">The name it carries.</param>
        private sealed record class Named(string Name)
        {
            /// <summary>
            /// The name it carries.
            /// </summary>
            /// <remarks>
            /// Redeclared rather than left to the positional parameter, because a synthesised init accessor needs
            /// IsExternalInit and that is not among the polyfills this solution generates for .NET Framework.
            /// </remarks>
            public string Name { get; } = Name;
        }
    }
}
