namespace PSADT.CodeAnalysis.Tests.TestHelpers
{
    /// <summary>
    /// Selects the syntax that <see cref="SuppressibleDiagnosticAnalyzer"/> anchors its diagnostic to. The
    /// suppressor decides what to do purely from the reported location, so moving the anchor is how the
    /// tests reach its guard clauses.
    /// </summary>
    internal enum DiagnosticAnchor
    {
        /// <summary>
        /// The identifier of every public or protected instance field, which is where CA1051 reports.
        /// </summary>
        VisibleField = 0,

        /// <summary>
        /// The identifier of every local variable. That is still a variable declarator, but it declares a
        /// local rather than a field.
        /// </summary>
        LocalVariable = 1,

        /// <summary>
        /// The identifier of every method declaration, which has no variable declarator anywhere above it.
        /// </summary>
        MethodName = 2,
    }
}
