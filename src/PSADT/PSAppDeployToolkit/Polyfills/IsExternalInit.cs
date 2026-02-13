using System.ComponentModel;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace System.Runtime.CompilerServices
#pragma warning restore IDE0130
{
    /// <summary>
    /// Polyfill for init-only setters on .NET Framework 4.7.2.
    /// This class is used by the compiler to mark init-only property setters.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class IsExternalInit
    {
    }
}
