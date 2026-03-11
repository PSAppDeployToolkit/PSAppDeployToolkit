using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Windows.Win32.UI.Shell;

namespace PSADT.Interop.ComTypes
{
    /// <summary>
    /// Provides Unicode methods for setting and retrieving URLs in an Internet shortcut.
    /// </summary>
    [ComImport, Guid("CABB0DA0-DA57-11CF-9974-0020AFD79762")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [CoClass(typeof(InternetShortcut))]
    [SuppressMessage("Interoperability", "SYSLIB1096:Convert to 'GeneratedComInterface'", Justification = "GeneratedComInterfaceAttribute is not available in .NET Framework 4.7.2.")]
    internal interface IUniformResourceLocatorW
    {
        /// <summary>
        /// Sets the URL of the Internet shortcut.
        /// </summary>
        /// <param name="pcszURL">The URL string to set.</param>
        /// <param name="dwInFlags">Flags that control URL validation and canonicalization.</param>
        void SetURL([MarshalAs(UnmanagedType.LPWStr)] string pcszURL, IURL_SETURL_FLAGS dwInFlags);

        /// <summary>
        /// Gets the URL of the Internet shortcut.
        /// </summary>
        /// <param name="ppszURL">Receives the URL string. The caller must free this memory using CoTaskMemFree.</param>
        void GetURL([MarshalAs(UnmanagedType.LPWStr)] out string? ppszURL);

        /// <summary>
        /// Invokes a command on the URL (e.g., opens the URL in a browser).
        /// </summary>
        /// <param name="purlici">A reference to a <see cref="URLINVOKECOMMANDINFOW"/> structure specifying the command to invoke.</param>
        void InvokeCommand(in URLINVOKECOMMANDINFOW purlici);
    }
}
