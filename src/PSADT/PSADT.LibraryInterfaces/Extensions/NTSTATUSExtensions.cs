using PSADT.LibraryInterfaces.Utilities;
using Windows.Win32.Foundation;

namespace PSADT.LibraryInterfaces.Extensions
{
    /// <summary>
    /// Provides extension methods for the NTSTATUS enumeration to enhance error handling.
    /// </summary>
    /// <remarks>This class contains methods that facilitate the evaluation of NTSTATUS values, particularly
    /// in error handling scenarios.</remarks>
    internal static class NTSTATUSExtensions
    {
        /// <summary>
        /// Throws an exception if the specified NTSTATUS value indicates a failure; otherwise, returns the original
        /// status.
        /// </summary>
        /// <remarks>This method is typically used to enforce error handling in scenarios where NTSTATUS
        /// values are returned from system calls or APIs.</remarks>
        /// <param name="status">The NTSTATUS value to evaluate for success or failure.</param>
        /// <returns>The original NTSTATUS value if it indicates success; otherwise, an exception is thrown.</returns>
        internal static NTSTATUS ThrowOnFailure(this NTSTATUS status)
        {
            return status != NTSTATUS.STATUS_SUCCESS ? throw ExceptionUtilities.GetException(status) : status;
        }
    }
}
