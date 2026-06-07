# PSADT Public Function Unit Test Coverage Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Also keep `.github/instructions/pester.md` and `.github/instructions/powershell.md` open while working — they are the authoritative conventions this plan operationalizes.

**Goal:** Add a meaningful Pester v5 unit test file for every one of the 114 remaining untested public functions under `src/PSAppDeployToolkit/Public/`, following the exact patterns already established in `src/PSAppDeployToolkit.Build/Tests/Unit/`.

**Architecture:** One primary unit test file per public function, named `Verb-ADTNoun.Tests.ps1`, placed in `src/PSAppDeployToolkit.Build/Tests/Unit/`. Each file imports the real `PSAppDeployToolkit` module, mocks `Write-ADTLogEntry`, isolates side effects with `$TestDrive` / `TestRegistry:\`, and tests the **public contract** (parameter validation, parameter sets, output type, key behaviour, important error paths) — not implementation details. Functions are tackled in tiers from easiest/highest-value (pure helpers) to hardest (interactive UI), so confidence and momentum build before the tricky cases.

**Tech Stack:** PowerShell 5.1 + 7.x compatible, Pester v5.7.1, PSScriptAnalyzer (`desktop-5.1.14393.206-windows` profile), `New-PesterConfiguration` build runner.

---

## Critical Orientation (read once before starting)

**Where tests live.** Despite `pester.md` line 24 saying `src/Tests/Unit/Verb-ADTNoun.Tests.ps1`, the *actual, build-wired* directory is:

```
src/PSAppDeployToolkit.Build/Tests/Unit/
```

This is set in `src/PSAppDeployToolkit.Build/PSAppDeployToolkit.Build.psm1:108`
(`UnitTests = [System.IO.Path]::Combine($PSScriptRoot, 'Tests', 'Unit')`) and consumed by
`src/PSAppDeployToolkit.Build/Private/Invoke-ADTPesterUnitTesting.ps1:18`. **Put every new test file there.** Do not create a `src/Tests/Unit/` directory.

**How the suite runs in CI/build.** `Invoke-ADTPesterUnitTesting` builds a `New-PesterConfiguration`, points `Run.Path` at the directory above, enables JaCoCo code coverage over `src/PSAppDeployToolkit/*/*.ps1` with a 100% target, and fails the build if `FailedCount -gt 0`. Adding test files automatically raises measured coverage.

**The fast inner TDD loop (use this, not the full build).** Each test file imports the module directly, so you can run a single file in isolation:

```powershell
Invoke-Pester -Path 'src/PSAppDeployToolkit.Build/Tests/Unit/Verb-ADTNoun.Tests.ps1' -Output Detailed
```

If you change `.ps1` source and re-run in the same session and get an error containing `assembly of a different file hash is already loaded`, restart the PowerShell session (documented in `.github/copilot-instructions.md:29`).

**The non-negotiable file skeleton** (every test file starts with this — copied verbatim from the existing suite):

```powershell
BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Verb-ADTNoun' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    # Context blocks here
}
```

**Conventions distilled from `pester.md` (apply to every task):**
- Test the **public contract**, keep tests **proportionate** to risk, prefer **fewer high-value tests** over shallow boilerplate.
- Always `-ModuleName PSAppDeployToolkit` when mocking PSADT commands.
- Use `$TestDrive` for files, `TestRegistry:\` for registry — never write to real `HKLM`/system dirs.
- Prefer specific assertions: validate exception **type**, stable **message fragment**, or **ErrorId** — not a bare `Should -Throw`.
- Use `-ForEach` for repeated validation cases (null/empty/whitespace, invalid enum, boundaries). Do **not** hide multiple cases in a manual `foreach` inside one `It`.
- Recreate mutable fixtures in `BeforeEach`; clean up in `AfterEach`/`AfterAll`; never carry state between `It` blocks.
- For environment-dependent scenarios, use `-Skip` / `Set-ItResult -Skipped` with a clear reason.
- Respect PS 5.1 compatibility (no ternary/null-coalescing/pipeline-chain). Use `SuppressMessageAttribute` narrowly only when PSScriptAnalyzer cannot see variable usage inside Pester scriptblocks (see existing files for the exact attribute string).

---

## The Standard Test Recipe (the repeatable per-function workflow)

Every function task below follows this same TDD micro-sequence. The worked example in **Task 1** shows it filled in completely; later tasks reference this recipe and add only function-specific notes.

**Step A — Read the contract.** Open `src/PSAppDeployToolkit/Public/Verb-ADTNoun.ps1`. Note: parameter sets, validation attributes, pipeline binding, `[OutputType()]`, `SupportsShouldProcess`, whether `.NOTES` says an active ADT session is required, and which collaborators it calls (other `*-ADT*` commands, built-ins like `Copy-Item`, `Start-Process`, `Get-CimInstance`).

**Step B — Decide the seam** (pick the narrowest that proves user-facing behaviour):
- Pure/deterministic → call it with real inputs, assert on output.
- Filesystem → real fixtures in `$TestDrive`.
- Registry → real keys under `TestRegistry:\`.
- Orchestration/branching/error-propagation → `Mock -ModuleName PSAppDeployToolkit` the collaborator(s); assert with `Should -Invoke` only when forwarding/delegation *is* the contract.
- Environment/hardware/session/UI → `-Skip` guard or contract-only tests (see tier guidance).

**Step C — Write the failing test file** (start from the skeleton above). Minimum meaningful coverage:
1. `Context 'Functionality'` — the primary behaviour and output contract; default behaviour when optional params omitted; parameter sets; pipeline input if supported.
2. `Context 'Input Validation'` — for each validated parameter, a `-ForEach` or `$shouldParams`-splat test for null/empty/whitespace and invalid enum/boundary values.
3. `Context 'Error Handling'` (when relevant) — important failure path asserting a specific exception type / ErrorId.

**Step D — Run it and confirm it fails for the right reason.**
```powershell
Invoke-Pester -Path 'src/PSAppDeployToolkit.Build/Tests/Unit/Verb-ADTNoun.Tests.ps1' -Output Detailed
```

**Step E — Adjust until green.** Tests assert the *existing* contract (the source already exists), so failures mean the test's expectation is wrong (wrong ErrorId, wrong type, missing mock). Fix the test, not the source — unless you discover a genuine bug, in which case stop and flag it.

**Step F — Lint the test file.**
```powershell
Invoke-ScriptAnalyzer -Path 'src/PSAppDeployToolkit.Build/Tests/Unit/Verb-ADTNoun.Tests.ps1' -Settings .vscode\PSScriptAnalyzerSettings.psd1
```
Expected: no warnings (add narrow `SuppressMessageAttribute` only for the known "var used inside scriptblock" case, copying the exact pattern from `Set-ADTRegistryKey.Tests.ps1:7`).

**Step G — Commit one function per commit.**
```powershell
git add src/PSAppDeployToolkit.Build/Tests/Unit/Verb-ADTNoun.Tests.ps1
git commit -m "test: add unit tests for Verb-ADTNoun"
```

---

## Reusable Snippets (copy these — they match the existing suite exactly)

**Null/empty/whitespace validation (parameter with `ValidateNotNullOrWhiteSpace`):**
```powershell
Context 'Input Validation' {
    It 'Should verify that <Param> is not null, empty or whitespace' {
        $shouldParams = @{
            Throw = $true
            ExceptionType = [System.Management.Automation.ParameterBindingException]
            ErrorId = 'ParameterArgumentValidationError,Verb-ADTNoun'
        }
        { Verb-ADTNoun -<Param> $null } | Should @shouldParams
        { Verb-ADTNoun -<Param> '' } | Should @shouldParams
        { Verb-ADTNoun -<Param> " `f`n`r`t`v" } | Should @shouldParams
    }
}
```
> Note the literal whitespace string `" `f`n`r`t`v"` — that exact token is the suite-wide convention for the whitespace case.

**Enum / set validation with `-ForEach` (preferred for repeated cases):**
```powershell
It 'Rejects invalid <Param> value <_>' -ForEach @($null, '', 'bogus') {
    { Verb-ADTNoun -<Param> $_ } | Should -Throw -ErrorId 'ParameterArgumentValidationError,Verb-ADTNoun'
}
```

**Mocking a PSADT collaborator and asserting forwarding:**
```powershell
BeforeAll {
    Mock -ModuleName PSAppDeployToolkit Start-ADTProcess { }
}
It 'Forwards the expected arguments to Start-ADTProcess' {
    Verb-ADTNoun -SomeParam 'x'
    Should -Invoke -ModuleName PSAppDeployToolkit Start-ADTProcess -Times 1 -Exactly -ParameterFilter {
        $FilePath -eq 'expected.exe'
    }
}
```

**Registry fixture (from `Set-ADTRegistryKey.Tests.ps1`):**
```powershell
BeforeAll {
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'TestRegistry', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
    $TestRegistry = (New-Item -Path 'TestRegistry:\TestLocation' -ItemType Directory).PSPath
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}
```

**Skip guard for environment-dependent behaviour:**
```powershell
It 'Returns battery status on a real machine' -Skip:(-not (Get-CimInstance Win32_Battery -ErrorAction SilentlyContinue)) {
    Test-ADTBattery -PassThru | Should -BeOfType ([PSADT.Module.BatteryInfo])  # confirm real type from source
}
```

---

# PHASE 1 — Pure / Deterministic Helpers (highest value, lowest cost)

These have no external side effects and deterministic outputs. Do these first; they validate the recipe and produce immediate coverage wins. **Functions (12):** `Convert-ADTValueType`, `Convert-ADTValuesFromRemainingArguments`, `Get-ADTBoundParametersAndDefaultValues`, `Get-ADTMsiExitCodeMessage`, `Get-ADTObjectProperty`, `Invoke-ADTObjectMethod`, `New-ADTErrorRecord`, `New-ADTValidateScriptErrorRecord`, `New-ADTLogFileName`, `Out-ADTPowerShellEncodedCommand`, `Remove-ADTInvalidFileNameChars`, `Select-ADTUniqueObject`.

### Task 1: `Convert-ADTValueType` — WORKED EXAMPLE (full recipe, complete code)

**Files:**
- Read: `src/PSAppDeployToolkit/Public/Convert-ADTValueType.ps1`
- Create: `src/PSAppDeployToolkit.Build/Tests/Unit/Convert-ADTValueType.Tests.ps1`

**Step A — Contract (already read):** Parameters `-Value` (`[System.Nullable[System.Int64]]`, `Mandatory`, `ValueFromPipeline`, `ValidateNotNullOrEmpty`) and `-To` (`[PSADT.Utilities.ValueTypeConverter+ValueTypes]`, `Mandatory`, `ValidateNotNullOrEmpty`). `[OutputType([System.ValueType])]`. No ADT session required. Casts an Int64 to the requested value type **without range errors** (e.g. `256` to `SByte` wraps to `0`).

**Step B — Seam:** Pure. Call directly, assert output value/type.

**Step C — Write the failing test:**

```powershell
BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Convert-ADTValueType' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Functionality' {
        It 'Wraps an out-of-range value into the target type instead of throwing (256 -> SByte = 0)' {
            Convert-ADTValueType -Value 256 -To SByte | Should -Be 0
        }
        It 'Converts <Value> to <To> yielding <Expected>' -ForEach @(
            @{ Value = 7;    To = 'Int32';  Expected = 7 }
            @{ Value = 255;  To = 'Byte';   Expected = 255 }
            @{ Value = 256;  To = 'Byte';   Expected = 0 }
            @{ Value = -1;   To = 'Byte';   Expected = 255 }
        ) {
            Convert-ADTValueType -Value $Value -To $To | Should -Be $Expected
        }
        It 'Returns a System.ValueType' {
            Convert-ADTValueType -Value 1 -To Int32 | Should -BeOfType ([System.ValueType])
        }
        It 'Accepts -Value from the pipeline' {
            256 | Convert-ADTValueType -To Byte | Should -Be 0
        }
    }

    Context 'Input Validation' {
        It 'Should verify that Value is not null' {
            { Convert-ADTValueType -Value $null -To Int32 } | Should -Throw -ErrorId 'ParameterArgumentValidationError,Convert-ADTValueType'
        }
        It 'Should reject an invalid -To value' {
            { Convert-ADTValueType -Value 1 -To 'NotAType' } | Should -Throw -ErrorId 'ParameterArgumentTransformationError,Convert-ADTValueType'
        }
    }
}
```

**Step D — Run, expect FAIL** if any expectation is off (e.g. an ErrorId differs). Example command:
```powershell
Invoke-Pester -Path 'src/PSAppDeployToolkit.Build/Tests/Unit/Convert-ADTValueType.Tests.ps1' -Output Detailed
```
Expected initially: discovery passes; any red `It` tells you which expectation to correct against the real contract.

**Step E — Adjust until all green.** Confirm the exact `-To` rejection ErrorId by reading the parameter type/transformation behaviour; correct the test if it's `ParameterArgumentValidationError` vs `ParameterArgumentTransformationError`.

**Step F — Lint:**
```powershell
Invoke-ScriptAnalyzer -Path 'src/PSAppDeployToolkit.Build/Tests/Unit/Convert-ADTValueType.Tests.ps1' -Settings .vscode\PSScriptAnalyzerSettings.psd1
```
Expected: no output (clean).

**Step G — Commit:**
```powershell
git add src/PSAppDeployToolkit.Build/Tests/Unit/Convert-ADTValueType.Tests.ps1
git commit -m "test: add unit tests for Convert-ADTValueType"
```

### Tasks 2–12: Remaining Phase 1 helpers

Apply the **Standard Test Recipe** to each. Per-function seam notes:

| Task | Function | Seam & key assertions |
|---|---|---|
| 2 | `Out-ADTPowerShellEncodedCommand` | Pure: assert the returned string equals the expected Base64 of the UTF-16LE command (compute expected with `[Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($cmd))`). Validate null/empty/whitespace input. |
| 3 | `Remove-ADTInvalidFileNameChars` | Pure string: feed a name containing `[IO.Path]::GetInvalidFileNameChars()`, assert they are stripped; assert a clean name is unchanged; pipeline input. |
| 4 | `Get-ADTMsiExitCodeMessage` | Lookup: assert a known MSI code (e.g. `1603`) maps to its documented message substring; assert an unknown code path. |
| 5 | `New-ADTErrorRecord` | Pure: assert returns `[System.Management.Automation.ErrorRecord]`, and that `Exception`, `ErrorId`, `ErrorCategory`, `TargetObject` round-trip from the params. Validate mandatory params. |
| 6 | `New-ADTValidateScriptErrorRecord` | Pure: assert returns an `ErrorRecord` with the expected category/ID; validate mandatory params. |
| 7 | `Get-ADTObjectProperty` | Reflection: pass a real object (e.g. `[System.IO.FileInfo]`), assert it returns the named property value; assert error on missing property. |
| 8 | `Invoke-ADTObjectMethod` | Reflection: invoke a known method (e.g. `'abc'.ToUpper()` style via the function's parameter shape), assert result; cover named-parameter set if present. |
| 9 | `Select-ADTUniqueObject` | Collection: feed duplicates, assert de-duplicated output and order/type contract; pipeline input. |
| 10 | `Convert-ADTValuesFromRemainingArguments` | Pure: pass a representative `$args`-style list, assert the produced structure/hashtable. Read source carefully for the exact shape. |
| 11 | `Get-ADTBoundParametersAndDefaultValues` | Needs a host function/`$PSCmdlet`. Use a small in-test advanced function that calls it, or invoke with a crafted `Invocation`. Assert defaults are merged with bound params. (Read source for the call convention.) |
| 12 | `New-ADTLogFileName` | Pure-ish: assert filename format/extension and that invalid chars are handled; may read a config default — mock `Get-ADTConfig` if it does. |

---

# PHASE 2 — Filesystem-Isolatable (`$TestDrive`)

Real fixtures under `$TestDrive`; no machine-global writes. **Functions (8):** `Remove-ADTFile`, `Remove-ADTFolder`, `New-ADTZipFile`, `Get-ADTFileVersion`, `Get-ADTExecutableInfo`, `Get-ADTPEFileArchitecture`, `Set-ADTItemPermission`, `New-ADTMsiTransform`.

Apply the recipe; model fixtures on `New-ADTFolder.Tests.ps1` and `Copy-ADTFile.Tests.ps1`. Per-function notes:

| Task | Function | Seam & key assertions |
|---|---|---|
| 13 | `Remove-ADTFile` | Create files/dirs in `$TestDrive`; assert removal, wildcard support, `-Recurse`, pipeline, and `-ErrorAction Stop` vs `SilentlyContinue` on a missing path. Recreate fixtures in `BeforeEach`. |
| 14 | `Remove-ADTFolder` | As above for folders; assert recursive removal and non-existent-path error semantics. |
| 15 | `New-ADTZipFile` | Zip files from `$TestDrive` source into `$TestDrive` dest; assert archive exists and (optionally) entry count via `[IO.Compression.ZipFile]::OpenRead`. Validate path params. |
| 16 | `Get-ADTFileVersion` | Use a known system binary (e.g. `notepad.exe`/`$env:SystemRoot\System32\kernel32.dll`) for the happy path; assert returns a version string/object. Guard with `-Skip` if the file is absent. Validate path params. |
| 17 | `Get-ADTExecutableInfo` | Point at a real PE (`kernel32.dll`); assert the returned info object shape/type. Skip-guard on absence. |
| 18 | `Get-ADTPEFileArchitecture` | Feed a known 64-bit and (if available) 32-bit PE; assert returned architecture enum/string. Skip-guard if a suitable file isn't present. |
| 19 | `Set-ADTItemPermission` | Operate on a file in `$TestDrive`; assert an ACE is added (inspect `Get-Acl`). Some scenarios may need elevation — use `-Skip:(-not (Test-ADTCallerIsAdmin))` where required. Validate enum params with `-ForEach`. |
| 20 | `New-ADTMsiTransform` | Requires a base MSI + transform tooling; if no fixture MSI is available, write parameter-validation + path-validation tests and `Set-ItResult -Skipped` the behavioural case with a clear reason. |

---

# PHASE 3 — Registry-Isolatable (`TestRegistry:\`)

Model on `Set-ADTRegistryKey.Tests.ps1`. **Functions (1 cleanly isolatable here):** `Remove-ADTRegistryKey`. (Defer history + all-users registry functions live in Phase 6 because they need session/HKU.)

| Task | Function | Seam & key assertions |
|---|---|---|
| 21 | `Remove-ADTRegistryKey` | Create keys/values under `TestRegistry:\TestLocation`; assert key removal, value-only removal (`-Name`), `-Recurse`, and `-SID` validation. Recreate fixtures in `BeforeEach`. Use the `SuppressMessageAttribute` pattern for `$TestRegistry`. |

---

# PHASE 4 — Mock-Orchestrators (mock PSADT collaborators / boundary built-ins)

These coordinate other commands; test branching, parameter forwarding, retry/error propagation by mocking the narrowest collaborator. Use `Should -Invoke` only where forwarding is the contract.

### In-memory module-state group (callbacks) — Functions: `Add-ADTModuleCallback`, `Get-ADTModuleCallback`, `Remove-ADTModuleCallback`, `Clear-ADTModuleCallback`

| Task | Function | Seam & key assertions |
|---|---|---|
| 22 | `Add-ADTModuleCallback` | Add a callback, then assert via `Get-ADTModuleCallback` it is registered for the given hook point. Clean up in `AfterEach` with `Clear-ADTModuleCallback`. Validate the hookpoint enum and callback param. |
| 23 | `Get-ADTModuleCallback` | After adding known callbacks, assert retrieval/filtering by hook point; assert empty when none. |
| 24 | `Remove-ADTModuleCallback` | Add then remove; assert it's gone; removing an unregistered callback path. |
| 25 | `Clear-ADTModuleCallback` | Add several, clear, assert none remain. |
> These four share state; consider authoring 22–25 together so add/get/remove/clear assertions reinforce each other, but keep one test file per function and reset state in `BeforeEach`/`AfterEach`.

### Retry / orchestration

| Task | Function | Seam & key assertions |
|---|---|---|
| 26 | `Invoke-ADTCommandWithRetries` | Pass a scriptblock that throws N-1 times then succeeds (use a script-scoped counter); assert it eventually returns success and was invoked the expected number of times. Assert it rethrows after exhausting retries. Validate retry-count/`-WaitSeconds` params (use tiny waits). |

### Process-wrapper group — mock the launcher, not the OS

| Task | Function | Seam & key assertions |
|---|---|---|
| 27 | `Invoke-ADTRegSvr32` | Mock `Start-ADTProcess` (`-ModuleName PSAppDeployToolkit`); assert it's invoked with `regsvr32` and the expected `/s` + DLL args via `Should -Invoke -ParameterFilter`. Validate DLL path param. |
| 28 | `Register-ADTDll` | Mock `Invoke-ADTRegSvr32`; assert forwarding with the register flag. |
| 29 | `Unregister-ADTDll` | Mock `Invoke-ADTRegSvr32`; assert forwarding with the unregister (`/u`) flag. |
| 30 | `Install-ADTMSUpdates` | Mock the per-file installer collaborator + filesystem enumeration; assert it iterates `.msu`/`.msp` and forwards. Use `$TestDrive` for fixture update files. |
| 31 | `Install-ADTSCCMSoftwareUpdates` | Mock the CIM/SCCM collaborator; assert branch logic and that it forwards/queries as expected. Skip behavioural CIM calls with a clear reason if no CCM provider. |
| 32 | `Invoke-ADTSCCMTask` | Mock `Invoke-CimMethod`/collaborator; assert the correct trigger GUID is sent per task `-ForEach`. |
| 33 | `Test-ADTMSUpdates` | Mock the update-query collaborator; assert boolean contract for installed vs missing KB. |
| 34 | `Mount-ADTWimFile` | Mock `Mount-WindowsImage`/collaborator + `$TestDrive` paths; assert forwarding and returned mount info shape. Validate path/index params. |
| 35 | `Dismount-ADTWimFile` | Mock `Dismount-WindowsImage`/collaborator; assert `-Save`/`-Discard` branch forwarding. |

### Application group

| Task | Function | Seam & key assertions |
|---|---|---|
| 36 | `Get-ADTApplication` | Mock the registry-uninstall enumeration collaborator (or seed `TestRegistry:\` if the function reads a configurable hive); assert filtering by `-Name`/`-ProductCode`, wildcard vs exact (`-NameMatch`), and output object shape. This is high-value — invest here. |
| 37 | `Uninstall-ADTApplication` | Mock `Get-ADTApplication` to return a known app + mock `Start-ADTMsiProcess`/`Start-ADTProcess`; assert it resolves apps and forwards uninstall strings. Assert no-op when nothing matches. |

### App-execution blocking group

| Task | Function | Seam & key assertions |
|---|---|---|
| 38 | `Block-ADTAppExecution` | Mock the IFEO registry writes (seed/inspect `TestRegistry:\`) + scheduled-task/`Set-ADTRegistryKey` collaborators; assert it sets the Debugger value per process `-ForEach`. Requires care — may need session; if so, mock the session getter. |
| 39 | `Unblock-ADTAppExecution` | Mock/inspect the same registry seam; assert the block is removed. |

### Terminal-server install mode

| Task | Function | Seam & key assertions |
|---|---|---|
| 40 | `Enable-ADTTerminalServerInstallMode` | Mock `Start-ADTProcess`/`change user`; assert it invokes `change user /install`. |
| 41 | `Disable-ADTTerminalServerInstallMode` | As above asserting `/execute`. |

### Content cache

| Task | Function | Seam & key assertions |
|---|---|---|
| 42 | `Copy-ADTContentToCache` | Likely needs a session (cache path from config). Mock `Get-ADTSession`/`Get-ADTConfig` to supply a `$TestDrive` cache path; assert files are copied there. |
| 43 | `Remove-ADTContentFromCache` | Same seam; assert removal from the `$TestDrive` cache. |

### MSI property helpers (operate on an MSI database object)

| Task | Function | Seam & key assertions |
|---|---|---|
| 44 | `Get-ADTMsiTableProperty` | Needs a real/sample MSI or a mocked COM `WindowsInstaller.Installer`. If no fixture MSI, write param-validation tests and `Set-ItResult -Skipped` the behavioural case with a reason. |
| 45 | `Set-ADTMsiProperty` | Same approach; assert it opens the DB and writes the property (mock the COM seam) or skip behavioural with a reason. |
| 46 | `New-ADTMsiTransform` | (If not done in Phase 2.) Same MSI-fixture constraint. |
| 47 | `ConvertTo-ADTNTAccountOrSID` | Use **well-known** SIDs for determinism: assert `S-1-5-18` ↔ `NT AUTHORITY\SYSTEM` both directions. Guard locale-specific names with care; cover the parameter sets (`-AccountName`, `-SID`, `-WellKnownSIDName`). Validate null/empty inputs. |

---

# PHASE 5 — Environment-Dependent (skip-guarded, contract-focused)

These read live OS/hardware/user state. Strategy: assert the **output contract** (type/shape, no-throw) on the current machine, and `-Skip` the cases that need specific hardware/state. Keep behavioural assertions honest — do not fake the environment.

**Functions (≈26):** `Get-ADTFreeDiskSpace`, `Get-ADTPendingReboot`, `Get-ADTOperatingSystemInfo`, `Get-ADTLoggedOnUser`, `Get-ADTUserProfiles`, `Get-ADTRunningProcesses`, `Get-ADTWindowTitle`, `Get-ADTPresentationSettingsEnabledUsers`, `Get-ADTUserNotificationState`, `Get-ADTUserToastNotificationMode`, `Get-ADTEnvironmentVariable`, `Set-ADTEnvironmentVariable`, `Remove-ADTEnvironmentVariable`, `Get-ADTPowerShellProcessPath`, `Test-ADTBattery`, `Test-ADTNetworkConnection`, `Test-ADTPowerPoint`, `Test-ADTMicrophoneInUse`, `Test-ADTOobeCompleted`, `Test-ADTEspActive`, `Test-ADTUserInFocusMode`, `Test-ADTUserIsBusy`, `Update-ADTDesktop`, `Update-ADTEnvironmentPsProvider`, `Update-ADTGroupPolicy`, `Set-ADTPowerShellCulture`.

| Task | Function | Seam & key assertions |
|---|---|---|
| 48 | `Get-ADTPowerShellProcessPath` | Deterministic-ish: assert it returns an existing `powershell.exe`/`pwsh.exe` path (`Should -Exist`). Good early win. |
| 49 | `Get-ADTFreeDiskSpace` | Assert returns a numeric/`double` for `C:`; validate drive param; error on a bogus drive. |
| 50 | `Get-ADTOperatingSystemInfo` | Assert returns the documented info type with non-empty key fields. |
| 51 | `Get-ADTPendingReboot` | Assert returns the documented object with boolean members; tolerate either reboot state. |
| 52 | `Get-ADTLoggedOnUser` | Assert returns a (possibly empty) collection of the documented session type without throwing. |
| 53 | `Get-ADTUserProfiles` | Assert returns profile objects including a well-known profile; cover `-ExcludeDefaultUser`/`-ExcludeSystemProfiles` switches. |
| 54 | `Get-ADTRunningProcesses` | Pass a known running process name (e.g. the current host); assert it's found; assert empty for a bogus name. |
| 55 | `Get-ADTWindowTitle` | Assert no-throw and the output shape; `-Skip` interactive-only assertions. |
| 56 | `Get-ADTPresentationSettingsEnabledUsers` | Assert no-throw + output shape; skip if unsupported. |
| 57 | `Get-ADTUserNotificationState` | Assert returns the documented enum/type without throwing. |
| 58 | `Get-ADTUserToastNotificationMode` | Same contract approach. |
| 59 | `Get-ADTEnvironmentVariable` | Set a process-scope var with `[Environment]::SetEnvironmentVariable` in `BeforeEach`, assert retrieval; cover `-Target` set; clean up in `AfterEach`. |
| 60 | `Set-ADTEnvironmentVariable` | Process scope only in tests; set then read back via `[Environment]::GetEnvironmentVariable`; `-Skip` Machine/User scope (needs elevation/persists). Clean up. |
| 61 | `Remove-ADTEnvironmentVariable` | Process scope: set then remove, assert gone. Clean up. |
| 62 | `Test-ADTBattery` | Assert returns the documented type; behavioural case `-Skip:(-not Win32_Battery present)`. |
| 63 | `Test-ADTNetworkConnection` | Assert boolean contract; tolerate connected/disconnected. |
| 64 | `Test-ADTPowerPoint` | Assert boolean/no-throw; `-Skip` the "PowerPoint running" case (not installed in CI). |
| 65 | `Test-ADTMicrophoneInUse` | Assert boolean/no-throw. |
| 66 | `Test-ADTOobeCompleted` | Assert boolean; on a normal dev/CI box assert `$true`. |
| 67 | `Test-ADTEspActive` | Assert boolean/no-throw. |
| 68 | `Test-ADTUserInFocusMode` | Assert boolean/no-throw. |
| 69 | `Test-ADTUserIsBusy` | Assert boolean/no-throw. |
| 70 | `Update-ADTDesktop` | `SupportsShouldProcess`? Assert `-WhatIf` no-throw; behavioural via mock of the interop collaborator if one exists, else `Set-ItResult -Skipped`. |
| 71 | `Update-ADTEnvironmentPsProvider` | Assert it refreshes the session's env drive without throwing. |
| 72 | `Update-ADTGroupPolicy` | Mock `Start-ADTProcess`/`gpupdate`; assert invocation. Don't run real `gpupdate`. |
| 73 | `Set-ADTPowerShellCulture` | `-Skip` (persists machine culture / needs elevation) or mock the registry seam; validate the culture param against `[CultureInfo]`. |

---

# PHASE 6 — Session / Config / Module-State Dependent

These require an initialized module and/or active `ADTSession`. Two viable approaches — prefer (a):
**(a)** Mock the session/config getters (`Get-ADTSession`, `Get-ADTConfig`, `Get-ADTStringTable`, `Test-ADTSessionActive`, `Initialize-ADTModuleIfUninitialized`) with `-ModuleName PSAppDeployToolkit` to return controlled fixtures, then assert the function's logic/branching.
**(b)** For the session lifecycle functions themselves, stand up a minimal real session in `BeforeAll`/tear down in `AfterAll` inside `$TestDrive`, guarding with `-Skip` where elevation or UI is required.

**Functions (≈18):** `Initialize-ADTModule`, `Initialize-ADTModuleIfUninitialized`, `Test-ADTModuleInitialized`, `Get-ADTCommandTable`, `Initialize-ADTFunction`, `Complete-ADTFunction`, `Invoke-ADTFunctionErrorHandler`, `Resolve-ADTErrorRecord`, `Get-ADTConfig`, `Get-ADTStringTable`, `Get-ADTEnvironmentTable`, `Export-ADTEnvironmentTableToSessionState`, `Open-ADTSession`, `Close-ADTSession`, `Get-ADTSession`, `Test-ADTSessionActive`, `Invoke-ADTAllUsersRegistryAction`, `Get-ADTDeferHistory`, `Set-ADTDeferHistory`, `Reset-ADTDeferHistory`.

| Task | Function | Seam & key assertions |
|---|---|---|
| 74 | `Test-ADTModuleInitialized` | Assert boolean reflecting module init state (non-throwing check). Good starting point for this phase. |
| 75 | `Get-ADTCommandTable` | Assert returns the internal command table (read-only dictionary type); non-empty. |
| 76 | `Initialize-ADTModule` | Run against `$TestDrive` scratch where possible; assert idempotency and that `Test-ADTModuleInitialized` becomes `$true`. Tear down. |
| 77 | `Initialize-ADTModuleIfUninitialized` | Assert it initializes when needed and is a no-op otherwise; cover `-PassThruActiveSession`. |
| 78 | `Resolve-ADTErrorRecord` | Mostly pure: feed a crafted `ErrorRecord`, assert the formatted string contains the message/position fragments; cover `-ExcludeErrorRecord`/format switches. |
| 79 | `Invoke-ADTFunctionErrorHandler` | Construct inside a host advanced function; assert it logs (mock `Write-ADTLogEntry`) and honours bound `-ErrorAction`. Read source for the exact call convention. |
| 80 | `Initialize-ADTFunction` | Call from a host function with `$PSCmdlet`/`$ExecutionContext.SessionState`; assert it sets up expected state without throwing. |
| 81 | `Complete-ADTFunction` | Pair with the above; assert clean completion. |
| 82 | `Get-ADTConfig` | Mock the session/module init to supply a fixture config, or assert against the real default config's documented type and a few key sections. |
| 83 | `Get-ADTStringTable` | Assert returns the localized string table (hashtable) with expected top-level keys for the current culture. |
| 84 | `Get-ADTEnvironmentTable` | Assert returns the environment table with well-known keys (e.g. computer/OS entries). |
| 85 | `Export-ADTEnvironmentTableToSessionState` | Call with a fresh `SessionState`; assert variables are created in that scope. |
| 86 | `Get-ADTSession` | Without a session, assert it throws the documented "no active session" error (specific ErrorId). With a mocked/real session, assert it returns it. |
| 87 | `Test-ADTSessionActive` | Assert `$false` with no session; `$true` after one is opened (mock or real). Non-throwing. |
| 88 | `Open-ADTSession` | Stand up a minimal session in `$TestDrive` (mock UI/host bits); assert a session object is created and `Test-ADTSessionActive` is `$true`. `-Skip` UI-bound paths. Tear down with `Close-ADTSession` in `AfterEach`. |
| 89 | `Close-ADTSession` | After opening, assert it closes cleanly and clears active state; cover `-ExitCode` forwarding (mock the exit seam — do **not** let it exit the test host). |
| 90 | `Invoke-ADTAllUsersRegistryAction` | Mock `Get-ADTUserProfiles` to return `$TestDrive`/`TestRegistry:\`-backed fixtures + the HKU mount seam; assert the scriptblock runs per profile. High-value but intricate — read source carefully. |
| 91 | `Get-ADTDeferHistory` | Requires session (defer history is session/registry backed). Mock session + `TestRegistry:\`; assert retrieval shape. |
| 92 | `Set-ADTDeferHistory` | Same seam; set then read back via `Get-ADTDeferHistory`. |
| 93 | `Reset-ADTDeferHistory` | Same seam; set then reset, assert cleared. |

---

# PHASE 7 — Interactive / UI (contract-only or deferred)

These render WPF/WinForms dialogs, toasts, balloon tips, or send input — not meaningfully unit-testable headlessly. Strategy: **parameter-validation + output-type contract tests only**, mocking the UI-presentation collaborator so nothing renders; mark behavioural rendering with `Set-ItResult -Skipped -Because 'requires interactive desktop'`. Do not block the suite on a real window.

**Functions (≈14):** `Show-ADTInstallationProgress`, `Close-ADTInstallationProgress`, `Show-ADTInstallationPrompt`, `Show-ADTInstallationWelcome`, `Show-ADTInstallationRestartPrompt`, `Show-ADTDialogBox`, `Show-ADTBalloonTip`, `Show-ADTNotifyIcon`, `Close-ADTNotifyIcon`, `Show-ADTHelpConsole`, `Send-ADTKeys`, `Set-ADTActiveSetup`, plus UI helpers `Get-ADTUserToastNotificationMode`/`Get-ADTUserNotificationState` (if not covered in Phase 5).

| Task | Function | Seam & key assertions |
|---|---|---|
| 94 | `Show-ADTDialogBox` | Mock the underlying dialog presenter (`-ModuleName PSAppDeployToolkit`); assert parameter validation (button/icon enums via `-ForEach`), default param behaviour, and that the presenter is invoked with mapped args. Skip the real-render assertion. |
| 95 | `Show-ADTInstallationPrompt` | Mock presenter; validate message/button params; `Should -Invoke` forwarding. Skip render. |
| 96 | `Show-ADTInstallationProgress` | Mock presenter; assert it requires a session (specific error without one) and forwards status text. Skip render. |
| 97 | `Close-ADTInstallationProgress` | Mock presenter; assert it closes/no-throws when no progress shown. |
| 98 | `Show-ADTInstallationWelcome` | Mock presenter + `Get-ADTRunningProcesses`; assert close-apps parsing and defer logic branch; skip render. High-value branching even without UI. |
| 99 | `Show-ADTInstallationRestartPrompt` | Mock presenter; validate countdown params; skip render. |
| 100 | `Show-ADTBalloonTip` | Mock presenter; validate params; skip render. |
| 101 | `Show-ADTNotifyIcon` | Mock presenter; validate params; skip render. |
| 102 | `Close-ADTNotifyIcon` | Mock presenter; assert no-throw close. |
| 103 | `Show-ADTHelpConsole` | Validate it launches the console host via a mocked launcher; skip the real window. |
| 104 | `Send-ADTKeys` | Mock the SendKeys/interop seam; assert it forwards the key string to the target window param; skip real input injection. |
| 105 | `Set-ADTActiveSetup` | Mock/inspect `TestRegistry:\` for the ActiveSetup keys + mock session; assert keys are written per the StubExePath/Version params; cover `-PurgeActiveSetupKey`. This one is more testable than the pure-UI cases — invest. |

---

# PHASE 8 — `Write-ADTLogEntry` (special case)

| Task | Function | Seam & key assertions |
|---|---|---|
| 106 | `Write-ADTLogEntry` | This is the function every *other* test mocks, so it has no test of its own. Do **not** mock it here. Redirect its output to a `$TestDrive` log file (supply `-LogFileDirectory`/session fixture), then assert: the line format, severity mapping (`-ForEach` over Info/Warning/Error), and that it honours the configured log style (CMTrace vs Legacy). Mock only `Get-ADTConfig`/session if needed to point logging at `$TestDrive`. High-value: it underpins the whole module. |

---

## Final Verification (after each phase and at the end)

**Step 1 — Run the full unit suite locally via the fast loop:**
```powershell
$cfg = New-PesterConfiguration
$cfg.Run.Path = 'src/PSAppDeployToolkit.Build/Tests/Unit'
$cfg.Output.Verbosity = 'Detailed'
Invoke-Pester -Configuration $cfg
```
Expected: `Failed: 0`. Skipped tests are acceptable when each carries a clear `-Because` reason.

**Step 2 — Lint all new/changed test files:**
```powershell
Invoke-ScriptAnalyzer -Path 'src/PSAppDeployToolkit.Build/Tests/Unit' -Settings .vscode\PSScriptAnalyzerSettings.psd1 -Recurse
```
Expected: no warnings (only narrowly-justified `SuppressMessageAttribute` suppressions remain).

**Step 3 — Confirm the build-integrated runner still passes** (the real gate CI uses):
```powershell
# Full build (heavier): runs Invoke-ADTPesterUnitTesting among other steps.
.\build.ps1
```
Expected: unit-testing step reports 0 failed; coverage delta is positive versus baseline.

**Step 4 — Dispatch a `code-reviewer` pass** (separate lane, per repo conventions): review a sample of each tier's test files against `pester.md` — public-contract focus, specific assertions, correct seams, no machine-global writes, no inter-`It` state.

---

## Execution Guidance & Sequencing

- **Order:** Phases 1 → 8. Within a phase, the table order is roughly easy→hard. Phase 1 + the easy wins in Phase 5 (Tasks 48–49) are the best place to validate the recipe.
- **Commit cadence:** one function per commit (`test: add unit tests for <Function>`). This keeps review small and bisection easy.
- **Scope discipline (YAGNI):** prefer 4–8 high-value `It`s per function over exhaustive matrices. Use `-ForEach` to collapse repetition. Don't manufacture `Context` structure for trivial functions (see `Test-ADTCallerIsAdmin.Tests.ps1`).
- **When a function can't be unit-tested meaningfully** (MSI COM, real UI, hardware), write the parameter-validation + contract tests that *are* possible and `Set-ItResult -Skipped -Because '<reason>'` the rest. Never leave a function with zero file.
- **If you find a real bug** while writing a test (the contract doesn't behave as documented), stop and surface it rather than weakening the test to pass.
- **DRY:** if three+ functions share a fixture (e.g. a sample PE file, a fixture MSI, an HKU mount helper), add it once and reference it; do not copy large fixtures into every file.

---

## Coverage Ledger (track progress)

114 functions across 8 phases (Task numbers 1–106; some tasks cover grouped functions). Maintain a simple checklist as you go — a function is "done" when its test file exists, passes locally, lints clean, and is committed:

- [ ] Phase 1 — Pure helpers (12)
- [ ] Phase 2 — Filesystem (8)
- [ ] Phase 3 — Registry (1)
- [ ] Phase 4 — Mock-orchestrators (~26)
- [ ] Phase 5 — Environment (~26)
- [ ] Phase 6 — Session/config/state (~20)
- [ ] Phase 7 — Interactive/UI (~14)
- [ ] Phase 8 — `Write-ADTLogEntry` (1)
