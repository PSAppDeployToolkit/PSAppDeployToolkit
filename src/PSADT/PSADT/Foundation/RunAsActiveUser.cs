using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Principal;
using PSADT.AccountManagement;
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
        /// Gets the active user session associated with the caller, or the most recent active user session if the
        /// caller's session is not active.
        /// </summary>
        /// <remarks>If the caller's session is active, it is prioritized; otherwise, the method searches
        /// for the most recent active user session based on logon time.</remarks>
        /// <param name="sessionInfo">An optional list of session information to filter through. If not provided, the method retrieves the current
        /// session information.</param>
        /// <returns>A RunAsActiveUser object representing the active user session for the caller, or null if no active user
        /// session is found.</returns>
        public static RunAsActiveUser? Get(IReadOnlyList<SessionInfo>? sessionInfo = null)
        {
            // Get all active sessions for subsequent filtration.
            sessionInfo ??= SessionInfo.Get();

            // Determine the account that will be used to execute client/server commands in the user's context.
            // Favour the caller's session if it's found and is currently an active user session on the device.
            return sessionInfo.FirstOrDefault(static s => (s.SID == AccountUtilities.CallerSid || s.SessionId == AccountUtilities.CallerSessionId) && s.IsActiveUserSession) is not SessionInfo callerSession
                ? sessionInfo.Where(static s => s.IsActiveUserSession).OrderByDescending(static s => s.LogonTime).FirstOrDefault()?.ToRunAsActiveUser()
                : callerSession.ToRunAsActiveUser();
        }

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
            ArgumentException.ThrowIfNullOrWhiteSpace(nTAccount?.Value, nameof(nTAccount));
            ArgumentException.ThrowIfNullOrWhiteSpace(sID?.Value, nameof(sID));
            NTAccountValue = nTAccount.Value;
            SIDValue = sID.Value;
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
        public RunAsActiveUser(SessionInfo session) : this((session ?? throw new ArgumentNullException(nameof(session))).NTAccount, session.SID, session.SessionId, session.IsLocalAdmin)
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
        public string UserName
        {
            get
            {
                int divider = NTAccount.Value.IndexOf('\\');
                return divider != -1
                    ? NTAccount.Value.Substring(divider + 1)
                    : NTAccountValue;
            }
        }

        /// <summary>
        /// Represents the domain name associated with the current context.
        /// </summary>
        [IgnoreDataMember]
        public string? DomainName
        {
            get
            {
                int divider = NTAccount.Value.IndexOf('\\');
                return divider != -1
                    ? NTAccount.Value.Substring(0, divider)
                    : null;
            }
        }

        /// <summary>
        /// Represents the session ID of the user.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly uint SessionId;

        /// <summary>
        /// Indicates whether the current user has local administrator privileges.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly bool? IsLocalAdmin;

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
