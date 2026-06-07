# MASTER HANDOFF — PSADT Unit-Test Coverage + Real-Fixture Testing Strategy

> **For Claude (fresh session): READ THIS WHOLE FILE FIRST. It is self-contained — you need no prior conversation context.** It tells you where you are, the exact work left, the conventions and gotchas (learned the hard way), how to run the dual-review pipeline, how to parallelize safely, and the real-fixture strategy to layer on top. Two companion docs in this folder give extra detail: `2026-06-06-psadt-unit-test-coverage.md` (original recipe) and `2026-06-07-real-fixtures-testing-strategy.md` (fixtures deep-dive). This master doc supersedes them where they differ.

---

## 0. Where things stand (as of this handoff)

- **Worktree:** `F:\pester.worktrees\psadt-unit-tests` — work here, NOT `F:\pester` (a different worktree/branch). Branch: `psadt-unit-tests`, created off `develop`. ~84 commits ahead of `develop`.
- **Done:** 80 of 142 public functions now have unit tests (the suite started at ~28). **62 public functions remain untested** — the full list is in §6.
- **3 production bugs found via real-fixture testing and fixed (user-approved, with regression tests):**
  1. `New-ADTValidateScriptErrorRecord` — `$ProvidedValue.ToString()` crashed on `$null` (param is `[AllowNull()]`). Fixed: conditional `TargetName`/`TargetType` in the splat.
  2. `Mount-ADTWimFile` / `Dismount-ADTWimFile` — `-WhatIf` threw `NamedParameterNotFound` (the `begin` block splatted `$PSBoundParameters` incl. `WhatIf`/`Confirm` into `Get-WindowsImage`/`Get-ADTMountedWimFile`, which lack them). Fixed: strip `WhatIf`/`Confirm` before splatting.
  3. `Set-ADTMsiProperty` — `$x.Replace("'","''")` SQL escaping is rejected by the MSI engine (fails on single quotes). Fixed: parameterized MSI records (`CreateRecord` + `?` placeholders).
- **1 pre-existing flaky test repaired by the user + me:** `Test-ADTServiceExists.Tests.ps1` had a `while($true)` loop that never terminated (`Get-Service` emits a *non-terminating* error, so its `catch`/`break` never fired). Now uses a single random-GUID service name.
- **Working tree:** clean. Every committed test file is UTF-8-with-BOM, lint-clean, and passes in isolation.

**Your mission:** (A) finish unit coverage for the remaining 62 functions, (B) implement the real-fixture strategy (fake EXE + COM-authored MSI + integration suite), (C) do a final review and open the PR. Use parallel sub-agents per §5.

---

## 1. Orientation & commands

- **Module import (every test file's first lines — verbatim):**
  ```powershell
  BeforeAll {
      Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
      Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
  }
  ```
- **Test location (ALL unit test files):** `src/PSAppDeployToolkit.Build/Tests/Unit/Verb-ADTNoun.Tests.ps1`. (Note: `pester.md` line 24 says `src/Tests/Unit` — that's stale; the build-wired path is the Build module's `Tests/Unit`, set in `PSAppDeployToolkit.Build.psm1` and consumed by `Private/Invoke-ADTPesterUnitTesting.ps1`.)
- **Fast inner loop (one file):**
  ```
  pwsh -NoProfile -Command "Invoke-Pester -Path 'src/PSAppDeployToolkit.Build/Tests/Unit/<Name>.Tests.ps1' -Output Detailed"
  ```
- **Lint (must be clean):**
  ```
  pwsh -NoProfile -Command "Invoke-ScriptAnalyzer -Path 'src/PSAppDeployToolkit.Build/Tests/Unit/<Name>.Tests.ps1' -Settings .vscode\PSScriptAnalyzerSettings.psd1"
  ```
- **Pester 5.7.1, PowerShell 7.x** present. Tests must also be **PS 5.1-compatible** (no ternary `? :`, no `??`, no `&&`/`||` pipeline chains, no `?.`).
- **Do NOT run the full `Tests/Unit` directory as a gate** — a couple of pre-existing tests are slow/elevation-dependent; gate on the specific files you changed instead.
- **Build runners (for reference):** `Invoke-ADTPesterUnitTesting` (coverage on, fails build on any failure) and `Invoke-ADTPesterIntegrationTesting` (coverage off, runs `Tests/Integration` — currently empty).

---

## 2. TWO FOOTGUNS that will silently corrupt your work

### 2.1 UTF-8 BOM is mandatory and the Write/Edit tools DON'T emit it
`.editorconfig` mandates `charset = utf-8-bom`; every sibling test file is UTF-8-with-BOM + CRLF. After writing/editing **every** file (test files AND any source file), normalize it. **Run this via the PowerShell tool, NEVER the Bash tool** (see 2.2):
```powershell
$p = '<ABSOLUTE_PATH>'
$c = [System.IO.File]::ReadAllText($p)
$c = $c -replace "`r?`n", "`r`n"
[System.IO.File]::WriteAllText($p, $c, (New-Object System.Text.UTF8Encoding $true))
```
Verify the first 3 bytes are `efbbbf`:
```powershell
$b = [System.IO.File]::ReadAllBytes('<ABSOLUTE_PATH>')[0..2]; '{0:x2}{1:x2}{2:x2}' -f $b[0],$b[1],$b[2]
```

### 2.2 The bash-backtick trap
If you run the BOM command through the **Bash** tool, bash interprets the backticks in `` "`r?`n" `` as command substitution and **corrupts the file** (it lowercased every capital `N` in one incident — `ModuleName`→`Modulename`, `NewGuid`→`newGuid` — and tests still passed because PowerShell is case-insensitive, so it nearly slipped through). **Always use the PowerShell tool for any pwsh command containing backticks.** After normalizing, sanity-check casing with a case-sensitive grep: `grep -nE "Modulename|newGuid|Typenames|Displayname|BeOftype" <file>` must return nothing.

---

## 3. Test-authoring conventions (the quality bar)

Distilled from `.github/instructions/pester.md` + `powershell.md` + this session's reviews. Study these existing files as exemplars: `Convert-ADTRegistryPath.Tests.ps1`, `New-ADTFolder.Tests.ps1`, `Set-ADTRegistryKey.Tests.ps1`, `Get-ADTServiceStartMode.Tests.ps1` (env-dependent), and the ones produced this session — **`Unblock-ADTAppExecution.Tests.ps1` (gold standard for mock-orchestration)**, `Convert-ADTValueType.Tests.ps1`, `Get-ADTMsiTableProperty.Tests.ps1` (COM fixture), `Block-ADTAppExecution.Tests.ps1` (session-mocked behaviour).

1. **Skeleton:** the import block above, then `Describe 'Verb-ADTNoun' { BeforeAll { Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { } } ... }`. Keep the `Write-ADTLogEntry` mock even if unused (harmless, consistent).
2. **Test the public contract**, proportionately. Prefer a handful of high-value `It`s over shallow boilerplate. Don't over-build.
3. **Specific assertions:** exception **type** + **ErrorId** (via a `$shouldParams` splat), or exact value/type. Never a bare `Should -Throw` when a precise signal exists.
4. **`-ForEach`** for repeated validation cases, with the varying value in the `It` name. The null/empty/whitespace triple is `@($null, '', " `f`n`r`t`v")` (that exact whitespace token is the suite convention).
5. **Mocking:** always `-ModuleName PSAppDeployToolkit`. Mock the **narrowest** collaborator that proves the behaviour. Use `Should -Invoke ... -Times N -Exactly -ParameterFilter { ... }` for forwarding/branching. Keep mocks for orchestration; use real **local** fixtures (`$TestDrive`, `TestRegistry:\`) where they add confidence without machine-global side effects.
6. **Isolation:** recreate mutable fixtures in `BeforeEach`; clean up in `AfterEach`/`AfterAll`; **never** carry state between `It` blocks or rely on test order across files. For functions that mutate module state (e.g. callbacks under `$Script:ADT.Callbacks`), clean up in `AfterEach` so nothing leaks to sibling files.
7. **Never** write to real `HKLM`, `Program Files`, real services, the real desktop/Start Menu, or mount a WIM in a unit test. That's integration (§7).
8. **Suppressions:** add `[System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', '<var>', Justification = '...')]` ONLY for variables used solely inside Pester scriptblocks (copy the exact form from `Set-ADTRegistryKey.Tests.ps1:7`). Apply narrowly.
9. **One function → one file → one commit.** Message: `test: add unit tests for <Function>` with trailer `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`. Do NOT push. Only `git add` your specific file(s), never `git add -A`/`.`.

### 3.1 Hard-won gotchas (these will save you hours)
- **Mandatory-param tests hang.** Calling a function with a mandatory param omitted triggers an interactive prompt that hangs under `-NonInteractive`. Don't test it by invocation — assert metadata instead:
  `(Get-Command <F>).Parameters['<P>'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Contain $true`
- **ErrorIds by validator:** `ValidateNotNullOrWhiteSpace`/`ValidateNotNullOrEmpty` → `ParameterArgumentValidationError,<Func>`. Enum/type transform failure → `ParameterArgumentTransformationError,<Func>`. For `-SID`-style params, `$null` → Validation, empty/whitespace → Transformation (verify per function).
- **`ValidateScript` path failures** throw `[System.ArgumentException]` with ErrorId `Invalid<Param>ParameterValue,<Func>` (NOT `ParameterBindingException`) — because PSADT's `New-ADTValidateScriptErrorRecord` builds an `ArgumentException`.
- **Array params in `-ParameterFilter` need `-contains`, not `-eq`.** `@(0) -eq 0` returns `@(0)` which is then falsy → the filter silently fails. Use `$SuccessExitCodes -contains 0`.
- **`-ErrorVariable` doesn't populate inside `{ } | Should -Throw`** (child scope). Call the function directly in the test body to capture `-ErrorVariable`.
- **`ReadOnlyCollection` return values:** the pipeline unrolls them, so `$result | Should -BeOfType` sees the element type. Use `Should -ActualValue $result -BeOfType (...)` or assert `.Count`. Mutating one throws `MethodException` (explicit-interface members), not `NotSupportedException`.
- **`-WhatIf` from outside the module:** PSADT strips ShouldProcess common params at the module boundary, so for some functions `-WhatIf` from a test throws `ParameterBindingException` (a known PSADT limitation → skip with an honest reason) — BUT for Mount/Dismount-WimFile it was a genuine bug (now fixed). Distinguish: if the function forwards `$PSBoundParameters` into a collaborator lacking `-WhatIf`, that's a bug; if it's the generic boundary strip, it's a documented limitation.
- **`Get-ADTSession` IS mockable** (`Mock -ModuleName PSAppDeployToolkit Get-ADTSession { <fake> }`) — don't skip session-dependent behaviour claiming otherwise. `[Microsoft.Win32.Registry]::SetValue()` and other .NET **statics** are NOT mockable (Pester can't proxy them) — those are legitimately skippable.
- **`Get-ADTApplication` has no mock seam** (reads the live registry via `Get-ChildItem` on real uninstall hives). Its deterministic negative/validation tests are the real guarantee; data-dependent assertions skip honestly on a clean machine.
- **MSI fixtures:** author headlessly via `WindowsInstaller.Installer` COM, `OpenDatabase` **mode 3** (`msiOpenDatabaseModeCreate`), populate via `CREATE TABLE`/`INSERT` SQL, `Commit()`, release every COM handle + GC, then **copy the file** before reading (the P/Invoke reader `MsiOpenDatabase` will hit a file lock otherwise). This pattern is live in `Get-ADTMsiTableProperty.Tests.ps1`.

---

## 4. The execution pipeline (per batch)

This pipeline produced consistently high quality this session; keep using it.

1. **Implement** a batch (one sub-agent, a small group of related functions). Per function: read the source → write the test → fast-loop until `Failed: 0` → BOM-normalize (PowerShell tool) + verify `efbbbf` + casing intact → lint clean → commit (one per function). Fix the TEST to match real behaviour; **never** change a function to make a test pass — if you find a real bug, STOP and surface it (see §4.1).
2. **Spec-compliance review** (one sub-agent, read-only, `general-purpose`): independently re-reads code + runs the tests; verifies contract coverage, no over-build, single-file commits, BOM/casing, and that skips are justified (not masking inability or a bug). Do NOT trust the implementer's report.
3. **Code-quality review** (one sub-agent, `code-reviewer`): assertions test real behaviour, specificity, proportionality, `-ForEach`, state hygiene, PS 5.1 compat. For small homogeneous batches you may COMBINE steps 2+3 into one reviewer (it worked well for the callback/env batches); for complex/mock-heavy or bug-suspect batches, keep them separate and scrutinize skips hard.
4. **Fold in fixes** (send back to the same implementer sub-agent via its agentId; it keeps context). Re-run/BOM/lint/commit.
5. Mark tasks done; move to the next batch.

**Reviews are read-only → run them concurrently with the next batch's implementer.** Commits must serialize (see §5).

### 4.1 Bug-handling policy (the user has set precedent)
When a test reveals a real production bug, do **not** weaken/skip the test to go green. Surface it. The user has consistently chosen **"fix the source (minimal) + add a regression test."** For a clear, low-risk fix that matches an existing pattern, apply it (source + regression test in ONE commit, message `fix: ...`). For anything larger or design-affecting, present the options and let the user choose before touching source. Three bugs were handled this way already (§0).

---

## 5. Parallelization protocol (safe concurrency)

**The only hard constraint is the shared git index** — two `git commit`s racing corrupt/deadlock the index. Everything else parallelizes.

**Recommended pattern — "parallel authoring, serialized commit":**
1. Dispatch **N implementer sub-agents in one message** (they run concurrently), each owning a **disjoint** set of functions (no two touch the same file). Each agent: writes its file(s), runs Pester to green, BOM-normalizes (PowerShell tool), lints — **but does NOT `git commit`.** It reports per file: path, pass/skip counts, BOM ok, lint ok, any suspected bug.
   - Safe because: each `Invoke-Pester` runs in its own pwsh process with its own `$TestDrive`/`TestRegistry:` and its own in-memory module instance (so even module-state functions like the callbacks don't cross-contaminate); different files don't collide.
2. **You (controller) commit sequentially** after agents return: for each finished file, `git add <path>; git commit -m "test: add unit tests for <Function>" ...`. Single committer = no index race. Per-function commit granularity preserved.
3. Then dispatch spec + quality reviewers (read-only) — these can also run in parallel.
4. Fold fixes (parallel authoring again; controller commits).

**Alternative (higher throughput, more bookkeeping):** dispatch implementers with `isolation: "worktree"` so each has its own index and commits in its own worktree; then fast-forward/cherry-pick each worktree's commits onto `psadt-unit-tests` sequentially. Only worth it at large fan-out; the pattern above is simpler and sufficient.

**Concurrency cap:** keep ~3–5 implementer agents in flight (CPU/module-import load). Batches are grouped in §6 to be disjoint and parallel-friendly.

---

## 6. Remaining unit coverage — 62 functions, batched

Apply §3 conventions and the §4 pipeline. Each batch is disjoint (parallel-safe). For env/UI/session functions, prefer **contract + validation** assertions and `-Skip`/`Set-ItResult -Skipped -Because '...'` for paths needing specific hardware/state/elevation/interactive desktop — never fake the environment.

### Batch K — Environment vars + simple state checks (7) — straightforward
`Get-ADTEnvironmentVariable`, `Set-ADTEnvironmentVariable`, `Remove-ADTEnvironmentVariable` (Process scope only in tests: set via `[Environment]::SetEnvironmentVariable(...,'Process')` in `BeforeEach`, assert, clean up in `AfterEach`; `-Skip` Machine/User scope — elevation/persists). `Test-ADTNetworkConnection`, `Test-ADTOobeCompleted`, `Test-ADTBattery` (assert documented return type/bool; `-Skip` hardware-specific cases like "no battery"), `Test-ADTPowerPoint` (bool/no-throw; skip "PowerPoint running").

### Batch L — More state checks + window/profile (6)
`Test-ADTMicrophoneInUse`, `Test-ADTEspActive`, `Test-ADTUserInFocusMode`, `Test-ADTUserIsBusy` (bool/no-throw contract; tolerate either state), `Get-ADTWindowTitle` (no-throw + shape; skip interactive-only), `Get-ADTUserProfiles` (returns profile objects incl. a well-known profile; cover `-ExcludeNTAccount`/`-ExcludeDefaultUser`/`-ExcludeSystemProfiles` switches per source).

### Batch M — Notification/desktop/group-policy/culture (7)
`Get-ADTPresentationSettingsEnabledUsers`, `Get-ADTUserNotificationState`, `Get-ADTUserToastNotificationMode` (documented type/no-throw), `Update-ADTDesktop` (assert `-WhatIf` no-throw / mock the interop seam), `Update-ADTEnvironmentPsProvider` (refreshes session env drive; no-throw), `Update-ADTGroupPolicy` (mock `Start-ADTProcess`/`gpupdate`; assert invocation — do NOT run real gpupdate), `Set-ADTPowerShellCulture` (`-Skip`/mock registry seam; validate culture param against `[System.Globalization.CultureInfo]`).

### Batch N — Module lifecycle & command table (6) — Phase 6 start
`Test-ADTModuleInitialized` (bool, non-throwing), `Get-ADTCommandTable` (returns the internal read-only command dictionary; non-empty), `Initialize-ADTModule`, `Initialize-ADTModuleIfUninitialized` (idempotency; `-PassThruActiveSession`), `Resolve-ADTErrorRecord` (feed a crafted `ErrorRecord`; assert formatted output contains message/position fragments; cover format switches), `Get-ADTConfig` (assert returns the config object/type with key sections — mock session/init if needed).

### Batch O — Function lifecycle helpers (5)
`Initialize-ADTFunction`, `Complete-ADTFunction`, `Invoke-ADTFunctionErrorHandler` (call from a small in-test advanced function with `$PSCmdlet`/`$ExecutionContext.SessionState`; assert state setup/teardown, logging via mocked `Write-ADTLogEntry`, and honouring bound `-ErrorAction`), `Get-ADTStringTable` (localized hashtable with expected top-level keys), `Get-ADTEnvironmentTable` (well-known keys e.g. computer/OS entries).

### Batch P — Session lifecycle (6) — mock session getters or stand up a minimal session
`Get-ADTSession` (no session → throws documented ErrorId; with mocked/real session → returns it), `Test-ADTSessionActive` ($false without, $true after), `Open-ADTSession` (stand up a minimal session in `$TestDrive`, mock UI/host bits, assert created + active; tear down via `Close-ADTSession` in `AfterEach`; `-Skip` UI-bound paths), `Close-ADTSession` (closes cleanly; `-ExitCode` forwarding — mock the exit seam, do NOT let it exit the test host), `Export-ADTEnvironmentTableToSessionState` (creates vars in a fresh `SessionState`), `Invoke-ADTAllUsersRegistryAction` (mock `Get-ADTUserProfiles` + the HKU mount seam with `TestRegistry:`-backed fixtures; assert the scriptblock runs per profile — intricate, read source).

### Batch Q — Defer history (3)
`Get-ADTDeferHistory`, `Set-ADTDeferHistory`, `Reset-ADTDeferHistory` (session + `TestRegistry:`-backed; mock the session getter; set→read-back→reset round-trip).

### Batch R — Process execution (6) — DO AFTER the fake EXE (§7 Task A1)
`Start-ADTProcess`, `Start-ADTProcessAsUser`, `Start-ADTMsiProcess`, `Start-ADTMsiProcessAsUser`, `Start-ADTMspProcess`, `Start-ADTMspProcessAsUser`. Use the **fake installer EXE** for real exit-code/success-code/timeout/output-capture/`-PassThru` assertions (far stronger than mocks). `...AsUser` variants need a secondary user/session — `-Skip` the real cross-user path with a reason and cover param/forwarding via mocked `Start-ADTProcessAsUser` internals where possible. `Start-ADTMsiProcess` unit-level: mock the process launch + use a `$TestDrive` MSI path for arg-construction assertions; the REAL install lifecycle is integration (§7 Phase C).

### Batch S — Desktop/profile removal + Edge extension (3)
`Remove-ADTDesktopShortcut` (mock the desktop path to `$TestDrive`; assert removal), `Remove-ADTFileFromUserProfiles` (mock `Get-ADTUserProfiles` to return `$TestDrive` profiles; assert per-profile removal), `Remove-ADTEdgeExtension` (mock/inspect the Edge policy registry seam in `TestRegistry:`; mirror the existing `Add-ADTEdgeExtension.Tests.ps1`).

### Batch T — UI / interactive (12) — contract-only, mock the presenter, skip rendering
`Show-ADTDialogBox`, `Show-ADTInstallationPrompt`, `Show-ADTInstallationProgress`, `Close-ADTInstallationProgress`, `Show-ADTInstallationWelcome` (mock presenter + `Get-ADTRunningProcesses`; assert close-apps/defer branching — high value even without UI), `Show-ADTInstallationRestartPrompt`, `Show-ADTBalloonTip`, `Show-ADTNotifyIcon`, `Close-ADTNotifyIcon`, `Show-ADTHelpConsole`, `Send-ADTKeys` (mock the SendKeys/interop seam; assert forwarding), `Set-ADTActiveSetup` (mock/inspect `TestRegistry:` ActiveSetup keys + session; assert keys written per StubExePath/Version; cover `-PurgeActiveSetupKey` — more testable than pure UI, invest). For each: mock the UI-presentation collaborator (`-ModuleName PSAppDeployToolkit`) so nothing renders; assert param validation (button/icon enums via `-ForEach`), default behaviour, required-session errors, and presenter forwarding via `Should -Invoke`; `Set-ItResult -Skipped -Because 'requires interactive desktop'` for actual rendering.

### Batch U — Logging (1) — special
`Write-ADTLogEntry` — every other test MOCKS this, so it has no test of its own. Do NOT mock it here. Redirect its output to a `$TestDrive` log (supply `-LogFileDirectory`/a fixture session). Assert: line format, severity mapping (`-ForEach` Info/Warning/Error), and CMTrace-vs-Legacy log style. Mock only `Get-ADTConfig`/session if needed to point logging at `$TestDrive`. High value — underpins the whole module.

**Suggested parallel waves** (disjoint, ~4 agents/wave): Wave 1 = K, L, M, S. Wave 2 = N, O, Q. Wave 3 = P, T. Wave 4 = U + Batch R (R after the fake EXE exists). Adjust to taste; the only rule is disjoint files + serialized commits (§5).

---

## 7. Real-fixture testing strategy (layer on top)

Full rationale in `2026-06-07-real-fixtures-testing-strategy.md`. **Confirmed decisions:** fake EXE = **compile at test time** (no committed binary); test MSI = **WindowsInstaller COM** authoring (no new build dependency). Core principle: **two layers, never mixed** — unit (fast, isolated, no machine mutation, no cross-test ordering) vs integration (real lifecycle, elevation-gated, ordering only *within* a file).

### Phase A — Shared fixture toolkit (no machine mutation)
- **Task A1 — Fake installer EXE (do FIRST; highest value, low risk).** Create `src/PSAppDeployToolkit.Build/Tests/Support/TestFixtures.psm1` with `Get-ADTFakeInstaller` that compiles a tiny C# console app **on first use** via `Add-Type -OutputType ConsoleApplication -OutputAssembly "$TestDrive\FakeInstaller.exe"` (cache per-run). CLI: `--exit-code N` (default 0), `--sleep <ms>`, `--stdout <text>`, `--stderr <text>`, `--write-file <path>` (writes only where told), `--fail-times N --state-file <path>` (fail first N invocations via a counter file, then succeed). NO registry/service/global writes. Smoke-test `--exit-code 3 → $LASTEXITCODE 3`. This is a side-effect-free, deterministic real subprocess — unblocks Batch R and strengthens `Invoke-ADTCommandWithRetries`.
- **Task A2 — `New-ADTTestMsiDatabase` helper** in the same module: author a Property-table MSI (optionally summary stream) in `$TestDrive` via `WindowsInstaller.Installer` COM mode 3 (the working pattern from `Get-ADTMsiTableProperty.Tests.ps1`), release COM + copy-before-read. Refactor the two existing MSI test files to use it (DRY). For **reading** only — not installing.
- **Task A3 — `New-ADTTestRegFile`** (a `.reg` fixture in `$TestDrive`) and, if a non-mounting WIM read is needed, a `New-ADTTestWim` (capture a tiny `$TestDrive` folder via `New-WindowsImage`; mounting is integration-only).
- The `Support/` dir must NOT match the unit runner's `*.Tests.ps1` glob (verify) so helpers aren't executed as tests.

### Phase B — Use local fixtures in the unit suite
- Drive Batch R (process functions) and `Invoke-ADTCommandWithRetries` with the fake EXE for REAL exit-code/output/timeout behaviour. Keep mocks for genuine side-effecting seams (e.g. `Invoke-ADTRegSvr32` mocking `Start-ADTProcess` is correct). Don't blanket-replace orchestration mocks.

### Phase C — Integration suite (the real install→verify→uninstall lifecycle)
Run by the existing `Invoke-ADTPesterIntegrationTesting`; files in `src/PSAppDeployToolkit.Build/Tests/Integration/`; import the **built** module (match `SampleIntegrationTest.Tests.ps1`); `-Tag Integration`; **whole-Describe skip when `-not (Test-ADTCallerIsAdmin)`** with a clear reason. Ordering only via `BeforeAll`/`AfterAll` within a single file; `AfterAll` does best-effort cleanup so a mid-run failure can't poison the machine.
- **C1 — Test install MSI (COM-authored):** a richer MSI that on install creates a namespaced file, an `HKLM\SOFTWARE\PSADT.Test\...` key, a benign stopped service, and a shortcut — all fully removable — plus a `SIMULATEFAIL=1` property (launch condition / custom action) forcing a non-zero install. Author via COM (decision), built into `$TestDrive`/temp by an integration `BeforeAll`.
- **C2 — `Start-ADTMsiProcess.Integration.Tests.ps1`:** ordered install → verify (real `Get-Service`/`Test-Path`/`Get-ADTApplication`) → uninstall → verify removed; separate `Context` for `SIMULATEFAIL=1` asserting the non-zero exit contract and no leftover artifacts.
- **C3 — `Mount-ADTWimFile.Integration.Tests.ps1`:** build a tiny WIM with a marker file → `Mount-ADTWimFile` → assert marker readable + `Get-ADTMountedWimFile` reports it → `Dismount-ADTWimFile -Discard` → assert unmounted; `AfterAll` force-dismounts leftovers.
- **C4 (optional, env-gated):** SCCM / IFEO-real scenarios — skip unless prerequisites present.

### Phase D — Codify
Update `.github/instructions/pester.md` with the unit/integration split, the no-cross-test-ordering / no-function-scaffolds-fixture rules, and the fixture toolkit. Add `Tests/Support/README.md`.

---

## 8. Finishing

When coverage + fixtures are done:
1. Run each changed file's tests green; lint clean; BOM verified.
2. Optionally run the integration suite once on an elevated runner.
3. Dispatch a final `code-reviewer` over the whole branch diff (`git diff develop..HEAD`) for a holistic pass.
4. Use **superpowers:finishing-a-development-branch** to open the PR (target `develop`; this repo also has an `upstream` — confirm target with the user). Summarize: N functions covered, 3 bugs fixed (+ the WIM/MSI fixes), the new fixture toolkit + integration suite.

## 9. Coverage ledger (update as you go)
- [ ] Batch K (env vars + state, 7)
- [ ] Batch L (state + window/profile, 6)
- [ ] Batch M (notification/desktop/GP/culture, 7)
- [ ] Batch N (module lifecycle, 6)
- [ ] Batch O (function lifecycle helpers, 5)
- [ ] Batch P (session lifecycle, 6)
- [ ] Batch Q (defer history, 3)
- [ ] Batch R (process exec, 6) — after fake EXE
- [ ] Batch S (desktop/profile removal, 3)
- [ ] Batch T (UI/interactive, 12)
- [ ] Batch U (Write-ADTLogEntry, 1)
- [ ] Phase A (fixture toolkit: fake EXE, MSI helper, reg/wim)
- [ ] Phase B (apply fixtures to unit suite)
- [ ] Phase C (integration suite: MSI lifecycle, WIM lifecycle)
- [ ] Phase D (codify in pester.md + README)
- [ ] Final review + PR

Total remaining functions: **62** (sum of K–U = 7+6+7+6+5+6+3+6+3+12+1 = 62). ✔
