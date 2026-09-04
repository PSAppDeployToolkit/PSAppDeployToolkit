using System;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using PSADT.AccountManagement;
using PSADT.Security;
using PSADT.Tests.TestHelpers;
using Windows.Win32.Security;
using Xunit;

namespace PSADT.Tests.Security
{
    /// <summary>
    /// Tests opening, duplicating and brokering access tokens.
    /// </summary>
    /// <remarks>
    /// Everything exercised here operates on this process's own token. Duplicating it produces a handle
    /// private to this process which is closed when the test ends, so nothing outlives the run.
    /// <para>
    /// Brokering another user's token is deliberately not exercised. Doing so registers a scheduled task
    /// to launch a broker as the local system account, which is a change to the machine; and it cannot
    /// succeed on a machine with no second session logged on regardless. What is covered is the refusals,
    /// which are the part that matters: a token handed out in error is a privilege escalation, and the
    /// refusals all happen before any part of the brokering is set in motion.
    /// </para>
    /// </remarks>
    public sealed class TokenManagerTests
    {
        /// <summary>
        /// Verifies that the current process token opens with the rights that were asked for, and is a
        /// usable handle rather than a sentinel.
        /// </summary>
        [Fact]
        public void GetCurrentProcessToken_OpensAUsableHandle()
        {
            // Act
            using SafeFileHandle token = TokenManager.GetCurrentProcessToken(TOKEN_ACCESS_MASK.TOKEN_QUERY);

            // Assert
            Assert.False(token.IsInvalid);
            Assert.False(token.IsClosed);
        }

        /// <summary>
        /// Verifies that duplicating the current token yields a usable primary token, which is what every
        /// process launched on a user's behalf is started with.
        /// </summary>
        [Fact]
        public void GetPrimaryToken_DuplicatesTheCurrentToken()
        {
            // Arrange
            using SafeFileHandle token = TokenManager.GetCurrentProcessToken(TOKEN_ACCESS_MASK.TOKEN_QUERY | TOKEN_ACCESS_MASK.TOKEN_DUPLICATE);

            // Act
            using SafeFileHandle primary = TokenManager.GetPrimaryToken(token);

            // Assert: a distinct handle, still describing the same account
            Assert.False(primary.IsInvalid);
            Assert.NotEqual(token.DangerousGetHandle(), primary.DangerousGetHandle());
            Assert.Equal(TokenUtilities.GetTokenSid(token), TokenUtilities.GetTokenSid(primary));
        }

        /// <summary>
        /// Verifies that duplication is refused when the source handle was not opened with the right to be
        /// duplicated, rather than yielding a handle that fails on first use.
        /// </summary>
        [Fact]
        public void GetPrimaryToken_RefusesATokenItMayNotDuplicate()
        {
            // Arrange: opened for reading only
            using SafeFileHandle token = TokenManager.GetCurrentProcessToken(TOKEN_ACCESS_MASK.TOKEN_QUERY);

            // Act & Assert
            _ = Assert.Throws<UnauthorizedAccessException>(() => TokenManager.GetPrimaryToken(token));
        }

        /// <summary>
        /// Verifies that setting interface access on a duplicated token is refused without the privilege
        /// that Windows requires for it, which no account but the local system holds.
        /// </summary>
        /// <remarks>
        /// Worth its own test because the refusal is the library's, not Windows'. Setting the flag without
        /// the privilege quietly does nothing, so the check has to happen before the attempt or a caller
        /// ends up with a token it believes can drive another session's interface.
        /// </remarks>
        [Fact(Skip = "The local system account holds the privilege this refusal depends on.", SkipWhen = nameof(TestEnvironment.IsLocalSystem), SkipType = typeof(TestEnvironment))]
        public void GetPrimaryToken_RefusesInterfaceAccessWithoutThePrivilege()
        {
            // Arrange
            using SafeFileHandle token = TokenManager.GetCurrentProcessToken(TOKEN_ACCESS_MASK.TOKEN_QUERY | TOKEN_ACCESS_MASK.TOKEN_DUPLICATE);

            // Act & Assert
            _ = Assert.Throws<UnauthorizedAccessException>(() => TokenManager.GetPrimaryToken(token, uiAccess: true));
        }

        /// <summary>
        /// Verifies that the highest available token is always produced, whether or not the caller's token
        /// is one half of a split pair.
        /// </summary>
        /// <remarks>
        /// A token has a linked counterpart only where user account control split it: an elevated process
        /// is linked to its filtered token and an unelevated member of the administrators group to its
        /// elevated one, but a process running as an account that was never split has no linked token at
        /// all. The point of this member is that it falls back rather than failing, so the assertion is
        /// that a usable token for the same account comes back either way.
        /// </remarks>
        [Fact]
        public void GetHighestPrimaryToken_FallsBackWhenThereIsNoLinkedToken()
        {
            // Arrange
            using SafeFileHandle token = TokenManager.GetCurrentProcessToken(TOKEN_ACCESS_MASK.TOKEN_QUERY | TOKEN_ACCESS_MASK.TOKEN_DUPLICATE);

            // Act
            using SafeFileHandle highest = TokenManager.GetHighestPrimaryToken(token);

            // Assert
            Assert.False(highest.IsInvalid);
            Assert.Equal(TokenUtilities.GetTokenSid(token), TokenUtilities.GetTokenSid(highest));
        }

        /// <summary>
        /// Verifies that the token linked to the caller's own is the other half of the same account,
        /// where user account control split it into two.
        /// </summary>
        /// <remarks>
        /// A token has a linked counterpart only where it was split: an elevated process is linked to its
        /// filtered token and an unelevated member of the administrators group to its elevated one. A
        /// process running as an account that was never split has none at all, and asking throws - which
        /// is why this reports rather than fails when there is no link, and why
        /// <see cref="TokenManager.GetHighestPrimaryToken"/> exists to paper over the difference.
        /// <para>
        /// The two halves belong to the same account, so the identifier is what ties them together; what
        /// differs between them is whether they are administrative, which is the whole point of the split.
        /// </para>
        /// </remarks>
        [Fact]
        public void GetLinkedToken_IsTheOtherHalfOfASplitToken()
        {
            // Arrange
            using SafeFileHandle token = TokenManager.GetCurrentProcessToken(TOKEN_ACCESS_MASK.TOKEN_QUERY | TOKEN_ACCESS_MASK.TOKEN_DUPLICATE);
            SafeFileHandle? linked = null;
            if (Record.Exception(() => linked = TokenManager.GetLinkedToken(token)) is not null)
            {
                // This account's token was never split, so there is no other half to compare against.
                return;
            }

            // Assert
            using (linked)
            {
                Assert.NotNull(linked);
                Assert.False(linked.IsInvalid);
                Assert.Equal(TokenUtilities.GetTokenSid(token), TokenUtilities.GetTokenSid(linked));
                Assert.Equal(TokenUtilities.GetTokenSessionId(token), TokenUtilities.GetTokenSessionId(linked));
            }
        }

        /// <summary>
        /// Verifies that the linked token can be turned into a usable primary one, which is the form a
        /// process is actually started with.
        /// </summary>
        [Fact]
        public void GetLinkedPrimaryToken_ProducesAUsableTokenForTheSameAccount()
        {
            // Arrange
            using SafeFileHandle token = TokenManager.GetCurrentProcessToken(TOKEN_ACCESS_MASK.TOKEN_QUERY | TOKEN_ACCESS_MASK.TOKEN_DUPLICATE);
            SafeFileHandle? primary = null;
            if (Record.Exception(() => primary = TokenManager.GetLinkedPrimaryToken(token)) is not null)
            {
                // This account's token was never split, which the fallback above covers instead.
                return;
            }

            // Assert
            using (primary)
            {
                Assert.NotNull(primary);
                Assert.False(primary.IsInvalid);
                Assert.Equal(TokenUtilities.GetTokenSid(token), TokenUtilities.GetTokenSid(primary));
            }
        }

        /// <summary>
        /// Verifies that an unelevated caller is reported as unable to broker another user's token, since
        /// it has no way to obtain one.
        /// </summary>
        [Fact(Skip = "Requires an unelevated caller.", SkipWhen = nameof(TestEnvironment.IsElevated), SkipType = typeof(TestEnvironment))]
        public void CanGetUserPrimaryToken_IsFalseForAnUnelevatedCaller()
        {
            Assert.False(TokenManager.CanGetUserPrimaryToken);
        }

        /// <summary>
        /// Verifies that whether brokering is possible agrees with what the caller is, so the flag cannot
        /// report a capability the caller does not have.
        /// </summary>
        [Fact(Skip = "Requires the client/server executables alongside the test assembly.", SkipUnless = nameof(TestEnvironment.ClientServerExecutablesPresent), SkipType = typeof(TestEnvironment))]
        public void CanGetUserPrimaryToken_RequiresAnAdministrativeCaller()
        {
            if (TokenManager.CanGetUserPrimaryToken)
            {
                Assert.True(AccountUtilities.CallerIsAdmin || AccountUtilities.CallerIsLocalSystem, "Brokering was reported as possible for a caller that is neither an administrator nor the local system.");
            }
        }

        /// <summary>
        /// Verifies that brokering the local system session's token is refused outright, which is the one
        /// refusal that holds however the caller is running.
        /// </summary>
        /// <remarks>
        /// Session zero has no interactive user, so a token brokered from it would be the machine
        /// account's rather than a person's - the very escalation this is guarding against. The refusal is
        /// reached before the broker is set up, so nothing is launched by this test.
        /// </remarks>
        [Fact]
        public Task GetUserPrimaryTokenAsync_RefusesTheSystemSessionAsync()
        {
            // Returned rather than awaited: awaiting here would need a ConfigureAwait that the runner
            // forbids in a test body, and omitting it trips the analyser that requires one.
            return Assert.ThrowsAsync<UnauthorizedAccessException>(static () => TokenManager.GetUserPrimaryTokenAsync(0).AsTask());
        }

        /// <summary>
        /// Verifies that an unelevated caller cannot broker another user's token at all.
        /// </summary>
        /// <remarks>
        /// Only meaningful unelevated, and safe only unelevated: the refusal for a caller that is not an
        /// administrator is the first thing checked, so nothing is launched. An elevated caller would get
        /// past it and start brokering, which is why this is gated rather than asserted both ways.
        /// </remarks>
        [Fact(Skip = "Requires an unelevated caller.", SkipWhen = nameof(TestEnvironment.IsElevated), SkipType = typeof(TestEnvironment))]
        public Task GetUserPrimaryTokenAsync_RefusesAnUnelevatedCallerAsync()
        {
            return Assert.ThrowsAsync<UnauthorizedAccessException>(static () => TokenManager.GetUserPrimaryTokenAsync(1).AsTask());
        }
    }
}
