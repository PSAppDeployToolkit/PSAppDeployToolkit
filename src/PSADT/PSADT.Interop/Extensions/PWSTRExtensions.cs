using Windows.Win32.Foundation;

namespace PSADT.Interop.Extensions
{
    /// <summary>
    /// Provides extension methods for working with PWSTR values.
    /// </summary>
    internal static class PWSTRExtensions
    {
        /// <summary>
        /// Determines whether the underlying string pointer is null.
        /// </summary>
        /// <remarks>Use this method to validate whether a PWSTR points to a valid string before
        /// performing operations. This can help prevent null reference errors when working with
        /// unmanaged strings.</remarks>
        /// <param name="pwstr">The PWSTR instance to evaluate for a null pointer.</param>
        /// <returns><see langword="true"/> if the underlying pointer is null; otherwise, <see langword="false"/>.</returns>
        internal static bool IsNull(this PWSTR pwstr)
        {
            unsafe
            {
                return pwstr.Value == null;
            }
        }

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
