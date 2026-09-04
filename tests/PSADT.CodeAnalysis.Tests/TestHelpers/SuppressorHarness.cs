using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace PSADT.CodeAnalysis.Tests.TestHelpers
{
    /// <summary>
    /// Compiles a snippet in memory and runs <see cref="DataMemberPublicFieldSuppressor"/> alongside
    /// <see cref="SuppressibleDiagnosticAnalyzer"/>, handing back the reported diagnostics with their
    /// suppression state intact.
    /// </summary>
    internal static class SuppressorHarness
    {
        /// <summary>
        /// The metadata name of the attribute the suppressor looks up first.
        /// </summary>
        private const string DataMemberAttributeMetadataName = "System.Runtime.Serialization.DataMemberAttribute";

        /// <summary>
        /// The metadata name of the attribute the suppressor looks up second.
        /// </summary>
        private const string IgnoreDataMemberAttributeMetadataName = "System.Runtime.Serialization.IgnoreDataMemberAttribute";

        /// <summary>
        /// The ID Roslyn reports in place of an analyzer that threw. Left unchecked, a faulting suppressor
        /// would be indistinguishable from one that decided not to suppress anything.
        /// </summary>
        private const string AnalyzerFailureDiagnosticId = "AD0001";

        /// <summary>
        /// The assemblies every compilation here needs before anything is added for a particular test.
        /// </summary>
        /// <remarks>
        /// On .NET the serialization assembly names its type references against System.Runtime rather than
        /// against System.Private.CoreLib, so a reference set built from typeof(object) alone leaves
        /// System.Attribute out of reach and every attribute in a snippet fails to bind. No runtime type
        /// reports System.Runtime as its assembly, since that name only ever belongs to a facade, so it has
        /// to be loaded by name. .NET Framework needs nothing beyond mscorlib.
        /// </remarks>
        private static readonly ImmutableArray<MetadataReference> CoreReferences =
        [
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
#if !NETFRAMEWORK
            MetadataReference.CreateFromFile(System.Reflection.Assembly.Load("System.Runtime").Location),
#endif
        ];

        /// <summary>
        /// The core references plus the assembly declaring the two serialization attributes. Which
        /// assembly that is differs between .NET Framework and .NET, so it is located through the type
        /// rather than named.
        /// </summary>
        private static readonly ImmutableArray<MetadataReference> SerializationReferences =
        [
            .. CoreReferences,
            MetadataReference.CreateFromFile(typeof(DataMemberAttribute).Assembly.Location),
        ];

        /// <summary>
        /// Runs the analyzer and the suppressor over a snippet compiled against the serialization
        /// attributes.
        /// </summary>
        /// <param name="source">The C# source to compile.</param>
        /// <param name="anchor">The syntax to anchor the reported diagnostics to.</param>
        /// <param name="reportUnrelatedId">
        /// Whether the reported diagnostics carry an ID the suppressor knows nothing about.
        /// </param>
        /// <returns>Every diagnostic the analyzers reported, suppressed ones included.</returns>
        internal static Task<ImmutableArray<Diagnostic>> AnalyzeAsync(string source, DiagnosticAnchor anchor = DiagnosticAnchor.VisibleField, bool reportUnrelatedId = false)
        {
            return AnalyzeAsync(source, anchor, reportUnrelatedId, SerializationReferences, serializationAttributesAvailable: true);
        }

        /// <summary>
        /// Runs the analyzer and the suppressor over a snippet compiled without the assembly declaring the
        /// serialization attributes, so that the suppressor's type lookups come back empty.
        /// </summary>
        /// <param name="source">The C# source to compile.</param>
        /// <returns>Every diagnostic the analyzers reported, suppressed ones included.</returns>
        internal static Task<ImmutableArray<Diagnostic>> AnalyzeWithoutSerializationAttributesAsync(string source)
        {
            return AnalyzeAsync(source, DiagnosticAnchor.VisibleField, reportUnrelatedId: false, CoreReferences, serializationAttributesAvailable: false);
        }

        /// <summary>
        /// Runs the analyzer and the suppressor over a snippet with an explicit reference set.
        /// </summary>
        /// <param name="source">The C# source to compile.</param>
        /// <param name="anchor">The syntax to anchor the reported diagnostics to.</param>
        /// <param name="reportUnrelatedId">Whether the reported diagnostics carry an unrelated ID.</param>
        /// <param name="references">The metadata references for the compilation.</param>
        /// <param name="serializationAttributesAvailable">
        /// Whether the reference set is expected to resolve the two serialization attributes. Asserting
        /// this in both directions keeps a reference-set change from quietly turning a positive test into
        /// one that proves nothing, since an unresolved attribute stops the suppressor before it ever
        /// looks at a field.
        /// </param>
        /// <returns>Every diagnostic the analyzers reported, suppressed ones included.</returns>
        private static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(string source, DiagnosticAnchor anchor, bool reportUnrelatedId, ImmutableArray<MetadataReference> references, bool serializationAttributesAvailable)
        {
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            CSharpCompilation compilation = CSharpCompilation.Create("SuppressorTests", [CSharpSyntaxTree.ParseText(source, cancellationToken: cancellationToken)], references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            Assert.Equal(serializationAttributesAvailable, compilation.GetTypeByMetadataName(DataMemberAttributeMetadataName) is not null);
            Assert.Equal(serializationAttributesAvailable, compilation.GetTypeByMetadataName(IgnoreDataMemberAttributeMetadataName) is not null);
            if (serializationAttributesAvailable)
            {
                Assert.Empty(compilation.GetDiagnostics(cancellationToken).Where(static diagnostic => diagnostic.Severity is DiagnosticSeverity.Error));
            }

            // Suppressed diagnostics are dropped from the result by default, which would make a
            // suppression indistinguishable from an analyzer that never ran at all. Asking for them keeps
            // both halves of every assertion available: the diagnostic is still there, and it is marked.
            CompilationWithAnalyzers compilationWithAnalyzers = compilation.WithAnalyzers([new SuppressibleDiagnosticAnalyzer(anchor, reportUnrelatedId), new DataMemberPublicFieldSuppressor()], new CompilationWithAnalyzersOptions(new AnalyzerOptions([]), onAnalyzerException: null, concurrentAnalysis: false, logAnalyzerExecutionTime: false, reportSuppressedDiagnostics: true));
            ImmutableArray<Diagnostic> diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync(cancellationToken).ConfigureAwait(false);
            Assert.Empty(diagnostics.Where(static diagnostic => diagnostic.Id.Equals(AnalyzerFailureDiagnosticId, StringComparison.Ordinal)));
            return diagnostics;
        }

        /// <summary>
        /// Gets the suppression state of each reported diagnostic in source order, so a test can assert on
        /// every diagnostic a snippet produced rather than only the first.
        /// </summary>
        /// <param name="diagnostics">The reported diagnostics.</param>
        /// <returns>One flag per diagnostic, in source order.</returns>
        internal static bool[] SuppressionStates(this ImmutableArray<Diagnostic> diagnostics)
        {
            return [.. diagnostics.OrderBy(static diagnostic => diagnostic.Location.SourceSpan.Start).Select(static diagnostic => diagnostic.IsSuppressed)];
        }
    }
}
