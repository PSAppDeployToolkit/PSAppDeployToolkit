using PSADT.Interop.SafeHandles;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;

/// <summary>
/// Provides extension methods for the IUniformResourceLocatorW interface to facilitate working with uniform
/// resource locator (URL) objects.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Roslynator", "RCS1110:Declare type inside namespace", Justification = "Polyfills aren't meant to be part of a namespace.")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0047:Declare types in namespaces", Justification = "Polyfills aren't meant to be part of a namespace.")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0182: Avoid unused internal types.", Justification = "This is used across InternalsVisibleTo boundaries.")]
internal static class IUniformResourceLocatorWExtensions
{
    /// <summary>
    /// Retrieves the URL associated with the specified uniform resource locator (URL) object.
    /// </summary>
    /// <param name="this">The instance of the <see cref="IUniformResourceLocatorW"/> interface from which to retrieve the URL.</param>
    /// <param name="ppszURL">When this method returns, contains a <see cref="SafeCoTaskMemHandle"/> representing the URL string, or <see
    /// langword="null"/> if no URL is available. The caller is responsible for disposing the handle when it is no
    /// longer needed.</param>
    internal static void GetURL(this IUniformResourceLocatorW @this, out SafeCoTaskMemHandle? ppszURL)
    {
        @this.GetURL(out PWSTR ppszURLLocal);
        ppszURL = !ppszURLLocal.IsNull()
            ? new(ppszURLLocal, ownsHandle: true)
            : null;
    }
}
