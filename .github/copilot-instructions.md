# PSAppDeployToolkit Copilot Coding Agent Instructions

## Repository Overview

**PSAppDeployToolkit** is a PowerShell-based, open-source framework for Windows software deployment that integrates seamlessly with existing deployment solutions (e.g. Microsoft Intune, SCCM, Tanium, BigFix etc.) to enhance the software deployment process. It achieves this by combining a battle-tested prescriptive workflow, an extensive library of functions for common deployment tasks, a customizable branded User Experience, and full-fidelity logging - to produce consistently high deployment success rates of over 98%.
 
- **Repository Size**: Over 100MB with 150+ PowerShell scripts, PowerShell data files and compiled binaries
- **Languages**: PowerShell (primary), C# (.NET Framework 4.7.2), XAML for UI  
- **Target Runtime**: Windows PowerShell 5.1+ and PowerShell 7+
- **Dependencies**: .NET Framework 4.7.2+, Visual Studio/MSBuild for C# compilation
- **Exported Functions**: Module public functions have ADT prefix (e.g., Get-ADTApplication)

## Build System & Validation

### Required Dependencies
**ALWAYS install these dependencies first before any build operations:**
```powershell
# Bootstrap script (recommended first step)
./actions_bootstrap.ps1

# Manual dependency installation if bootstrap fails
Install-Module -Name Pester -RequiredVersion 5.7.1 -Force -Scope CurrentUser
Install-Module -Name InvokeBuild -Force -Scope CurrentUser  
Install-Module -Name PSScriptAnalyzer -RequiredVersion 1.24.0 -Force -Scope CurrentUser
Install-Module -Name platyPS -RequiredVersion 0.14.2 -Force -Scope CurrentUser
```

### Build Commands (in order of typical execution)
```powershell
# Navigate to source directory first
cd src

# Full build pipeline (takes 10-15 minutes)
Invoke-Build -File PSAppDeployToolkit.build.ps1

# Quick local testing (2-3 minutes)
Invoke-Build -Task TestLocal -File PSAppDeployToolkit.build.ps1

# Individual validation steps
Invoke-Build -Task ValidateRequirements -File PSAppDeployToolkit.build.ps1
Invoke-Build -Task TestModuleManifest -File PSAppDeployToolkit.build.ps1
Invoke-Build -Task EncodingCheck -File PSAppDeployToolkit.build.ps1
Invoke-Build -Task FormattingCheck -File PSAppDeployToolkit.build.ps1
Invoke-Build -Task Analyze -File PSAppDeployToolkit.build.ps1
Invoke-Build -Task Test -File PSAppDeployToolkit.build.ps1
```

### Important Build Requirements
- **Visual Studio/MSBuild**: Required for C# compilation. Build will fail without proper VS installation.
- **PowerShell 5.1+**: Minimum requirement. Scripts are designed for Windows PowerShell compatibility.
- **File Encoding**: ALL PowerShell files MUST be UTF-8 with BOM. The build will fail on incorrect encoding.
- **Code Formatting**: Uses Allman/OTBS formatting rules. Run `FormattingCheck` task to validate.
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
/
├── .github/                    # GitHub workflows and templates
│   └── workflows/module-build.yml  # Main CI/CD pipeline
├── .vscode/                    # VS Code configuration
│   ├── tasks.json              # Pre-configured build tasks
│   └── PSScriptAnalyzerSettings.psd1  # Linting rules
├── src/                        # Main source code
│   ├── PSAppDeployToolkit/     # PowerShell module source
│   │   ├── PSAppDeployToolkit.psd1  # Module manifest
│   │   ├── PSAppDeployToolkit.psm1  # Main module file
│   │   ├── Public/             # Eported functions
│   │   ├── Private/            # Internal helper functions
│   │   ├── Config/config.psd1  # Default configuration
│   │   ├── Frontend/           # v3 and v4 executable wrappers
│   │   ├── Strings/            # Localization files
│   │   └── lib/                # Compiled C# assemblies
│   ├── PSADT/                  # C# solution for core libraries
│   ├── PSADT.Invoke/           # C# solution for executable wrapper
│   ├── Tests/                  # Pester test suites
│   │   ├── Unit/               # Unit tests
│   │   └── Integration/        # Integration tests
│   └── PSAppDeployToolkit.build.ps1  # Main build script
├── examples/                   # Sample deployment scripts
├── lib/                        # External libraries (iNKORE.UI.WPF)
└── actions_bootstrap.ps1       # Dependency installation script
```

### Critical Configuration Files
- **Module Manifest**: `src/PSAppDeployToolkit/PSAppDeployToolkit.psd1`
- **Main Build Script**: `src/PSAppDeployToolkit.build.ps1` (comprehensive build pipeline)
- **CI/CD Pipeline**: `.github/workflows/module-build.yml` (Windows-only GitHub Actions)
- **PSScriptAnalyzer Rules**: `.vscode/PSScriptAnalyzerSettings.psd1` (PowerShell 5.1 compatibility rules)
- **Default Config**: `src/PSAppDeployToolkit/Config/config.psd1` (toolkit configuration settings)

## Development Workflow

### Before Making Changes
1. **ALWAYS run the bootstrap**: `./actions_bootstrap.ps1`
2. **Test module loading**: `Test-ModuleManifest ./src/PSAppDeployToolkit/PSAppDeployToolkit.psd1`
3. **Run local tests**: `Invoke-Build -Task TestLocal -File ./src/PSAppDeployToolkit.build.ps1`

### During Development  
- **Code Formatting**: Follow Allman/OTBS style. Use `FormattingCheck` task to validate.
- **Encoding Requirements**: Save all `.ps1`, `.psm1`, `.psd1` files as UTF-8 with BOM.
- **Function Naming**: All public functions use `ADT` prefix (e.g., `Get-ADTApplication`).
- **Comment-Based Help**: Required for all public functions with `.SYNOPSIS`, `.DESCRIPTION`, `.EXAMPLE`.

### Testing Strategy
```powershell
# Unit tests (preferred for function-specific changes)
Invoke-Build -Task Test -File ./src/PSAppDeployToolkit.build.ps1

# Single function testing (VS Code task available)
Import-Module ./src/PSAppDeployToolkit/PSAppDeployToolkit.psd1
Invoke-Pester './src/Tests/Unit/FunctionName.Tests.ps1' -Output Detailed

# Integration tests (for larger changes)
Invoke-Build -Task IntegrationTest -File ./src/PSAppDeployToolkit.build.ps1
```

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
4. **Test incrementally**: Run `TestLocal` after each significant change

### File Modification Guidelines
- **PowerShell files**: Always maintain UTF-8 with BOM encoding
- **C# files**: Located in `src/PSADT/` and `src/PSADT.Invoke/` directories
- **Configuration**: Modify `src/PSAppDeployToolkit/Config/config.psd1` for default settings
- **Examples**: Use `examples/` directory as reference for typical usage patterns

This repository is well-architected with comprehensive build automation. Following these instructions will ensure successful development and integration with the existing codebase and CI/CD pipeline.
