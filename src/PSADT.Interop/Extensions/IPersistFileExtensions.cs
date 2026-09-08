using PSADT.Interop.SafeHandles;
using PSADT.Interop.Utilities;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;

/// <summary>
/// Provides extension methods for the IPersistFile interface, enabling simplified access to file-related
/// operations.
/// </summary>
/// <remarks>This static class contains methods that extend the functionality of IPersistFile, allowing
/// developers to more easily retrieve and manage file names associated with IPersistFile instances.</remarks>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Roslynator", "RCS1110:Declare type inside namespace", Justification = "Polyfills aren't meant to be part of a namespace.")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0047:Declare types in namespaces", Justification = "Polyfills aren't meant to be part of a namespace.")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0182: Avoid unused internal types.", Justification = "This is used across InternalsVisibleTo boundaries.")]
internal static class IPersistFileExtensions
{
    /// <summary>
    /// Retrieves the current file name associated with the specified IPersistFile instance.
    /// </summary>
    /// <remarks>This method allocates memory for the file name, which should be freed by the caller
    /// when no longer needed.</remarks>
    /// <param name="this">The IPersistFile instance from which to retrieve the current file name.</param>
    /// <param name="ppszFileName">An output parameter that receives the file name as a SafeCoTaskMemHandle, or null if no file name is
    /// associated.</param>
    internal static void GetCurFile(this IPersistFile @this, out SafeCoTaskMemHandle? ppszFileName)
    {
        // GetCurFile reports "no current file" with S_FALSE, handing back the object's Save As prompt
        // (e.g. "*.url") rather than a path. S_FALSE is a success, so the managed wrapper hides it.
        HRESULT hResult = @this.GetCurFile(out PWSTR ppszFileNameLocal);
        SafeCoTaskMemHandle? handle = !ppszFileNameLocal.IsNull()
            ? new(ppszFileNameLocal, ownsHandle: true)
            : null;

        // Throw on any failure, just like the CsWin32 interface would.
        if (hResult.Failed)
        {
            using (handle)
            {
                throw ExceptionUtilities.GetException(hResult);
            }
        }

        // A return value of S_FALSE means the object has no current file,
        // and what it handed back is a prompt for a Save As dialog.    
        if (hResult == HRESULT.S_FALSE)
        {
            using (handle)
            {
                ppszFileName = null;
                return;
            }
        }
        ppszFileName = handle;
    }
}
