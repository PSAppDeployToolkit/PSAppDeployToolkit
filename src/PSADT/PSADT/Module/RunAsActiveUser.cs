using System;
using System.Security.Principal;
using PSADT.TerminalServices;

namespace PSADT.Module
{
    /// <summary>
    /// Represents a user running under a specific Windows NT account context, including associated security details.
    /// </summary>
    /// <remarks>The <see cref="RunAsActiveUser"/> class encapsulates details about a user, including their NT
    /// account, security identifier (SID), username, and domain name. This class is useful for scenarios where
    /// operations need to be performed under the context of a specific user.</remarks>
    public sealed record RunAsActiveUser
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RunAsActiveUser"/> class with the specified account details.
        /// </summary>
        /// <param name="nTAccount">The NT account associated with the user. Cannot be <see langword="null"/>.</param>
        /// <param name="sID">The security identifier (SID) for the user. Cannot be <see langword="null"/>.</param>
        /// <param name="sessionId">The session ID of the user.</param>
        /// <exception cref="ArgumentNullException">Thrown if any of the parameters are <see langword="null"/>.</exception>
        public RunAsActiveUser(NTAccount nTAccount, SecurityIdentifier sID, uint sessionId, bool? isLocalAdmin)
        {
            NTAccount = nTAccount ?? throw new ArgumentNullException(nameof(nTAccount));
            SID = sID ?? throw new ArgumentNullException(nameof(sID));
            string[] accountParts = nTAccount.Value.Split('\\');
            UserName = accountParts[1]; DomainName = accountParts[0];
            SessionId = sessionId;
            IsLocalAdmin = isLocalAdmin;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RunAsActiveUser"/> class using the specified session
        /// information.
        /// </summary>
        /// <remarks>This constructor extracts the NT account, SID, and session ID from the provided
        /// <paramref name="session"/> and initializes the <see cref="RunAsActiveUser"/> instance with these
        /// values.</remarks>
        /// <param name="session">The session information containing the NT account, security identifier (SID), and session ID of the active
        /// user.</param>
        public RunAsActiveUser(SessionInfo session) : this(session.NTAccount, session.SID, session.SessionId, session.IsLocalAdmin)
        {
        }

        /// <summary>
        /// Represents a Windows NT account.
        /// </summary>
        /// <remarks>This field holds an instance of the <see cref="System.Security.Principal.NTAccount"/>
        /// class, which encapsulates a Windows NT account name. It is used to identify a user or group in a Windows
        /// environment.</remarks>
        public readonly NTAccount NTAccount;

        /// <summary>
        /// Represents the security identifier (SID) associated with the current object.
        /// </summary>
        /// <remarks>A security identifier (SID) is a unique value used to identify a user, group, or
        /// computer account in Windows security. This field is read-only and provides access to the SID associated with
        /// the object, which can be used for security-related operations.</remarks>
        public readonly SecurityIdentifier SID;

        /// <summary>
        /// Gets the username associated with the user.
        /// </summary>
        public readonly string UserName;

        /// <summary>
        /// Represents the domain name associated with the current context.
        /// </summary>
        public readonly string DomainName;

        /// <summary>
        /// Represents the session ID of the user.
        /// </summary>
        public readonly uint SessionId;

        /// <summary>
        /// Indicates whether the current user has local administrator privileges.
        /// </summary>
        public readonly bool? IsLocalAdmin;
    }
}
