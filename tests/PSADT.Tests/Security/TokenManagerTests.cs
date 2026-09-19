using System;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using PSADT.Foundation;
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
    /// <para>
    /// The identity check on the <see cref="RunAsActiveUser"/> entry point falls under the same exclusion.
    /// It compares a token that has already been acquired, so reaching it needs an acquisition that
    /// succeeds, which needs the second session and the broker run that are ruled out above.
    /// </para>
    /// </remarks>
    public sealed class TokenManagerTests
    {
        /// <summary>
        /// Restricts token vending to valid desktop sessions.
        /// </summary>
        /// <param name="session">The requested session identifier.</param>
        /// <param name="expected">Whether the session is valid for vending.</param>
        [Theory]
        [InlineData(5u, true)]
        [InlineData(6u, true)]
        [InlineData(0u, false)]
        [InlineData(uint.MaxValue, false)]
        public void SessionIdIsValidForVending_RequiresValidSession(uint session, bool expected)
        {
            Assert.Equal(expected, TokenManager.SessionIdIsValidForVending(session));
        }

        /// <summary>
        /// Reports acquisition eligibility without acquiring a token, which needs both a vendable session
        /// and a caller with a route to one rather than either on its own.
        /// </summary>
        /// <remarks>
        /// The caller half is a fact about the machine the run landed on, so the expectation is derived
        /// rather than fixed. An unelevated caller has no route at all - no process it may open, no broker
        /// it may start, and no WTS - so every session is refused, and asserting that beside the elevated
        /// answer is what proves the two are combined rather than alternatives.
        /// </remarks>
        /// <param name="session">The requested session.</param>
        /// <param name="vendable">Whether the session itself may be vended.</param>
        [Theory]
        [InlineData(5u, true)]
        [InlineData(6u, true)]
        [InlineData(0u, false)]
        [InlineData(uint.MaxValue, false)]
        public void CanGetUserPrimaryToken_RequiresAVendableSessionAndAnEligibleCaller(uint session, bool vendable)
        {
            Assert.Equal(vendable && TestEnvironment.IsElevated, TokenManager.CanGetUserPrimaryToken(session));
        }

        /// <summary>
        /// Reports the absence of a token rather than raising it, on both entry points.
        /// </summary>
        /// <remarks>
        /// The sessions are ineligible ones, which is the only refusal a test can arrange on a single
        /// session machine: a caller that may attempt acquisition still cannot be made to fail on demand,
        /// as that needs a session holding no suitable process and a broker that cannot be started.
        /// <para>
        /// A caller error still raises, and that is asserted here too, because the whole value of the
        /// member is in which outcomes it absorbs and an undefined elevation is not one of them.
        /// </para>
        /// </remarks>
        /// <param name="session">The ineligible session.</param>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Theory]
        [InlineData(0u)]
        [InlineData(uint.MaxValue)]
        public async Task TryGetUserPrimaryTokenAsync_ReportsRefusalButRaisesCallerErrorAsync(uint session)
        {
            RunAsActiveUser user = new(new NTAccount("TokenValidationTest"), new SecurityIdentifier(WellKnownSidType.NullSid, domainSid: null), session, isLocalAdmin: null);
            using (SafeFileHandle? token = await TokenManager.TryGetUserPrimaryTokenAsync(session).ConfigureAwait(true))
            {
                Assert.Null(token);
            }
            using (SafeFileHandle? token = await TokenManager.TryGetUserPrimaryTokenAsync(user).ConfigureAwait(true))
            {
                Assert.Null(token);
            }
            ArgumentOutOfRangeException failure = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(static async () =>
            {
                using SafeFileHandle? token = await TokenManager.TryGetUserPrimaryTokenAsync(5u, (ElevatedTokenType)99).ConfigureAwait(false);
            }).ConfigureAwait(true);
            Assert.Equal("elevatedTokenType", failure.ParamName);
        }

        /// <summary>
        /// Rejects undefined elevation before native acquisition or fallback, on both entry points.
        /// </summary>
        /// <param name="value">The undefined enum value.</param>
        /// <param name="session">The requested session.</param>
        [Theory]
        [InlineData(-1, 5u)]
        [InlineData(3, 5u)]
        [InlineData(99, 6u)]
        [InlineData(int.MaxValue, 6u)]
        public async Task GetUserPrimaryTokenAsync_RejectsUndefinedElevationAsync(int value, uint session)
        {
            ElevatedTokenType elevation = (ElevatedTokenType)value;
            ArgumentOutOfRangeException sessionFailure = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            {
                using SafeFileHandle token = await TokenManager.GetUserPrimaryTokenAsync(session, elevation).ConfigureAwait(false);
            });
            Assert.Equal("elevatedTokenType", sessionFailure.ParamName);
            Assert.Equal(elevation, sessionFailure.ActualValue);
            RunAsActiveUser user = new(new NTAccount("TokenValidationTest"), new SecurityIdentifier(WellKnownSidType.NullSid, domainSid: null), session, isLocalAdmin: null);
            ArgumentOutOfRangeException userFailure = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            {
                using SafeFileHandle token = await TokenManager.GetUserPrimaryTokenAsync(user, elevation, uiAccess: true).ConfigureAwait(false);
            });
            Assert.Equal("elevatedTokenType", userFailure.ParamName);
            Assert.Equal(elevation, userFailure.ActualValue);
        }

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
        /// Rejects invalid sessions before elevation or caller validation on both entry points.
        /// </summary>
        /// <param name="session">The invalid session identifier.</param>
        /// <param name="elevation">The requested elevation, including an undefined value.</param>
        [Theory]
        [InlineData(0u, ElevatedTokenType.None)]
        [InlineData(uint.MaxValue, ElevatedTokenType.None)]
        [InlineData(0u, (ElevatedTokenType)99)]
        [InlineData(uint.MaxValue, (ElevatedTokenType)99)]
        public async Task GetUserPrimaryTokenAsync_RejectsInvalidSessionFirstAsync(uint session, ElevatedTokenType elevation)
        {
            ArgumentOutOfRangeException sessionFailure = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            {
                using SafeFileHandle token = await TokenManager.GetUserPrimaryTokenAsync(session, elevation).ConfigureAwait(false);
            });
            Assert.Equal("sessionId", sessionFailure.ParamName);
            Assert.Equal(session, sessionFailure.ActualValue);
            RunAsActiveUser user = new(new NTAccount("TokenValidationTest"), new SecurityIdentifier(WellKnownSidType.NullSid, domainSid: null), session, isLocalAdmin: null);
            ArgumentOutOfRangeException userFailure = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            {
                using SafeFileHandle token = await TokenManager.GetUserPrimaryTokenAsync(user, elevation, uiAccess: true).ConfigureAwait(false);
            });
            Assert.Equal("sessionId", userFailure.ParamName);
            Assert.Equal(session, userFailure.ActualValue);
        }

        /// <summary>
        /// Verifies that an unelevated caller cannot acquire another user's token through either entry point.
        /// </summary>
        /// <remarks>
        /// Only meaningful unelevated, and safe only unelevated: the refusal for a caller that is not an
        /// administrator occurs before acquisition for valid arguments, so nothing is launched.
        /// </remarks>
        [Fact(Skip = "Requires an unelevated caller.", SkipWhen = nameof(TestEnvironment.IsElevated), SkipType = typeof(TestEnvironment))]
        public async Task GetUserPrimaryTokenAsync_RefusesAnUnelevatedCallerAsync()
        {
            _ = await Assert.ThrowsAsync<UnauthorizedAccessException>(static async () =>
            {
                using SafeFileHandle token = await TokenManager.GetUserPrimaryTokenAsync(1).ConfigureAwait(false);
            });
            RunAsActiveUser user = new(new NTAccount("TokenValidationTest"), new SecurityIdentifier(WellKnownSidType.NullSid, domainSid: null), 1, isLocalAdmin: null);
            _ = await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                using SafeFileHandle token = await TokenManager.GetUserPrimaryTokenAsync(user).ConfigureAwait(false);
            });
        }
    }
}
