using System;
using System.Security.Principal;
using Windows.Win32.Security;

namespace PSADT.Extensions
{
    /// <summary>
    /// Provides extension methods for working with <see cref="PSID"/> instances.
    /// </summary>
    /// <remarks>This class contains methods to facilitate the conversion of <see cref="PSID"/> objects to
    /// other types, such as <see cref="SecurityIdentifier"/>. These methods are designed to simplify common operations
    /// involving <see cref="PSID"/> instances.</remarks>
    internal static class PSIDExtensions
    {
        /// <summary>
        /// Converts the specified <see cref="PSID"/> to a <see cref="SecurityIdentifier"/>.
        /// </summary>
        /// <param name="pSid">The <see cref="PSID"/> instance to convert. Must not be null.</param>
        /// <returns>A <see cref="SecurityIdentifier"/> representing the specified <see cref="PSID"/>.</returns>
        internal static SecurityIdentifier ToSecurityIdentifier(this PSID pSid)
        {
            unsafe
            {
                return new((IntPtr)pSid.Value);
            }
        }
    }
}
