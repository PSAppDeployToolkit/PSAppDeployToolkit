using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Security.Principal;
using PSAppDeployToolkit.Attributes;
using PSAppDeployToolkit.Tests.TestHelpers;
using Xunit;

namespace PSAppDeployToolkit.Tests.Attributes
{
    /// <summary>
    /// Tests the shared behaviour behind the three "not empty or white space" validators.
    /// </summary>
    /// <remarks>
    /// The base class carries all the behaviour and the three sealed subclasses only choose a pair of flags, so this
    /// file owns the matrix and each subclass file asserts nothing but its own choice. Driven through the subclasses
    /// because the base is abstract and there is no other way to reach a given pair.
    /// <para>
    /// The flags are not symmetrical, which is the thing most likely to be got wrong: <c>allowEmpty</c> relaxes the
    /// check on strings only. An empty collection is refused whatever it is set to.
    /// </para>
    /// </remarks>
    public sealed class BaseValidateNotEmptyOrWhiteSpaceAttributeTests
    {
        /// <summary>
        /// Verifies that a string with something in it is accepted by all three validators.
        /// </summary>
        [Fact]
        public void Validate_AcceptsAStringWithContent()
        {
            ArgumentAttributes.Validate(NotNullOrWhiteSpace(), "content");
            ArgumentAttributes.Validate(AllowNull(), "content");
            ArgumentAttributes.Validate(AllowEmpty(), "content");
        }

        /// <summary>
        /// Verifies that a string of nothing but white space is refused by all three, whatever the flags.
        /// </summary>
        /// <remarks>
        /// The one rule none of the three relaxes, and the reason the type is named as it is: allowing empty is not the
        /// same as allowing blank.
        /// </remarks>
        /// <param name="whiteSpace">A string carrying nothing but white space.</param>
        [Theory]
        [InlineData(" ")]
        [InlineData("   ")]
        [InlineData("\t")]
        [InlineData("\r\n")]
        public void Validate_RefusesAWhiteSpaceStringWhateverTheFlags(string whiteSpace)
        {
            _ = Assert.Throws<ArgumentException>(() => ArgumentAttributes.Validate(NotNullOrWhiteSpace(), whiteSpace));
            _ = Assert.Throws<ArgumentException>(() => ArgumentAttributes.Validate(AllowNull(), whiteSpace));
            _ = Assert.Throws<ArgumentException>(() => ArgumentAttributes.Validate(AllowEmpty(), whiteSpace));
        }

        /// <summary>
        /// Verifies that the message distinguishes a blank string from an empty one.
        /// </summary>
        /// <remarks>
        /// A caller told "the argument is empty or white space" when empty was in fact permitted would go looking for
        /// the wrong mistake, so the validator that permits empty says only "white space".
        /// </remarks>
        [Fact]
        public void Validate_SaysWhichRuleAStringBroke()
        {
            Assert.Contains(
                "The argument is empty or white space.",
                Assert.Throws<ArgumentException>(static () => ArgumentAttributes.Validate(NotNullOrWhiteSpace(), "  ")).Message,
                StringComparison.Ordinal);
            Assert.Contains(
                "The argument is white space.",
                Assert.Throws<ArgumentException>(static () => ArgumentAttributes.Validate(AllowEmpty(), "  ")).Message,
                StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that a script block is judged by the text it carries.
        /// </summary>
        /// <remarks>
        /// A script block reaches these attributes wherever a caller can supply a callback, and an empty one is a
        /// caller who meant to write something.
        /// </remarks>
        [Fact]
        public void Validate_JudgesAScriptBlockByItsText()
        {
            ArgumentAttributes.Validate(NotNullOrWhiteSpace(), ScriptBlock.Create("Write-Host 'hello'"));
            _ = Assert.Throws<ArgumentException>(static () => ArgumentAttributes.Validate(NotNullOrWhiteSpace(), ScriptBlock.Create(string.Empty)));
            _ = Assert.Throws<ArgumentException>(static () => ArgumentAttributes.Validate(NotNullOrWhiteSpace(), ScriptBlock.Create("   ")));
        }

        /// <summary>
        /// Verifies that an account is judged by its name rather than by whether it resolves.
        /// </summary>
        /// <remarks>
        /// An account that does not exist is still a meaningful argument - it may be created later, or resolved on
        /// another machine - so the validator only asks whether the caller named anything.
        /// </remarks>
        [Fact]
        public void Validate_JudgesAnAccountByItsName()
        {
            ArgumentAttributes.Validate(NotNullOrWhiteSpace(), new NTAccount("BUILTIN\\Administrators"));
            ArgumentAttributes.Validate(NotNullOrWhiteSpace(), new NTAccount("no-such-account-exists"));
            _ = Assert.Throws<ArgumentException>(static () => ArgumentAttributes.Validate(NotNullOrWhiteSpace(), new NTAccount("   ")));
        }

        /// <summary>
        /// Verifies that nothing at all is refused or permitted according to the null flag.
        /// </summary>
        /// <param name="shape">What the absence is called, for the failure message.</param>
        /// <param name="nothing">The value standing for absence.</param>
        [Theory]
        [MemberData(nameof(Nothings))]
        public void Validate_HonoursTheNullFlag(string shape, object? nothing)
        {
            Assert.NotNull(shape);
            _ = Assert.Throws<ArgumentNullException>(() => ArgumentAttributes.Validate(NotNullOrWhiteSpace(), nothing));
            _ = Assert.Throws<ArgumentNullException>(() => ArgumentAttributes.Validate(AllowEmpty(), nothing));
            ArgumentAttributes.Validate(AllowNull(), nothing);
        }

        /// <summary>
        /// Verifies that an empty string is refused or permitted according to the empty flag.
        /// </summary>
        [Fact]
        public void Validate_HonoursTheEmptyFlagForStrings()
        {
            _ = Assert.Throws<ArgumentException>(static () => ArgumentAttributes.Validate(NotNullOrWhiteSpace(), string.Empty));
            _ = Assert.Throws<ArgumentException>(static () => ArgumentAttributes.Validate(AllowNull(), string.Empty));
            ArgumentAttributes.Validate(AllowEmpty(), string.Empty);
        }

        /// <summary>
        /// Verifies that an empty collection is refused even by the validator that permits an empty string.
        /// </summary>
        /// <remarks>
        /// The asymmetry worth pinning. <c>allowEmpty</c> is consulted only where a string is being judged; the
        /// empty-collection refusal is unconditional. A reader of the type's name would reasonably expect otherwise.
        /// </remarks>
        [Fact]
        public void Validate_RefusesAnEmptyCollectionEvenWhenEmptyStringsArePermitted()
        {
            foreach (ValidateArgumentsAttribute attribute in AllThree())
            {
                Assert.Contains(
                    "The argument is an empty collection.",
                    Assert.Throws<ArgumentException>(() => ArgumentAttributes.Validate(attribute, Array.Empty<string>())).Message,
                    StringComparison.Ordinal);
            }
        }

        /// <summary>
        /// Verifies that an empty dictionary is refused, whether it is judged as a dictionary or as a read-only one.
        /// </summary>
        /// <remarks>
        /// Two separate branches reach the same refusal. Anything implementing the non-generic
        /// <see cref="IDictionary"/> takes the first; a type offering only
        /// <see cref="IReadOnlyDictionary{TKey, TValue}"/> takes a reflected path that reads <c>Count</c> off the
        /// interface. Nothing in the framework takes the second - <see cref="Dictionary{TKey, TValue}"/> and its
        /// read-only wrapper both implement <see cref="IDictionary"/> - so it is reached here with a stand-in, which
        /// is the only way to exercise it at all.
        /// </remarks>
        [Fact]
        public void Validate_RefusesAnEmptyDictionary()
        {
            _ = Assert.Throws<ArgumentException>(static () => ArgumentAttributes.Validate(NotNullOrWhiteSpace(), new Hashtable()));
            _ = Assert.Throws<ArgumentException>(static () => ArgumentAttributes.Validate(NotNullOrWhiteSpace(), new ReadOnlyDictionaryOnly(0, "content")));
        }

        /// <summary>
        /// Verifies that a dictionary's values are judged by the same rules as a collection's elements.
        /// </summary>
        /// <remarks>
        /// Previously only the count was checked, so a dictionary holding a blank value passed where the same value in
        /// a list was refused. Both dictionary branches are covered, since each walks its values a different way.
        /// </remarks>
        [Fact]
        public void Validate_InspectsADictionaryValues()
        {
            ArgumentAttributes.Validate(NotNullOrWhiteSpace(), new Hashtable { { "key", "content" } });
            ArgumentAttributes.Validate(NotNullOrWhiteSpace(), new ReadOnlyDictionaryOnly(1, "content"));
            Assert.Contains(
                "empty or white space values",
                Assert.Throws<ArgumentException>(static () => ArgumentAttributes.Validate(NotNullOrWhiteSpace(), new Hashtable { { "key", "   " } })).Message,
                StringComparison.Ordinal);
            Assert.Contains(
                "empty or white space values",
                Assert.Throws<ArgumentException>(static () => ArgumentAttributes.Validate(NotNullOrWhiteSpace(), new ReadOnlyDictionaryOnly(1, "   "))).Message,
                StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that a dictionary carrying a null value is refused.
        /// </summary>
        [Fact]
        public void Validate_RefusesADictionaryCarryingANullValue()
        {
            Assert.Contains(
                "contains a null value",
                Assert.Throws<ArgumentException>(static () => ArgumentAttributes.Validate(NotNullOrWhiteSpace(), new Hashtable { { "key", null } })).Message,
                StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that the empty flag reaches a dictionary's values too.
        /// </summary>
        [Fact]
        public void Validate_HonoursTheEmptyFlagForDictionaryValues()
        {
            _ = Assert.Throws<ArgumentException>(static () => ArgumentAttributes.Validate(NotNullOrWhiteSpace(), new Hashtable { { "key", string.Empty } }));
            ArgumentAttributes.Validate(AllowEmpty(), new Hashtable { { "key", string.Empty } });
            _ = Assert.Throws<ArgumentException>(static () => ArgumentAttributes.Validate(AllowEmpty(), new Hashtable { { "key", "   " } }));
        }

        /// <summary>
        /// Verifies that a dictionary's keys are not judged.
        /// </summary>
        /// <remarks>
        /// Only values are inspected. A blank key is a different kind of mistake and no parameter in the module takes a
        /// dictionary whose keys come from a caller, so it is left alone rather than guessed at.
        /// </remarks>
        [Fact]
        public void Validate_DoesNotJudgeADictionaryKeys()
        {
            ArgumentAttributes.Validate(NotNullOrWhiteSpace(), new Hashtable { { "   ", "content" } });
        }

        /// <summary>
        /// Verifies that a collection is inspected element by element.
        /// </summary>
        [Fact]
        public void Validate_RefusesACollectionCarryingABlankElement()
        {
            Assert.Contains(
                "empty or white space elements",
                Assert.Throws<ArgumentException>(static () => ArgumentAttributes.Validate(NotNullOrWhiteSpace(), new[] { "first", "  ", "third" })).Message,
                StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that the empty flag reaches a collection's elements as well as a bare string.
        /// </summary>
        [Fact]
        public void Validate_HonoursTheEmptyFlagForCollectionElements()
        {
            _ = Assert.Throws<ArgumentException>(static () => ArgumentAttributes.Validate(NotNullOrWhiteSpace(), new[] { "first", string.Empty }));
            ArgumentAttributes.Validate(AllowEmpty(), new[] { "first", string.Empty });
            _ = Assert.Throws<ArgumentException>(static () => ArgumentAttributes.Validate(AllowEmpty(), new[] { "first", "  " }));
        }

        /// <summary>
        /// Verifies that a collection carrying nothing at all is refused, in each of PowerShell's shapes for absence.
        /// </summary>
        [Fact]
        public void Validate_RefusesACollectionCarryingNothing()
        {
            foreach (object? nothing in new object?[] { null, System.Management.Automation.Internal.AutomationNull.Value, NullString.Value, DBNull.Value })
            {
                Assert.Contains(
                    "contains a null element",
                    Assert.Throws<ArgumentException>(() => ArgumentAttributes.Validate(NotNullOrWhiteSpace(), new object?[] { "first", nothing })).Message,
                    StringComparison.Ordinal);
            }
        }

        /// <summary>
        /// Verifies that a collection's script block elements are judged by the text they carry.
        /// </summary>
        /// <remarks>
        /// The element scan recognised strings alone, so an array of empty script blocks passed where a single empty one
        /// was refused. Reachable in the module: Invoke-ADTAllUsersRegistryAction takes a script block array.
        /// </remarks>
        [Fact]
        public void Validate_JudgesScriptBlockElementsByTheirText()
        {
            ArgumentAttributes.Validate(NotNullOrWhiteSpace(), new[] { ScriptBlock.Create("Write-Host 'first'"), ScriptBlock.Create("Write-Host 'second'") });
            Assert.Contains(
                "empty or white space elements",
                Assert.Throws<ArgumentException>(static () => ArgumentAttributes.Validate(
                    NotNullOrWhiteSpace(),
                    new[] { ScriptBlock.Create("Write-Host 'first'"), ScriptBlock.Create(string.Empty) })).Message,
                StringComparison.Ordinal);
            _ = Assert.Throws<ArgumentException>(static () => ArgumentAttributes.Validate(
                NotNullOrWhiteSpace(),
                new[] { ScriptBlock.Create("   ") }));
        }

        /// <summary>
        /// Verifies that a collection's account elements are judged by their names.
        /// </summary>
        [Fact]
        public void Validate_JudgesAccountElementsByTheirNames()
        {
            ArgumentAttributes.Validate(NotNullOrWhiteSpace(), new[] { new NTAccount("BUILTIN\\Administrators"), new NTAccount("BUILTIN\\Users") });
            _ = Assert.Throws<ArgumentException>(static () => ArgumentAttributes.Validate(
                NotNullOrWhiteSpace(),
                new[] { new NTAccount("BUILTIN\\Administrators"), new NTAccount("   ") }));
        }

        /// <summary>
        /// Verifies that a collection element carrying no text at all is left alone.
        /// </summary>
        /// <remarks>
        /// Only the shapes that carry text are judged. An element with none - a number, a date - is accepted, which is
        /// the same stance the attribute takes on such a value passed on its own.
        /// </remarks>
        [Fact]
        public void Validate_LeavesCollectionElementsCarryingNoTextAlone()
        {
            ArgumentAttributes.Validate(NotNullOrWhiteSpace(), new object[] { 42, Guid.Empty, DateTime.MinValue });
        }

        /// <summary>
        /// Verifies that a dictionary's script block and account values are judged the same way.
        /// </summary>
        /// <remarks>
        /// The third level of the same rule. All three - the argument, a collection's elements, a dictionary's values -
        /// now read text from the same set of shapes, so none can drift from the others.
        /// </remarks>
        [Fact]
        public void Validate_JudgesDictionaryValuesOfEveryTextCarryingShape()
        {
            ArgumentAttributes.Validate(NotNullOrWhiteSpace(), new Hashtable { { "script", ScriptBlock.Create("Write-Host 'hello'") } });
            _ = Assert.Throws<ArgumentException>(static () => ArgumentAttributes.Validate(NotNullOrWhiteSpace(), new Hashtable { { "script", ScriptBlock.Create("   ") } }));
            _ = Assert.Throws<ArgumentException>(static () => ArgumentAttributes.Validate(NotNullOrWhiteSpace(), new Hashtable { { "account", new NTAccount("   ") } }));
        }

        /// <summary>
        /// Verifies that the empty flag reaches every text-carrying shape, at every level.
        /// </summary>
        /// <remarks>
        /// What the shared reading is for: an empty script block is permitted where empty is permitted, and refused
        /// where it is not, and the answer does not depend on whether it arrived on its own, in a list or in a
        /// dictionary.
        /// </remarks>
        [Fact]
        public void Validate_HonoursTheEmptyFlagForEveryShapeAtEveryLevel()
        {
            foreach (object empty in new object[] { ScriptBlock.Create(string.Empty), string.Empty })
            {
                _ = Assert.Throws<ArgumentException>(() => ArgumentAttributes.Validate(NotNullOrWhiteSpace(), empty));
                ArgumentAttributes.Validate(AllowEmpty(), empty);
                _ = Assert.Throws<ArgumentException>(() => ArgumentAttributes.Validate(NotNullOrWhiteSpace(), new[] { empty }));
                ArgumentAttributes.Validate(AllowEmpty(), new[] { empty });
                _ = Assert.Throws<ArgumentException>(() => ArgumentAttributes.Validate(NotNullOrWhiteSpace(), new Hashtable { { "key", empty } }));
                ArgumentAttributes.Validate(AllowEmpty(), new Hashtable { { "key", empty } });
            }
        }

        /// <summary>
        /// Verifies that a collection of non-nullable value types is not scanned element by element.
        /// </summary>
        /// <remarks>
        /// A deliberate short-circuit: an integer can be neither null nor white space, so scanning would only cost
        /// time. What makes it worth a test is the boundary - a collection of nullable integers is scanned, and
        /// a null in one is caught.
        /// </remarks>
        [Fact]
        public void Validate_SkipsTheElementScanForNonNullableValueTypes()
        {
            ArgumentAttributes.Validate(NotNullOrWhiteSpace(), new[] { 1, 2, 3 });
            _ = Assert.Throws<ArgumentException>(static () => ArgumentAttributes.Validate(NotNullOrWhiteSpace(), new int?[] { 1, null }));
        }

        /// <summary>
        /// Verifies that a generic collection that is not an array is inspected the same way.
        /// </summary>
        [Fact]
        public void Validate_InspectsAGenericCollection()
        {
            ArgumentAttributes.Validate(NotNullOrWhiteSpace(), new List<string> { "first", "second" });
            _ = Assert.Throws<ArgumentException>(static () => ArgumentAttributes.Validate(NotNullOrWhiteSpace(), new List<string> { "first", "  " }));
            _ = Assert.Throws<ArgumentException>(static () => ArgumentAttributes.Validate(NotNullOrWhiteSpace(), new List<string>()));
        }

        /// <summary>
        /// Verifies that a value the validator has no rule for is accepted.
        /// </summary>
        /// <remarks>
        /// Every branch is a type test and there is no final else, so anything unrecognised falls through and passes.
        /// Recorded as the actual contract rather than assumed: an attribute named "validate not empty" accepting an
        /// arbitrary object is surprising, and a caller relying on it to reject one would be disappointed.
        /// </remarks>
        [Fact]
        public void Validate_AcceptsAValueItHasNoRuleFor()
        {
            ArgumentAttributes.Validate(NotNullOrWhiteSpace(), 42);
            ArgumentAttributes.Validate(NotNullOrWhiteSpace(), Guid.Empty);
            ArgumentAttributes.Validate(NotNullOrWhiteSpace(), DateTime.MinValue);
        }

        /// <summary>
        /// Verifies that a value wrapped by PowerShell is unwrapped before being judged.
        /// </summary>
        [Fact]
        public void Validate_UnwrapsAPSObject()
        {
            ArgumentAttributes.Validate(NotNullOrWhiteSpace(), PSObject.AsPSObject("content"));
            _ = Assert.Throws<ArgumentException>(static () => ArgumentAttributes.Validate(NotNullOrWhiteSpace(), PSObject.AsPSObject("  ")));
        }

        /// <summary>
        /// Verifies that a wrapped element inside a collection is unwrapped too.
        /// </summary>
        /// <remarks>
        /// A collection coming back from the pipeline holds wrapped elements, so an element scan that did not unwrap
        /// would pass every blank string in one.
        /// </remarks>
        [Fact]
        public void Validate_UnwrapsCollectionElements()
        {
            _ = Assert.Throws<ArgumentException>(static () => ArgumentAttributes.Validate(
                NotNullOrWhiteSpace(),
                new object[] { PSObject.AsPSObject("first"), PSObject.AsPSObject("  ") }));
        }

        /// <summary>
        /// The shapes PowerShell uses to mean nothing.
        /// </summary>
        public static TheoryData<string, object?> Nothings =>
            new()
            {
                { "null", null },
                { "AutomationNull", System.Management.Automation.Internal.AutomationNull.Value },
                { "NullString", NullString.Value },
                { "DBNull", DBNull.Value },
            };

        /// <summary>
        /// One of each validator, for the rules none of them relaxes.
        /// </summary>
        /// <returns>The three validators.</returns>
        private static ValidateArgumentsAttribute[] AllThree()
        {
            return [NotNullOrWhiteSpace(), AllowNull(), AllowEmpty()];
        }

        /// <summary>
        /// The validator permitting neither absence nor emptiness.
        /// </summary>
        /// <remarks>
        /// Fully qualified because PowerShell 7 ships a validator of the same name, which makes the bare name
        /// ambiguous in a file that also uses PowerShell's own types.
        /// </remarks>
        /// <returns>The validator.</returns>
        private static PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpaceAttribute NotNullOrWhiteSpace()
        {
            return new();
        }

        /// <summary>
        /// The validator permitting absence but not emptiness.
        /// </summary>
        /// <returns>The validator.</returns>
        private static AllowNullButNotEmptyOrWhiteSpaceAttribute AllowNull()
        {
            return new();
        }

        /// <summary>
        /// The validator permitting emptiness but not absence.
        /// </summary>
        /// <returns>The validator.</returns>
        private static AllowEmptyButNotNullOrWhiteSpaceAttribute AllowEmpty()
        {
            return new();
        }

        /// <summary>
        /// A dictionary that offers only <see cref="IReadOnlyDictionary{TKey, TValue}"/>.
        /// </summary>
        /// <remarks>
        /// Exists to reach the reflected read-only-dictionary branch, which nothing in the framework can reach because
        /// every dictionary it ships also implements the non-generic <see cref="IDictionary"/> and is caught by the
        /// earlier test. Only <c>Count</c> is ever read, so the rest is the minimum the interface demands.
        /// </remarks>
        /// <param name="count">How many entries to claim.</param>
        /// <param name="value">The single value to report, however many entries are claimed.</param>
        private sealed class ReadOnlyDictionaryOnly(int count, string value) : IReadOnlyDictionary<string, string>
        {
            /// <inheritdoc/>
            public int Count { get; } = count;

            /// <inheritdoc/>
            public IEnumerable<string> Keys => throw new NotSupportedException();

            /// <inheritdoc/>
            public IEnumerable<string> Values { get; } = count > 0 ? [value] : [];

            /// <inheritdoc/>
            public string this[string key] => throw new NotSupportedException();

            /// <inheritdoc/>
            public bool ContainsKey(string key)
            {
                throw new NotSupportedException();
            }

            /// <inheritdoc/>
            public bool TryGetValue(string key, out string value)
            {
                throw new NotSupportedException();
            }

            /// <inheritdoc/>
            public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
            {
                throw new NotSupportedException();
            }

            /// <inheritdoc/>
            IEnumerator IEnumerable.GetEnumerator()
            {
                throw new NotSupportedException();
            }
        }
    }
}
