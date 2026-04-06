# PSAppDeployToolkit Copilot Coding Agent Instructions

## Repository Overview

**PSAppDeployToolkit** is an open source PowerShell module/framework for Windows software deployment that features an extensive library of functions for common deployment tasks and a customizable branded User Interface.

- **Languages**: PowerShell (primary), C#, XAML
- **Dependencies**: A current .NET SDK is required for builds. Visual Studio is useful for C# and XAML work. `build.ps1` bootstraps the required PowerShell module dependencies.
- **Branches**: `main` is the integration branch for active development. Release branches such as `4.0.x` and `4.1.x` are maintained as needed.

## General Coding Conventions
- When editing existing code, prefer repository consistency over introducing style-only rewrites. Follow the surrounding file's patterns unless a broader cleanup is explicitly requested.
- When working on a feature branch from `main`, public function contracts should remain backward compatible wherever practical. Avoid breaking changes to parameter names, parameter behaviour, and output types unless the work clearly justifies it. On release branches such as `4.1.x`, avoid breaking changes to public contracts entirely.
- Ignore IDE0028 in this repository context because it is an IntelliSense bug and should not block work.
- Prefer delegate-based refactors to keep delegates inlined at the call site rather than introducing named local delegate functions when possible.

## Language-Specific Coding Conventions

Detailed coding standards are maintained in dedicated instruction files that Copilot applies automatically based on file type:

- `.github/instructions/powershell.md` — PowerShell conventions (applied to `*.ps1`, `*.psm1`, `*.psd1`)
- `.github/instructions/pester.md` — Pester test conventions (applied to `*.Tests.ps1`)
- `.github/instructions/csharp.md` — C# conventions (applied to `*.cs`)

## Build System & Validation

The main build dependency is a current .NET SDK. Visual Studio is recommended for C# and XAML work.

- Run `build.cmd` or `build.ps1` from the repository root to invoke `Invoke-ADTModuleBuild` via `src/PSAppDeployToolkit.Build/PSAppDeployToolkit.Build.psd1`.
- Prefer the existing VS Code tasks and build entry points over ad-hoc commands when validating changes.
- Common validation tasks include `Build`, `Test`, `Analyze`, `FormattingCheck`, and `ValidateRequirements`.

## Repository Orientation

- `src/PSAppDeployToolkit/` contains the PowerShell module source.
- `src/PSAppDeployToolkit/Public/` contains exported PowerShell functions, generally one function per file.
- `src/PSAppDeployToolkit/Private/` contains internal PowerShell helpers.
- `src/PSADT/` contains the main C# projects, including core utilities, interop, UI, client/server components, and tests.
- `src/Tests/` contains Pester tests, including `Unit/` and `Integration/`.
- `src/PSAppDeployToolkit.Build/` contains the PowerShell build module.
- `examples/` contains sample deployment scripts and usage examples.

## Key Files

- `src/PSAppDeployToolkit/PSAppDeployToolkit.psd1` - module manifest.
- `src/PSAppDeployToolkit/PSAppDeployToolkit.psm1` - main module entry point.
- `.vscode/PSScriptAnalyzerSettings.psd1` - PowerShell analyzer configuration.
- `.github/workflows/module-build.yml` - CI pipeline.

## C# Notes

- Most first-party C# projects in `src/PSADT/` target .NET Framework 4.7.2 and a current modern .NET Windows target, but check the specific `.csproj` before assuming dual-targeting.
- `PSADT.Interop` contains Win32 interop and CsWin32-generated symbols.
- `PSADT` contains core C# utilities.
- `PSAppDeployToolkit` contains PowerShell-facing C# types.
