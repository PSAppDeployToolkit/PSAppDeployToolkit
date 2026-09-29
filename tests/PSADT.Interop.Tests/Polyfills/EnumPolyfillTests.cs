using System;
using Xunit;

namespace PSADT.Interop.Tests.Polyfills
{
    /// <summary>
    /// Tests the generic Enum polyfills: GetNames&lt;TEnum&gt;, GetValues&lt;TEnum&gt;, IsDefined&lt;TEnum&gt; and
    /// Parse&lt;TEnum&gt;. On net472 these bind to the extension members PSADT.Interop generates; on net8.0 to the
    /// framework. Both legs run the same assertions, so a failure on net8.0 means the expectation is wrong and a
    /// failure on net472 alone means the polyfill diverges from it.
    /// </summary>
    /// <remarks>
    /// The sample enumeration carries an alias - two names for one value - because the alignment of GetNames with
    /// GetValues across an alias is the invariant the toolkit's Win32 error lookup is built on, and the one a naive
    /// implementation gets wrong.
    /// </remarks>
    public sealed class EnumPolyfillTests
    {
        /// <summary>
        /// Verifies that GetNames returns every declared name, including an alias sharing another's value.
        /// </summary>
        [Fact]
        public void GetNames_ReturnsEveryDeclaredName()
        {
            // Act
            string[] names = Enum.GetNames<Sample>();

            // Assert
            Assert.Equal(4, names.Length);
            Assert.Contains("First", names, StringComparer.Ordinal);
            Assert.Contains("Second", names, StringComparer.Ordinal);
            Assert.Contains("Third", names, StringComparer.Ordinal);
            Assert.Contains("AliasOfFirst", names, StringComparer.Ordinal);
        }

        /// <summary>
        /// Verifies that GetValues returns a value for every name, an alias included.
        /// </summary>
        [Fact]
        public void GetValues_ReturnsAValuePerName()
        {
            // Act
            Sample[] values = Enum.GetValues<Sample>();

            // Assert
            Assert.Equal(Enum.GetNames<Sample>().Length, values.Length);
            Assert.Contains(Sample.First, values);
            Assert.Contains(Sample.Second, values);
            Assert.Contains(Sample.Third, values);
        }

        /// <summary>
        /// Verifies that GetNames and GetValues line up index for index, which is what lets a caller pair a name
        /// with its value by position rather than by a second lookup.
        /// </summary>
        [Fact]
        public void GetNamesAndGetValues_AreAlignedByIndex()
        {
            // Act
            string[] names = Enum.GetNames<Sample>();
            Sample[] values = Enum.GetValues<Sample>();

            // Assert: parsing the name at each index yields the value at that index, which holds across the alias
            // where comparing against ToString would not, since an alias and the value's canonical name differ.
            Assert.Equal(names.Length, values.Length);
            for (int i = 0; i < names.Length; i++)
            {
                Assert.Equal(values[i], Enum.Parse<Sample>(names[i].AsSpan()));
            }
        }

        /// <summary>
        /// Verifies that IsDefined answers for a declared value and refuses one that names nothing.
        /// </summary>
        [Fact]
        public void IsDefined_AnswersForDeclaredAndUndeclaredValues()
        {
            // Assert
            Assert.True(Enum.IsDefined(Sample.Second));
            Assert.False(Enum.IsDefined((Sample)99));
        }

        /// <summary>
        /// Verifies that Parse over a span turns a name back into its value, resolves an alias to the shared value,
        /// and refuses a name that is not declared.
        /// </summary>
        [Fact]
        public void Parse_ResolvesNamesAndRefusesUnknownOnes()
        {
            // Assert: the span overload is chosen on both legs by passing a span rather than a string.
            Assert.Equal(Sample.Second, Enum.Parse<Sample>("Second".AsSpan()));
            Assert.Equal(Sample.First, Enum.Parse<Sample>("AliasOfFirst".AsSpan()));
            _ = Assert.Throws<ArgumentException>(static () => Enum.Parse<Sample>("Nonexistent".AsSpan()));
        }

        /// <summary>
        /// A small enumeration with an alias, so that the name and value sequences differ from a plain one.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1069:Enums values should not be duplicated", Justification = "The alias is deliberate: it is what makes the name and value sequences differ, which is the invariant under test.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Roslynator", "RCS1234:Duplicate enum value", Justification = "The alias is deliberate; see the type-level justification.")]
        private enum Sample
        {
            /// <summary>
            /// The first member.
            /// </summary>
            First = 1,

            /// <summary>
            /// The second member.
            /// </summary>
            Second = 2,

            /// <summary>
            /// The third member.
            /// </summary>
            Third = 3,

            /// <summary>
            /// A second name for the first member's value.
            /// </summary>
            AliasOfFirst = 1,
        }
    }
}
