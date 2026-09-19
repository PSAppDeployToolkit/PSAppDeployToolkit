using System;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;
using PSADT.AccountManagement;
using PSADT.Security;
using PSADT.Tests.TestHelpers;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.Security.Authentication.Identity;
using Xunit;

namespace PSADT.Tests.Security
{
    /// <summary>
    /// Verifies independent logon provenance, token suitability, and duplicate ownership without brokering.
    /// </summary>
    public sealed class ProcessTokenProviderTests
    {
        /// <summary>
        /// Rejects every independently mismatched or unsuitable candidate field.
        /// </summary>
        /// <param name="difference">The field to invalidate, or none for an ordinary desktop token.</param>
        /// <param name="expected">Whether the candidate is suitable.</param>
        [Theory]
        [InlineData("none", true)]
        [InlineData("sid", false)]
        [InlineData("session", false)]
        [InlineData("luidLow", false)]
        [InlineData("luidHigh", false)]
        [InlineData("impersonation", false)]
        [InlineData("elevated", false)]
        [InlineData("lowIntegrity", false)]
        [InlineData("highIntegrity", false)]
        [InlineData("restricted", false)]
        [InlineData("appContainer", false)]
        [InlineData("uiAccess", false)]
        [InlineData("runas", false)]
        [InlineData("netonly", false)]
        [InlineData("lsaSid", false)]
        [InlineData("lsaSession", false)]
        [InlineData("lsaTime", false)]
        public void IsSuitable_RequiresEveryPredicate(string difference, bool expected)
        {
            ProcessTokenSession session = new(5, new("S-1-5-21-1-2-3-1001"), 100);
            ProcessTokenLogon reference = CreateLogon();
            ProcessTokenLogon logon = new(in reference.AuthenticationId,
                difference.Equals("lsaSession", StringComparison.Ordinal) ? 0u : reference.SessionId,
                difference.Equals("lsaSid", StringComparison.Ordinal) ? new(WellKnownSidType.LocalSystemSid, domainSid: null) : reference.Sid,
                difference.Equals("netonly", StringComparison.Ordinal) ? SECURITY_LOGON_TYPE.NewCredentials : reference.LogonType,
                difference.Equals("runas", StringComparison.Ordinal) || difference.Equals("netonly", StringComparison.Ordinal) ? 0u : reference.UserFlags,
                difference.Equals("lsaTime", StringComparison.Ordinal) ? 999 : reference.LogonTime);
            ProcessTokenMetadata token = new(
                difference.Equals("sid", StringComparison.Ordinal) ? new(WellKnownSidType.LocalSystemSid, domainSid: null) : session.Sid,
                difference.Equals("session", StringComparison.Ordinal) ? 6u : session.SessionId,
                new LUID { LowPart = difference.Equals("luidLow", StringComparison.Ordinal) ? 99u : 42u, HighPart = difference.Equals("luidHigh", StringComparison.Ordinal) ? 1 : 0 },
                difference.Equals("impersonation", StringComparison.Ordinal) ? TOKEN_TYPE.TokenImpersonation : TOKEN_TYPE.TokenPrimary,
                difference.Equals("elevated", StringComparison.Ordinal),
                new(difference.Equals("lowIntegrity", StringComparison.Ordinal) ? "S-1-16-4096" : difference.Equals("highIntegrity", StringComparison.Ordinal) ? "S-1-16-12288" : "S-1-16-8192"),
                difference.Equals("restricted", StringComparison.Ordinal), difference.Equals("appContainer", StringComparison.Ordinal), difference.Equals("uiAccess", StringComparison.Ordinal), logon);

            Assert.Equal(expected, ProcessTokenProvider.IsSuitable(session, reference, token));
        }

        /// <summary>
        /// Requires exactly one independently matching original logon, ignoring secondary logons.
        /// </summary>
        [Fact]
        public void FindReference_RejectsMissingAndAmbiguousReferences()
        {
            ProcessTokenSession session = new(5, new("S-1-5-21-1-2-3-1001"), 100);
            ProcessTokenLogon reference = CreateLogon();
            ProcessTokenLogon secondary = new(new LUID { LowPart = 43 }, 5, session.Sid, SECURITY_LOGON_TYPE.Interactive, 0, 200);
            ProcessTokenLogon unresolved = new(in reference.AuthenticationId, session.SessionId, Sid: null, reference.LogonType, reference.UserFlags, reference.LogonTime);
            Assert.Null(ProcessTokenProvider.FindReference(session, []));
            Assert.Null(ProcessTokenProvider.FindReference(session, [unresolved]));
            Assert.Null(ProcessTokenProvider.FindReference(session, [secondary]));
            Assert.Same(reference, ProcessTokenProvider.FindReference(session, [secondary, reference]));
            Assert.Null(ProcessTokenProvider.FindReference(session, [reference, reference]));
        }

        /// <summary>
        /// Disposes a refused duplicate while keeping the borrowed source open.
        /// </summary>
        /// <param name="suitableDuplicate">Whether the duplicate matches the source metadata.</param>
        /// <param name="stableReference">Whether the final owner/logon observation remains unchanged.</param>
        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        public void TryDuplicate_TransfersOnlyAcceptedHandles(bool suitableDuplicate, bool stableReference)
        {
            ProcessTokenSession session = new(5, new("S-1-5-21-1-2-3-1001"), 100);
            ProcessTokenLogon reference = CreateLogon();
            using SafeFileHandle source = new(new IntPtr(123), ownsHandle: false);
            using SafeFileHandle duplicate = new(new IntPtr(456), ownsHandle: false);
            bool success = ProcessTokenProvider.TryDuplicate(source, session, reference,
                handle => CreateToken(reference, elevated: ReferenceEquals(handle, duplicate) && !suitableDuplicate),
                _ => duplicate, () => stableReference, out SafeFileHandle? result);
            using (result)
            {
                Assert.Equal(suitableDuplicate && stableReference, success);
                Assert.False(source.IsClosed);
                if (suitableDuplicate && stableReference)
                {
                    Assert.Same(duplicate, result);
                    Assert.False(duplicate.IsClosed);
                }
                else
                {
                    Assert.Null(result);
                    Assert.True(duplicate.IsClosed);
                }
            }
        }

        /// <summary>
        /// Closes the duplicate when post-duplication inspection fails.
        /// </summary>
        [Fact]
        public void TryDuplicate_DisposesOnQueryFailure()
        {
            ProcessTokenSession session = new(5, new("S-1-5-21-1-2-3-1001"), 100);
            ProcessTokenLogon reference = CreateLogon();
            using SafeFileHandle source = new(new IntPtr(123), ownsHandle: false);
            using SafeFileHandle duplicate = new(new IntPtr(456), ownsHandle: false);
            _ = Assert.Throws<UnauthorizedAccessException>(() => ProcessTokenProvider.TryDuplicate(source, session, reference,
                handle => ReferenceEquals(handle, source) ? CreateToken(reference) : throw new UnauthorizedAccessException(),
                _ => duplicate, static () => true, out _));
            Assert.True(duplicate.IsClosed);
            Assert.False(source.IsClosed);
        }

        /// <summary>
        /// Refuses unsuitable source tokens before asking Windows for a duplicate.
        /// </summary>
        [Fact]
        public void TryDuplicate_RejectsSourceBeforeDuplication()
        {
            ProcessTokenSession session = new(5, new("S-1-5-21-1-2-3-1001"), 100);
            ProcessTokenLogon reference = CreateLogon();
            using SafeFileHandle source = new(new IntPtr(123), ownsHandle: false);
            bool success = ProcessTokenProvider.TryDuplicate(source, session, reference,
                _ => CreateToken(reference, elevated: true),
                static _ => throw new InvalidOperationException("Must not duplicate."), static () => throw new InvalidOperationException("Must not recheck."), out SafeFileHandle? result);
            using (result)
            {
                Assert.False(success);
                Assert.Null(result);
                Assert.False(source.IsClosed);
            }
        }

        /// <summary>
        /// Copies the caller's native elevation type rather than defaulting every token to unsplit.
        /// </summary>
        [Fact]
        public void ReadToken_PreservesNativeElevationType()
        {
            using SafeFileHandle token = TokenManager.GetCurrentProcessToken(TOKEN_ACCESS_MASK.TOKEN_QUERY);
            TOKEN_ELEVATION_TYPE expected = TokenUtilities.GetTokenInformation<TOKEN_ELEVATION_TYPE>(token, TOKEN_INFORMATION_CLASS.TokenElevationType);
            Assert.Equal(expected, ProcessTokenProvider.ReadToken(token).ElevationType);
        }

        /// <summary>
        /// Exercises native acquisition without launching a child or invoking the SYSTEM broker.
        /// </summary>
        [Fact(Skip = "Requires an elevated interactive desktop.", SkipUnless = nameof(TestEnvironment.IsElevated), SkipType = typeof(TestEnvironment))]
        public void TryGetToken_NativeAcquisitionReturnsValidatedDesktopToken()
        {
            Assert.SkipWhen(AccountUtilities.CallerIsLocalSystem || AccountUtilities.CallerSessionId is 0, "Requires a non-SYSTEM desktop caller.");
            ProcessTokenSession session = ProcessTokenProvider.ReadSession(AccountUtilities.CallerSessionId);
            bool success = ProcessTokenProvider.TryGetToken(session.SessionId, session.Sid, ElevatedTokenType.None, uiAccess: false, out SafeFileHandle? token);
            using (token)
            {
                Assert.Equal(token is not null, success);
                Assert.SkipWhen(!success, "No accessible unambiguous ordinary desktop token was available.");
                Assert.NotNull(token);
                ProcessTokenMetadata metadata = ProcessTokenProvider.ReadToken(token);
                Assert.True(ProcessTokenProvider.IsSuitable(session, metadata.Logon, metadata));
                Assert.False(token.IsInvalid);
            }
        }

        /// <summary>
        /// Selects elevation without treating an inaccessible split token as an unsplit standard user.
        /// </summary>
        /// <param name="request">The requested elevation.</param>
        /// <param name="elevated">Whether the candidate is elevated.</param>
        /// <param name="type">The native elevation type.</param>
        /// <param name="expected">Whether selection succeeds.</param>
        [Theory]
        [InlineData(ElevatedTokenType.None, false, TOKEN_ELEVATION_TYPE.TokenElevationTypeDefault, true)]
        [InlineData(ElevatedTokenType.None, false, TOKEN_ELEVATION_TYPE.TokenElevationTypeLimited, true)]
        [InlineData(ElevatedTokenType.None, true, TOKEN_ELEVATION_TYPE.TokenElevationTypeFull, false)]
        [InlineData(ElevatedTokenType.HighestAvailable, false, TOKEN_ELEVATION_TYPE.TokenElevationTypeDefault, true)]
        [InlineData(ElevatedTokenType.HighestAvailable, false, TOKEN_ELEVATION_TYPE.TokenElevationTypeLimited, false)]
        [InlineData(ElevatedTokenType.HighestAvailable, true, TOKEN_ELEVATION_TYPE.TokenElevationTypeFull, true)]
        [InlineData(ElevatedTokenType.HighestMandatory, false, TOKEN_ELEVATION_TYPE.TokenElevationTypeDefault, false)]
        [InlineData(ElevatedTokenType.HighestMandatory, false, TOKEN_ELEVATION_TYPE.TokenElevationTypeLimited, false)]
        [InlineData(ElevatedTokenType.HighestMandatory, true, TOKEN_ELEVATION_TYPE.TokenElevationTypeFull, true)]
        [InlineData(ElevatedTokenType.HighestMandatory, true, TOKEN_ELEVATION_TYPE.TokenElevationTypeDefault, true)]
        [InlineData((ElevatedTokenType)99, true, TOKEN_ELEVATION_TYPE.TokenElevationTypeFull, false)]
        public void IsSuitable_SelectsRequestedElevation(ElevatedTokenType request, bool elevated, int type, bool expected)
        {
            ProcessTokenLogon reference = CreateLogon();
            ProcessTokenSession session = new(reference.SessionId, reference.Sid!, 100);
            ProcessTokenMetadata token = new(reference.Sid!, reference.SessionId, reference.AuthenticationId, TOKEN_TYPE.TokenPrimary,
                elevated, new(elevated ? "S-1-16-12288" : "S-1-16-8192"), Restricted: false, AppContainer: false, UIAccess: false, reference, (TOKEN_ELEVATION_TYPE)type);
            Assert.Equal(expected, ProcessTokenProvider.IsSuitable(session, reference, token, request));
        }

        /// <summary>
        /// Accepts alternate authentication IDs only through a validated original-logon counterpart.
        /// </summary>
        /// <param name="difference">The counterpart or candidate evidence to invalidate.</param>
        /// <param name="expected">Whether the candidate is accepted.</param>
        [Theory]
        [InlineData("none", true)]
        [InlineData("missing", false)]
        [InlineData("runas", false)]
        [InlineData("sid", false)]
        [InlineData("session", false)]
        [InlineData("luid", false)]
        [InlineData("impersonation", false)]
        [InlineData("restricted", false)]
        [InlineData("appContainer", false)]
        [InlineData("unsplit", false)]
        [InlineData("netonly", false)]
        public void IsSuitable_RequiresOriginalKernelLinkedCounterpart(string difference, bool expected)
        {
            ProcessTokenLogon reference = CreateLogon();
            ProcessTokenSession session = new(reference.SessionId, reference.Sid!, 100);
            ProcessTokenLogon secondary = new(new LUID { LowPart = 43 }, reference.SessionId, reference.Sid,
                difference.Equals("netonly", StringComparison.Ordinal) ? SECURITY_LOGON_TYPE.NewCredentials : SECURITY_LOGON_TYPE.Interactive, 0, 102);
            ProcessTokenMetadata candidate = new(reference.Sid!, reference.SessionId, secondary.AuthenticationId, TOKEN_TYPE.TokenPrimary,
                Elevated: true, new("S-1-16-12288"), Restricted: false, AppContainer: false, UIAccess: false, secondary, TOKEN_ELEVATION_TYPE.TokenElevationTypeFull);
            ProcessTokenMetadata counterpart = new(difference.Equals("sid", StringComparison.Ordinal) ? new(WellKnownSidType.LocalSystemSid, domainSid: null) : reference.Sid!,
                difference.Equals("session", StringComparison.Ordinal) ? 6u : reference.SessionId,
                difference.Equals("luid", StringComparison.Ordinal) ? secondary.AuthenticationId : reference.AuthenticationId,
                difference.Equals("impersonation", StringComparison.Ordinal) ? TOKEN_TYPE.TokenImpersonation : TOKEN_TYPE.TokenPrimary,
                Elevated: false, new("S-1-16-8192"), difference.Equals("restricted", StringComparison.Ordinal), difference.Equals("appContainer", StringComparison.Ordinal), UIAccess: false,
                difference.Equals("runas", StringComparison.Ordinal) ? secondary : reference,
                difference.Equals("unsplit", StringComparison.Ordinal) ? TOKEN_ELEVATION_TYPE.TokenElevationTypeDefault : TOKEN_ELEVATION_TYPE.TokenElevationTypeLimited);
            Assert.Equal(expected, ProcessTokenProvider.IsSuitable(session, reference, candidate, ElevatedTokenType.HighestMandatory,
                linkedToken: difference.Equals("missing", StringComparison.Ordinal) ? null : counterpart));
        }

        /// <summary>
        /// Matches UIAccess exactly and accepts only desktop integrity levels.
        /// </summary>
        /// <param name="requested">Whether UIAccess is requested.</param>
        /// <param name="present">Whether the candidate has UIAccess.</param>
        /// <param name="integrity">The candidate integrity SID.</param>
        /// <param name="expected">Whether the candidate is accepted.</param>
        [Theory]
        [InlineData(true, true, "S-1-16-8192", true)]
        [InlineData(true, true, "S-1-16-8448", true)]
        [InlineData(true, false, "S-1-16-8192", false)]
        [InlineData(false, true, "S-1-16-8448", false)]
        [InlineData(false, false, "S-1-16-8448", false)]
        [InlineData(true, true, "S-1-16-4096", false)]
        [InlineData(true, true, "S-1-16-16384", false)]
        public void IsSuitable_MatchesUiAccess(bool requested, bool present, string integrity, bool expected)
        {
            ProcessTokenLogon reference = CreateLogon();
            ProcessTokenSession session = new(reference.SessionId, reference.Sid!, 100);
            ProcessTokenMetadata candidate = new(reference.Sid!, reference.SessionId, reference.AuthenticationId, TOKEN_TYPE.TokenPrimary,
                Elevated: false, new(integrity), Restricted: false, AppContainer: false, present, reference);
            Assert.Equal(expected, ProcessTokenProvider.IsSuitable(session, reference, candidate, uiAccess: requested));
        }

        /// <summary>
        /// Allows UIAccess adjustment while validating the resulting duplicate and retaining source ownership.
        /// </summary>
        /// <param name="sourceUiAccess">Whether UIAccess already exists.</param>
        /// <param name="duplicateUiAccess">Whether adjustment succeeded.</param>
        [Theory]
        [InlineData(false, true)]
        [InlineData(true, true)]
        [InlineData(false, false)]
        public void TryDuplicate_RevalidatesUiAccess(bool sourceUiAccess, bool duplicateUiAccess)
        {
            ProcessTokenLogon reference = CreateLogon();
            ProcessTokenSession session = new(reference.SessionId, reference.Sid!, 100);
            using SafeFileHandle source = new(new IntPtr(123), ownsHandle: false);
            using SafeFileHandle duplicate = new(new IntPtr(456), ownsHandle: false);
            bool success = ProcessTokenProvider.TryDuplicate(source, session, reference,
                handle => new(reference.Sid!, reference.SessionId, reference.AuthenticationId, TOKEN_TYPE.TokenPrimary, Elevated: false,
                    new("S-1-16-8192"), Restricted: false, AppContainer: false, ReferenceEquals(handle, source) ? sourceUiAccess : duplicateUiAccess, reference),
                _ => duplicate, static () => true, out SafeFileHandle? result, uiAccess: true);
            using (result)
            {
                Assert.Equal(duplicateUiAccess, success);
                Assert.Equal(!duplicateUiAccess, duplicate.IsClosed);
                Assert.False(source.IsClosed);
            }
        }

        /// <summary>
        /// Declines an inaccessible session without returning a token.
        /// </summary>
        [Fact]
        public void TryGetToken_InvalidSessionReturnsFalse()
        {
            Assert.False(ProcessTokenProvider.TryGetToken(uint.MaxValue, expectedSid: null, ElevatedTokenType.HighestMandatory,
                uiAccess: true, out SafeFileHandle? token));
            using (token)
            {
                Assert.Null(token);
            }
        }

        /// <summary>
        /// Accepts equal SID values from independent session, logon, and token observations.
        /// </summary>
        [Fact]
        public void IsSuitable_AcceptsEqualSidValuesFromDistinctInstances()
        {
            ProcessTokenLogon reference = CreateLogon();
            ProcessTokenLogon observedLogon = CreateLogon();
            ProcessTokenSession session = new(reference.SessionId, new("S-1-5-21-1-2-3-1001"), 100);
            ProcessTokenMetadata token = new(new("S-1-5-21-1-2-3-1001"), session.SessionId, reference.AuthenticationId,
                TOKEN_TYPE.TokenPrimary, Elevated: false, new("S-1-16-8192"),
                Restricted: false, AppContainer: false, UIAccess: false, observedLogon);

            Assert.NotSame(session.Sid, reference.Sid);
            Assert.NotSame(session.Sid, token.Sid);
            Assert.NotSame(reference.Sid, observedLogon.Sid);
            Assert.Equal(reference, observedLogon);
            Assert.Same(reference, ProcessTokenProvider.FindReference(session, [reference]));
            Assert.True(ProcessTokenProvider.IsSuitable(session, reference, token));
            Assert.Equal(session, new ProcessTokenSession(session.SessionId, new("S-1-5-21-1-2-3-1001"), session.LogonTime));
        }

        /// <summary>
        /// Creates the independent original Winlogon reference for deterministic tests.
        /// </summary>
        /// <returns>The reference logon.</returns>
        private static ProcessTokenLogon CreateLogon()
        {
            return new(new LUID { LowPart = 42 }, 5, new("S-1-5-21-1-2-3-1001"), SECURITY_LOGON_TYPE.Interactive, Interop.MSV_SUB_AUTHENTICATION_FILTER.LOGON_WINLOGON, 101);
        }

        /// <summary>
        /// Creates candidate metadata without consulting the machine's security context.
        /// </summary>
        /// <param name="reference">The token's logon record.</param>
        /// <param name="elevated">Whether the token is elevated.</param>
        /// <returns>The candidate metadata.</returns>
        private static ProcessTokenMetadata CreateToken(ProcessTokenLogon reference, bool elevated = false)
        {
            return new(reference.Sid!, reference.SessionId, reference.AuthenticationId, TOKEN_TYPE.TokenPrimary, elevated, new("S-1-16-8192"), Restricted: false, AppContainer: false, UIAccess: false, reference);
        }
    }
}
