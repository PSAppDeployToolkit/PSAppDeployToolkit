using PSADT.Interop.SafeHandles;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;

namespace PSADT.Interop.Extensions
{
    /// <summary>
    /// Provides extension methods for the IPersistFile interface, enabling simplified access to file-related
    /// operations.
    /// </summary>
    /// <remarks>This static class contains methods that extend the functionality of IPersistFile, allowing
    /// developers to more easily retrieve and manage file names associated with IPersistFile instances.</remarks>
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
            @this.GetCurFile(out PWSTR ppszFileNameLocal);
            ppszFileName = !ppszFileNameLocal.IsNull()
                ? new(ppszFileNameLocal, true)
                : null;
        }
    }
}
