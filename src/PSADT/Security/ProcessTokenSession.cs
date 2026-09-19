using System.Security.Principal;

namespace PSADT.Security
{
    /// <summary>
    /// Captures the independently resolved desktop owner and session logon time.
    /// </summary>
    /// <param name="SessionId">The desktop session identifier.</param>
    /// <param name="Sid">The resolved owner SID.</param>
    /// <param name="LogonTime">The WTS logon time used to detect session replacement.</param>
    internal sealed record class ProcessTokenSession(uint SessionId, SecurityIdentifier Sid, long LogonTime)
    {
        /// <summary>
        /// The desktop session identifier.
        /// </summary>
        internal readonly uint SessionId = SessionId;

        /// <summary>
        /// The resolved owner SID.
        /// </summary>
        internal readonly SecurityIdentifier Sid = Sid;

        /// <summary>
        /// The WTS logon time.
        /// </summary>
        internal readonly long LogonTime = LogonTime;
    }
}
