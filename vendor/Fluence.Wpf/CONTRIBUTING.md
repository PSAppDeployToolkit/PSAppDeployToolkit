# Contributing

Fluence.Wpf welcomes bug fixes and focused control work. Read the [developer handbook](AGENTS.md) for project conventions and the [documentation site](https://fluencewpf.com) for user-facing documentation.

## Build

Use Windows with the .NET SDKs needed by the target frameworks. From the repository root:

```powershell
dotnet restore Fluence.Wpf.sln
dotnet build Fluence.Wpf.sln -c Debug
```

The library targets .NET Framework 4.7.2, .NET 8 for Windows, and .NET 10 for Windows. The gallery targets .NET Framework 4.7.2 and .NET 10; the MVVM demo targets .NET 10. The PowerShell module is outside the solution and has its own build and verification scripts.

## Run the .NET tests

The xunit.v3 suite uses Microsoft Testing Platform. Build the solution, then run the executable for the target framework. Run the .NET Framework suite as two complementary class filters because its whole-assembly process can abort. The .NET 10 suite can run in one pass locally; CI splits both target frameworks into two lanes. See the [test project README](Fluence.Wpf.Tests/README.md) for exact commands.

```powershell
Fluence.Wpf.Tests/bin/Debug/net10.0-windows10.0.26100.0/Fluence.Wpf.Tests.exe --filter-not-trait "Category=Screenshots" --no-ansi --progress off
```

The .NET 8 smoke suite is a separate project. Run tests relevant to a change across each affected target framework. UI tests need Windows and an interactive desktop.

## Verify the PowerShell module

Build and stage the library assemblies before testing the module:

```powershell
dotnet build Fluence.Wpf/Fluence.Wpf.csproj -c Release
pwsh -NoProfile -File Fluence.Wpf.PowerShell.Module/build/Build-Module.ps1 -Configuration Release
pwsh -NoProfile -File Fluence.Wpf.PowerShell.Module/build/Test-Module.ps1
powershell.exe -NoProfile -STA -File Fluence.Wpf.PowerShell.Module/build/Test-Module.ps1
```

The [module README](Fluence.Wpf.PowerShell.Module/README.md) documents prerequisites, PowerShell 7 MTA, and render lanes. Run the lanes applicable to a module change.

## Source and documentation rules

- Keep the repository's BSD 3-Clause header on every C# source file. Document public APIs with XML comments.
- Follow the nullable and analyzer settings in `Directory.Build.props` and `.editorconfig`. Warnings are build errors.
- Use the existing theme architecture and canonical resource keys. The [theming guide](https://fluencewpf.com/docs/theming) and [developer handbook](AGENTS.md) describe the contract.
- Public website authoring and publishing are maintained in the separate [website repository](https://github.com/sintaxasn/Fluence.Wpf.Website). Use this repository for library source, the public changelog, and authored product guides.
- Preserve public change facts in [CHANGELOG.md](CHANGELOG.md) and document breaking changes there. The workflow in [.github/workflows/build.yml](.github/workflows/build.yml) is the source of truth for CI packaging and publication behavior.

Check the text policy before opening a pull request:

```powershell
pwsh .claude/hooks/post-tool-util.ps1 -CheckAll
git diff --check
```

## Pull requests

Describe the user-facing change, affected target frameworks, and the verification you ran. Include screenshots for visual changes in Light, Dark, and High Contrast when relevant. Link the documentation page or example that teaches the new behavior. The workflow in [.github/workflows/build.yml](.github/workflows/build.yml) is the source of truth for CI packaging and publication behavior.
