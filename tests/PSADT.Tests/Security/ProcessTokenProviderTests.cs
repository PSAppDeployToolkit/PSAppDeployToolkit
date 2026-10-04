using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;
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

            Assert.Equal(expected, ProcessTokenProvider.IsSuitable(session, new(reference), token));
        }

        /// <summary>
        /// Requires one unambiguous original logon, ignoring secondary logons and refusing a repeated record.
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
            ProcessTokenReference? single = ProcessTokenProvider.FindReference(session, [secondary, reference]);
            Assert.Same(reference, single?.Logon);
            Assert.Null(single?.Counterpart);
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
            bool success = ProcessTokenProvider.TryDuplicate(source, session, new(reference),
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
            _ = Assert.Throws<UnauthorizedAccessException>(() => ProcessTokenProvider.TryDuplicate(source, session, new(reference),
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
            bool success = ProcessTokenProvider.TryDuplicate(source, session, new(reference),
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
                Assert.SkipWhen(!success, "No accessible ordinary desktop token was available.");
                Assert.NotNull(token);
                ProcessTokenMetadata metadata = ProcessTokenProvider.ReadToken(token);
                Assert.True(ProcessTokenProvider.IsSuitable(session, new(metadata.Logon), metadata));
                Assert.False(token.IsInvalid);
            }
        }

        /// <summary>
        /// Acquires the elevated half of the caller's own split logon, where a failure cannot be an environment problem.
        /// </summary>
        /// <remarks>Once the caller is an elevated split-token administrator owning its own session, its own process is a
        /// qualifying source, so success is guaranteed and this asserts rather than skipping. It is the only test that runs
        /// the stability recheck and the counterpart-backed candidate together, which no unit test reaches.</remarks>
        [Fact(Skip = "Requires an elevated interactive desktop.", SkipUnless = nameof(TestEnvironment.IsElevated), SkipType = typeof(TestEnvironment))]
        public void TryGetToken_AcquiresTheElevatedHalfOfTheCallersOwnSplitLogon()
        {
            Assert.SkipWhen(AccountUtilities.CallerIsLocalSystem || AccountUtilities.CallerSessionId is 0, "Requires a non-SYSTEM desktop caller.");
            using SafeFileHandle self = TokenManager.GetCurrentProcessToken(TOKEN_ACCESS_MASK.TOKEN_QUERY);
            ProcessTokenMetadata metadata = ProcessTokenProvider.ReadToken(self);
            ProcessTokenSession session = ProcessTokenProvider.ReadSession(AccountUtilities.CallerSessionId);
            Assert.SkipUnless(metadata.Elevated && !metadata.UIAccess && metadata.ElevationType is TOKEN_ELEVATION_TYPE.TokenElevationTypeFull
                && metadata.TokenType is TOKEN_TYPE.TokenPrimary && session.Sid.Equals(metadata.Sid),
                "Requires an elevated split-token caller that owns its own session.");
            foreach (ElevatedTokenType elevation in new[] { ElevatedTokenType.HighestMandatory, ElevatedTokenType.HighestAvailable })
            {
                bool success = ProcessTokenProvider.TryGetToken(session.SessionId, session.Sid, elevation, uiAccess: false, out SafeFileHandle? token);
                using (token)
                {
                    Assert.True(success, $"{elevation} must succeed when the caller's own elevated token is itself a candidate.");
                    Assert.NotNull(token);
                    ProcessTokenMetadata acquired = ProcessTokenProvider.ReadToken(token);
                    Assert.True(acquired.Elevated);
                    Assert.Equal(TOKEN_TYPE.TokenPrimary, acquired.TokenType);
                    Assert.Equal(session.Sid, acquired.Sid);
                    Assert.Equal(session.SessionId, acquired.SessionId);
                }
            }
        }

        /// <summary>
        /// Acquires a UIAccess token from a process that already has one, the only route open without SeTcbPrivilege.
        /// </summary>
        [Fact(Skip = "Requires an elevated interactive desktop.", SkipUnless = nameof(TestEnvironment.IsElevated), SkipType = typeof(TestEnvironment))]
        public void TryGetToken_AcquiresUiAccessFromAnExistingUiAccessProcess()
        {
            Assert.SkipWhen(AccountUtilities.CallerIsLocalSystem || AccountUtilities.CallerSessionId is 0, "Requires a non-SYSTEM desktop caller.");
            ProcessTokenSession session = ProcessTokenProvider.ReadSession(AccountUtilities.CallerSessionId);
            bool success = ProcessTokenProvider.TryGetToken(session.SessionId, session.Sid, ElevatedTokenType.None, uiAccess: true, out SafeFileHandle? token);
            using (token)
            {
                Assert.Equal(token is not null, success);
                Assert.SkipWhen(!success, "No accessible unelevated UIAccess desktop token was available.");
                Assert.NotNull(token);
                ProcessTokenMetadata metadata = ProcessTokenProvider.ReadToken(token);
                Assert.True(metadata.UIAccess);
                Assert.False(metadata.Elevated);
                Assert.Equal(TOKEN_TYPE.TokenPrimary, metadata.TokenType);
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
            Assert.Equal(expected, ProcessTokenProvider.IsSuitable(session, new(reference), token, request));
        }

        /// <summary>
        /// Accepts alternate authentication IDs only through a validated original-logon counterpart.
        /// </summary>
        /// <remarks>The counterpart's token type is deliberately unconstrained: without SeTcbPrivilege Windows only ever
        /// returns an impersonation token for a linked token, and it is read for metadata rather than handed back.</remarks>
        /// <param name="difference">The counterpart or candidate evidence to invalidate.</param>
        /// <param name="expected">Whether the candidate is accepted.</param>
        [Theory]
        [InlineData("none", true)]
        [InlineData("missing", false)]
        [InlineData("runas", false)]
        [InlineData("sid", false)]
        [InlineData("session", false)]
        [InlineData("luid", false)]
        [InlineData("impersonation", true)]
        [InlineData("restricted", false)]
        [InlineData("candidateSession", false)]
        [InlineData("candidateSid", false)]
        [InlineData("candidateNoSid", false)]
        [InlineData("appContainer", false)]
        [InlineData("unsplit", false)]
        [InlineData("netonly", false)]
        public void IsSuitable_RequiresOriginalKernelLinkedCounterpart(string difference, bool expected)
        {
            ProcessTokenLogon reference = CreateLogon();
            ProcessTokenSession session = new(reference.SessionId, reference.Sid!, 100);
            ProcessTokenLogon secondary = new(new LUID { LowPart = 43 },
                difference.Equals("candidateSession", StringComparison.Ordinal) ? 6u : reference.SessionId,
                difference.Equals("candidateNoSid", StringComparison.Ordinal) ? null
                    : difference.Equals("candidateSid", StringComparison.Ordinal) ? new(WellKnownSidType.LocalSystemSid, domainSid: null) : reference.Sid,
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
            Assert.Equal(expected, ProcessTokenProvider.IsSuitable(session, new(reference), candidate, ElevatedTokenType.HighestMandatory,
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
        [InlineData(true, true, "S-1-16-12288", true)]
        [InlineData(false, false, "S-1-16-12288", false)]
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
            Assert.Equal(expected, ProcessTokenProvider.IsSuitable(session, new(reference), candidate, uiAccess: requested));
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
            bool success = ProcessTokenProvider.TryDuplicate(source, session, new(reference),
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
        /// Declines a source that would need UIAccess added when the caller cannot add it, without attempting the duplication.
        /// </summary>
        [Fact]
        public void TryGetCandidateToken_DeclinesUiAccessItCannotAdd()
        {
            Assert.SkipWhen(AccountUtilities.CallerIsLocalSystem || AccountUtilities.CallerSessionId is 0, "Requires a non-SYSTEM desktop caller.");
            Assert.SkipWhen(PrivilegeManager.HasPrivilege(Interop.SE_PRIVILEGE.SeTcbPrivilege), "Requires a caller without SeTcbPrivilege.");
            using SafeFileHandle source = TokenManager.GetCurrentProcessToken(TOKEN_ACCESS_MASK.TOKEN_QUERY | TOKEN_ACCESS_MASK.TOKEN_DUPLICATE);
            ProcessTokenMetadata metadata = ProcessTokenProvider.ReadToken(source);
            ProcessTokenSession session = ProcessTokenProvider.ReadSession(AccountUtilities.CallerSessionId);
            ElevatedTokenType elevation = metadata.Elevated ? ElevatedTokenType.HighestMandatory : ElevatedTokenType.None;
            Assert.SkipUnless(!metadata.UIAccess && ProcessTokenProvider.IsSuitable(session, new(metadata.Logon), metadata, elevation), "The host's own token is not an ordinary desktop token.");
            List<string> thrown = [];
            int threadId = Environment.CurrentManagedThreadId;
            void RecordException(object? sender, FirstChanceExceptionEventArgs e)
            {
                if (Environment.CurrentManagedThreadId == threadId)
                {
                    thrown.Add($"{e.Exception.GetType().Name}: {e.Exception.Message}");
                }
            }

            AppDomain.CurrentDomain.FirstChanceException += RecordException;
            bool success;
            SafeFileHandle? duplicate;
            try
            {
                success = TryGetCandidateToken(source, metadata, session, new(metadata.Logon), elevation, uiAccess: true, out duplicate);
            }
            finally
            {
                AppDomain.CurrentDomain.FirstChanceException -= RecordException;
            }
            using (duplicate)
            {
                Assert.False(success);
                Assert.Null(duplicate);
                Assert.Empty(thrown);
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
            Assert.Same(reference, ProcessTokenProvider.FindReference(session, [reference])?.Logon);
            Assert.True(ProcessTokenProvider.IsSuitable(session, new(reference), token));
            Assert.Equal(session, new ProcessTokenSession(session.SessionId, new("S-1-5-21-1-2-3-1001"), session.LogonTime));
        }

        /// <summary>
        /// Carries both halves of a UAC split logon, which are indistinguishable in LSA, and still refuses a third record.
        /// </summary>
        [Fact]
        public void FindReference_AcceptsSplitPairAndRefusesMore()
        {
            ProcessTokenSession session = new(5, new("S-1-5-21-1-2-3-1001"), 100);
            ProcessTokenLogon limited = CreateLogon();
            ProcessTokenLogon elevated = CreateLogon(43);
            ProcessTokenReference? pair = ProcessTokenProvider.FindReference(session, [elevated, limited]);
            Assert.NotNull(pair);
            Assert.True(pair.Includes(limited));
            Assert.True(pair.Includes(elevated));
            Assert.Equal(pair, ProcessTokenProvider.FindReference(session, [limited, elevated]));
            Assert.Null(ProcessTokenProvider.FindReference(session, [limited, elevated, CreateLogon(44)]));
        }

        /// <summary>
        /// Ties a split pair's halves together by the kernel token link, because LSA alone cannot say they are one logon.
        /// </summary>
        /// <remarks>Run against both orderings of the pair, because which half the candidate belongs to decides which member
        /// is looked for as its counterpart. Real machines put the elevated half first, which is the less obvious branch.</remarks>
        /// <param name="difference">The counterpart evidence to invalidate.</param>
        /// <param name="elevatedSortsFirst">Whether the elevated half holds the lower identifier, as it does in practice.</param>
        /// <param name="expected">Whether the candidate is accepted.</param>
        [Theory]
        [InlineData("none", false, true)]
        [InlineData("missing", false, false)]
        [InlineData("self", false, false)]
        [InlineData("stranger", false, false)]
        [InlineData("impersonation", false, true)]
        [InlineData("unsplit", false, false)]
        [InlineData("none", true, true)]
        [InlineData("missing", true, false)]
        [InlineData("self", true, false)]
        [InlineData("stranger", true, false)]
        [InlineData("impersonation", true, true)]
        [InlineData("unsplit", true, false)]
        public void IsSuitable_RequiresLinkedCounterpartForSplitPair(string difference, bool elevatedSortsFirst, bool expected)
        {
            ProcessTokenLogon limited = CreateLogon(elevatedSortsFirst ? 43u : 42u);
            ProcessTokenLogon elevated = CreateLogon(elevatedSortsFirst ? 42u : 43u);
            ProcessTokenSession session = new(limited.SessionId, limited.Sid!, 100);
            ProcessTokenMetadata candidate = new(limited.Sid!, limited.SessionId, limited.AuthenticationId, TOKEN_TYPE.TokenPrimary,
                Elevated: false, new("S-1-16-8192"), Restricted: false, AppContainer: false, UIAccess: false, limited, TOKEN_ELEVATION_TYPE.TokenElevationTypeLimited);
            ProcessTokenLogon counterpartLogon = difference.Equals("self", StringComparison.Ordinal) ? limited
                : difference.Equals("stranger", StringComparison.Ordinal) ? CreateLogon(44) : elevated;
            ProcessTokenMetadata counterpart = new(limited.Sid!, limited.SessionId, counterpartLogon.AuthenticationId,
                difference.Equals("impersonation", StringComparison.Ordinal) ? TOKEN_TYPE.TokenImpersonation : TOKEN_TYPE.TokenPrimary,
                Elevated: true, new("S-1-16-12288"), Restricted: false, AppContainer: false, UIAccess: false, counterpartLogon,
                difference.Equals("unsplit", StringComparison.Ordinal) ? TOKEN_ELEVATION_TYPE.TokenElevationTypeDefault : TOKEN_ELEVATION_TYPE.TokenElevationTypeFull);
            Assert.Equal(expected, ProcessTokenProvider.IsSuitable(session, new(limited, elevated), candidate, ElevatedTokenType.None,
                linkedToken: difference.Equals("missing", StringComparison.Ordinal) ? null : counterpart));
        }

        /// <summary>
        /// Refuses a candidate from a third logon even when its kernel counterpart is one of the pair's halves.
        /// </summary>
        [Fact]
        public void IsSuitable_RefusesACandidateFromOutsideTheSplitPair()
        {
            ProcessTokenLogon limited = CreateLogon();
            ProcessTokenLogon elevated = CreateLogon(43);
            ProcessTokenLogon stranger = CreateLogon(44);
            ProcessTokenSession session = new(limited.SessionId, limited.Sid!, 100);
            ProcessTokenMetadata candidate = new(limited.Sid!, limited.SessionId, stranger.AuthenticationId, TOKEN_TYPE.TokenPrimary,
                Elevated: false, new("S-1-16-8192"), Restricted: false, AppContainer: false, UIAccess: false, stranger, TOKEN_ELEVATION_TYPE.TokenElevationTypeLimited);
            ProcessTokenMetadata counterpart = new(limited.Sid!, limited.SessionId, limited.AuthenticationId, TOKEN_TYPE.TokenPrimary,
                Elevated: true, new("S-1-16-12288"), Restricted: false, AppContainer: false, UIAccess: false, limited, TOKEN_ELEVATION_TYPE.TokenElevationTypeFull);
            Assert.False(ProcessTokenProvider.IsSuitable(session, new(limited, elevated), candidate, ElevatedTokenType.None, linkedToken: counterpart));
        }

        /// <summary>
        /// Refuses a reference whose own records would not survive the Winlogon filter, on either half.
        /// </summary>
        /// <param name="spoilCounterpart">Whether to spoil the counterpart rather than the only record.</param>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void IsSuitable_RefusesAReferenceThatFailsItsOwnFilter(bool spoilCounterpart)
        {
            ProcessTokenLogon valid = CreateLogon();
            ProcessTokenLogon unflagged = new(new LUID { LowPart = 43 }, valid.SessionId, valid.Sid, valid.LogonType, 0, valid.LogonTime);
            ProcessTokenSession session = new(valid.SessionId, valid.Sid!, 100);
            ProcessTokenLogon candidateLogon = spoilCounterpart ? valid : unflagged;
            ProcessTokenMetadata candidate = new(valid.Sid!, valid.SessionId, candidateLogon.AuthenticationId, TOKEN_TYPE.TokenPrimary,
                Elevated: false, new("S-1-16-8192"), Restricted: false, AppContainer: false, UIAccess: false, candidateLogon, TOKEN_ELEVATION_TYPE.TokenElevationTypeLimited);
            ProcessTokenMetadata counterpart = new(valid.Sid!, valid.SessionId, unflagged.AuthenticationId, TOKEN_TYPE.TokenPrimary,
                Elevated: true, new("S-1-16-12288"), Restricted: false, AppContainer: false, UIAccess: false, unflagged, TOKEN_ELEVATION_TYPE.TokenElevationTypeFull);
            ProcessTokenReference reference = spoilCounterpart ? new(valid, unflagged) : new(unflagged);
            Assert.False(ProcessTokenProvider.IsSuitable(session, reference, candidate, ElevatedTokenType.None, linkedToken: spoilCounterpart ? counterpart : null));
        }

        /// <summary>
        /// Requires an elevated token to carry high integrity, so one demoted below it cannot pass as elevated.
        /// </summary>
        /// <param name="integrity">The candidate integrity SID.</param>
        /// <param name="expected">Whether the candidate is accepted.</param>
        [Theory]
        [InlineData("S-1-16-12288", true)]
        [InlineData("S-1-16-8192", false)]
        [InlineData("S-1-16-8448", false)]
        [InlineData("S-1-16-16384", false)]
        public void IsSuitable_RequiresHighIntegrityForAnElevatedToken(string integrity, bool expected)
        {
            ProcessTokenLogon reference = CreateLogon();
            ProcessTokenSession session = new(reference.SessionId, reference.Sid!, 100);
            ProcessTokenMetadata token = new(reference.Sid!, reference.SessionId, reference.AuthenticationId, TOKEN_TYPE.TokenPrimary,
                Elevated: true, new(integrity), Restricted: false, AppContainer: false, UIAccess: false, reference, TOKEN_ELEVATION_TYPE.TokenElevationTypeFull);
            Assert.Equal(expected, ProcessTokenProvider.IsSuitable(session, new(reference), token, ElevatedTokenType.HighestMandatory));
        }

        /// <summary>
        /// Refuses session zero even when every other piece of evidence agrees, because it is not a desktop.
        /// </summary>
        [Fact]
        public void IsSuitable_RefusesSessionZero()
        {
            ProcessTokenLogon logon = new(new LUID { LowPart = 42 }, 0, new("S-1-5-21-1-2-3-1001"), SECURITY_LOGON_TYPE.Interactive,
                Interop.MSV_SUB_AUTHENTICATION_FILTER.LOGON_WINLOGON, 101);
            ProcessTokenSession session = new(0, logon.Sid!, 100);
            ProcessTokenMetadata token = new(logon.Sid!, 0, logon.AuthenticationId, TOKEN_TYPE.TokenPrimary, Elevated: false,
                new("S-1-16-8192"), Restricted: false, AppContainer: false, UIAccess: false, logon);
            Assert.False(ProcessTokenProvider.IsSuitable(session, new(logon), token));
        }

        /// <summary>
        /// Creates the independent original Winlogon reference for deterministic tests.
        /// </summary>
        /// <param name="identifier">The logon identifier, which distinguishes the halves of a split pair.</param>
        /// <returns>The reference logon.</returns>
        private static ProcessTokenLogon CreateLogon(uint identifier = 42)
        {
            return new(new LUID { LowPart = identifier }, 5, new("S-1-5-21-1-2-3-1001"), SECURITY_LOGON_TYPE.Interactive, Interop.MSV_SUB_AUTHENTICATION_FILTER.LOGON_WINLOGON, 101);
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

        /// <summary>
        /// Asks the provider about one candidate source, which stays private rather than being widened for the tests.
        /// </summary>
        /// <param name="source">The borrowed token.</param>
        /// <param name="metadata">The source metadata.</param>
        /// <param name="session">The desktop owner.</param>
        /// <param name="reference">The original logon, which may be a split pair.</param>
        /// <param name="elevatedTokenType">The requested elevation.</param>
        /// <param name="uiAccess">Whether UIAccess is required.</param>
        /// <param name="duplicate">The validated duplicate.</param>
        /// <returns>Whether acquisition succeeded.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the method cannot be found or does not return a boolean.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3011:Reflection should not be used to increase accessibility of classes, methods, or fields", Justification = "The candidate step is deliberately private, so the tests reach it by reflection rather than widening it.")]
        private static bool TryGetCandidateToken(SafeFileHandle source, ProcessTokenMetadata metadata, ProcessTokenSession session, ProcessTokenReference reference, ElevatedTokenType elevatedTokenType, bool uiAccess, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out SafeFileHandle? duplicate)
        {
            MethodInfo method = typeof(ProcessTokenProvider).GetMethod("TryGetCandidateToken", BindingFlags.Static | BindingFlags.NonPublic) ?? throw new InvalidOperationException("The TryGetCandidateToken method was not found.");
            object?[] arguments = [source, metadata, session, reference, elevatedTokenType, uiAccess, null, null];
            bool success = method.Invoke(null, arguments) is bool result ? result : throw new InvalidOperationException("The TryGetCandidateToken method did not return a boolean.");
            duplicate = arguments[7] as SafeFileHandle;
            return success && duplicate is not null;
        }
    }
}
