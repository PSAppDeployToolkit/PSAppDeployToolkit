---
applyTo: "**/*.ps1,**/*.psm1,**/*.psd1"
excludeFrom: "**/*.Tests.ps1"
---

# PowerShell Coding Conventions for PSAppDeployToolkit

These conventions define how PowerShell source files should be written in this repository. Keep them focused on PSAppDeployToolkit-specific expectations rather than generic PowerShell advice.

## Core Rules

### PowerShell 5.1 compatibility

- All code must work on both Windows PowerShell 5.1 and PowerShell 7+.
- Do not use PowerShell 7-only syntax such as ternary, null-coalescing, pipeline chain, or null-conditional operators.
- Use `[System.Management.Automation.Language.NullString]::Value` instead of `$null` when passing null strings to .NET APIs that expect `[string]`.
- PSScriptAnalyzer enforces the `desktop-5.1.14393.206-windows` compatibility profile.

### Repository defaults

- Root `.editorconfig` settings apply: UTF-8 with BOM, 4-space indentation, trimmed trailing whitespace, and a final newline.
- Preserve the existing public/private layout under `src/PSAppDeployToolkit/Public` and `src/PSAppDeployToolkit/Private`.
- Avoid style-only churn. Match the surrounding file unless there is a repository-wide reason to normalize the code.

### Public contract stability

- Public function contracts should remain backward compatible wherever practical.
- On feature branches from `main`, avoid breaking changes to public parameter names, parameter behaviour, and output types unless there is a strong reason and no lower-risk alternative.
- If a public parameter must be renamed, prefer aliases.
- If a function is substantially redesigned, prefer a new function plus a compatibility wrapper over silently changing the existing contract.
- On release branches such as `4.1.x`, avoid breaking changes to public contracts.

## Function Shape

- One function per file. File name matches function name.
- Public functions use the `ADT` noun prefix.
- Private functions use the `Private:` scope prefix.
- Keep the repository's `# MARK:` file header.
- Use `[CmdletBinding()]` on functions.
- Public state-changing commands should explicitly set `SupportsShouldProcess` and call `$PSCmdlet.ShouldProcess()` around the mutation.

### Lifecycle pattern

Most public functions follow the repository's standard `begin` / `process` / `end` pattern.

- In `begin`, initialize with `Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState` (for commands that should continue on error by default, add `-ErrorAction SilentlyContinue` to this).
- In `end`, finalize with `Complete-ADTFunction -Cmdlet $PSCmdlet`.
- In the `process` block, keep the standard nested `try` / `catch` pattern so `Write-Error -ErrorRecord $_` preserves the correct position information and `Invoke-ADTFunctionErrorHandler` handles logging and caller `ErrorAction` semantics.

## Errors, Sessions, and Parameters

### Error handling

- Prefer structured errors via `New-ADTErrorRecord` and `New-ADTValidateScriptErrorRecord`.
- Use `$PSCmdlet.ThrowTerminatingError()` for fatal errors in `begin` blocks or validation logic.
- When calling `Invoke-ADTFunctionErrorHandler`, propagate an explicitly bound `-ErrorAction` when present.

### Session handling

- Functions that require an active ADT session should obtain it in `begin` and terminate cleanly if unavailable.
- Functions that optionally use a session should prefer `Initialize-ADTModuleIfUninitialized -PassThruActiveSession`.
- Use `Test-ADTSessionActive` when you need a non-throwing session check.

### Parameters and validation

- Design parameter sets deliberately.
- Only declare pipeline binding when the command genuinely supports it.
- Use `[SupportsWildcards()]` only when wildcard behaviour is intentionally supported and implemented.
- Prefer the custom validators already used by the module:
  - `[PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]`
  - `[PSAppDeployToolkit.Attributes.AllowNullButNotEmptyOrWhiteSpace()]`
  - `[PSAppDeployToolkit.Attributes.ValidateGreaterThanZero()]`
- Use fully qualified .NET type names in parameters and code rather than PowerShell type accelerators.
- Use targeted `SuppressMessageAttribute` annotations when PSScriptAnalyzer cannot see legitimate usage inside delegates or script blocks.

## Preferred Patterns

- Prefer `& { process { } }` for pipeline filtering and transformation where the repository already uses that pattern.
- Prefer `.Where()` and `.ForEach()` for collections already in memory.
- Prefer static .NET methods where they are clearer or more efficient, for example `[System.IO.Path]::Combine()` or `[System.String]::IsNullOrWhiteSpace()`.
- Use hashtable splatting for complex parameter forwarding.
- Prefer helper functions already provided by the module when they fit the problem:
  - `Resolve-ADTFileSystemPath` for file and directory resolution
  - `Get-ADTBoundParametersAndDefaultValues` when forwarding parameters and defaults must participate
  - `$Script:CommandTable` when referencing internal commands indirectly
- Keep the pipeline clean. Suppress unwanted output with `$null = ...` when the return value is not part of the contract.

## Help, Logging, and Output

- Public functions require comment-based help.
- `.NOTES` must state whether an active ADT session is required.
- Keep examples realistic and aligned with real module usage, common path patterns, and ADT session variables where relevant.
- Declare `[OutputType()]` when a function returns output.
- Public functions should log meaningful state transitions and exceptional conditions with `Write-ADTLogEntry` while keeping noise low in tight loops.
- Keep public command contracts explicit: if a function returns nothing, keep the pipeline clean; if it returns data, document and type it consistently.

## Code Style

- Use the repository's existing Allman-style braces and 4-space indentation.
- Keep files UTF-8 with BOM.
- Match surrounding naming, spacing, and structure unless there is a repository-wide reason to do otherwise.
