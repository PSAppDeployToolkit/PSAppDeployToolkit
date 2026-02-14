using PSADT.LibraryInterfaces.Utilities;
using Windows.Win32.Foundation;

namespace PSADT.LibraryInterfaces.Extensions
{
    /// <summary>
    /// Provides extension methods for handling WIN32 error codes, enabling streamlined error management.
    /// </summary>
    /// <remarks>This class contains methods that facilitate error handling by evaluating WIN32 error codes
    /// and throwing exceptions when necessary.</remarks>
    internal static class WIN32_ERRORExtensions
    {
        /// <summary>
        /// Throws an exception if the specified WIN32 error code indicates a failure; otherwise, returns the error
        /// code.
        /// </summary>
        /// <remarks>This method is useful for error handling in scenarios where WIN32 error codes are
        /// returned, allowing for immediate exception throwing based on the error state.</remarks>
        /// <param name="win32Error">The WIN32 error code to evaluate. If the value is not equal to WIN32_ERROR.ERROR_SUCCESS, an exception is
        /// thrown.</param>
        /// <returns>The original WIN32 error code if it indicates success; otherwise, an exception is thrown.</returns>
        internal static WIN32_ERROR ThrowOnFailure(this WIN32_ERROR win32Error)
        {
            return win32Error != WIN32_ERROR.ERROR_SUCCESS ? throw ExceptionUtilities.GetException(win32Error) : win32Error;
        }
    }
}
