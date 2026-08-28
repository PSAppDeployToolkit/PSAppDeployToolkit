using System;
using System.IO;
using System.Runtime.Serialization;
using System.Security.Principal;
using System.Threading.Tasks;
using PSADT.AccountManagement;
using PSADT.Foundation;
using PSADT.TerminalServices;
using Xunit;

namespace PSADT.Tests.Foundation
{
    /// <summary>
    /// Tests the description of the user that work is carried out on behalf of.
    /// </summary>
    /// <remarks>
    /// This crosses a process boundary: it is built in the deployment process and read again in a client
    /// running as the user, so it is serialised on the way. The account and its identifier are held as
    /// strings and rebuilt on demand, because neither of the types they represent survives a data contract
    /// round trip - which makes the round trip the thing most worth pinning here.
    /// </remarks>
    public sealed class RunAsActiveUserTests
    {
        /// <summary>
        /// Verifies that a qualified account name is split into its domain and user parts, since a client
        /// is launched with them separately.
        /// </summary>
        [Fact]
        public void Constructor_SplitsAQualifiedAccountName()
        {
            // Act
            RunAsActiveUser user = new(new NTAccount(@"CONTOSO\jbloggs"), WellKnownSid, sessionId: 3, isLocalAdmin: true);

            // Assert
            Assert.Equal("jbloggs", user.UserName);
            Assert.Equal("CONTOSO", user.DomainName);
            Assert.Equal(@"CONTOSO\jbloggs", user.NTAccount.Value);
        }

        /// <summary>
        /// Verifies that an unqualified account name is taken as the user with no domain, rather than
        /// being split at a separator that is not there.
        /// </summary>
        [Fact]
        public void Constructor_LeavesAnUnqualifiedAccountNameAlone()
        {
            // Act
            RunAsActiveUser user = new(new NTAccount("jbloggs"), WellKnownSid, sessionId: 3, isLocalAdmin: null);

            // Assert
            Assert.Equal("jbloggs", user.UserName);
            Assert.Null(user.DomainName);
            Assert.Null(user.IsLocalAdmin);
        }

        /// <summary>
        /// Verifies that the account and identifier are rebuilt from what was stored, since both are
        /// exposed as the types callers expect but neither is held as one.
        /// </summary>
        [Fact]
        public void NTAccountAndSID_AreRebuiltFromWhatWasStored()
        {
            // Arrange
            RunAsActiveUser user = new(new NTAccount(@"CONTOSO\jbloggs"), WellKnownSid, sessionId: 3, isLocalAdmin: false);

            // Assert
            Assert.Equal(WellKnownSid, user.SID);
            Assert.Equal(@"CONTOSO\jbloggs", user.NTAccount.Value);
        }

        /// <summary>
        /// Verifies that an account with no content is refused, since a client launched with one would
        /// have nothing to run as.
        /// </summary>
        [Fact]
        public void Constructor_RefusesABlankAccount()
        {
            _ = Assert.Throws<ArgumentException>(static () => new RunAsActiveUser(new NTAccount("   "), WellKnownSid, sessionId: 1, isLocalAdmin: null));
        }

        /// <summary>
        /// Verifies that a null account, identifier or session is refused.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "This is deliberate as part of unit testing.")]
        [Fact]
        public void Constructor_RefusesNullArguments()
        {
            _ = Assert.Throws<ArgumentNullException>(static () => new RunAsActiveUser(null!, WellKnownSid, sessionId: 1, isLocalAdmin: null));
            _ = Assert.Throws<ArgumentNullException>(static () => new RunAsActiveUser(new NTAccount(@"CONTOSO\jbloggs"), null!, sessionId: 1, isLocalAdmin: null));
            _ = Assert.Throws<ArgumentNullException>(static () => new RunAsActiveUser(null!));
        }

        /// <summary>
        /// Verifies that everything survives a data contract round trip, which is what happens between the
        /// deployment process building this and a client reading it.
        /// </summary>
        [Fact]
        public void Serialization_RoundTripsEveryMember()
        {
            // Arrange
            RunAsActiveUser original = new(new NTAccount(@"CONTOSO\jbloggs"), WellKnownSid, sessionId: 7, isLocalAdmin: true);
            DataContractSerializer serializer = new(typeof(RunAsActiveUser));

            // Act
            using MemoryStream stream = new();
            serializer.WriteObject(stream, original);
            stream.Position = 0;

            // Assigned through a local rather than cast inline: the two target frameworks disagree on
            // whether ReadObject's return is nullable, so a null-forgiving operator is necessary on one
            // and flagged as redundant on the other.
            object? deserialized = serializer.ReadObject(stream);
            Assert.NotNull(deserialized);
            RunAsActiveUser restored = (RunAsActiveUser)deserialized;

            // Assert
            Assert.Equal(original, restored);
            Assert.Equal(original.NTAccount.Value, restored.NTAccount.Value);
            Assert.Equal(original.SID, restored.SID);
            Assert.Equal(original.UserName, restored.UserName);
            Assert.Equal(original.DomainName, restored.DomainName);
            Assert.Equal(original.SessionId, restored.SessionId);
            Assert.Equal(original.IsLocalAdmin, restored.IsLocalAdmin);
        }

        /// <summary>
        /// Verifies that two descriptions of the same user in the same session are equal, which is what
        /// decides whether the caller is the logged-on user.
        /// </summary>
        [Fact]
        public void Equality_IsByTheAccountSessionAndRights()
        {
            // Arrange
            RunAsActiveUser user = new(new NTAccount(@"CONTOSO\jbloggs"), WellKnownSid, sessionId: 3, isLocalAdmin: true);

            // Assert
            Assert.Equal(user, new RunAsActiveUser(new NTAccount(@"CONTOSO\jbloggs"), WellKnownSid, sessionId: 3, isLocalAdmin: true));
            Assert.NotEqual(user, new RunAsActiveUser(new NTAccount(@"CONTOSO\jbloggs"), WellKnownSid, sessionId: 4, isLocalAdmin: true));
            Assert.NotEqual(user, new RunAsActiveUser(new NTAccount(@"CONTOSO\someone"), WellKnownSid, sessionId: 3, isLocalAdmin: true));
            Assert.NotEqual(user, new RunAsActiveUser(new NTAccount(@"CONTOSO\jbloggs"), WellKnownSid, sessionId: 3, isLocalAdmin: false));
        }

        /// <summary>
        /// Verifies that a session carries over into a user to run as unchanged, which is how every real
        /// one is built.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task Constructor_CarriesOverASessionAsync()
        {
            // Arrange
            SessionInfo? session = await SessionInfo.GetAsync(AccountUtilities.CallerSessionId).ConfigureAwait(true);
            if (session is null)
            {
                // A process in a session with no interactive user has nothing to carry over.
                return;
            }

            // Act
            RunAsActiveUser user = new(session);

            // Assert
            Assert.Equal(session.NTAccount.Value, user.NTAccount.Value);
            Assert.Equal(session.SID, user.SID);
            Assert.Equal(session.SessionId, user.SessionId);
            Assert.Equal(session.IsLocalAdmin, user.IsLocalAdmin);
            Assert.Equal(session.ToRunAsActiveUser(), user);
        }

        /// <summary>
        /// Verifies that the caller's own session is chosen out of a set of sessions, since work is done
        /// in the caller's session wherever it can be.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task GetAsync_PrefersTheCallersOwnSessionAsync()
        {
            // Arrange: only the caller's own session, so nothing about any other user is queried
            SessionInfo? session = await SessionInfo.GetAsync(AccountUtilities.CallerSessionId).ConfigureAwait(true);
            if (session?.IsActiveUserSession is not true)
            {
                // Nothing to choose from: this is a session with no active interactive user.
                return;
            }

            // Act
            RunAsActiveUser? chosen = await RunAsActiveUser.GetAsync([session]).ConfigureAwait(true);

            // Assert
            Assert.Equal(session.ToRunAsActiveUser(), chosen);
        }

        /// <summary>
        /// Verifies that a set of sessions with nobody actively signed in yields nothing, rather than a
        /// user that work would then be attempted on behalf of.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task GetAsync_ReportsNothingWhenNoSessionIsActiveAsync()
        {
            Assert.Null(await RunAsActiveUser.GetAsync([]).ConfigureAwait(true));
        }

        /// <summary>
        /// An identifier that resolves on any machine, used where the test is about the surrounding
        /// handling rather than about the account itself.
        /// </summary>
        private static readonly SecurityIdentifier WellKnownSid = new(WellKnownSidType.LocalSystemSid, domainSid: null);
    }
}
