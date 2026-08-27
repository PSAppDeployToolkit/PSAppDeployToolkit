using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace PSADT.CodeAnalysis.Tests.TestHelpers
{
    /// <summary>
    /// Reports a warning carrying CA1051's diagnostic ID so that
    /// <see cref="DataMemberPublicFieldSuppressor"/> has something to suppress, or one carrying an
    /// unrelated ID to show that the suppressor leaves it alone.
    /// </summary>
    /// <remarks>
    /// A suppressor only ever sees diagnostics reported by analyzers running alongside it, so something
    /// has to report CA1051 for these tests to have any input at all. The real rule ships inside the .NET
    /// SDK rather than as a package this repository restores, and its own triggering conditions are not
    /// what is under test here. Standing in for it also keeps the input exact: each test decides which
    /// diagnostics exist and where they point, which is the whole of what the suppressor reacts to.
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class SuppressibleDiagnosticAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>
        /// The ID the suppressor is configured to suppress, which is CA1051: "Do not declare visible
        /// instance fields".
        /// </summary>
        internal const string SuppressibleDiagnosticId = "CA1051";

        /// <summary>
        /// An ID the suppressor knows nothing about.
        /// </summary>
        internal const string UnrelatedDiagnosticId = "TEST0001";

        /// <summary>
        /// The descriptor for CA1051. The severity must stay below error, because Roslyn does not offer
        /// errors to suppressors.
        /// </summary>
        private static readonly DiagnosticDescriptor SuppressibleDescriptor = new(SuppressibleDiagnosticId, "Do not declare visible instance fields", "Do not declare visible instance fields", "Design", DiagnosticSeverity.Warning, isEnabledByDefault: true);

        /// <summary>
        /// The descriptor for the unrelated diagnostic, identical to the other one apart from its ID.
        /// </summary>
        private static readonly DiagnosticDescriptor UnrelatedDescriptor = new(UnrelatedDiagnosticId, "Unrelated test diagnostic", "Unrelated test diagnostic", "Design", DiagnosticSeverity.Warning, isEnabledByDefault: true);

        /// <summary>
        /// The syntax the diagnostic is anchored to.
        /// </summary>
        private readonly DiagnosticAnchor _anchor;

        /// <summary>
        /// The descriptor to report with.
        /// </summary>
        private readonly DiagnosticDescriptor _descriptor;

        /// <summary>
        /// Creates an analyzer that reports at the given anchor.
        /// </summary>
        /// <param name="anchor">The syntax to anchor the diagnostic to.</param>
        /// <param name="reportUnrelatedId">
        /// Whether to report <see cref="UnrelatedDiagnosticId"/> instead of
        /// <see cref="SuppressibleDiagnosticId"/>.
        /// </param>
        internal SuppressibleDiagnosticAnalyzer(DiagnosticAnchor anchor, bool reportUnrelatedId = false)
        {
            _anchor = anchor;
            _descriptor = reportUnrelatedId ? UnrelatedDescriptor : SuppressibleDescriptor;
        }

        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_descriptor];

        /// <inheritdoc />
        public override void Initialize(AnalysisContext context)
        {
            // Nothing in the test compilations is generated code, but opting in explicitly keeps the
            // reported set from depending on how Roslyn classifies a tree that has no file path.
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            switch (_anchor)
            {
                case DiagnosticAnchor.VisibleField:
                    {
                        context.RegisterSyntaxNodeAction(AnalyzeFieldDeclaration, SyntaxKind.FieldDeclaration);
                        break;
                    }

                case DiagnosticAnchor.LocalVariable:
                    {
                        context.RegisterSyntaxNodeAction(AnalyzeLocalDeclaration, SyntaxKind.LocalDeclarationStatement);
                        break;
                    }

                case DiagnosticAnchor.MethodName:
                    {
                        context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
                        break;
                    }

                default:
                    return;
            }
        }

        /// <summary>
        /// Reports on each declarator of a public or protected instance field, at the field identifier.
        /// This is where CA1051 reports, because a field symbol's location is its identifier token.
        /// </summary>
        /// <param name="context">The syntax node analysis context.</param>
        private void AnalyzeFieldDeclaration(SyntaxNodeAnalysisContext context)
        {
            FieldDeclarationSyntax field = (FieldDeclarationSyntax)context.Node;
            if (!IsVisibleInstanceField(field))
            {
                return;
            }
            foreach (VariableDeclaratorSyntax declarator in field.Declaration.Variables)
            {
                context.ReportDiagnostic(Diagnostic.Create(_descriptor, declarator.Identifier.GetLocation()));
            }
        }

        /// <summary>
        /// Reports on each declarator of a local variable declaration, at the local's identifier. That is
        /// still a variable declarator, but it declares a local rather than a field.
        /// </summary>
        /// <param name="context">The syntax node analysis context.</param>
        private void AnalyzeLocalDeclaration(SyntaxNodeAnalysisContext context)
        {
            foreach (VariableDeclaratorSyntax declarator in ((LocalDeclarationStatementSyntax)context.Node).Declaration.Variables)
            {
                context.ReportDiagnostic(Diagnostic.Create(_descriptor, declarator.Identifier.GetLocation()));
            }
        }

        /// <summary>
        /// Reports at a method's identifier, which has no variable declarator anywhere above it.
        /// </summary>
        /// <param name="context">The syntax node analysis context.</param>
        private void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
        {
            context.ReportDiagnostic(Diagnostic.Create(_descriptor, ((MethodDeclarationSyntax)context.Node).Identifier.GetLocation()));
        }

        /// <summary>
        /// Determines whether a field declaration is one CA1051 would report on, which is a field that is
        /// public or protected and neither static nor constant.
        /// </summary>
        /// <param name="field">The field declaration to test.</param>
        /// <returns><see langword="true"/> if the field is a visible instance field.</returns>
        private static bool IsVisibleInstanceField(FieldDeclarationSyntax field)
        {
            return (field.Modifiers.Any(SyntaxKind.PublicKeyword) || field.Modifiers.Any(SyntaxKind.ProtectedKeyword)) && !field.Modifiers.Any(SyntaxKind.StaticKeyword) && !field.Modifiers.Any(SyntaxKind.ConstKeyword);
        }
    }
}
