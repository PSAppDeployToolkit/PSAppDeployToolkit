# PSAppDeployToolkit Copilot Coding Agent Instructions

## Repository Overview

**PSAppDeployToolkit** is a PowerShell-based, open-source framework for Windows software deployment that integrates seamlessly with existing deployment solutions (e.g. Microsoft Intune, SCCM, Tanium, BigFix etc.) to enhance the software deployment process. It achieves this by combining a battle-tested prescriptive workflow, an extensive library of functions for common deployment tasks, a customizable branded User Experience, and full-fidelity logging - to produce consistently high deployment success rates of over 98%.
 
- **Repository Size**: Over 100MB with 150+ PowerShell scripts, PowerShell data files and compiled binaries
- **Languages**: PowerShell (primary), C# (.NET Framework 4.7.2 and .NET 8), XAML for UI  
- **Target Runtime**: Windows PowerShell 5.1+ and PowerShell 7+
- **Dependencies**: .NET Framework 4.7.2+, .NET 8 SDK, Visual Studio for C# development
- **Exported Functions**: Module public functions have ADT prefix (e.g., Get-ADTApplication)

## Build System & Validation

### Required Dependencies
The only build dependency this project has is the .NET SDK, or Visual Studio. For everything else, `build.ps1` handles it.

### Build Commands (in order of typical execution)
`powershell.exe -ExecutionPolicy Bypass -File build.ps1`

### Important Build Requirements
- **Visual Studio/MSBuild/.NET SDK**: Required for C# compilation. Build will fail without proper VS installation.
- **PowerShell 5.1+**: Minimum requirement. Scripts are designed for Windows PowerShell compatibility.
- **File Encoding**: ALL PowerShell files MUST be UTF-8 with BOM. The build will fail on incorrect encoding.
- **Code Formatting**: Uses Allman/OTBS formatting rules. The build script will validate.
- **Admin Rights**: Some build steps require elevated privileges for C# compilation and file operations.

### Quick Module Validation
```powershell
# Test module manifest (always run this first)
Test-ModuleManifest ./src/PSAppDeployToolkit/PSAppDeployToolkit.psd1

# Load module for testing (may show CimCmdlets warning - this is expected on non-Windows)
Import-Module ./src/PSAppDeployToolkit/PSAppDeployToolkit.psd1 -Force

# Verify function count (will change as new functions are added)
(Import-PowerShellDataFile ./src/PSAppDeployToolkit/PSAppDeployToolkit.psd1).FunctionsToExport.Count

# Run script analysis
Invoke-ScriptAnalyzer -Path ./src/PSAppDeployToolkit -Recurse -Settings ./.vscode/PSScriptAnalyzerSettings.psd1
```

## Project Structure & Key Files

### Core Directory Layout
```
/                                      ← Git repository root
├── .github/                           # GitHub workflows and templates
│   └── workflows/module-build.yml     # Main CI/CD pipeline
├── .vscode/                           # VS Code configuration
│   ├── tasks.json                     # Pre-configured build tasks
│   └── PSScriptAnalyzerSettings.psd1  # Linting rules
├── src/                               # Main source code
│   ├── PSAppDeployToolkit/            # PowerShell module source
│   │   ├── PSAppDeployToolkit.psd1    # Module manifest
│   │   ├── PSAppDeployToolkit.psm1    # Main module file
│   │   ├── Public/                    # Exported functions
│   │   ├── Private/                   # Internal helper functions
│   │   ├── Config/config.psd1         # Default configuration
│   │   ├── Frontend/                  # v3 and v4 executable frontends
│   │   ├── Strings/                   # Localization files
│   │   └── lib/                       # Compiled C# assemblies (output target)
│   ├── PSADT/                         # C# solution directory (open this in Visual Studio)
│   │   ├── PSADT/                     # Core utilities library
│   │   ├── PSADT.LibraryInterfaces/   # Win32 P/Invoke declarations
│   │   ├── PSADT.UserInterface/       # WPF Fluent & WinForms Classic dialogs
│   │   ├── PSADT.ClientServer.Server/ # IPC server for SYSTEM-to-user communication
│   │   ├── PSADT.ClientServer.Client/ # Client executable for user-context UI
│   │   ├── PSADT.ClientServer.Client.Launcher/ # GUI launcher (no console)
│   │   ├── PSAppDeployToolkit/        # PowerShell-specific C# types
│   │   └── PSADT.Tests/               # xUnit test project
│   ├── PSADT.Invoke/                  # C# solution for executable wrapper
│   └── Tests/                         # Pester test suites
│       ├── Unit/                      # Unit tests
│       └── Integration/               # Integration tests
├── examples/                          # Sample deployment scripts
├── lib/                               # External libraries (iNKORE.UI.WPF.Modern)
└── build.ps1                          # Main module build script
```

### C# Solution Architecture (src/PSADT/)

All C# projects multi-target **.NET Framework 4.7.2** (Windows PowerShell 5.1) and **.NET 8** (PowerShell 7+).

| Project | Type | Purpose |
|---------|------|---------|
| **PSADT.LibraryInterfaces** | Library | Win32 P/Invoke declarations, safe handles, native method wrappers. Uses Microsoft.Windows.CsWin32 source generator. |
| **PSADT** | Library | Core utilities: process management, file system, security tokens, terminal services, registry, window management, etc. |
| **PSAppDeployToolkit** | Library | PowerShell-specific: deployment sessions, logging utilities, module database. References System.Management.Automation. |
| **PSADT.UserInterface** | Library | Dialog management with both WPF Fluent (modern) and WinForms Classic styles. Uses iNKORE.UI.WPF.Modern for Fluent UI. |
| **PSADT.ClientServer.Server** | Library | IPC server using anonymous pipes, command serialization with System.Text.Json, encrypted communication. |
| **PSADT.ClientServer.Client** | Exe | Client process spawned in user context to display UI dialogs when PowerShell runs as SYSTEM. |
| **PSADT.ClientServer.Client.Launcher** | WinExe | GUI launcher that starts the client without a visible console window. |
| **PSADT.Tests** | Test | xUnit tests with FluentAssertions and coverlet for code coverage. |

### Project Dependencies (bottom-up)
```
PSADT.LibraryInterfaces (P/Invoke layer)
        ↓
      PSADT (Core utilities)
        ↓
PSAppDeployToolkit (PowerShell types) ←────────────┐
        ↓                                          │
PSADT.UserInterface (UI dialogs) ←── iNKORE.UI.WPF.Modern (in lib/)
        ↓                                          │
PSADT.ClientServer.Server (IPC server) ────────────┘
        ↓
PSADT.ClientServer.Client (Client executable)
        ↓
PSADT.ClientServer.Client.Launcher (GUI launcher)
```

### Critical Configuration Files
- **Module Manifest**: `src/PSAppDeployToolkit/PSAppDeployToolkit.psd1`
- **Main Build Script**: `build.ps1` (comprehensive build pipeline)
- **CI/CD Pipeline**: `.github/workflows/module-build.yml` (Windows-only GitHub Actions)
- **PSScriptAnalyzer Rules**: `.vscode/PSScriptAnalyzerSettings.psd1` (PowerShell 5.1 compatibility rules)
- **Default Config**: `src/PSAppDeployToolkit/Config/config.psd1` (toolkit configuration settings)

## Development Environments

This project uses **two different development environments** depending on what you're working on:

### Visual Studio (C# Development)
When working on the C# support libraries in Visual Studio:
- **Solution Location**: `src/PSADT/` directory
- **Build Method**: Use Visual Studio's standard build (F5, Ctrl+Shift+B, or Build menu)
- **Test Execution**: Use Visual Studio's Test Explorer for xUnit tests in PSADT.Tests
- **Output**: Assemblies build to each project's `bin/` directory
- **Note**: The `build.ps1` script is **NOT** needed when working in Visual Studio on C# code

### VS Code (PowerShell Development)
When working on the PowerShell module in VS Code:
- **Build Method**: Run `build.ps1` which handles C# compilation, PowerShell validation, and packaging
- **Test Execution**: Pester tests in `src/Tests/`
- **Output**: Compiled assemblies are copied to `src/PSAppDeployToolkit/lib/`

## Development Workflow

### Before Making Changes
1. **Test module loading**: `Test-ModuleManifest ./src/PSAppDeployToolkit/PSAppDeployToolkit.psd1`

### During Development  
- **Code Formatting**: Follow Allman/OTBS style. The build script will validate.
- **Encoding Requirements**: Save all `.ps1`, `.psm1`, `.psd1` files as UTF-8 with BOM.
- **Function Naming**: All public functions use `ADT` prefix (e.g., `Get-ADTApplication`).
- **Comment-Based Help**: Required for all public functions with `.SYNOPSIS`, `.DESCRIPTION`, `.EXAMPLE`.
- **Minimize Unnecessary Changes**: Preserve existing XML documentation comments when refactoring code. Don't strip them unnecessarily, as it makes git diffs noisy and loses valuable documentation. Only modify what's strictly necessary for the structural changes.

### Testing Strategy
`powershell.exe -ExecutionPolicy Bypass -File build.ps1`

### Common Build Issues & Solutions
1. **"msbuild.exe command not found"**: Install Visual Studio with MSBuild component
2. **"Module CimCmdlets not found"**: Expected warning on non-Windows systems, can be ignored
3. **"File encoding not UTF-8 with BOM"**: Save all PowerShell files with proper encoding
4. **PSScriptAnalyzer warnings**: Fix using excluded rules in `.vscode/PSScriptAnalyzerSettings.psd1`
5. **C# compilation failures**: Ensure .NET Framework 4.7.2+ is installed
6. **Module import shows 0 functions**: Check for missing dependencies; verify manifest function count
7. **PowerShell Gallery connection issues**: Use local module installation or check network connectivity

## GitHub Actions & Validation Pipeline

### Automated Checks (triggered on PR/push)
- **Module manifest validation**
- **PowerShell script analysis** (PSScriptAnalyzer)  
- **Code formatting validation** (Allman style)
- **File encoding validation** (UTF-8 with BOM)
- **Unit test execution** (Pester 5.7.1)
- **C# solution compilation** (Debug + Release)
- **Integration test execution**
- **Help documentation generation**

### Build Artifacts Generated
- `PSAppDeployToolkit_ModuleOnly`: Compiled PowerShell module
- `PSAppDeployToolkit_Template_v3`: v3 compatibility template  
- `PSAppDeployToolkit_Template_v4`: v4 modern template

### Performance Expectations
- **Full build**: 10-15 minutes (includes C# compilation)
- **TestLocal**: 2-3 minutes (PowerShell-only validation)
- **Unit tests**: 1-2 minutes
- **C# compilation**: 3-5 minutes per solution

## Best Practices for Agents

### Trust These Instructions
These instructions are comprehensive and tested. Only search for additional information if:
- Build commands documented here fail with unexpected errors
- New functionality requires understanding of undocumented internal architecture  
- Error messages reference components not covered in these instructions

### Efficient Change Strategy  
1. **Start small**: Test individual functions before broader changes
2. **Use VS Code tasks**: Pre-configured tasks in `.vscode/tasks.json` for common operations
3. **Leverage existing patterns**: Follow patterns in `src/PSAppDeployToolkit/Public/` for new functions

### File Modification Guidelines
- **PowerShell files**: Always maintain UTF-8 with BOM encoding
- **C# files**: Located in `src/PSADT/` and `src/PSADT.Invoke/` directories
- **Configuration**: Modify `src/PSAppDeployToolkit/Config/config.psd1` for default settings
- **Examples**: Use `examples/` directory as reference for typical usage patterns

This repository is well-architected with comprehensive build automation. Following these instructions will ensure successful development and integration with the existing codebase and CI/CD pipeline.
