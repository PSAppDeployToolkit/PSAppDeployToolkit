using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace PSADT.CodeAnalysis
{
    /// <summary>
    /// Suppresses CA1051 diagnostics for visible fields decorated with <c>DataMemberAttribute</c> or <c>IgnoreDataMemberAttribute</c>.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class DataMemberPublicFieldSuppressor : DiagnosticSuppressor
    {
        /// <summary>
        /// The ID of the diagnostic to suppress, which is CA1051: "Do not declare visible instance fields".
        /// </summary>
        private const string suppressedDiagnosticId = "CA1051";

        /// <summary>
        /// The suppression descriptor for CA1051 diagnostics, with a justification that visible fields decorated with serialization attributes are intentional.
        /// </summary>
        private static readonly SuppressionDescriptor SuppressionDescriptor = new("PSADTDSCA1051", suppressedDiagnosticId, "Visible fields decorated with serialization attributes are intentional.");

        /// <inheritdoc />
        public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions => [SuppressionDescriptor];

        /// <inheritdoc />
        public override void ReportSuppressions(SuppressionAnalysisContext context)
        {
            if (context.Compilation.GetTypeByMetadataName("System.Runtime.Serialization.DataMemberAttribute") is not INamedTypeSymbol dataMemberAttribute)
            {
                return;
            }
            if (context.Compilation.GetTypeByMetadataName("System.Runtime.Serialization.IgnoreDataMemberAttribute") is not INamedTypeSymbol ignoreDataMemberAttribute)
            {
                return;
            }
            foreach (Diagnostic diagnostic in context.ReportedDiagnostics)
            {
                if (!diagnostic.Id.Equals(suppressedDiagnosticId, StringComparison.Ordinal))
                {
                    continue;
                }
                if (diagnostic.Location.SourceTree is not SyntaxTree sourceTree)
                {
                    continue;
                }
                SyntaxNode node = sourceTree.GetRoot(context.CancellationToken).FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
                if ((node as VariableDeclaratorSyntax ?? node.FirstAncestorOrSelf<VariableDeclaratorSyntax>()) is not VariableDeclaratorSyntax variableDeclarator)
                {
                    continue;
                }
                if (context.GetSemanticModel(sourceTree).GetDeclaredSymbol(variableDeclarator, context.CancellationToken) is not IFieldSymbol fieldSymbol)
                {
                    continue;
                }
                if (fieldSymbol.GetAttributes().Any(attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, dataMemberAttribute) || SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, ignoreDataMemberAttribute)))
                {
                    context.ReportSuppression(Suppression.Create(SuppressionDescriptor, diagnostic));
                }
            }
        }
    }
}
