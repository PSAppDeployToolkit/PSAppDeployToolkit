# Reference

Technical description of the `Fluence.Wpf.PowerShell` module surface. The cmdlet pages are generated from the module's comment-based help by `Fluence.Wpf.PowerShell.Module/build/Export-ModuleReference.ps1`; edit the help in the function and regenerate rather than editing a page.

## Cmdlets

| Cmdlet | Purpose |
| --- | --- |
| [Show-FluenceMessage](Show-FluenceMessage.md) | Message or confirmation dialog with a button preset; returns the clicked button name. |
| [Show-FluenceDialog](Show-FluenceDialog.md) | Dialog built from prompt and button specifications; returns a `Fluence.DialogResult`. |
| [Get-FluenceInput](Get-FluenceInput.md) | Single-prompt dialog; returns the value or `$null`. |
| [New-FluencePrompt](New-FluencePrompt.md) | Builds a `Fluence.Prompt` specification. |
| [New-FluenceButton](New-FluenceButton.md) | Builds a `Fluence.Button` specification. |
| [Show-FluenceListSelection](Show-FluenceListSelection.md) | Pick one item, or several with `-MultiSelect`, from a list. |
| [Show-FluenceRestartPrompt](Show-FluenceRestartPrompt.md) | Restart now / Restart later prompt with a countdown; returns `Restart`, `Later` or `TimedOut`. |
| [Show-FluenceProgress](Show-FluenceProgress.md) | Opens a non-modal progress window; returns a `Fluence.ProgressHandle`. |
| [Update-FluenceProgress](Update-FluenceProgress.md) | Changes the message, detail or percentage of an open progress window. |
| [Close-FluenceProgress](Close-FluenceProgress.md) | Closes a progress window. |
| [Show-FluenceWindow](Show-FluenceWindow.md) | Hosts a full FluenceWindow from a content scriptblock, a XAML string or a XAML file. |
| [Close-FluenceWindow](Close-FluenceWindow.md) | Closes a hosted window, optionally stashing a result. |
| [Set-FluenceTheme](Set-FluenceTheme.md) | Applies Auto, Light, Dark or HighContrast, optionally with a backdrop. |
| [Set-FluenceAccent](Set-FluenceAccent.md) | Pins a custom accent colour or returns to the system accent. |
| [Set-FluenceBackdrop](Set-FluenceBackdrop.md) | Applies Mica, Acrylic, Tabbed, None or Auto. |
| [Get-FluenceTheme](Get-FluenceTheme.md) | Reads the current theme state. |

## Objects

- [Result objects](result-objects.md): `Fluence.DialogResult`, `Fluence.ProgressHandle`, `Fluence.WindowResult`, `Fluence.ThemeInfo`, the specification objects `Fluence.Prompt` and `Fluence.Button`, and the string results of the wrapper cmdlets.
- [Input types](input-types.md): every `-InputType` value of `New-FluencePrompt`, the control it renders, the value type it returns and the validation that applies.

## Common parameter values

| Parameter | Values | Default |
| --- | --- | --- |
| `-Theme` | `Auto`, `Light`, `Dark`, `HighContrast` | whatever is applied; `Auto` on the first call in a process |
| `-Backdrop` | `Mica`, `Acrylic`, `Tabbed`, `None`, `Auto` | whatever is applied; `Mica` on the first call in a process |
| `-Icon` | `None`, `Info`, `Success`, `Warning`, `Error`, `Question` | `None` on `Show-FluenceDialog`, `Info` on `Show-FluenceMessage`, `Warning` on `Show-FluenceRestartPrompt` |
| `-Position` | `Center`, `TopRight`, `BottomRight` | `Center` |
| `-MessageAlignment` | `Left`, `Center` | `Left` |
| `-Timeout` | 1 to 86400 seconds | none |

## Environment variables

| Variable | Read by | Effect |
| --- | --- | --- |
| `FLUENCE_PS_UI` | the Pester suite | `1` runs the UI-tagged render tests, which open real windows. Set by `Test-Module.ps1 -IncludeUi`. |

## Files

| File | Purpose |
| --- | --- |
| `Fluence.Wpf.PowerShell.psd1` | Module manifest: version, exported functions, format file, gallery metadata. |
| `Fluence.Wpf.PowerShell.psm1` | Root module: loads WPF, dot-sources `Private/` and `Public/`, loads `Fluence.Wpf.dll`, registers the removal handler. |
| `lib/net472/`, `lib/net8.0-windows10.0.26100.0/` | Staged library builds for Windows PowerShell and PowerShell 7. Gitignored; produced by `build/Build-Module.ps1`. |
| `Formats/Fluence.Format.ps1xml` | Views for `Fluence.Button`, `Fluence.Prompt`, `Fluence.ProgressHandle` and the outcome-only `Fluence.DialogResult`. |
| `PSScriptAnalyzerSettings.psd1` | Analyzer rules for the gate: Error and Warning severities, 5.1 and 7.0 syntax compatibility, the 5.1 cmdlet compatibility profile. |
