# Fluence.Wpf.PowerShell

Declarative Fluent (Windows 11) dialogs, prompts, progress windows and full windows for PowerShell, built on the [Fluence.Wpf](../README.md) control library.

Write a specification (prompts, buttons, an optional icon or image) and get a themed WPF dialog back in one call. No WPF project, no XAML unless you want it, no C#. Works on Windows PowerShell 5.1 and PowerShell 7.4 or later.

The user documentation is under [docs/powershell/](../docs/powershell/README.md): a [tutorial](../docs/powershell/tutorial.md), how-to guides, a [reference](../docs/powershell/reference/README.md) page per cmdlet and an [explanation](../docs/powershell/explanation.md) of the design. This file is the short version.

---

## Requirements

- Windows PowerShell 5.1 (`powershell.exe`) or PowerShell 7.4 or later (`pwsh`)
- Windows 10 1809 or later (Mica and Tabbed backdrops need Windows 11)
- The Fluence.Wpf assemblies staged under `src\Fluence.Wpf.PowerShell\lib\` (see Import)

---

## Import

From the repository, build the library once and stage it into the module, then import the manifest:

```powershell
dotnet build ..\Fluence.Wpf\Fluence.Wpf.csproj -c Release
pwsh -NoProfile -File build\Build-Module.ps1
Import-Module .\src\Fluence.Wpf.PowerShell\Fluence.Wpf.PowerShell.psd1
```

Staging first checks that the manifest `ModuleVersion` and `PSData.Prerelease` exactly match `VersionPrefix` and `VersionSuffix` in `Directory.Build.props`. A mismatch, including a stable/prerelease transition, stops before changing the existing `lib` folder.

From a release zip, extract the `Fluence.Wpf.PowerShell` folder into a module path and `Import-Module Fluence.Wpf.PowerShell`. The staged module is self-contained and imports on both editions; the loader picks `lib\net472` for Windows PowerShell and `lib\net8.0-windows10.0.26100.0` for PowerShell 7.

---

## Quick start

A one-line message:

```powershell
Show-FluenceMessage -Message 'Install complete.' -Icon Success
```

A single value:

```powershell
$name = Get-FluenceInput -Message 'Your name?'
```

A validated form:

```powershell
$r = Show-FluenceDialog -Title 'Sign in' -Prompts @(
    New-FluencePrompt -Name User -Message 'Account' -ValidateNotEmpty
    New-FluencePrompt -Name Pass -Message 'Password' -InputType Password -ValidateNotEmpty
) -Buttons (New-FluenceButton -Text 'Login' -IsDefault), 'Cancel'

if ($r.Login) { "Signed in as $($r.User)" }
```

A progress window around long work:

```powershell
$progress = Show-FluenceProgress -Title 'Contoso Suite' -Message 'Installing...'
try
{
    Update-FluenceProgress -Handle $progress -Detail 'Step 1 of 3' -PercentComplete 33
    # ...
}
finally
{
    Close-FluenceProgress -Handle $progress
}
```

A full window from XAML, with its controls wired in `-Initialize`:

```powershell
Show-FluenceWindow -Xaml $xaml -WatchSystemTheme -Data @{ Tick = 0 } -Initialize {
    param($Window, $Data)
    $Window.FindName('CycleButton').add_Click({
            $Data.Tick++
            Set-FluenceBackdrop -Backdrop 'Acrylic' -Window $Window
        }.GetNewClosure())
}
```

On an MTA host (`pwsh -MTA`) the `-Content`, `-Initialize` and handler scriptblocks run on a module-owned UI runspace and cannot see caller variables or functions; pass values through `-Data` and keep the blocks self-contained.

---

## Commands

| Command | Description |
| --- | --- |
| `Show-FluenceMessage` | Message or confirmation dialog with a button preset (`OK`, `OKCancel`, `YesNo`, `YesNoCancel`); returns the clicked button name. A dismiss returns the safe name, never `$null`. `-DefaultButton`, `-Timeout`, `-Countdown`, `-Image`, `-MessageAlignment`, `-Position`. |
| `Show-FluenceDialog` | Dialog built from prompt and button specifications; returns a `Fluence.DialogResult` with a value per prompt, a flag per button, `Cancelled` and `TimedOut`. |
| `Get-FluenceInput` | Single-prompt dialog; returns the value, or `$null` on cancel or timeout. |
| `New-FluencePrompt` | Builds a prompt specification: label, one of fourteen input types, default, validation. |
| `New-FluenceButton` | Builds a button specification: caption, result name, default and cancel flags. |
| `Show-FluenceListSelection` | Pick one item, or several with `-MultiSelect`, from a list; returns the selection or `$null`. |
| `Show-FluenceRestartPrompt` | Restart now / Restart later with a countdown; returns `Restart`, `Later` or `TimedOut`. Never restarts the machine. |
| `Show-FluenceProgress` | Opens a non-modal, topmost progress window; returns a `Fluence.ProgressHandle`. |
| `Update-FluenceProgress` | Changes the message, detail or percentage of an open progress window. |
| `Close-FluenceProgress` | Closes a progress window; a no-op on a closed handle. |
| `Show-FluenceWindow` | Hosts a full `FluenceWindow` from a content scriptblock, a XAML string or a XAML file; blocks until closed and returns the stashed result. |
| `Close-FluenceWindow` | Closes a hosted window, optionally stashing a result. |
| `Set-FluenceTheme` | Applies `Auto`, `Light`, `Dark` or `HighContrast` process-wide, optionally with a backdrop. |
| `Set-FluenceAccent` | Pins a custom accent colour or returns to the system accent. |
| `Set-FluenceBackdrop` | Applies `Mica`, `Acrylic`, `Tabbed` or `None`, optionally on an open window. |
| `Get-FluenceTheme` | Reads the current theme, resolved theme, backdrop and dark-mode flag. |

Every dialog cmdlet takes `-Theme` and `-Backdrop`; `Show-FluenceDialog`, `Show-FluenceProgress` and `Show-FluenceWindow` also take `-Accent`. Each applies only when you pass it, so `Set-FluenceTheme`, `Set-FluenceBackdrop` and `Set-FluenceAccent` still hold for later dialogs. The first Fluence call in a process seeds `Auto` (follow Windows), `Mica` and the system accent.

---

## Examples

`examples\` holds ready-to-run scripts. Run any of them with `pwsh -File examples\QuickStart.ps1` or `powershell.exe -File examples\QuickStart.ps1`.

| Script | What it shows |
| --- | --- |
| `QuickStart.ps1` | One-line success message |
| `Message.ps1` | YesNo confirmation with branch logic |
| `SignIn.ps1` | Account and password form with validation |
| `Form.ps1` | Mixed form: Text, Number, Choice, Date, Checkbox |
| `ImageDialog.ps1` | A branded message with an image, centred text and a corner position |
| `ListSelection.ps1` | Single and multi-select list dialogs |
| `RestartPrompt.ps1` | Restart now / Restart later with a countdown |
| `Progress.ps1` | A five-step loop behind a progress window |
| `HelloWindow.ps1` | A Mica window whose button cycles the backdrop and rotates a greeting |
| `ThemeAndAccent.ps1` | Light, Dark and Auto themes, custom accents, and a window icon that follows the theme |
| `ControlsTour.ps1` | Common controls in scrolling cards; a toggle drives an `InfoBar` |
| `LoadXamlFile.ps1` | The window UI loaded from `MainWindow.xaml` on disk |

---

## Tests and gate

`build\Test-Module.ps1` imports PSScriptAnalyzer 1.25.0 and Pester 5.8.0 by exact version. It runs the analyzer with `PSScriptAnalyzerSettings.psd1`, a second analyzer pass for the Allman brace rule `PSPlaceOpenBrace`, and the Pester suite under `tests\`. Other installed versions do not satisfy the gate. The default run is the logic lane; `-IncludeUi` also runs the UI-tagged cases, which open and close real windows. Run it in a dedicated process under both editions. The runner fails if inline WPF windows remain after Pester and shuts down its owned dispatcher before exiting:

```powershell
pwsh -NoProfile -File build\Test-Module.ps1
powershell.exe -NoProfile -STA -File build\Test-Module.ps1
```

Install the pinned build tools in each edition before running its gate (this setup requires network access to PowerShell Gallery):

```powershell
Install-Module PSScriptAnalyzer -RequiredVersion 1.25.0 -Repository PSGallery -Scope CurrentUser -Force
Install-Module Pester -RequiredVersion 5.8.0 -Repository PSGallery -Scope CurrentUser -Force -SkipPublisherCheck
```

On Windows PowerShell 5.1, first enable TLS 1.2 and install its NuGet provider in that separate setup step:

```powershell
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12
Install-PackageProvider -Name NuGet -RequiredVersion 2.8.5.208 -Scope CurrentUser -Force
```

PowerShell 7 ships a compatible NuGet provider with PackageManagement (3.0.0.1 in the validated host). Packaging requires an already available provider of at least 2.8.5.208 and fails before staging if none is available; it does not bootstrap providers. `Publish-Module` targets only a temporary local repository. Install PowerShellGet's packaging dependencies during setup before attempting an offline package run.

CI has a separate `powershell` job that downloads the `build` job's library binaries, stages them, runs the logic lane in all three host modes, and uploads module packages and test results. The .NET release depends only on `build`, so a PowerShell tool or feed outage cannot block the library artifacts or release. Both jobs must pass before merging the integration change; PowerShell Gallery publication remains a follow-up.

`build\Package-Module.ps1` writes `Fluence.Wpf.PowerShell-<version>.zip` and `Fluence.Wpf.PowerShell.<version>.nupkg` to `artifacts\`. `build\Export-ModuleReference.ps1` regenerates the reference pages under `docs\powershell\reference\` from the comment-based help.

---

## License

BSD 3-Clause. See [LICENSE](../LICENSE) at the repository root.
