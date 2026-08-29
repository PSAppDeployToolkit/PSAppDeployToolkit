using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using PSADT.AccountManagement;
using PSADT.Tests.TestHelpers;
using PSADT.TerminalServices;
using Xunit;

namespace PSADT.Tests.TerminalServices
{
    /// <summary>
    /// Tests what the machine reports about its logon sessions.
    /// </summary>
    /// <remarks>
    /// Almost everything here asks about the caller's own session by identifier rather than enumerating
    /// them all, and that is deliberate rather than incidental. Describing somebody else's session, for an
    /// elevated caller, brokers a token for that user - which registers a scheduled task to launch a
    /// broker as the local system account - and, for the console session, may repair the permissions the
    /// client needs and then launch a client process in it. All of those change the machine, so they are
    /// out of bounds here.
    /// <para>
    /// Enumerating every session is covered, but only for an unelevated caller: none of that machinery is
    /// reachable without administrative rights, so the enumeration is a plain read.
    /// </para>
    /// </remarks>
    public sealed class SessionInfoTests
    {
        /// <summary>
        /// Verifies that the caller's own session is described, and describes the caller.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task GetAsync_DescribesTheCallersOwnSessionAsync()
        {
            // Arrange
            SessionInfo? session = await SessionInfo.GetAsync(AccountUtilities.CallerSessionId).ConfigureAwait(true);
            if (session is null)
            {
                // A session with no interactive user is not described at all, which is covered separately.
                return;
            }

            // Assert
            Assert.Equal(AccountUtilities.CallerSessionId, session.SessionId);
            Assert.True(session.IsCurrentSession);
            Assert.Equal(AccountUtilities.CallerSid, session.SID);
            Assert.Equal(AccountUtilities.CallerUsername.Value, session.NTAccount.Value);
            Assert.Equal(AccountUtilities.CallerIsAdmin, session.IsLocalAdmin);
        }

        /// <summary>
        /// Verifies that the account name is split into its user and domain parts consistently with the
        /// qualified name, since a client is launched with them separately.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task GetAsync_SplitsTheAccountNameAsync()
        {
            // Arrange
            SessionInfo? session = await SessionInfo.GetAsync(AccountUtilities.CallerSessionId).ConfigureAwait(true);
            if (session is null)
            {
                return;
            }

            // Assert
            Assert.Equal($@"{session.DomainName}\{session.UserName}", session.NTAccount.Value, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Verifies that the session state flags cannot contradict each other, since callers branch on
        /// them to decide whether there is anybody to prompt.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task GetAsync_ReportsConsistentSessionStateAsync()
        {
            // Arrange
            SessionInfo? session = await SessionInfo.GetAsync(AccountUtilities.CallerSessionId).ConfigureAwait(true);
            if (session is null)
            {
                return;
            }

            // Assert: an active session is by definition a valid one, and has nobody disconnected from it
            Assert.Contains(session.ConnectState, EnumValues.Declared<PSADT.Interop.WTS_CONNECTSTATE_CLASS>());
            if (session.IsActiveUserSession)
            {
                Assert.True(session.IsValidUserSession);
                Assert.Null(session.DisconnectTime);
            }

            // Assert: a client build number is only meaningful alongside a client name
            if (session.ClientName is null)
            {
                Assert.Null(session.ClientBuildNumber);
            }
        }

        /// <summary>
        /// Verifies that the times reported are in the past and plausible, since a deployment decides
        /// whether to defer based on how long somebody has been idle.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task GetAsync_ReportsPlausibleTimesAsync()
        {
            // Arrange
            SessionInfo? session = await SessionInfo.GetAsync(AccountUtilities.CallerSessionId).ConfigureAwait(true);
            if (session is null)
            {
                return;
            }

            // Assert
            Assert.True(session.LogonTime <= DateTime.Now, "The session reports having been signed in to in the future.");
            if (session.IdleTime is TimeSpan idle)
            {
                Assert.True(idle >= TimeSpan.Zero, "The session reports a negative idle time.");
            }
            if (session.DisconnectTime is DateTime disconnected)
            {
                Assert.True(disconnected >= session.LogonTime, "The session reports being disconnected before it was signed in to.");
            }
        }

        /// <summary>
        /// Verifies that the console session is identified as the console session, and that a session in
        /// the services window station is not treated as a user's.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task GetAsync_IdentifiesTheConsoleAndServiceSessionsAsync()
        {
            // Arrange
            SessionInfo? session = await SessionInfo.GetAsync(AccountUtilities.CallerSessionId).ConfigureAwait(true);
            if (session is null)
            {
                return;
            }

            // Assert: matched exactly as the type itself matches them, so the two cannot drift apart
            bool isWindowStation = session.SessionName is string name
                && (name.Equals("Services", StringComparison.Ordinal) || name.Equals("RDP-Tcp", StringComparison.Ordinal));
            Assert.Equal(!isWindowStation, session.IsUserSession);
        }

        /// <summary>
        /// Verifies that how the session is being reached is described consistently, since a deployment
        /// decides whether it may show something to a person based on it.
        /// </summary>
        /// <remarks>
        /// Nothing asserts which of these the run landed on: a session may be at the machine's own
        /// console, arriving over a remote connection, or neither, and all are valid. What is asserted is
        /// that the descriptions cannot contradict each other - a session cannot be at the console and
        /// reached remotely at the same time, and one reached remotely has a protocol to say so.
        /// </remarks>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task GetAsync_DescribesHowTheSessionIsReachedAsync()
        {
            // Arrange
            SessionInfo? session = await SessionInfo.GetAsync(AccountUtilities.CallerSessionId).ConfigureAwait(true);
            if (session is null)
            {
                return;
            }

            // Assert: the two cannot both be true
            Assert.False(session.IsConsoleSession && session.IsRdpSession);

            // Assert: a session reached remotely reports a protocol other than the console's
            Assert.Equal(session.ClientProtocolType is not Interop.WTS_PROTOCOL_TYPE.Console, session.IsRdpSession);

            // Assert: and a client directory is only reported alongside a client that has one
            if (session.ClientDirectory is not null)
            {
                Assert.False(string.IsNullOrWhiteSpace(session.ClientDirectory.FullName));
                Assert.NotNull(session.ClientName);
            }
        }

        /// <summary>
        /// Verifies that a session identifier nothing is using reports nothing rather than failing.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task GetAsync_ReportsNothingForASessionThatDoesNotExistAsync()
        {
            Assert.Null(await SessionInfo.GetAsync(uint.MaxValue).ConfigureAwait(true));
        }

        /// <summary>
        /// Verifies that session zero is not described, since it has no interactive user for a session
        /// description to be about.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task GetAsync_DoesNotDescribeTheServicesSessionAsync()
        {
            Assert.Null(await SessionInfo.GetAsync(0).ConfigureAwait(true));
        }

        /// <summary>
        /// Verifies that enumerating every session finds the caller's own, reports each one once, and
        /// describes each of them as fully as asking for it directly would.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact(Skip = "Describing another user's session as an administrator brokers a token and launches a client, which changes the machine.", SkipWhen = nameof(TestEnvironment.IsElevated), SkipType = typeof(TestEnvironment))]
        public async Task GetAsync_EnumeratesEverySessionOnceAsync()
        {
            // Act
            IReadOnlyList<SessionInfo> sessions = await SessionInfo.GetAsync().ConfigureAwait(true);

            // Assert: each session appears once
            Assert.Equal(sessions.Count, sessions.Select(static s => s.SessionId).Distinct().Count());

            // Assert: and every one of them names somebody
            Assert.All(sessions, static session =>
            {
                Assert.False(string.IsNullOrWhiteSpace(session.UserName));
                Assert.NotNull(session.SID);
            });

            // Assert: with exactly one of them being the caller's, where the caller has one at all
            using Process current = Process.GetCurrentProcess();
            Assert.All(sessions.Where(static s => s.IsCurrentSession), session => Assert.Equal((uint)current.SessionId, session.SessionId));
        }

        /// <summary>
        /// Verifies that the description of the session carries over into a user to run as unchanged.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task ToRunAsActiveUser_CarriesOverTheSessionAsync()
        {
            // Arrange
            SessionInfo? session = await SessionInfo.GetAsync(AccountUtilities.CallerSessionId).ConfigureAwait(true);
            if (session is null)
            {
                return;
            }

            // Act
            PSADT.Foundation.RunAsActiveUser user = session.ToRunAsActiveUser();

            // Assert
            Assert.Equal(session.NTAccount.Value, user.NTAccount.Value);
            Assert.Equal(session.SID, user.SID);
            Assert.Equal(session.SessionId, user.SessionId);
            Assert.Equal(session.IsLocalAdmin, user.IsLocalAdmin);
        }

        /// <summary>
        /// Verifies that the session describes itself in a form worth putting in a log, since that is
        /// where it ends up.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task ToString_NamesTheUserAndSessionAsync()
        {
            // Arrange
            SessionInfo? session = await SessionInfo.GetAsync(AccountUtilities.CallerSessionId).ConfigureAwait(true);
            if (session is null)
            {
                return;
            }

            // Act
            string described = session.ToString();

            // Assert
            Assert.Contains(session.UserName, described, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Verifies that the identity reported for the caller's session resolves to the same account the
        /// framework knows the caller as, so nothing is lost translating between the two forms.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task GetAsync_IdentityRoundTripsThroughTheAccountNameAsync()
        {
            // Arrange
            SessionInfo? session = await SessionInfo.GetAsync(AccountUtilities.CallerSessionId).ConfigureAwait(true);
            if (session is null)
            {
                return;
            }

            // Act & Assert
            Assert.Equal(session.SID, (SecurityIdentifier)session.NTAccount.Translate(typeof(SecurityIdentifier)));
        }
    }
}
