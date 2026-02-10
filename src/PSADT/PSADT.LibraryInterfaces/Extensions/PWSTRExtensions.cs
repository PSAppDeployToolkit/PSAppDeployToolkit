using Windows.Win32.Foundation;

namespace PSADT.LibraryInterfaces.Extensions
{
    /// <summary>
    /// Provides extension methods for working with PWSTR values.
    /// </summary>
    internal static class PWSTRExtensions
    {
        /// <summary>
        /// Converts a PWSTR value to a platform-specific pointer type.
        /// </summary>
        /// <param name="pwstr">The PWSTR value to convert to an IntPtr.</param>
        /// <returns>A nint (IntPtr) representing the pointer value of the specified PWSTR.</returns>
        internal static nint ToIntPtr(this PWSTR pwstr)
        {
            unsafe
            {
                return (nint)pwstr.Value;
            }
        }
    }
}
