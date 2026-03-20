# PSAppDeployToolkit Copilot Coding Agent Instructions

## Repository Overview

**PSAppDeployToolkit** is an open source PowerShell module/framework for Windows software deployment that features an extensive library of functions for common deployment tasks and a customizable branded User Interface.

- **Languages**: PowerShell (primary), C#, XAML
- **Dependencies**: A current .NET SDK is required for builds. Visual Studio is useful for C# and XAML work. `build.ps1` bootstraps the required PowerShell module dependencies.
- **Branches**: `main` is the integration branch for active development. Release branches such as `4.0.x` and `4.1.x` are maintained as needed.

## General Coding Conventions
- When editing existing code, prefer repository consistency over introducing style-only rewrites. Follow the surrounding file's patterns unless a broader cleanup is explicitly requested.
- When working on a feature branch from `main`, public function contracts should remain backward compatible wherever practical. Avoid breaking changes to parameter names, parameter behaviour, and output types unless the work clearly justifies it. On release branches such as `4.1.x`, avoid breaking changes to public contracts entirely.

## Language-Specific Coding Conventions

Detailed coding standards are maintained in dedicated instruction files that Copilot applies automatically based on file type:

- `.github/instructions/powershell.md` — PowerShell conventions (applied to `*.ps1`, `*.psm1`, `*.psd1`)
- `.github/instructions/pester.md` — Pester test conventions (applied to `*.Tests.ps1`)
- `.github/instructions/csharp.md` — C# conventions (applied to `*.cs`)

## Build System & Validation

### Required Dependencies
The main build dependency is a current .NET SDK. Visual Studio is recommended for C# and XAML work. `build.ps1` handles the PowerShell-side build bootstrap.

### Build Commands (in order of typical execution)
Run `build.cmd` or `build.ps1` from the repository root.
This imports `src/PSAppDeployToolkit.Build/PSAppDeployToolkit.Build.psd1` and executes `Invoke-ADTModuleBuild` with the default steps.

Common VS Code tasks map to the same build system and include `Build`, `Test`, `Analyze`, `FormattingCheck`, and `ValidateRequirements`.

Prefer these existing tasks and build entry points over ad-hoc commands when validating changes.

## Project Structure & Key Files

### Core Directory Layout

- `.github/` — GitHub workflows, templates, and Copilot instruction files
  - `workflows/module-build.yml` — Main CI/CD pipeline
  - `instructions/` — Language-specific Copilot coding conventions
- `.vscode/` — VS Code configuration
  - `tasks.json` — Pre-configured build tasks
  - `PSScriptAnalyzerSettings.psd1` — Linting rules
- `src/` — Main source code
  - `PSAppDeployToolkit/` — PowerShell module source
    - `PSAppDeployToolkit.psd1` — Module manifest
    - `PSAppDeployToolkit.psm1` — Main module file
    - `Public/` — Exported functions (one function per file)
    - `Private/` — Internal helper functions
    - `opt/` — Optional components (ADMX templates, v3/v4 frontends)
    - `lib/` — Compiled C# assemblies (build output target)
  - `PSADT/` — C# source code directory (solution files `PSADT.slnx` and `PSADT.Invoke.slnx` are in the repository root)
    - `PSADT/` — Core utilities library
    - `PSADT.Interop/` — Win32 P/Invoke declarations (CsWin32)
    - `PSADT.UserInterface/` — WPF Fluent and WinForms Classic dialogs
    - `PSADT.ClientServer.Common/` — Shared client/server types
    - `PSADT.ClientServer.Server/` — IPC server for SYSTEM-to-user communication
    - `PSADT.ClientServer.Client/` — Client executable for user-context UI
    - `PSADT.ClientServer.Client.Launcher/` — GUI launcher (no console window)
    - `PSADT.ClientServer.Client.Compatible/` — WinForms client variant for compatibility
    - `PSADT.ClientServer.Client.Launcher.Compatible/` — WinForms launcher variant for compatibility
    - `PSAppDeployToolkit/` — PowerShell-specific C# types
    - `PSADT.UserInterface.TestHarness/` — UI test harness application
    - `PSADT.Tests/` — xUnit test project
  - `PSADT.Invoke/` — C# solution for executable wrapper
  - `PSAppDeployToolkit.Build/` — PowerShell build module
  - `Tests/` — Pester test suites (`Unit/`, `Integration/`)
- `examples/` — Sample deployment scripts
- `lib/` — External WPF libraries (source, compiled into module)
- `build.ps1` — Main module build script

### C# Solution Architecture (src/PSADT/)

Most first-party C# projects in `src/PSADT/` multi-target **.NET Framework 4.7.2** and the current modern .NET Windows target. Check the specific `.csproj` before assuming dual-targeting, because some projects are single-targeted.

| Project | Type | Purpose | Dependencies |
|---------|------|---------|--------------|
| **PSADT.Interop** | Library | Win32 P/Invoke declarations, safe handles, native method wrappers. Uses Microsoft.Windows.CsWin32 source generator. | None |
| **PSADT** | Library | Core utilities: process management, file system, security tokens, terminal services, registry, window management, etc. | PSADT.Interop |
| **PSAppDeployToolkit** | Library | PowerShell-specific: deployment sessions, logging utilities, module database. References System.Management.Automation. | PSADT |
| **PSADT.UserInterface** | Library | Dialog management with both WPF Fluent (modern) and WinForms Classic styles. | PSAppDeployToolkit, WPF library in `lib/` |
| **PSADT.ClientServer.Common** | Library | Shared types for client/server communication: commands, serialization contracts. | PSAppDeployToolkit, PSADT.UserInterface |
| **PSADT.ClientServer.Server** | Library | IPC server using anonymous pipes, command serialization with System.Text.Json, encrypted communication. | PSADT.ClientServer.Common |
| **PSADT.ClientServer.Client** | Exe | Client process spawned in user context to display UI dialogs when PowerShell runs as SYSTEM. | PSADT.ClientServer.Common |
| **PSADT.ClientServer.Client.Launcher** | WinExe | GUI launcher that starts the client without a visible console window. | PSADT.ClientServer.Client |
| **PSADT.ClientServer.Client.Compatible** | Exe | WinForms-based client variant for compatibility scenarios. | PSADT.ClientServer.Client, Common, Server, UserInterface, PSADT |
| **PSADT.ClientServer.Client.Launcher.Compatible** | WinExe | WinForms-based launcher variant for compatibility scenarios. | PSADT.ClientServer.Client.Launcher, Common, Server, UserInterface, PSADT |
| **PSADT.UserInterface.TestHarness** | WinExe | Interactive test harness for PSADT.UserInterface dialogs. | PSADT.Interop, PSADT.UserInterface, PSAppDeployToolkit |
| **PSADT.Tests** | Test | xUnit tests with FluentAssertions and coverlet for code coverage. | PSADT |

### Critical Configuration Files
- **Module Manifest**: `src/PSAppDeployToolkit/PSAppDeployToolkit.psd1`
- **CI/CD Pipeline**: `.github/workflows/module-build.yml` (Windows-only GitHub Actions)
- **PSScriptAnalyzer Rules**: `.vscode/PSScriptAnalyzerSettings.psd1` (PowerShell 5.1 compatibility rules)
- **Default Config**: `src/PSAppDeployToolkit/Config/config.psd1` (toolkit configuration settings)
