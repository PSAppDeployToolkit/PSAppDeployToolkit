# Token provenance diagnostics

A standalone Windows console harness for evaluating a possible process-token fast path. Normal runs are read-only; an explicit `--probe-process` option enables the bounded launch experiment described below. It deliberately does **not** reference PSADT or initialize TokenManager, AccountUtilities, or the client/server stack.

## Build and run

Build `PSADT.TokenDiagnostics` in Visual Studio using the solution's normal build configuration. The project targets .NET 8 on Windows and uses the centrally managed CsWin32 generator; it does not introduce hand-written P/Invoke declarations.

From the repository root in PowerShell:

```powershell
$harness = '.\support\PSADT.TokenDiagnostics\bin\Debug\net8.0-windows\PSADT.TokenDiagnostics.exe'
& $harness --help
& $harness
& $harness --session 5
& $harness --session 5 | Set-Content '.\support\PSADT.TokenDiagnostics\bin\Debug\net8.0-windows\session-5.json' -Encoding UTF8
```

Replace `5` with the session to inspect. With no arguments, the harness uses its own process's desktop session. This is not necessarily the session of the active console user; supply `--session` when inspecting another session. Sessions zero and `uint.MaxValue` are rejected.

Use an already elevated administrator console to inspect another account's processes and logons. The harness does not request elevation, explicitly enable privileges, impersonate, call WTSQueryUserToken, invoke a token broker, create credential-based logons, or change ACLs. Without `--probe-process`, it does not duplicate tokens for execution or launch target processes. Reading a linked-token handle is a query; inability to obtain or inspect it is reported.

Exit codes:

- `0`: report generated, caller token readable, and WTS owner/SID snapshot unchanged during collection; any explicitly requested launch probe also succeeded.
- `1`: a required query failed, the owner's SID could not be resolved, the WTS owner snapshot changed, or an explicit probe was rejected or failed.
- `2`: invalid arguments or an unsupported session identifier.

Exit zero does **not** mean every process or LSA query succeeded, nor that any token is safe to reuse. Inspect the error arrays and per-process errors.

## Report contents

- Caller account, primary token, and linked-token observations.
- WTS account/session/state/logon time before and after collection. SID resolution is independent of candidate tokens; a translation failure is retained rather than replaced with a process SID.
- Independently enumerated LSA logons in the requested session, including authentication LUID, account SID, logon type, raw UserFlags, and LOGON_WINLOGON (`0x8000`).
- Every process whose session could be read and matched, with token SID/session, authentication LUID, token-object ID, elevation/type, integrity SID, restricting-SID status, AppContainer status, UIAccess, and the LSA record queried using that token's LUID.
- Linked tokens inspected separately where Windows reports a full or limited UAC token.
- Native failures, including their Win32/NTSTATUS codes. LSA enumeration errors can concern records outside the target session because their metadata could not be read to filter them.

The LUID format is `high-part:low-part` in hexadecimal. Authentication IDs identify logons; token IDs identify token objects. Logon times are raw FILETIME values, not interchangeable keys. Summary token observations can refer to the same underlying token/logon multiple times.

`Restricted` uses IsTokenRestricted (restricting SIDs); it is not an exhaustive description of removed privileges or every possible token modification. Read-only mode observes identity and provenance, **not** suitability for CreateProcessAsUser or equivalence to WTSQueryUserToken. Schema version 2 also includes `LaunchProbe`, which is null unless explicitly requested; `ReadOnly` is false when probe mode is requested, even if validation rejects it before launch.

Reports contain account names, SIDs, logon identifiers, and process names. Keep them private and outside source control. The suggested `bin` destination is ignored; command lines, passwords, and token handles are not included in JSON.

## Initial observation on the development machine

The first default-session run and an explicit session-5 rerun both completed successfully from an elevated administrator account different from the desktop user:

| Observation | Result |
| --- | --- |
| WTS desktop owner | LocalUser, active session 5 |
| Caller | Different SID, elevated full token |
| Owner primary tokens | 93 observed; all shared one authentication LUID |
| Owner logon provenance | One matching LSA record; UserFlags `0x8120`, including LOGON_WINLOGON |
| Elevated administrator provenance | Different LUID/SID; LOGON_WINLOGON absent |
| Process primary-token query failures | 0 of 136 observed processes |
| LSA query failures | 0 |
| Linked-token query failures | 39, Win32 1312 (`ERROR_NO_SUCH_LOGON_SESSION`) |
| Owner AppContainer tokens | 31, despite Winlogon provenance |
| Owner tokens with restricting SIDs | 33, despite Winlogon provenance |
| WTS minus LSA owner logon time | 0.8511369 seconds |

Counts are a point-in-time observation, not expectations built into the harness. The elevated administrator was an Entra account; this does not validate an Entra account as the primary desktop owner. The AppContainer/restricted counts overlap and should not be added.

These observations support querying Winlogon provenance as an elevated non-SYSTEM administrator and demonstrate rejection of the different-account administrator by SID. They also show why provenance alone is insufficient and why exact WTS/LSA timestamp equality is unsuitable on this machine. They do **not** establish that there is always one uniquely identifiable original Winlogon logon.

No linked token was successfully observed in this run. Error 1312 is recorded without inferring its cause or treating it as proof that the account can never have a linked token. Successful linked-token validation remains outstanding.

## Secondary-logon experiment

The desktop user subsequently opened an ordinary same-account `runas` command prompt and a `/netonly` command prompt from the original unelevated LocalUser desktop. The netonly prompt used fictitious network credentials and performed no network operations.

| Command prompt | Token SID | Token session | LSA session | LSA logon type | UserFlags | Winlogon flag |
| --- | --- | --- | --- | --- | --- | --- |
| Original desktop logon | LocalUser | 5 | 5 | 2 (Interactive) | `0x8120` | Present |
| Same-account runas | LocalUser | 5 | 0 | 2 (Interactive) | `0x0120` | Absent |
| Netonly | LocalUser | 5 | 0 | 9 (NewCredentials) | `0x0000` | Absent |

All three tokens were unelevated, medium-integrity, non-AppContainer, without restricting SIDs or UIAccess, but had distinct authentication LUIDs. Therefore SID, TokenSessionId, elevation, and those suitability checks alone could not distinguish them. The LSA session field must not be assumed to equal TokenSessionId. The top-level `Logons` array filters by LSA session, so these secondary records appeared in the per-token `Logon` data rather than that array.

## Explicit duplication and launch probe

After reviewing a current read-only report, run `& $harness --session 5 --probe-process <PID>`, replacing the session and PID with the intended current source. This is an **opt-in execution experiment**, not a read-only query. No arbitrary executable or command-line argument is accepted.

The probe:

1. Requires a resolved, unchanged WTS owner in the caller's own desktop session and exactly one independently enumerated matching Winlogon reference, with no LSA enumeration errors.
2. Opens the selected process token with QUERY and DUPLICATE rights and validates that same token handle against the reference. Same SID/session alone is insufficient.
3. Requires matching token/LSA SID, desktop session, and authentication LUID; a primary, unelevated, medium-integrity token; interactive logon type; Winlogon provenance; and no AppContainer, restricting SIDs, or UIAccess.
4. Duplicates using the rights requested by TokenManager.GetPrimaryToken: QUERY, DUPLICATE, ASSIGN_PRIMARY, ADJUST_DEFAULT, and ADJUST_SESSIONID. It revalidates the resulting token and creates a non-inherited user environment block.
5. Attempts CreateProcessAsUser with only the fixed system command `cmd.exe /d /c exit 0`. If process creation itself fails, it records that failure and tries CreateProcessWithToken with LOGON_WITH_PROFILE. A child validation or execution failure does not trigger another launch.
6. Creates the child suspended and without inherited handles, validates its actual process token, rechecks the WTS owner, resumes it, and waits at most ten seconds. On failure it attempts to terminate **only its own newly created child** and waits at most two additional seconds. Cleanup failures are recorded.

The same-session restriction is deliberate: CreateProcessWithToken does not establish cross-session launching. The probe does not exercise the toolkit's job/handle inheritance, output capture, full environment behavior, or production launch orchestration. Process creation and profile/environment handling can have normal OS side effects; the normal report mode remains read-only.

### Observed probe results

- The same-account runas and different-account administrator prompts were refused by the token validation checks, before duplication or child creation.
- The netonly prompt was refused earlier by Windows: opening its token with duplication rights returned Win32 5 (Access denied). No duplication or launch occurred. This is a native-access denial, **not** a successful execution of the provenance predicate; the separate read-only evidence above establishes the missing provenance.
- The original desktop command prompt's token duplicated successfully. Its duplicate had a different TokenId but preserved the owner SID, session, authentication LUID, primary/unelevated/medium-integrity state, and absence of AppContainer, restricting SIDs, and UIAccess.
- CreateProcessAsUser succeeded. The suspended child's inspected token matched the expected owner and logon identity; it then resumed and exited with code zero. No cleanup failure was reported.
- CreateProcessWithToken was not needed and remains untested. No explicit privilege enabling, broker invocation, impersonation, or ACL changes were performed. Success applies to this caller's existing authorization context, not every administrator configuration.

The JSON evidence is retained locally under the ignored build-output directory: `secondary-logons.json`, `probe-reject-same-account.json`, `probe-reject-netonly.json`, `probe-reject-administrator.json`, and `probe-original-logon.json`. Process identifiers and authentication LUIDs are transient and are not hardcoded in the harness.

## Further validation before production use

Run the same harness in environments where these cases already exist; the harness intentionally does not create them:

- Repeat same-account ordinary `runas`, other-account `runas`, and `runas /netonly` across supported Windows/account-provider configurations; the current LocalUser case is covered above.
- An administrator logged on as the desktop owner, with both normal and elevated processes from the linked UAC pair.
- Credential-prompt elevation, consent elevation, and UAC-disabled base-token behavior.
- Domain and Entra desktop owners, not just secondary administrator logons.
- RDP, disconnected/reconnected sessions, multiple users, and session-ID reuse after logoff.
- Missing Winlogon flags, inaccessible/exiting processes, and ambiguous LSA records.

Do not convert missing evidence into a match. A future production implementation should retain broker fallback and separately validate elevation, integrity, token restrictions, UIAccess requirements, and actual duplication rights.

The current evidence supports proceeding to a limited `ElevatedTokenType.None`, `uiAccess: false` implementation with conservative rejection/fallback tests. Production token selection is still unchanged. Integrated launch behavior, deterministic session-change/ambiguity tests, and the scenarios listed above remain separate validation work; the POC is not a release-readiness assertion.

## Documentation

- [LsaGetLogonSessionData: session owner or local administrator access](https://learn.microsoft.com/en-us/windows/win32/api/ntsecapi/nf-ntsecapi-lsagetlogonsessiondata)
- [SECURITY_LOGON_SESSION_DATA and LOGON_WINLOGON](https://learn.microsoft.com/en-us/windows/win32/api/ntsecapi/ns-ntsecapi-security_logon_session_data)
- [TOKEN_STATISTICS and AuthenticationId](https://learn.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-token_statistics)
- [UAC-linked logon sessions and credential-prompt elevation](https://learn.microsoft.com/en-us/troubleshoot/windows-client/networking/mapped-drives-not-available-from-elevated-command)
