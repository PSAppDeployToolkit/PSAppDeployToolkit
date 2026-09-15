# Token provenance diagnostics

A standalone, read-only Windows console harness for evaluating a possible process-token fast path. It deliberately does **not** reference PSADT or initialize TokenManager, AccountUtilities, or the client/server stack.

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

Use an already elevated administrator console to inspect another account's processes and logons. The harness does not request elevation, enable privileges, impersonate, call WTSQueryUserToken, invoke a token broker, create logons, duplicate tokens for execution, launch target processes, or change ACLs. Reading a linked-token handle is a query; inability to obtain or inspect it is reported.

Exit codes:

- `0`: report generated, caller token readable, and WTS owner/SID snapshot unchanged during collection.
- `1`: a required query failed, the owner's SID could not be resolved, or the WTS owner snapshot changed.
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

`Restricted` uses IsTokenRestricted (restricting SIDs); it is not an exhaustive description of removed privileges or every possible token modification. The harness observes identity and provenance, **not** suitability for CreateProcessAsUser or equivalence to WTSQueryUserToken.

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

## Further validation before production use

Run the same harness in environments where these cases already exist; the harness intentionally does not create them:

- Same-account ordinary `runas`, other-account `runas`, and `runas /netonly` (NewCredentials).
- An administrator logged on as the desktop owner, with both normal and elevated processes from the linked UAC pair.
- Credential-prompt elevation, consent elevation, and UAC-disabled base-token behavior.
- Domain and Entra desktop owners, not just secondary administrator logons.
- RDP, disconnected/reconnected sessions, multiple users, and session-ID reuse after logoff.
- Missing Winlogon flags, inaccessible/exiting processes, and ambiguous LSA records.

Do not convert missing evidence into a match. A future production implementation should retain broker fallback and separately validate elevation, integrity, token restrictions, UIAccess requirements, and actual duplication rights.

## Documentation

- [LsaGetLogonSessionData: session owner or local administrator access](https://learn.microsoft.com/en-us/windows/win32/api/ntsecapi/nf-ntsecapi-lsagetlogonsessiondata)
- [SECURITY_LOGON_SESSION_DATA and LOGON_WINLOGON](https://learn.microsoft.com/en-us/windows/win32/api/ntsecapi/ns-ntsecapi-security_logon_session_data)
- [TOKEN_STATISTICS and AuthenticationId](https://learn.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-token_statistics)
- [UAC-linked logon sessions and credential-prompt elevation](https://learn.microsoft.com/en-us/troubleshoot/windows-client/networking/mapped-drives-not-available-from-elevated-command)
