using PSADT.Utilities;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace PSADT.LibraryInterfaces
{
    /// <summary>
    /// Provides internal helper methods for validating Windows package family and full names according to Windows
    /// package identity rules.
    /// </summary>
    internal static class KernelBase
    {
        /// <summary>
        /// Verifies whether the specified package family name is valid according to Windows package family name rules.
        /// </summary>
        /// <param name="packageFamilyName">The package family name to validate. Cannot be null.</param>
        /// <returns>A WIN32_ERROR value indicating the result of the validation. Returns WIN32_ERROR.NO_ERROR if the package
        /// family name is valid.</returns>
        internal static WIN32_ERROR VerifyPackageFamilyName(string packageFamilyName)
        {
            var res = PInvoke.VerifyPackageFamilyName(packageFamilyName);
            if (res != WIN32_ERROR.NO_ERROR)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error(res);
            }
            return res;
        }

        /// <summary>
        /// Validates whether the specified package family name is a well-formed package full name according to Windows
        /// package identity rules.
        /// </summary>
        /// <param name="packageFamilyName">The package family name to validate. Cannot be null.</param>
        /// <returns>A WIN32_ERROR value indicating the result of the validation. Returns WIN32_ERROR.NO_ERROR if the package
        /// family name is valid; otherwise, returns an error code describing the validation failure.</returns>
        internal static WIN32_ERROR VerifyPackageFullName(string packageFamilyName)
        {
            var res = PInvoke.VerifyPackageFullName(packageFamilyName);
            if (res != WIN32_ERROR.NO_ERROR)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error(res);
            }
            return res;
        }
    }
}
