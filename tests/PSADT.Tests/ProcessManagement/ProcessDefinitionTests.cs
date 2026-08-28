using System;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Runtime.Serialization;
using PSADT.ProcessManagement;
using Xunit;

namespace PSADT.Tests.ProcessManagement
{
    /// <summary>
    /// Tests the process matching rules, and the fields derived from a definition's name.
    /// </summary>
    /// <remarks>
    /// A definition names either a bare process, a wildcard pattern, or a fully qualified path, and the
    /// two matching methods are asymmetric on purpose. <see cref="ProcessDefinition.ProcessNameIsMatch"/>
    /// is asked about a bare process name and is deliberately permissive, because a name is all that is
    /// known when the running process list is first narrowed down. Everything a name cannot decide is
    /// settled afterwards by <see cref="ProcessDefinition.IsNameMatch"/> against the full image path.
    /// Reading either method as if it were the whole test makes the permissiveness look like a defect.
    /// </remarks>
    public sealed class ProcessDefinitionTests
    {
        /// <summary>
        /// Verifies that a definition keeps the name it was given, unaltered.
        /// </summary>
        /// <param name="name">The name to construct with.</param>
        [Theory]
        [InlineData("notepad")]
        [InlineData("notepad.exe")]
        [InlineData("note*")]
        [InlineData(@"C:\Windows\notepad.exe")]
        [InlineData(@"C:\Windows\*.exe")]
        public void Constructor_KeepsTheNameVerbatim(string name)
        {
            Assert.Equal(name, new ProcessDefinition(name).Name);
        }

        /// <summary>
        /// Verifies that a missing or blank name is rejected, since a definition that matches nothing is
        /// a programming error rather than a usable filter.
        /// </summary>
        /// <param name="name">The blank name to reject.</param>
        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("\t")]
        public void Constructor_RejectsABlankName(string name)
        {
            _ = Assert.Throws<ArgumentException>(() => new ProcessDefinition(name));
        }

        /// <summary>
        /// Verifies that a null name is rejected as absent rather than as invalid.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "This is deliberate as part of unit testing.")]
        [Fact]
        public void Constructor_RejectsANullName()
        {
            _ = Assert.Throws<ArgumentNullException>(static () => new ProcessDefinition((string)null!));
        }

        /// <summary>
        /// Verifies that a name consisting only of wildcards is rejected, because it would match every
        /// process on the machine and is almost certainly not what a caller meant.
        /// </summary>
        /// <param name="name">The wildcard-only name to reject.</param>
        [Theory]
        [InlineData("*")]
        [InlineData("**")]
        [InlineData("***")]
        public void Constructor_RejectsAWildcardOnlyName(string name)
        {
            _ = Assert.Throws<ArgumentException>(() => new ProcessDefinition(name));
        }

        /// <summary>
        /// Verifies that a name merely containing wildcards is accepted, so the rejection above is about
        /// a name that is nothing but wildcards.
        /// </summary>
        /// <param name="name">The name to accept.</param>
        [Theory]
        [InlineData("*pad")]
        [InlineData("note*")]
        [InlineData("*note*")]
        [InlineData("n*t*p*d")]
        [InlineData("*.exe")]
        public void Constructor_AcceptsANameContainingWildcards(string name)
        {
            Assert.Equal(name, new ProcessDefinition(name).Name);
        }

        /// <summary>
        /// Verifies that a description is kept when supplied and left unset when absent.
        /// </summary>
        [Fact]
        public void Constructor_KeepsASuppliedDescription()
        {
            Assert.Equal("Text editor", new ProcessDefinition("notepad", "Text editor").Description);
            Assert.Null(new ProcessDefinition("notepad", description: null).Description);
            Assert.Null(new ProcessDefinition("notepad").Description);
        }

        /// <summary>
        /// Verifies that an empty description is treated as absent, while one that has length but no
        /// content is rejected. The distinction is deliberate in the constructor and easy to invert.
        /// </summary>
        [Fact]
        public void Constructor_DistinguishesAnEmptyDescriptionFromABlankOne()
        {
            // Assert: zero length is accepted and leaves the description unset
            Assert.Null(new ProcessDefinition("notepad", string.Empty).Description);

            // Assert: non-zero length with no content is a bad argument
            _ = Assert.Throws<ArgumentException>(static () => new ProcessDefinition("notepad", "   "));
        }

        /// <summary>
        /// Verifies that a definition can be built from a dictionary, which is how PowerShell supplies
        /// one from a hashtable literal.
        /// </summary>
        [Fact]
        public void Constructor_ReadsNameAndDescriptionFromADictionary()
        {
            // Arrange
            OrderedDictionary properties = new() { { "Name", "notepad" }, { "Description", "Text editor" } };

            // Act
            ProcessDefinition definition = new(properties);

            // Assert
            Assert.Equal("notepad", definition.Name);
            Assert.Equal("Text editor", definition.Description);
        }

        /// <summary>
        /// Verifies that a dictionary with no description yields a definition without one, rather than
        /// failing.
        /// </summary>
        [Fact]
        public void Constructor_AcceptsADictionaryWithoutADescription()
        {
            // Arrange
            OrderedDictionary properties = new() { { "Name", "notepad" } };

            // Act
            ProcessDefinition definition = new(properties);

            // Assert
            Assert.Equal("notepad", definition.Name);
            Assert.Null(definition.Description);
        }

        /// <summary>
        /// Verifies that a dictionary missing the name is rejected, naming the parameter rather than
        /// failing later on a null.
        /// </summary>
        [Fact]
        public void Constructor_RejectsADictionaryWithoutAName()
        {
            _ = Assert.Throws<ArgumentNullException>(static () => new ProcessDefinition(new OrderedDictionary { { "Description", "Text editor" } }));
        }

        /// <summary>
        /// Verifies that a null dictionary is rejected.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "This is deliberate as part of unit testing.")]
        [Fact]
        public void Constructor_RejectsANullDictionary()
        {
            _ = Assert.Throws<ArgumentNullException>(static () => new ProcessDefinition((IDictionary)null!));
        }

        /// <summary>
        /// Verifies which names count as fully qualified, since that is what decides whether matching
        /// goes on to compare full image paths.
        /// </summary>
        /// <param name="name">The name to classify.</param>
        /// <param name="expected">Whether it should be treated as a fully qualified path.</param>
        [Theory]
        [InlineData("notepad", false)]
        [InlineData("notepad.exe", false)]
        [InlineData("note*", false)]
        [InlineData(@"Windows\notepad.exe", false)]
        [InlineData(@".\notepad.exe", false)]
        [InlineData(@"C:\Windows\notepad.exe", true)]
        [InlineData(@"C:\Windows\*.exe", true)]
        [InlineData(@"\\server\share\notepad.exe", true)]
        public void NameIsFullyQualifiedPath_ClassifiesTheName(string name, bool expected)
        {
            Assert.Equal(expected, new ProcessDefinition(name).NameIsFullyQualifiedPath());
        }

        /// <summary>
        /// Verifies that a name without wildcards matches only itself, ignoring case as the file system
        /// does.
        /// </summary>
        /// <param name="name">The definition's name.</param>
        /// <param name="input">The value to test against it.</param>
        /// <param name="expected">Whether they should match.</param>
        [Theory]
        [InlineData("notepad", "notepad", true)]
        [InlineData("notepad", "NOTEPAD", true)]
        [InlineData("notepad", "NotePad", true)]
        [InlineData("notepad", "notepad.exe", false)]
        [InlineData("notepad", "wordpad", false)]
        [InlineData(@"C:\Windows\notepad.exe", @"c:\windows\notepad.exe", true)]
        [InlineData(@"C:\Windows\notepad.exe", @"C:\Windows\wordpad.exe", false)]
        public void IsNameMatch_ComparesExactlyWhenThereIsNoWildcard(string name, string input, bool expected)
        {
            Assert.Equal(expected, new ProcessDefinition(name).IsNameMatch(input));
        }

        /// <summary>
        /// Verifies that a wildcard in the name becomes an anchored pattern, so it matches a whole value
        /// rather than any part of one.
        /// </summary>
        /// <param name="name">The definition's name.</param>
        /// <param name="input">The value to test against it.</param>
        /// <param name="expected">Whether they should match.</param>
        [Theory]
        [InlineData("note*", "notepad", true)]
        [InlineData("note*", "NOTEPAD", true)]
        [InlineData("note*", "note", true)]
        [InlineData("note*", "wordpad", false)]
        [InlineData("note*", "mynotepad", false)]
        [InlineData("*pad", "notepad", true)]
        [InlineData("*pad", "wordpad", true)]
        [InlineData("*pad", "notepad.exe", false)]
        [InlineData("*note*", "mynotepad", true)]
        [InlineData("n*d", "notepad", true)]
        [InlineData(@"C:\Windows\*.exe", @"C:\Windows\notepad.exe", true)]
        [InlineData(@"C:\Windows\*.exe", @"C:\Windows\System32\notepad.exe", true)]
        [InlineData(@"C:\Windows\*.exe", @"C:\Program Files\app.exe", false)]
        [InlineData(@"C:\Windows\*.exe", @"C:\Windows\notepad.dll", false)]
        public void IsNameMatch_AnchorsAWildcardPattern(string name, string input, bool expected)
        {
            Assert.Equal(expected, new ProcessDefinition(name).IsNameMatch(input));
        }

        /// <summary>
        /// Verifies that characters with meaning in a regular expression are matched literally, so a
        /// name taken from a file path cannot be reinterpreted as a pattern.
        /// </summary>
        [Fact]
        public void IsNameMatch_TreatsRegularExpressionCharactersLiterally()
        {
            // Arrange: the dot and the parentheses must match themselves, and only the asterisk is a wildcard
            ProcessDefinition definition = new("app (x86)*");

            // Assert
            Assert.True(definition.IsNameMatch("app (x86) helper"));
            Assert.False(definition.IsNameMatch("appZ(x86) helper"));
        }

        /// <summary>
        /// Verifies that a blank value is rejected rather than reported as not matching, so a caller
        /// cannot mistake a bad argument for a negative result.
        /// </summary>
        /// <param name="input">The blank value to reject.</param>
        [Theory]
        [InlineData("")]
        [InlineData("  ")]
        public void IsNameMatch_RejectsABlankInput(string input)
        {
            _ = Assert.Throws<ArgumentException>(() => new ProcessDefinition("notepad").IsNameMatch(input));
        }

        /// <summary>
        /// Verifies that a bare name is compared directly against the process name.
        /// </summary>
        /// <param name="name">The definition's name.</param>
        /// <param name="processName">The process name to test against it.</param>
        /// <param name="expected">Whether they should match.</param>
        [Theory]
        [InlineData("notepad", "notepad", true)]
        [InlineData("notepad", "NOTEPAD", true)]
        [InlineData("notepad", "wordpad", false)]
        [InlineData("note*", "notepad", true)]
        [InlineData("note*", "wordpad", false)]
        [InlineData("*pad", "wordpad", true)]
        public void ProcessNameIsMatch_ComparesABareNameDirectly(string name, string processName, bool expected)
        {
            Assert.Equal(expected, new ProcessDefinition(name).ProcessNameIsMatch(processName));
        }

        /// <summary>
        /// Verifies that a fully qualified name is reduced to its file name before being compared, since
        /// a process name carries no directory and no extension.
        /// </summary>
        /// <param name="name">The definition's name.</param>
        /// <param name="processName">The process name to test against it.</param>
        /// <param name="expected">Whether they should match.</param>
        [Theory]
        [InlineData(@"C:\Windows\notepad.exe", "notepad", true)]
        [InlineData(@"C:\Windows\notepad.exe", "NOTEPAD", true)]
        [InlineData(@"C:\Windows\notepad.exe", "wordpad", false)]
        [InlineData(@"C:\Windows\note*.exe", "notepad", true)]
        [InlineData(@"C:\Windows\note*.exe", "wordpad", false)]
        [InlineData(@"C:\Program Files\App\app.exe", "app", true)]
        public void ProcessNameIsMatch_ComparesOnlyTheFileNamePortion(string name, string processName, bool expected)
        {
            Assert.Equal(expected, new ProcessDefinition(name).ProcessNameIsMatch(processName));
        }

        /// <summary>
        /// Verifies that a path whose file name is entirely a wildcard matches every process name.
        /// </summary>
        /// <remarks>
        /// This looks alarming and is correct. The file name of <c>C:\Windows\*.exe</c> without its
        /// extension is <c>*</c>, and a process of any name whatsoever could be running from that path,
        /// so a test that has only the process name to go on cannot exclude anything. The path itself is
        /// applied separately, by <see cref="ProcessDefinition.IsNameMatch"/> against the full image
        /// path, which is what the test below confirms still discriminates.
        /// </remarks>
        [Fact]
        public void ProcessNameIsMatch_MatchesEveryNameWhenTheFileNameIsEntirelyAWildcard()
        {
            // Arrange
            ProcessDefinition definition = new(@"C:\Windows\*.exe");

            // Assert: the name alone cannot narrow anything down
            Assert.True(definition.ProcessNameIsMatch("notepad"));
            Assert.True(definition.ProcessNameIsMatch("svchost"));
            Assert.True(definition.ProcessNameIsMatch("anything at all"));

            // Assert: the path still does
            Assert.True(definition.IsNameMatch(@"C:\Windows\notepad.exe"));
            Assert.False(definition.IsNameMatch(@"C:\Program Files\App\app.exe"));
        }

        /// <summary>
        /// Verifies that a null process name is rejected, while an empty one is answered rather than
        /// rejected. The asymmetry with <see cref="ProcessDefinition.IsNameMatch"/> is in the source and
        /// is pinned here so a change to either guard is visible.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "This is deliberate as part of unit testing.")]
        [Fact]
        public void ProcessNameIsMatch_RejectsOnlyANullProcessName()
        {
            // Arrange
            ProcessDefinition definition = new("notepad");

            // Assert
            _ = Assert.Throws<ArgumentNullException>(() => definition.ProcessNameIsMatch(null!));
            Assert.False(definition.ProcessNameIsMatch(string.Empty));
        }

        /// <summary>
        /// Verifies that the compiled patterns are rebuilt after deserialisation, since they are not
        /// themselves serialised and everything the type does depends on them.
        /// </summary>
        /// <param name="name">The name to round-trip.</param>
        /// <param name="matching">A value the rebuilt definition should match.</param>
        /// <param name="notMatching">A value it should not match.</param>
        [Theory]
        [InlineData("notepad", "NOTEPAD", "wordpad")]
        [InlineData("note*", "notepad", "wordpad")]
        [InlineData(@"C:\Windows\note*.exe", @"C:\Windows\notepad.exe", @"C:\Windows\wordpad.exe")]
        public void Deserialization_RebuildsTheCompiledPatterns(string name, string matching, string notMatching)
        {
            // Arrange
            ProcessDefinition original = new(name, "A description");
            DataContractSerializer serializer = new(typeof(ProcessDefinition));

            // Act
            using MemoryStream stream = new();
            serializer.WriteObject(stream, original);
            stream.Position = 0;

            // Assigned through a local rather than cast inline: the two target frameworks disagree on
            // whether ReadObject's return is nullable, so a null-forgiving operator is necessary on one
            // and flagged as redundant on the other.
            object? deserialized = serializer.ReadObject(stream);
            Assert.NotNull(deserialized);
            ProcessDefinition restored = (ProcessDefinition)deserialized;

            // Assert
            Assert.Equal(original.Name, restored.Name);
            Assert.Equal(original.Description, restored.Description);
            Assert.True(restored.IsNameMatch(matching));
            Assert.False(restored.IsNameMatch(notMatching));
            Assert.Equal(original.NameIsFullyQualifiedPath(), restored.NameIsFullyQualifiedPath());
        }

        /// <summary>
        /// Verifies that equality is by name and description, since definitions are collected into lists
        /// that are compared as a whole.
        /// </summary>
        /// <remarks>
        /// The wildcard cases are the ones worth having. A name containing one is compiled into a pattern
        /// and the pattern kept, and a compiled pattern compares by reference - so two definitions for
        /// the same wildcard name used to come out unequal while the generated description rendered them
        /// identically. The compiled state is held apart from the comparison now.
        /// </remarks>
        [Fact]
        public void Equality_IsByNameAndDescription()
        {
            // Assert: a plain name, which is compared directly rather than compiled
            Assert.Equal(new ProcessDefinition("notepad", "Editor"), new ProcessDefinition("notepad", "Editor"));
            Assert.NotEqual(new ProcessDefinition("notepad", "Editor"), new ProcessDefinition("notepad", "Other"));
            Assert.NotEqual(new ProcessDefinition("notepad", "Editor"), new ProcessDefinition("wordpad", "Editor"));

            // Assert: a wildcard name, which is compiled into a pattern that is kept
            Assert.Equal(new ProcessDefinition("note*", "Editor"), new ProcessDefinition("note*", "Editor"));
            Assert.Equal(new ProcessDefinition("note*", "Editor").GetHashCode(), new ProcessDefinition("note*", "Editor").GetHashCode());
            Assert.NotEqual(new ProcessDefinition("note*", "Editor"), new ProcessDefinition("word*", "Editor"));

            // Assert: a fully qualified path with a wildcard, which is compiled into two patterns
            Assert.Equal(new ProcessDefinition(@"C:\Windows\note*.exe", "Editor"), new ProcessDefinition(@"C:\Windows\note*.exe", "Editor"));
            Assert.NotEqual(new ProcessDefinition(@"C:\Windows\note*.exe", "Editor"), new ProcessDefinition(@"C:\Windows\word*.exe", "Editor"));
        }
    }
}
