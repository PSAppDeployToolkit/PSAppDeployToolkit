# PowerShell reference

This section describes the 16 functions exported by `Fluence.Wpf.PowerShell` and the values they exchange. Each command page is generated from its function's comment-based help by [`Generate-PowerShellReference.ps1`](https://github.com/sintaxasn/Fluence.Wpf/blob/main/tools/Docs/Generate-PowerShellReference.ps1). Update source help and regenerate a page when command behavior changes.

## Dialogs and input

| Command | Purpose |
| --- | --- |
| [Show-FluenceMessage](Show-FluenceMessage.mdx) | Show a message or preset confirmation; return a button name. |
| [Show-FluenceDialog](Show-FluenceDialog.mdx) | Show a dialog from prompt and button specifications. |
| [Get-FluenceInput](Get-FluenceInput.mdx) | Ask for one value. |
| [Show-FluenceListSelection](Show-FluenceListSelection.mdx) | Select one or several list items. |
| [Show-FluenceRestartPrompt](Show-FluenceRestartPrompt.mdx) | Ask to restart now or later with an optional countdown. |
| [New-FluencePrompt](New-FluencePrompt.mdx) | Define a field, its default, and validation. |
| [New-FluenceButton](New-FluenceButton.mdx) | Define an action and its result name. |

## Progress and windows

| Command | Purpose |
| --- | --- |
| [Show-FluenceProgress](Show-FluenceProgress.mdx) | Open a progress window and return its handle. |
| [Update-FluenceProgress](Update-FluenceProgress.mdx) | Update a progress window through its handle. |
| [Close-FluenceProgress](Close-FluenceProgress.mdx) | Close a progress window. |
| [Show-FluenceWindow](Show-FluenceWindow.mdx) | Host a XAML or PowerShell built window. |
| [Close-FluenceWindow](Close-FluenceWindow.mdx) | Close a hosted window and optionally return a value. |

## Appearance

| Command | Purpose |
| --- | --- |
| [Set-FluenceTheme](Set-FluenceTheme.mdx) | Request a light, dark, high contrast, or system theme. |
| [Set-FluenceAccent](Set-FluenceAccent.mdx) | Select a custom or system accent. |
| [Set-FluenceBackdrop](Set-FluenceBackdrop.mdx) | Set a process backdrop or change an open window. |
| [Get-FluenceTheme](Get-FluenceTheme.mdx) | Read requested and resolved appearance state. |

## Values and module files

- [Input types](input-types.md) lists all 14 `New-FluencePrompt -InputType` values, controls, default values, and validation behavior.
- [Result objects](result-objects.md) defines dialog outcome flags, prompt and button specifications, progress handles, window results, and theme information.
- The [module manifest](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf.PowerShell.Module/src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1) declares exports and supported PowerShell editions. The [module setup guide](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf.PowerShell.Module/README.md) describes the two staged library builds.

## Shared values

| Option | Accepted values | Initial state |
| --- | --- | --- |
| Theme | `Auto`, `Light`, `Dark`, `HighContrast` | `Auto` on first Fluence call. |
| Backdrop | `Mica`, `Acrylic`, `Tabbed`, `None`, `Auto` | `Mica` on first Fluence call. |
| Dialog icon | `None`, `Info`, `Success`, `Warning`, `Error`, `Question` | Depends on command. |
| Position | `Center`, `TopRight`, `BottomRight` | `Center`. |
| Message alignment | `Left`, `Center` | `Left`. |
| Timeout | 1 through 86400 seconds | No timeout unless supplied. |

The test runner sets `FLUENCE_PS_UI=1` when `-IncludeUi` is used. This variable controls UI tagged Pester tests; it is not a module appearance option.
