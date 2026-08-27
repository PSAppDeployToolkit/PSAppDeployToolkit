using System.Collections.Immutable;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using PSADT.CodeAnalysis.Tests.TestHelpers;
using Xunit;

namespace PSADT.CodeAnalysis.Tests
{
    /// <summary>
    /// Tests the suppressor that lets CA1051 through for visible fields carrying a serialization
    /// attribute. Every test compiles a snippet in memory and runs the suppressor over it through the
    /// same Roslyn pipeline the compiler uses, so nothing outside the test is touched.
    /// </summary>
    /// <remarks>
    /// PSADT serializes types such as RunAsActiveUser and ProcessLaunchInfo across the client/server
    /// boundary, and the data contract serializer only sees fields it is pointed at. Those fields have to
    /// be visible, which is exactly what CA1051 objects to, so the suppressor is what keeps the rule on
    /// everywhere else without annotating each field by hand.
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "RCS1046:Add suffix 'Async' to asynchronous method name", Justification = "Test names describe the scenario under test; the async suffix would obscure them.")]
    public sealed class DataMemberPublicFieldSuppressorTests
    {
        /// <summary>
        /// Verifies that the suppressor advertises one suppression, and that it is aimed at CA1051 under
        /// the ID the repository uses. Roslyn only offers a suppressor the diagnostics named here, so a
        /// wrong ID means the suppressor is never consulted at all.
        /// </summary>
        [Fact]
        public void SupportedSuppressions_DeclaresASingleSuppressionForCA1051()
        {
            // Act
            SuppressionDescriptor descriptor = Assert.Single(new DataMemberPublicFieldSuppressor().SupportedSuppressions);

            // Assert
            Assert.Equal("PSADTDSCA1051", descriptor.Id);
            Assert.Equal("CA1051", descriptor.SuppressedDiagnosticId);
        }

        /// <summary>
        /// Verifies that the suppression carries a justification. Roslyn surfaces it wherever a
        /// programmatic suppression is explained, so an empty one leaves a reader with no reason.
        /// </summary>
        [Fact]
        public void SupportedSuppressions_ExplainsWhyTheDiagnosticIsSuppressed()
        {
            // Act
            SuppressionDescriptor descriptor = Assert.Single(new DataMemberPublicFieldSuppressor().SupportedSuppressions);

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(descriptor.Justification.ToString(CultureInfo.InvariantCulture)));
        }

        /// <summary>
        /// Verifies the case the suppressor exists for: a visible field that the data contract serializer
        /// is told to include.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task ReportSuppressions_SuppressesAFieldMarkedWithDataMember()
        {
            // Arrange
            const string source = """
                using System.Runtime.Serialization;

                public class Holder
                {
                    [DataMember]
                    public readonly int Value;
                }
                """;

            // Act
            ImmutableArray<Diagnostic> diagnostics = await SuppressorHarness.AnalyzeAsync(source).ConfigureAwait(true);

            // Assert
            Assert.Equal([true], diagnostics.SuppressionStates());
        }

        /// <summary>
        /// Verifies that the opposite serialization attribute suppresses too. A field marked to be skipped
        /// is still part of a serialized type's shape, and PSADT uses both attributes side by side.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task ReportSuppressions_SuppressesAFieldMarkedWithIgnoreDataMember()
        {
            // Arrange
            const string source = """
                using System.Runtime.Serialization;

                public class Holder
                {
                    [IgnoreDataMember]
                    public readonly int Value;
                }
                """;

            // Act
            ImmutableArray<Diagnostic> diagnostics = await SuppressorHarness.AnalyzeAsync(source).ConfigureAwait(true);

            // Assert
            Assert.Equal([true], diagnostics.SuppressionStates());
        }

        /// <summary>
        /// Verifies that the attribute is matched as a symbol rather than as written text, by spelling it
        /// out in full with no using directive in sight.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task ReportSuppressions_SuppressesWhenTheAttributeIsWrittenFullyQualified()
        {
            // Arrange
            const string source = """
                public class Holder
                {
                    [System.Runtime.Serialization.DataMember]
                    public readonly int Value;
                }
                """;

            // Act
            ImmutableArray<Diagnostic> diagnostics = await SuppressorHarness.AnalyzeAsync(source).ConfigureAwait(true);

            // Assert
            Assert.Equal([true], diagnostics.SuppressionStates());
        }

        /// <summary>
        /// Verifies that named arguments on the attribute make no difference, which is the form used
        /// wherever a contract member needs an explicit name or ordering.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task ReportSuppressions_SuppressesWhenTheAttributeCarriesArguments()
        {
            // Arrange
            const string source = """
                using System.Runtime.Serialization;

                public class Holder
                {
                    [DataMember(Name = "renamed", Order = 2)]
                    public readonly int Value;
                }
                """;

            // Act
            ImmutableArray<Diagnostic> diagnostics = await SuppressorHarness.AnalyzeAsync(source).ConfigureAwait(true);

            // Assert
            Assert.Equal([true], diagnostics.SuppressionStates());
        }

        /// <summary>
        /// Verifies that a protected field suppresses as well. CA1051 covers the whole visible surface, not
        /// just the public part of it.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task ReportSuppressions_SuppressesAProtectedField()
        {
            // Arrange
            const string source = """
                using System.Runtime.Serialization;

                public class Holder
                {
                    [DataMember]
                    protected readonly int Value;
                }
                """;

            // Act
            ImmutableArray<Diagnostic> diagnostics = await SuppressorHarness.AnalyzeAsync(source).ConfigureAwait(true);

            // Assert
            Assert.Equal([true], diagnostics.SuppressionStates());
        }

        /// <summary>
        /// Verifies that every field of a declaration sharing one attribute list is suppressed. CA1051
        /// reports per field, while the attribute sits on the declaration, so the suppressor has to walk
        /// from a declarator up to the attributes rather than the other way around.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task ReportSuppressions_SuppressesEveryDeclaratorOfAMultiFieldDeclaration()
        {
            // Arrange
            const string source = """
                using System.Runtime.Serialization;

                public class Holder
                {
                    [DataMember]
                    public int First, Second;
                }
                """;

            // Act
            ImmutableArray<Diagnostic> diagnostics = await SuppressorHarness.AnalyzeAsync(source).ConfigureAwait(true);

            // Assert
            Assert.Equal([true, true], diagnostics.SuppressionStates());
        }

        /// <summary>
        /// Verifies that a field with an initializer suppresses. The declarator then spans more than the
        /// identifier the diagnostic points at, which is the case where the innermost-node lookup has to
        /// widen rather than match exactly.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task ReportSuppressions_SuppressesAFieldWithAnInitializer()
        {
            // Arrange
            const string source = """
                using System.Runtime.Serialization;

                public class Holder
                {
                    [DataMember]
                    public int Value = 42;
                }
                """;

            // Act
            ImmutableArray<Diagnostic> diagnostics = await SuppressorHarness.AnalyzeAsync(source).ConfigureAwait(true);

            // Assert
            Assert.Equal([true], diagnostics.SuppressionStates());
        }

        /// <summary>
        /// Verifies that an undecorated visible field keeps its diagnostic. This is the rule doing its job,
        /// and is what the suppressor must not get in the way of.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task ReportSuppressions_LeavesAnUndecoratedFieldAlone()
        {
            // Arrange
            const string source = """
                public class Holder
                {
                    public int Value;
                }
                """;

            // Act
            ImmutableArray<Diagnostic> diagnostics = await SuppressorHarness.AnalyzeAsync(source).ConfigureAwait(true);

            // Assert
            Assert.Equal([false], diagnostics.SuppressionStates());
        }

        /// <summary>
        /// Verifies that some other attribute is not enough. Only the two serialization attributes justify
        /// a visible field.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task ReportSuppressions_LeavesAFieldCarryingAnUnrelatedAttributeAlone()
        {
            // Arrange
            const string source = """
                public class Holder
                {
                    [System.Obsolete]
                    public int Value;
                }
                """;

            // Act
            ImmutableArray<Diagnostic> diagnostics = await SuppressorHarness.AnalyzeAsync(source).ConfigureAwait(true);

            // Assert
            Assert.Equal([false], diagnostics.SuppressionStates());
        }

        /// <summary>
        /// Verifies that an attribute merely named DataMember does not suppress, which is the other half of
        /// proving the match is by symbol. A name comparison would have accepted this one.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task ReportSuppressions_LeavesAFieldCarryingASameNamedAttributeFromAnotherNamespaceAlone()
        {
            // Arrange
            const string source = """
                namespace Impostor
                {
                    public sealed class DataMemberAttribute : System.Attribute
                    {
                    }
                }

                public class Holder
                {
                    [Impostor.DataMember]
                    public int Value;
                }
                """;

            // Act
            ImmutableArray<Diagnostic> diagnostics = await SuppressorHarness.AnalyzeAsync(source).ConfigureAwait(true);

            // Assert
            Assert.Equal([false], diagnostics.SuppressionStates());
        }

        /// <summary>
        /// Verifies that the decision is made per field rather than per type or per file, using a type that
        /// mixes decorated and undecorated fields the way a partly serialized type does.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task ReportSuppressions_DecidesPerFieldWithinOneType()
        {
            // Arrange
            const string source = """
                using System.Runtime.Serialization;

                public class Holder
                {
                    [DataMember]
                    public readonly int Serialized;

                    public readonly int Incidental;

                    [IgnoreDataMember]
                    public readonly int Skipped;
                }
                """;

            // Act
            ImmutableArray<Diagnostic> diagnostics = await SuppressorHarness.AnalyzeAsync(source).ConfigureAwait(true);

            // Assert
            Assert.Equal([true, false, true], diagnostics.SuppressionStates());
        }

        /// <summary>
        /// Verifies that a diagnostic pointing at a local variable is left alone. A local is declared by a
        /// variable declarator just as a field is, so the declarator alone is not enough to act on.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task ReportSuppressions_LeavesADiagnosticOnALocalVariableAlone()
        {
            // Arrange
            const string source = """
                public class Holder
                {
                    public void Method()
                    {
                        int value = 0;
                    }
                }
                """;

            // Act
            ImmutableArray<Diagnostic> diagnostics = await SuppressorHarness.AnalyzeAsync(source, DiagnosticAnchor.LocalVariable).ConfigureAwait(true);

            // Assert
            Assert.Equal([false], diagnostics.SuppressionStates());
        }

        /// <summary>
        /// Verifies that a diagnostic pointing at something with no variable declarator above it is left
        /// alone, even in a file where the serialization attributes are present and in use.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task ReportSuppressions_LeavesADiagnosticOnAMethodNameAlone()
        {
            // Arrange
            const string source = """
                using System.Runtime.Serialization;

                public class Holder
                {
                    [DataMember]
                    public readonly int Value;

                    public void Method()
                    {
                    }
                }
                """;

            // Act
            ImmutableArray<Diagnostic> diagnostics = await SuppressorHarness.AnalyzeAsync(source, DiagnosticAnchor.MethodName).ConfigureAwait(true);

            // Assert
            Assert.Equal([false], diagnostics.SuppressionStates());
        }

        /// <summary>
        /// Verifies that a decorated field's diagnostic survives when it carries an ID the suppressor was
        /// not registered for. CA1051 is the only rule this suppressor is entitled to touch.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task ReportSuppressions_LeavesADiagnosticWithAnUnrelatedIdAlone()
        {
            // Arrange
            const string source = """
                using System.Runtime.Serialization;

                public class Holder
                {
                    [DataMember]
                    public readonly int Value;
                }
                """;

            // Act
            ImmutableArray<Diagnostic> diagnostics = await SuppressorHarness.AnalyzeAsync(source, reportUnrelatedId: true).ConfigureAwait(true);

            // Assert
            Assert.Equal([false], diagnostics.SuppressionStates());
        }

        /// <summary>
        /// Verifies that a compilation which cannot resolve the serialization attributes suppresses
        /// nothing. Without those two symbols there is nothing to compare a field's attributes against, so
        /// the suppressor has to decline rather than guess from the attribute's name.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task ReportSuppressions_SuppressesNothingWhenTheSerializationAttributesCannotBeResolved()
        {
            // Arrange
            const string source = """
                public class Holder
                {
                    [System.Runtime.Serialization.DataMember]
                    public int Value;
                }
                """;

            // Act
            ImmutableArray<Diagnostic> diagnostics = await SuppressorHarness.AnalyzeWithoutSerializationAttributesAsync(source).ConfigureAwait(true);

            // Assert
            Assert.Equal([false], diagnostics.SuppressionStates());
        }
    }
}
