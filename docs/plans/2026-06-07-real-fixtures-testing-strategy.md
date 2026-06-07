# Real-Fixture Testing Strategy (Unit fixtures + Integration lifecycle) — Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: when executing, use superpowers:executing-plans (or subagent-driven-development) task-by-task. Keep `.github/instructions/pester.md` and `powershell.md` open — this plan operationalizes them.

**Goal:** Reduce reliance on mocks by introducing (a) side-effect-free **local fixtures** + a **fake-installer EXE** for the unit suite, and (b) a proper **elevation-gated integration suite** that exercises the real install → verify → uninstall lifecycle (incl. forced-failure modes) using a purpose-built test MSI and the fake EXE.

**Architecture (the load-bearing decision):** Two distinct layers, never mixed.
- **Unit layer** (`src/PSAppDeployToolkit.Build/Tests/Unit/`): fast, isolated, **no machine-global mutation**, **no cross-test ordering**. Real fixtures only when they live in `$TestDrive` / `TestRegistry:\` (e.g. read an MSI, run the fake EXE, parse a `.reg`). Mocks remain legitimate for orchestration/branching/error-propagation.
- **Integration layer** (`src/PSAppDeployToolkit.Build/Tests/Integration/`, run by the existing `Invoke-ADTPesterIntegrationTesting`): real WIM mount, real MSI install/uninstall, real service/shortcut/registry verification, forced failures. **Elevation-gated** (`-Tag Integration`, skipped when not admin). Ordering happens **within a single file** via `BeforeAll`/`AfterAll`, never across files.

**Anti-patterns this plan explicitly forbids:**
- Ordering unit tests so one scaffolds resources for another (breaks isolation/parallelism/single-run; contradicts `pester.md`).
- Building a fixture by calling the **function under test** (circular coupling → cascading, undiagnosable failures). Fixtures are built by independent, trusted helper code.
- Any unit test that writes to real `HKLM`, `Program Files`, real services, the real desktop/Start Menu, or mounts a WIM.

**Tech stack:** PowerShell 5.1+7, Pester v5.7.1, a tiny C# console EXE (built via the existing `Invoke-ADTDotNetCompilation` tooling), WiX or WindowsInstaller COM for the test MSI, DISM for WIM.

---

## Existing facts this plan builds on (verified)
- The build already separates runners: `Invoke-ADTPesterUnitTesting` (coverage on, `Tests/Unit`) and `Invoke-ADTPesterIntegrationTesting` (coverage off, `Tests/Integration`). The integration suite is empty (only a commented `SampleIntegrationTest.Tests.ps1`).
- The integration sample imports the **built** module from `Artifacts`, and tags `-Tag Integration`.
- The Build module compiles C# via `Invoke-ADTDotNetCompilation.ps1` → a test EXE can be produced by the same toolchain.
- A real MSI **can** be authored headlessly via `WindowsInstaller.Installer` COM (`OpenDatabase` mode 3) — already proven in `Get-ADTMsiTableProperty.Tests.ps1` / `Set-ADTMsiProperty.Tests.ps1`.
- WIM **mount** and MSI **install** require elevation + mutate machine state → integration-only.
- Process-exec consumers of the fake EXE: `Start-ADTProcess`, `Start-ADTProcessAsUser`, `Start-ADTMsiProcess(+AsUser)`, `Start-ADTMspProcess(+AsUser)`, `Invoke-ADTCommandWithRetries`, `Invoke-ADTRegSvr32`.

---

# PHASE A — Shared unit-test fixture toolkit (no machine mutation)

Create one dot-sourced helper that builds **local** fixtures in `$TestDrive`, used by unit tests. Fixtures are built by independent code (raw COM/.NET/DISM-free), **never** by the functions under test.

### Task A1: Fake-installer EXE (the highest-value item)
**Files:**
- Create: `src/PSADT.TestTools/PSADT.TestTools.csproj` (or a folder the build's `Invoke-ADTDotNetCompilation` picks up — confirm the discovery glob first)
- Create: `src/PSADT.TestTools/FakeInstaller.cs`
- Build output consumed by tests at a known path (e.g. `Tests/Assets/FakeInstaller.exe`)

**Behaviour (a deterministic, side-effect-free CLI):**
- `--exit-code <N>` → returns N (default 0).
- `--sleep <ms>` → sleeps, to test timeouts/`Invoke-ADTCommandWithRetries`.
- `--stdout <text>` / `--stderr <text>` → writes to the respective stream (test output capture).
- `--write-file <path>` → writes a marker file **only at the caller-supplied path** (tests pass a `$TestDrive` path).
- `--fail-times <N> --state-file <path>` → fails (non-zero) the first N invocations using a counter file, then succeeds (deterministic retry testing without a flaky scriptblock).
- No registry, no service, no global writes. Mirrors an installer's `/SimulateFail` contract but harmless.

**Steps:** write csproj+cs → confirm build wiring (does `Invoke-ADTDotNetCompilation` build it? if not, add a minimal test-only build step or compile via `Add-Type -OutputType ConsoleApplication` in the fixture helper) → produce the exe → smoke-test `FakeInstaller.exe --exit-code 3; $LASTEXITCODE -eq 3` → commit.

**Decision to surface before building:** prefer a checked-in tiny `.csproj` compiled by the build, OR on-the-fly `Add-Type -OutputType ConsoleApplication` at test time (no repo binary). The latter avoids shipping a binary and is simpler; the former is faster per-run. Recommend **Add-Type at test-bootstrap** into `$TestDrive` unless the build team wants a committed artifact.

### Task A2: `New-ADTTestMsi` fixture helper (local, read-only consumers)
**Files:** Create `src/PSAppDeployToolkit.Build/Tests/Support/TestFixtures.psm1` (new `Support/` dir; not discovered by the `*.Tests.ps1` runner glob — verify).
- Function `New-ADTTestMsiDatabase -Path <TestDrive path> [-Properties @{}] [-WithSummaryInfo]` authoring a Property table (+ optional summary stream) via raw `WindowsInstaller.Installer` COM (mode 3), releasing COM handles and copying the file so P/Invoke readers don't hit a file lock (the technique already working in the MSI unit tests). This is for **reading** (Get-ADTMsiTableProperty) — NOT installing.
- Function `Get-ADTFakeInstallerPath` returning the Task-A1 exe (compiling on first use if needed).
- Function `New-ADTTestRegFile -Path <TestDrive path>` producing a `.reg` fixture.

**Steps:** write module → unit-test the helper itself briefly (it produces a readable MSI) → commit. Existing MSI unit tests can later be refactored to call this helper (DRY) — optional follow-up, not required.

### Task A3: Small real WIM fixture builder (for read-only/contract use only)
**Files:** add `New-ADTTestWim -Path <TestDrive path> -SourceFolder <TestDrive folder>` to `TestFixtures.psm1` using `New-WindowsImage`/`dism /capture` over a tiny `$TestDrive` folder.
- **Note:** capturing a WIM does NOT require mount/elevation; **mounting** it does. So this builder is usable in unit tests only for things that *read* WIM metadata without mounting. Actual mount/dismount stays in Phase C. If `New-WindowsImage` needs elevation on the target platform, gate with `-Skip` and move entirely to Phase C.

---

# PHASE B — Apply local fixtures to the UNIT suite (replace mocks where it adds confidence without side effects)

For each function below, **read the current test**, and where a mock merely simulates a local operation, replace it with the real local fixture; keep mocks for genuine orchestration seams.

### Task B1: Process-exec functions via the fake EXE
- `Start-ADTProcess` (+ `Start-ADTProcessAsUser` where feasible without a second user): use `FakeInstaller.exe` to assert REAL exit-code handling, `-SuccessExitCodes`/`-IgnoreExitCodes` logic, stdout/stderr capture, `-PassThru` result object, working-directory, and timeout behaviour — instead of mocking the process. This is far stronger than mock-based coverage and is a core PSADT function. One test file per function; `$TestDrive` for any output paths.
- `Invoke-ADTCommandWithRetries`: replace the throwaway scriptblock with `FakeInstaller.exe --fail-times 2 --state-file $TestDrive\c.txt` to exercise real retry-then-succeed.
- `Invoke-ADTRegSvr32`: still mock `Start-ADTProcess` (the registration is a real DLL side effect) — no change; this is the correct seam.

### Task B2: MSI read functions via `New-ADTTestMsiDatabase`
- Already done for `Get-ADTMsiTableProperty` / `Set-ADTMsiProperty`. Refactor them to call the shared `New-ADTTestMsiDatabase` helper (remove duplicated COM-authoring code). `Get-ADTMsiExitCodeMessage` needs **no** MSI — leave as-is.

### Task B3: `.reg` / registry read where local
- Any function that parses or reads a `.reg`/registry locally: use `New-ADTTestRegFile` + `TestRegistry:\`. (Audit first — there may be none in the public surface; if so, skip.)

---

# PHASE C — Integration suite: the real install → verify → uninstall lifecycle

This is where the proposal's "real world" testing belongs. Run by `Invoke-ADTPesterIntegrationTesting`; **elevation-gated**; ordered **within each file** via `BeforeAll`/`AfterAll`.

### Task C0: Integration harness conventions
- Every integration file: `-Tag Integration`; top-level guard `BeforeDiscovery`/`BeforeAll` that `Set-ItResult -Skipped`/skips the whole `Describe` when `-not (Test-ADTCallerIsAdmin)` with a clear reason. Import the built module from `Artifacts` (match the sample).
- Document in `pester.md` (or a new `Tests/Integration/README.md`) that integration tests mutate machine state, need elevation, and are NOT part of the fast unit gate.

### Task C1: Purpose-built test MSI (richer than the read-only one)
**Files:** `src/PSADT.TestTools/TestPackage/` — a WiX (`.wxs`) or COM-authored MSI that, on install, creates: a file under a TestDrive-like temp dir, an `HKLM\SOFTWARE\PSADT.Test\...` key, a benign Windows service (stopped), and a Start-Menu/desktop shortcut — all under clearly-namespaced, fully-removable locations. Supports a `SIMULATEFAIL=1` property (a launch condition or custom action returning failure) to force a non-zero install.
- Build via the .NET/WiX toolchain in the build (gate the build step on WiX availability; if absent, the integration MSI tests skip with a reason).

### Task C2: MSI install/uninstall lifecycle (one ordered file)
`Start-ADTMsiProcess.Integration.Tests.ps1`:
- `BeforeAll`: copy the test MSI to a temp dir.
- Ordered `It`s (within this file only): install (`-Action Install`) → assert the file/regkey/service/shortcut exist (real `Get-Service`, `Test-Path`, `Get-ADTApplication`) → repair/modify if applicable → uninstall (`-Action Uninstall`) → assert all artifacts removed.
- A separate `Context` for failure mode: install with `SIMULATEFAIL=1`, assert the documented non-zero exit handling (the function's error/exit-code contract) and that no artifacts were left behind.
- `AfterAll`: best-effort cleanup (force-uninstall by ProductCode) so a mid-run failure can't poison the machine.

### Task C3: WIM mount/read/dismount lifecycle (one ordered file)
`Mount-ADTWimFile.Integration.Tests.ps1`:
- `BeforeAll`: build a tiny WIM (Phase A3 builder or `dism /capture` of a temp folder containing a known marker file).
- Ordered: `Mount-ADTWimFile` → assert the marker file is readable at the mount path → assert `Get-ADTMountedWimFile` reports it → `Dismount-ADTWimFile -Discard` → assert unmounted.
- `AfterAll`: force-dismount any leftover mounts. Elevation-gated.

### Task C4: SCCM / other elevation-real scenarios (optional, environment-gated)
- `Invoke-ADTSCCMTask`, `Install-ADTSCCMSoftwareUpdates`, `Block/Unblock-ADTAppExecution` (real IFEO + scheduled task) — only meaningful with the relevant infra/elevation. Add as integration files **skipped unless** the environment provides the prerequisite (CCM client present / admin), with honest skip reasons. Do not block CI on these.

---

# PHASE D — Documentation & guardrails

### Task D1: Update `.github/instructions/pester.md`
- Add a short section codifying the unit/integration split, the "no cross-test ordering, no function-scaffolds-fixture" rules, the fake-EXE/test-MSI fixtures, and when to prefer a real local fixture over a mock. This makes the strategy durable for future contributors and for the remaining untested functions.

### Task D2: Fixture README
- `Tests/Support/README.md` documenting how to use `New-ADTTestMsiDatabase`, the fake EXE, and the integration harness (elevation, tags, cleanup contract).

---

## Impact on the in-flight unit-test effort (the 114-function plan)
- **No rework required** to completed unit tests; they remain valid. Phase B is an *enhancement* pass on a handful of process/MSI functions where a local real fixture beats the current mock.
- The remaining untested functions (Phases 5–8 of the coverage plan: environment, session, UI) gain the most from the **fake EXE** (process-launching ones) and from clear guidance on what's unit vs integration (so UI/elevation/session paths get correctly routed to integration or `-Skip` rather than forced into brittle unit tests).
- Net: this plan *raises the ceiling* on confidence (real lifecycle coverage) without lowering the floor (fast isolated unit gate stays green and quick).

## Sequencing recommendation
1. **Task A1 (fake EXE)** first — highest value, unblocks B1, low risk.
2. **Task A2 (MSI helper)** — consolidates what already works.
3. **Phase B1** — convert process-exec unit tests to the fake EXE (big confidence win on core functions).
4. **Phase C** — stand up the integration suite (C0 → C2 → C3), elevation-gated, off the fast CI path.
5. **Phase D** — codify so it sticks.

## Open decisions to confirm before building
1. **Fake EXE delivery:** on-the-fly `Add-Type -OutputType ConsoleApplication` into `$TestDrive` (no committed binary; recommended) vs a committed/built `PSADT.TestTools.exe`.
2. **Test MSI authoring:** WiX (clean, declarative, needs WiX in the build env) vs WindowsInstaller COM authoring (no extra dep, more code). Recommend WiX if the build env can carry it, else COM.
3. **Integration CI:** does CI have an elevated, interactive-capable runner? If not, integration runs become "local/manual + nightly elevated runner," and CI keeps only the unit gate.
4. **Scope of Phase B migration:** which existing mock-based unit tests are worth converting now vs leaving (recommend: only process-exec + MSI-read; leave pure-orchestration mocks alone).
