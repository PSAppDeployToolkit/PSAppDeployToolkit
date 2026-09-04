using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using PSADT.AccountManagement;
using PSADT.Foundation;
using PSADT.Security;
using Xunit;

namespace PSADT.Tests.AccountManagement
{
    /// <summary>
    /// Tests the facts cached about whoever is running the process.
    /// </summary>
    /// <remarks>
    /// These are read once in a static constructor and consulted everywhere afterwards, so what matters is
    /// that each one agrees with the same fact obtained independently - from the framework, from the
    /// process, or from another member of this same type. A machine-specific value is never asserted, so
    /// the file says the same thing whichever account happens to be running it.
    /// </remarks>
    public sealed class AccountUtilitiesTests
    {
        /// <summary>
        /// Verifies that the account and its group memberships match what the framework reports for the
        /// same identity.
        /// </summary>
        [Fact]
        public void Caller_MatchesTheCurrentWindowsIdentity()
        {
            // Arrange
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();

            // Assert
            Assert.Equal(identity.User, AccountUtilities.CallerSid);
            Assert.Equal(identity.Name, AccountUtilities.CallerUsername.Value);
            Assert.Equal(new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator), AccountUtilities.CallerIsAdmin);
            Assert.NotNull(identity.Groups);
            Assert.All(identity.Groups.Cast<SecurityIdentifier>(), static group => Assert.True(AccountUtilities.CallerGroups.Contains(group), $"{group} is on the caller's token but was not cached."));
        }

        /// <summary>
        /// Verifies that the process and session recorded are this process's own, since everything done on
        /// another user's behalf is addressed by session.
        /// </summary>
        [Fact]
        public void Caller_MatchesTheCurrentProcess()
        {
            // Arrange
            using Process current = Process.GetCurrentProcess();

            // Assert
            Assert.Equal((uint)current.Id, AccountUtilities.CallerProcessId);
            Assert.Equal((uint)current.SessionId, AccountUtilities.CallerSessionId);
        }

        /// <summary>
        /// Verifies that the account-kind flags agree with the caller's own security identifier, and that
        /// no more than one of them can be true.
        /// </summary>
        [Fact]
        public void Caller_ReportsAtMostOneServiceAccountKind()
        {
            // Arrange
            SecurityIdentifier caller = AccountUtilities.CallerSid;

            // Assert: each flag says what its identifier says
            Assert.Equal(caller.IsWellKnown(WellKnownSidType.LocalSystemSid), AccountUtilities.CallerIsLocalSystem);
            Assert.Equal(caller.IsWellKnown(WellKnownSidType.LocalServiceSid), AccountUtilities.CallerIsLocalService);
            Assert.Equal(caller.IsWellKnown(WellKnownSidType.NetworkServiceSid), AccountUtilities.CallerIsNetworkService);

            // Assert: and they are mutually exclusive, since one account cannot be two of them
            bool[] kinds = [AccountUtilities.CallerIsLocalSystem, AccountUtilities.CallerIsLocalService, AccountUtilities.CallerIsNetworkService];
            Assert.Empty(kinds.Where(static kind => kind).Skip(1));
        }

        /// <summary>
        /// Verifies that the flags derived from being the local system account cannot be set for an
        /// account that is not it.
        /// </summary>
        [Fact]
        public void Caller_DerivedSystemFlagsRequireTheSystemAccount()
        {
            if (!AccountUtilities.CallerIsLocalSystem)
            {
                Assert.False(AccountUtilities.CallerIsSystemInteractive);
                Assert.False(AccountUtilities.CallerUsingServiceUI);
            }
        }

        /// <summary>
        /// Verifies that whether the caller is running as a service agrees with the group its token
        /// carries, which is the only thing that decides it.
        /// </summary>
        /// <remarks>
        /// The service group is placed on a token by the service control manager when it starts a
        /// process, so it is present exactly when the caller is running under a service and never
        /// otherwise. Nothing asserts which of those this run is, since either is a valid way to be
        /// running the tests.
        /// </remarks>
        [Fact]
        public void CallerIsServiceAccount_AgreesWithTheGroupOnTheToken()
        {
            Assert.Equal(
                AccountUtilities.CallerGroups.Contains(new SecurityIdentifier(WellKnownSidType.ServiceSid, domainSid: null)),
                AccountUtilities.CallerIsServiceAccount);
        }

        /// <summary>
        /// Verifies that whether the caller can interact with a desktop agrees with the framework.
        /// </summary>
        [Fact]
        public void CallerIsInteractive_MatchesTheFramework()
        {
            Assert.Equal(Environment.UserInteractive, AccountUtilities.CallerIsInteractive);
        }

        /// <summary>
        /// Verifies that the local system identifier is the one Windows defines, since it is used to grant
        /// that account access to the pipes this library creates.
        /// </summary>
        [Fact]
        public void LocalSystemSid_IsTheWellKnownIdentifier()
        {
            Assert.True(AccountUtilities.LocalSystemSid.IsWellKnown(WellKnownSidType.LocalSystemSid));
            Assert.Equal("S-1-5-18", AccountUtilities.LocalSystemSid.Value, StringComparer.Ordinal);
        }

        /// <summary>
        /// Verifies that the machine's own account domain is identified, which is what distinguishes a
        /// local account from a domain one.
        /// </summary>
        /// <remarks>
        /// Read from the local security authority rather than derived from the caller, so it is the
        /// machine's answer whether or not the caller is a local account. It is checked against the
        /// caller's own domain only when the caller is local, since a domain account's domain is a
        /// different one by definition.
        /// </remarks>
        [Fact]
        public void LocalAccountDomainSid_IdentifiesTheMachine()
        {
            // Arrange
            SecurityIdentifier localDomain = AccountUtilities.LocalAccountDomainSid;

            // Assert: a domain identifier in its own right
            Assert.Equal(localDomain, localDomain.AccountDomainSid);

            // Assert: and the caller's domain, where the caller is a local account
            if (AccountUtilities.CallerSid.AccountDomainSid is SecurityIdentifier callerDomain && callerDomain.Equals(localDomain))
            {
                Assert.True(AccountUtilities.CallerSid.IsEqualDomainSid(localDomain));
            }
        }

        /// <summary>
        /// Verifies that the caller's privileges are the ones on its token, since this is a forward to the
        /// privilege reader and a caller may reasonably use either.
        /// </summary>
        [Fact]
        public void CallerPrivileges_AreThePrivilegesOnTheToken()
        {
            Assert.Equal(PrivilegeManager.GetPrivileges(), AccountUtilities.CallerPrivileges);
        }

        /// <summary>
        /// Verifies that the caller described as a user to run as is the caller, so a client process
        /// started with it runs as the account that asked for it.
        /// </summary>
        [Fact]
        public void CallerRunAsActiveUser_DescribesTheCaller()
        {
            // Act
            RunAsActiveUser caller = AccountUtilities.CallerRunAsActiveUser;

            // Assert
            Assert.Equal(AccountUtilities.CallerSid, caller.SID);
            Assert.Equal(AccountUtilities.CallerUsername.Value, caller.NTAccount.Value);
            Assert.Equal(AccountUtilities.CallerSessionId, caller.SessionId);
            Assert.Equal(AccountUtilities.CallerIsAdmin, caller.IsLocalAdmin);
        }

        /// <summary>
        /// Verifies that whether the caller is the logged-on user is decided by comparing the two
        /// descriptions, and that a caller with no session user is not treated as one.
        /// </summary>
        /// <remarks>
        /// A process in session zero has no interactive user to compare against, so the session-side
        /// description is absent and the answer has to be no rather than a match against nothing.
        /// </remarks>
        [Fact]
        public void CallerIsLoggedOnUser_ComparesTheCallerWithTheSessionUser()
        {
            // Act
            RunAsActiveUser? session = AccountUtilities.SessionRunAsActiveUser;

            // Assert
            if (session is null)
            {
                Assert.False(AccountUtilities.CallerIsLoggedOnUser);
            }
            else
            {
                Assert.Equal(session == AccountUtilities.CallerRunAsActiveUser, AccountUtilities.CallerIsLoggedOnUser);
                Assert.Equal(AccountUtilities.CallerSessionId, session.SessionId);
            }
        }
    }
}
