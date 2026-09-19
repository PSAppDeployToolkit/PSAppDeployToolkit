using System.Security.Principal;
using Windows.Win32.Foundation;
using Windows.Win32.Security.Authentication.Identity;

namespace PSADT.Security
{
    /// <summary>
    /// Captures copied LSA metadata without retaining pointers into an LSA buffer.
    /// </summary>
    /// <param name="AuthenticationId">The logon authentication identifier.</param>
    /// <param name="SessionId">The LSA session identifier.</param>
    /// <param name="Sid">The logon owner SID.</param>
    /// <param name="LogonType">The native logon type.</param>
    /// <param name="UserFlags">The LSA provenance flags.</param>
    /// <param name="LogonTime">The LSA logon time.</param>
    internal sealed record class ProcessTokenLogon(in LUID AuthenticationId, uint SessionId, SecurityIdentifier? Sid, SECURITY_LOGON_TYPE LogonType, Interop.MSV_SUB_AUTHENTICATION_FILTER UserFlags, long LogonTime)
    {
        /// <summary>
        /// The authentication identifier.
        /// </summary>
        internal readonly LUID AuthenticationId = AuthenticationId;

        /// <summary>
        /// The LSA session identifier.
        /// </summary>
        internal readonly uint SessionId = SessionId;

        /// <summary>
        /// The logon owner SID.
        /// </summary>
        internal readonly SecurityIdentifier? Sid = Sid;

        /// <summary>
        /// The native logon type.
        /// </summary>
        internal readonly SECURITY_LOGON_TYPE LogonType = LogonType;

        /// <summary>
        /// The LSA provenance flags.
        /// </summary>
        internal readonly Interop.MSV_SUB_AUTHENTICATION_FILTER UserFlags = UserFlags;

        /// <summary>
        /// The LSA logon time.
        /// </summary>
        internal readonly long LogonTime = LogonTime;
    }
}
