# PowerShell module guidance

Read this file when changing Fluence.Wpf.PowerShell.Module. It is a script module and is deliberately outside Fluence.Wpf.sln.

## Source conventions

- Keep Windows PowerShell 5.1 and PowerShell 7 compatible syntax. Use one public function per file, comment-based help, OutputType, fully qualified .NET types, Allman braces, and UTF-8 BOM.
- Use PSScriptAnalyzerSettings.psd1 as the analyzer policy. The module's type resolver handles library enum renames across supported library generations.
- Import the module in tests. Access private helpers through InModuleScope or the imported module object; do not dot-source a private helper copy.
- Do not add module dependencies or package references without user approval.

## Build and tests

Use the scripts under build/ to stage, test, and package the module:

- Build-Module.ps1 stages Release net472 and net8.0-windows10.0.26100.0 library outputs into the gitignored module lib folder.
- Test-Module.ps1 runs PSScriptAnalyzer plus Pester.
- Package-Module.ps1 creates the zip and nupkg under artifacts/.

The test suite uses Pester 5.8.0 and PSScriptAnalyzer 1.25.0. The default logic lane excludes UI-tagged rendering tests and is used by CI under PowerShell 7, PowerShell 7 MTA, and Windows PowerShell 5.1 STA. The -IncludeUi lane opens real windows and requires an interactive desktop; run it locally in one uninterrupted batch on each applicable host. A successful render lane requires passing assertions and a zero process exit code.

The module manifest version and library package version must agree. Inspect the current manifest and Directory.Build.props before changing versions. Keep release credentials and internal release procedures outside this tracked repository.
