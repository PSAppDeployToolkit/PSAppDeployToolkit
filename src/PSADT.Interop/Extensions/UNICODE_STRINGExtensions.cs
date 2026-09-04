using Windows.Win32.Foundation;

/// <summary>
/// Provides extension methods for working with the UNICODE_STRING structure.
/// </summary>
/// <remarks>This class contains methods intended to facilitate interoperability between managed code and
/// native code that uses the UNICODE_STRING structure. These methods help convert and manipulate UNICODE_STRING
/// instances in a manner suitable for .NET applications.</remarks>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Roslynator", "RCS1110:Declare type inside namespace", Justification = "Polyfills aren't meant to be part of a namespace.")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0047:Declare types in namespaces", Justification = "Polyfills aren't meant to be part of a namespace.")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0182: Avoid unused internal types.", Justification = "This is used across InternalsVisibleTo boundaries.")]
internal static class UNICODE_STRINGExtensions
{
    /// <summary>
    /// Converts the specified UNICODE_STRING structure to a managed string.
    /// </summary>
    /// <param name="unicodeString">The UNICODE_STRING structure to convert to a managed string.</param>
    /// <returns>A managed string representation of the specified UNICODE_STRING.</returns>
    internal static string ToManagedString(this UNICODE_STRING unicodeString)
    {
        return unicodeString.Buffer.ToIntPtr().ToStringUni(unicodeString.Length / sizeof(char));
    }
}
