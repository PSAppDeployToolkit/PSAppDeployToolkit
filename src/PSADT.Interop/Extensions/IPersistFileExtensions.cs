using System;
using System.Runtime.InteropServices;
using PSADT.Interop.SafeHandles;
using PSADT.Interop.Utilities;
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
        // GetCurFile reports "no current file" by answering S_FALSE, not by handing back nothing. In that
        // case what it does hand back is the prompt the object would show in a Save As dialog - "*.url"
        // for an internet shortcut, though the documentation is explicit that the string is the object's
        // own choice and gives "*.txt" as its example - which is not a path and must not be treated as
        // one. S_FALSE is a success code, so the generated wrapper returns normally and the distinction
        // is invisible through it as CsWin32 by default manages the HRESULT for us instead of returning it.
        HRESULT hResult = ((IPersistFilePreserveSig)@this).GetCurrentFile(out PWSTR ppszFileNameLocal);
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
        if (hResult.Value == S_FALSE)
        {
            using (handle)
            {
                ppszFileName = null;
                return;
            }
        }
        ppszFileName = handle;
    }

    /// <summary>
    /// The result GetCurFile answers with when the object has no current file. CsWin32 generates no
    /// constant for it, and it is a success code rather than a failure one.
    /// </summary>
    private const int S_FALSE = 1;

    /// <summary>
    /// The same interface as <see cref="IPersistFile"/>, declared so its results are returned rather than
    /// translated into exceptions.
    /// </summary>
    /// <remarks>
    /// Only <see cref="GetCurFile"/> is called through this. The other members are present because a COM
    /// interface is declared by its layout: each one occupies a slot in the virtual method table, and
    /// omitting any of them would place the ones that follow at the wrong offset.
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "SYSLIB1096:Convert to 'GeneratedComInterface'", Justification = "This has to stay a ComImport interface: it is cast to from the CsWin32 interface for the same identifier, which is itself ComImport, and the two forms cannot be mixed.")]
    [ComImport]
    [Guid("0000010b-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPersistFilePreserveSig
    {
        /// <summary>
        /// Retrieves the class identifier of the object. Present for layout only.
        /// </summary>
        /// <param name="pClassID">Receives the class identifier.</param>
        /// <returns>The result of the call.</returns>
        [PreserveSig]
        HRESULT GetClassID(out Guid pClassID);

        /// <summary>
        /// Reports whether the object has changed since it was last saved. Present for layout only.
        /// </summary>
        /// <returns>The result of the call.</returns>
        [PreserveSig]
        HRESULT IsDirty();

        /// <summary>
        /// Loads the object from the named file. Present for layout only.
        /// </summary>
        /// <param name="pszFileName">The file to load from.</param>
        /// <param name="dwMode">The access mode to open it with.</param>
        /// <returns>The result of the call.</returns>
        [PreserveSig]
        HRESULT Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, uint dwMode);

        /// <summary>
        /// Saves the object to the named file. Present for layout only.
        /// </summary>
        /// <param name="pszFileName">The file to save to, or null to save to the current one.</param>
        /// <param name="fRemember">Whether the named file becomes the current one.</param>
        /// <returns>The result of the call.</returns>
        [PreserveSig]
        HRESULT Save([MarshalAs(UnmanagedType.LPWStr)] string? pszFileName, [MarshalAs(UnmanagedType.Bool)] bool fRemember);

        /// <summary>
        /// Notifies the object that a save has finished. Present for layout only.
        /// </summary>
        /// <param name="pszFileName">The file that was saved to.</param>
        /// <returns>The result of the call.</returns>
        [PreserveSig]
        HRESULT SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string? pszFileName);

        /// <summary>
        /// Retrieves the object's current file, or the prompt it would offer if it has none.
        /// </summary>
        /// <param name="ppszFileName">Receives the file name, allocated by the callee.</param>
        /// <returns>S_OK if the name is a current file, or S_FALSE if it is a Save As prompt.</returns>
        /// <remarks>Named differently from the extension above only to avoid shadowing it; COM binds this
        /// by its position in the virtual method table, not by its name.</remarks>
        [PreserveSig]
        HRESULT GetCurrentFile(out PWSTR ppszFileName);
    }
}
