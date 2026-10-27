# Fluence.Wpf.PowerShell

`Fluence.Wpf.PowerShell` is a script module for Fluent dialogs and windows on Windows PowerShell 5.1 and PowerShell 7.4 or later. It uses the [Fluence.Wpf](../README.md) control library. Scripts can show messages, collect validated input, display progress, change appearance, and host custom XAML without compiling a WPF application.

The [PowerShell documentation](https://fluencewpf.com/docs/powershell/) contains a [first dialog tutorial](https://fluencewpf.com/docs/powershell/usage), task guides, [command reference](https://fluencewpf.com/docs/powershell/reference/), and [module design](https://fluencewpf.com/docs/powershell/explanation). This page covers module setup and development.

## Requirements

- Windows 10 1809 or later. Mica and Tabbed backdrops require Windows 11.
- Windows PowerShell 5.1 (`powershell.exe`) or PowerShell 7.4 or later (`pwsh`).
- A staged or packaged module containing `lib/net472` and `lib/net8.0-windows10.0.26100.0`.

The module manifest declares PowerShell 5.1 as its minimum. The module loader additionally requires PowerShell 7.4 or later for the Core edition because that library build targets .NET 8.

## Install a release ZIP

Extract the release ZIP so that the `Fluence.Wpf.PowerShell` folder, containing its `.psd1` file, sits in a directory on `$env:PSModulePath`. Then run:

```powershell
Import-Module Fluence.Wpf.PowerShell
Get-Command -Module Fluence.Wpf.PowerShell
```

The loader selects the `net472` library on Windows PowerShell and the `net8.0-windows10.0.26100.0` library on PowerShell 7. A release ZIP already contains both builds.

## Install from PowerShell Gallery

For a stable release, install the module for the current user:

```powershell
Install-Module -Name Fluence.Wpf.PowerShell -Repository PSGallery -Scope CurrentUser
Import-Module Fluence.Wpf.PowerShell
```

For a prerelease, use PowerShellGet 2.x and add `-AllowPrerelease` to `Install-Module`. For example:

```powershell
Install-Module -Name Fluence.Wpf.PowerShell -Repository PSGallery -Scope CurrentUser -AllowPrerelease
```

See Microsoft's [Install-Module reference](https://learn.microsoft.com/powershell/module/powershellget/install-module?view=powershellget-2.x) for prerelease installation details.

## Use the module from a source checkout

From the repository root, build and stage the two library targets, then import the manifest:

```powershell
pwsh -NoProfile -File .\Fluence.Wpf.PowerShell.Module\build\Build-Module.ps1 -Build
Import-Module .\Fluence.Wpf.PowerShell.Module\src\Fluence.Wpf.PowerShell\Fluence.Wpf.PowerShell.psd1
```

`Build-Module.ps1` defaults to Release. Without `-Build`, it stages existing Release outputs. It checks that `Directory.Build.props` and the manifest have the same version and prerelease value before changing the staged `lib` folder. The .NET SDK and repository build prerequisites are needed for source staging; they are not needed when using a release ZIP.

## Try the API

These message dialogs show the module's built-in appearance in light and dark themes.

![PowerShell message dialog in light mode](https://raw.githubusercontent.com/sintaxasn/Fluence.Wpf.Website/main/docs/powershell/images/message-light.png)

![PowerShell message dialog in dark mode](https://raw.githubusercontent.com/sintaxasn/Fluence.Wpf.Website/main/docs/powershell/images/message-dark.png)

The [capture notes](build/CAPTURE.md) document how these module screenshots were rendered and what they omit.

```powershell
$answer = Show-FluenceMessage -Message 'Install the update?' -Buttons YesNo -Icon Question
if ($answer -eq 'Yes')
{
    Show-FluenceMessage -Message 'Starting the update.' -Icon Info
}
```

For a form, use `New-FluencePrompt` with `Show-FluenceDialog`:

```powershell
$result = Show-FluenceDialog -Title 'Connection' -Prompts @(
    New-FluencePrompt -Name Server -Message 'Server' -ValidateNotEmpty
    New-FluencePrompt -Name Port -Message 'Port' -InputType Number -DefaultValue 443
) -Buttons 'Connect', 'Cancel'

if ($result.Connect)
{
    "Connecting to $($result.Server):$($result.Port)"
}
```

The [examples catalogue](examples/README.md) has six complete scripts. The [result object reference](https://fluencewpf.com/docs/powershell/reference/result-objects) describes the properties returned by each command.

Every window-opening command supports title-bar customization: `-Title` (also `-TitleBarText`) sets its text, and `-TitleBarIcon` accepts a local path, `file:` URI, or `pack:` URI. Only `Show-FluenceWindow` supports `-ShowIcon:$false` to hide the built-in host icon. For dialogs, `-Icon` remains the body severity/question glyph and `-Image` remains a body image; neither sets the title-bar icon. See the [dialog guide](https://fluencewpf.com/docs/powershell/how-to/dialogs) and [hosted-window guide](https://fluencewpf.com/docs/powershell/how-to/windows-from-xaml).

## Build reference and packages

After staging, regenerate all 16 command pages from the help in `src/Fluence.Wpf.PowerShell/Public/*.ps1`:

```powershell
pwsh -NoProfile -File .\Fluence.Wpf.PowerShell.Module\build\Export-ModuleReference.ps1
```

Build current library source, stage the module, and package a ZIP and a PowerShell Gallery format package:

```powershell
pwsh -NoProfile -File .\Fluence.Wpf.PowerShell.Module\build\Package-Module.ps1
```

Packages are written to `Fluence.Wpf.PowerShell.Module/artifacts/`. `Package-Module.ps1` needs an already available NuGet provider version 2.8.5.208 or later. It creates the `.nupkg` through a temporary local repository; it does not publish to PowerShell Gallery.

Local packaging requires the .NET SDK and rebuilds by default. `-UseExistingBuild` skips compilation for callers that have verified the existing binaries came from the intended revision. CI uses this switch after downloading binaries from the same run; matching assembly versions alone do not establish freshness.

Both package files are staged and checked before either final file is replaced. A handled promotion failure attempts to restore the exact previous pair, including a previously absent file. If restoration fails, the error identifies retained backups for recovery. This protects against ordinary handled failures; the two-file update is not atomic across a machine crash.

## Run the module gate

The test runner uses PSScriptAnalyzer 1.25.0 and Pester 5.8.0 by exact version. Install these in each PowerShell edition in which you run the gate. Windows PowerShell 5.1 may also need a NuGet provider and TLS 1.2 during that separate installation step.

```powershell
pwsh -NoProfile -File .\Fluence.Wpf.PowerShell.Module\build\Test-Module.ps1
powershell.exe -NoProfile -STA -File .\Fluence.Wpf.PowerShell.Module\build\Test-Module.ps1
```

Run `Test-Module.ps1 -IncludeUi` on an interactive desktop to include tests that open windows. The default gate checks source, build scripts, examples, and tests with PSScriptAnalyzer, then runs the Pester logic suite. It requires the library to have been staged first.

## Project layout

| Path | Purpose |
| --- | --- |
| `src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1` | Manifest and 16 exported function names. |
| `src/Fluence.Wpf.PowerShell/Public/` | One exported function per file; comment-based help is the reference source. |
| `src/Fluence.Wpf.PowerShell/Private/` | Loader, UI runspace, theming, and dialog implementation. |
| `examples/` | Runnable scripts and XAML. |
| `build/` | Stage, package, test, and reference generation scripts. |
| `tests/` | Pester tests. |

## License

BSD 3-Clause. See the repository [LICENSE](../LICENSE).
