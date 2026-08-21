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
- Prefer standard, compiler/analyzer-recognizable C# patterns over custom non-standard lifecycle hooks, especially for IDisposable/disposal implementations, to keep code review and commit ownership clear.
- Avoid changes that cause `DialogManager` static initialization when the client/server process has not used dialog functionality; do not change `DialogManager` static constructor setup unless explicitly requested. For the client/server WPF shutdown issue, prefer a narrow suppression of the known WPF Win32Exception native error 1400 during shutdown over initializing `DialogManager` or changing its static constructor setup.
- Keep edits specific to the requested change and avoid unrelated formatting/style refactors, even when touching nearby code.
- Centralize shared helper logic rather than duplicating boilerplate across call sites.
- For Meziantou Analyzer MA0045 suppressions in this repository, use category "Design" because the analyzer declares RuleCategories.Design.

## Language-Specific Coding Conventions

Detailed coding standards are maintained in dedicated instruction files that Copilot applies automatically based on file type:

- `.github/instructions/powershell.md` — PowerShell conventions (applied to `*.ps1`, `*.psm1`, `*.psd1`)
- `.github/instructions/pester.md` — Pester test conventions (applied to `*.Tests.ps1`)
- `.github/instructions/csharp.md` — C# conventions (applied to `*.cs`)

## Build System & Validation

The main build dependency is a current .NET SDK. Visual Studio is recommended for C# and XAML work.

- If there are only PowerShell changes, the module can be re-imported via `Import-Module .\src\PSAppDeployToolkit\PSAppDeployToolkit.psd1 -Force`. If this gives an error containing `assembly of a different file hash is already loaded`, the PowerShell session needs to be restarted.
- Run `build.cmd` or `build.ps1` from the repository root to perform a full build.
- Prefer the existing VS Code tasks and build entry points over ad-hoc commands when validating changes.
- Common validation tasks include `Build`, `Test`, `Analyze`, `FormattingCheck`, and `ValidateRequirements`.
- Only build from within Visual Studio; do not use `dotnet build` for validation/builds. When diagnosing build behavior in this repository, prefer Visual Studio/MSBuild workspace builds and Output window logs over `dotnet build` for issues suspected to be MSBuild-specific. Use `dotnet msbuild` when compiling and permit it for MSBuild diagnostics.
- Package versions are centrally managed in `Directory.Packages.props`. `nuget.config` pins restore to nuget.org alone, maps every package ID to that feed, and sets `signatureValidationMode` to `require`, so an unsigned package or one not signed by nuget.org fails restore with `NU3034`. If nuget.org rotates its signing certificates, regenerate the `trustedSigners` block with `dotnet nuget trust source nuget.org` rather than editing fingerprints by hand.
- Every project has a committed `packages.lock.json`. Changing a package version means regenerating them: restore the affected solution, then commit the changed lock files with the version change. CI sets `RestoreLockedMode` through `GITHUB_ACTIONS`, so a lock file that disagrees with its project fails the build with `NU1004` instead of being rewritten silently.
- `Directory.Build.props` declares `RuntimeIdentifiers` as `win-x64` because `build.ps1` publishes `PSADT.WindowsRuntime.TrimHarness` with `dotnet publish -r win-x64`; without it that runtime-specific restore fails locked mode for every project in the harness graph. The vendored `lib/Fluence.Wpf` subtree stops the `Directory.Build.props` walk and carries its own copy of these settings plus its own `nuget.config`.

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

## API Usage Guidelines

- Expect accurate, well-researched answers about Windows APIs and COM interfaces. Do not fabricate API names or suggest hacky workarounds when proper documented APIs exist (e.g., Appx COM interfaces for reading package identity).
- Acknowledge uncertainty rather than inventing answers.
