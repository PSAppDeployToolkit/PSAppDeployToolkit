using System;
using System.Runtime.Serialization;
using System.Security.Principal;
using PSADT.TerminalServices;

namespace PSADT.Foundation
{
    /// <summary>
    /// Represents a user running under a specific Windows NT account context, including associated security details.
    /// </summary>
    /// <remarks>The <see cref="RunAsActiveUser"/> class encapsulates details about a user, including their NT
    /// account, security identifier (SID), username, and domain name. This class is useful for scenarios where
    /// operations need to be performed under the context of a specific user.</remarks>
    [DataContract]
    public sealed record RunAsActiveUser
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RunAsActiveUser"/> class with the specified account details.
        /// </summary>
        /// <param name="nTAccount">The NT account associated with the user. Cannot be <see langword="null"/>.</param>
        /// <param name="sID">The security identifier (SID) for the user. Cannot be <see langword="null"/>.</param>
        /// <param name="sessionId">The session ID of the user.</param>
        /// <param name="isLocalAdmin">Indicates whether the user has local administrator privileges. Can be <see langword="null"/> if unknown.</param>
        /// <exception cref="ArgumentNullException">Thrown if any of the parameters are <see langword="null"/>.</exception>
        public RunAsActiveUser(NTAccount nTAccount, SecurityIdentifier sID, uint sessionId, bool? isLocalAdmin)
        {
            if (nTAccount?.Value is not string ntAccountValue || string.IsNullOrWhiteSpace(ntAccountValue))
            {
                throw new ArgumentNullException(nameof(nTAccount));
            }
            if (sID?.Value is not string sidValue || string.IsNullOrWhiteSpace(sidValue))
            {
                throw new ArgumentNullException(nameof(sID));
            }
            NTAccountValue = ntAccountValue;
            SIDValue = sidValue;
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
        public RunAsActiveUser(SessionInfo session) : this(session?.NTAccount ?? throw new ArgumentNullException(nameof(session)), session.SID, session.SessionId, session.IsLocalAdmin)
        {
        }

        /// <summary>
        /// Represents a Windows NT account.
        /// </summary>
        /// <remarks>This field holds an instance of the <see cref="System.Security.Principal.NTAccount"/>
        /// class, which encapsulates a Windows NT account name. It is used to identify a user or group in a Windows
        /// environment.</remarks>
        [IgnoreDataMember]
        public NTAccount NTAccount => new(NTAccountValue);

        /// <summary>
        /// Represents the security identifier (SID) associated with the current object.
        /// </summary>
        /// <remarks>A security identifier (SID) is a unique value used to identify a user, group, or
        /// computer account in Windows security. This field is read-only and provides access to the SID associated with
        /// the object, which can be used for security-related operations.</remarks>
        [IgnoreDataMember]
        public SecurityIdentifier SID => new(SIDValue);

        /// <summary>
        /// Gets the username associated with the user.
        /// </summary>
        [IgnoreDataMember]
        public string UserName => NTAccountValue.Contains("\\") ? NTAccount.Value.Substring(NTAccount.Value.IndexOf('\\') + 1) : NTAccountValue;

        /// <summary>
        /// Represents the domain name associated with the current context.
        /// </summary>
        [IgnoreDataMember]
        public string? DomainName => NTAccountValue.Contains("\\") ? NTAccount.Value.Substring(0, NTAccount.Value.IndexOf('\\')) : null;

        /// <summary>
        /// Represents the session ID of the user.
        /// </summary>
        [DataMember]
        public uint SessionId { get; private set; }

        /// <summary>
        /// Indicates whether the current user has local administrator privileges.
        /// </summary>
        [DataMember]
        public bool? IsLocalAdmin { get; private set; }

        /// <summary>
        /// Gets the NT account name string for serialization.
        /// </summary>
        [DataMember]
        private readonly string NTAccountValue;

        /// <summary>
        /// Gets the SID string for serialization.
        /// </summary>
        [DataMember]
        private readonly string SIDValue;
    }
}
