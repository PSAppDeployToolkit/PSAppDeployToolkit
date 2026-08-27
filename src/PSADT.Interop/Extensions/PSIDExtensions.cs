using System.Security.Principal;
using Windows.Win32.Security;

/// <summary>
/// Provides extension methods for working with <see cref="PSID"/> instances.
/// </summary>
/// <remarks>This class contains methods to facilitate the conversion of <see cref="PSID"/> objects to
/// other types, such as <see cref="SecurityIdentifier"/>. These methods are designed to simplify common operations
/// involving <see cref="PSID"/> instances.</remarks>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Roslynator", "RCS1110:Declare type inside namespace", Justification = "Polyfills aren't meant to be part of a namespace.")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0047:Declare types in namespaces", Justification = "Polyfills aren't meant to be part of a namespace.")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0182: Avoid unused internal types.", Justification = "This is used across InternalsVisibleTo boundaries.")]
internal static class PSIDExtensions
{
    /// <summary>
    /// Converts the specified <see cref="PSID"/> to a <see cref="SecurityIdentifier"/>.
    /// </summary>
    /// <param name="pSid">The <see cref="PSID"/> instance to convert. Must not be null.</param>
    /// <returns>A <see cref="SecurityIdentifier"/> representing the specified <see cref="PSID"/>.</returns>
    internal static SecurityIdentifier ToSecurityIdentifier(this PSID pSid)
    {
        return new((nint)pSid);
    }
}
