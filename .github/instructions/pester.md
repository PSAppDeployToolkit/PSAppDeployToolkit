---
applyTo: "**/*.Tests.ps1"
---

# Pester Test Conventions for PSAppDeployToolkit

These conventions define how **Pester v5** tests should be written in this repository. Keep them focused on repository-specific expectations and avoid copying weaker patterns from the current suite.

Shared PowerShell language rules live in `powershell.md`. This file only covers test-specific guidance for `*.Tests.ps1` files.

## Core Principles

- Test the **public contract** of a PSADT command, not incidental implementation details.
- Prefer a test-first or test-with-change workflow for bug fixes and new public behaviour when practical.
- Keep tests proportionate to the risk and complexity of the command.
- Prefer fewer high-value tests over broad but shallow boilerplate.

## Repository Expectations

### Public surface coverage

The long-term direction for this repository is for every public function under `src/PSAppDeployToolkit/Public/` to have meaningful coverage.

- In most cases, use one primary unit test file per public function: `src/Tests/Unit/Verb-ADTNoun.Tests.ps1`.
- Keep module-wide checks in `PSAppDeployToolkit-Module.Tests.ps1` and `ExportedFunctions.Tests.ps1`.
- When touching an untested public command, prefer adding meaningful coverage rather than leaving it untested.

### Test type boundaries

- Unit tests should run quickly, isolate behaviour, and avoid machine-level side effects.
- Prefer `$TestDrive` for file fixtures and `TestRegistry:\` for registry fixtures.
- Put real integration coverage under `src/Tests/Integration/`.
- Require elevation only when the scenario actually needs it.
- For environment-dependent tests, use `-Skip` or `Set-ItResult -Skipped` with a clear reason.

## Module Import Pattern

Use a consistent import pattern before the main `Describe` block:

```powershell
BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
```

Do not use `Set-Location` just to make relative imports work.

If `-ForEach` data depends on the imported module, add `BeforeDiscovery` as well. `BeforeDiscovery` handles discovery-time data; `BeforeAll` still handles execution-time setup.

## Structure & Naming

- Name the top-level `Describe` after the function or module area under test.
- Use `Context` blocks when they improve clarity, commonly for `Functionality`, `Input Validation`, `Error Handling`, or other scenario-specific groupings.
- Do not manufacture extra structure for simple commands.
- Use descriptive `It` names that state the expected behaviour.
- When using `-ForEach`, include the varying values in the test name.

## ForEach & Coverage

- Use `-ForEach` to reduce duplication and cover meaningful parameter combinations.
- Prefer meaningful combinations over exhaustively multiplying every optional parameter.
- Use `-ForEach` for repeated validation cases such as null, empty, whitespace, invalid enum values, or boundary values.
- Do not hide multiple validation cases inside a manual `foreach` loop in a single `It` block.

Cover the parameter shapes that materially affect behaviour:

- Parameter sets
- Meaningful parameter combinations
- Pipeline input and property binding when supported
- Default behaviour when optional parameters are omitted
- Boundary and invalid values where validation exists
- Important error paths and output contracts

## Isolation & State Management

- Recreate mutable fixtures in `BeforeEach` when tests modify them.
- Do not assign state in one `It` block and rely on it in another.
- Put cleanup in `AfterEach` or `AfterAll`, not at the end of an `It` block.
- Use `Write-Debug` only when the extra output materially helps explain failed filesystem or orchestration tests.

## Fixtures, Mocks, and Seams

- Prefer isolated real fixtures when they improve confidence without machine-level side effects.
- Prefer mocking PSADT-level collaborators or boundary functions when testing orchestration, parameter forwarding, branching, or error propagation.
- Only mock built-in cmdlets such as `Copy-Item` or `New-ItemProperty` when that built-in operation is the correct seam.
- Avoid unit tests that write to real HKLM locations, real system directories, or other machine-global state unless there is no practical isolated alternative.
- When choosing between a real fixture and a mock, prefer the narrowest seam that still verifies the behaviour users depend on.

### Mocking rules

- Always specify `-ModuleName PSAppDeployToolkit` when mocking PSADT commands.
- `Write-ADTLogEntry` is commonly mocked to avoid unnecessary overhead.
- Mock additional collaborators only when they are not part of the behaviour being verified.
- If a mock needs to delegate to the real implementation with a small adaptation, forward parameters with `$PesterBoundParameters`.
- Use `Should -Invoke` when delegation or parameter forwarding is part of the contract, not by default in every test.

## Assertions & Error Checks

- Prefer the most specific assertion practical.
- Avoid redundant follow-up assertions such as checking `$?` after `Should -Throw` or `Should -Not -Throw`.
- Do not rely on a bare `Should -Throw` unless there is no stable, more precise signal available.
- Prefer validating an exception type, stable message fragment, or error ID.
- Keep each `It` focused on one logical behaviour, even if that behaviour needs multiple assertions.

## Analyzer Expectations

- Test files must still respect the repository's PowerShell 5.1 compatibility rules from `powershell.md`.
- Use `SuppressMessageAttribute` only when PSScriptAnalyzer cannot see legitimate variable usage inside Pester scriptblocks.
- Apply suppressions narrowly and with a real justification.
- Do not suppress warnings to avoid restructuring a weak test.
