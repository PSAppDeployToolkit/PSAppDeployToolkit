# PowerShell values and results

The module uses `PSCustomObject` values with a `PSTypeName` for its specifications and structured results. Use their named properties in scripts. The [input type table](input-types.md) defines each prompt value type.

## Fluence.DialogResult

`Show-FluenceDialog` returns one property for each prompt and button, plus the outcome flags:

| Property | Type | Meaning |
| --- | --- | --- |
| Prompt `Name` | Depends on input type. | Value captured when the dialog closed; an untouched prompt keeps its default. |
| Button `Name` | `bool` | True for the selected action. |
| `Cancelled` | `bool` | True for a cancel action, Esc, or title bar dismissal. |
| `TimedOut` | `bool` | True when `-Timeout` expires; no button flag is true. |

Exactly one button flag, `Cancelled`, or `TimedOut` describes a normal close. A failure that leaves the UI without a result is represented by a minimal cancelled result. Prompt and button names must be unique without regard to case and cannot use the reserved result names.

The default console view shows only `Cancelled` and `TimedOut`. Explicit access, `Format-List *`, and serialization can expose prompt values. Password values are `SecureString` unless `-AsPlainText` was requested; dispose secure values from a dialog result even when the dialog was cancelled.

## Simple command results

| Command | Result |
| --- | --- |
| `Show-FluenceMessage` | Button name string: `OK`, `Cancel`, `Yes`, or `No`. A dismiss maps to the cancel button or final preset button; timeout uses `-DefaultButton` when supplied. |
| `Get-FluenceInput` | Entered value, or `$null` on cancel or timeout. |
| `Show-FluenceListSelection` | Selected value or array for `-MultiSelect`, or `$null` on cancel or timeout. |
| `Show-FluenceRestartPrompt` | `Restart`, `Later`, or `TimedOut`. |

## Fluence.Prompt and Fluence.Button

`New-FluencePrompt` returns a `Fluence.Prompt` specification for `Show-FluenceDialog -Prompts`.

| Prompt property | Meaning |
| --- | --- |
| `Name`, `Message`, `InputType` | Result key, visible label, and [input type](input-types.md). An omitted name is generated as `Input_` plus eight hex characters. |
| `DefaultValue`, `ValidateSet` | Initial value and allowed choices for `Choice` or `List`. |
| `As`, `MultiSelect`, `AsPlainText` | Radio or combo choice layout, multiple list values, or plain password result. |
| `ValidateNotEmpty`, `ValidatePattern`, `ValidateScript` | Validation rules evaluated on a non-cancel action. |

`New-FluenceButton` returns a `Fluence.Button` with `Text`, `Name`, `IsDefault`, and `IsCancel`. `Name` defaults to `Text`. The default button responds to Enter; the cancel button responds to Esc and bypasses validation. A bare `Cancel` string becomes a cancel button automatically.

## Fluence.ProgressHandle

`Show-FluenceProgress` returns a handle used by `Update-FluenceProgress` and `Close-FluenceProgress`.

| Property | Meaning |
| --- | --- |
| `Id` | Window GUID. |
| `Mode` | `Inline` for a calling STA UI thread or `Runspace` for the module UI runspace. |
| `IsOpen` | False after close. |
| `State` | Synchronized hashtable with `Message`, `Detail`, `PercentComplete`, `Indeterminate`, and `CloseRequested`. |
| `Spec` | Opening options including title, position, width, theme, backdrop, and accent. |
| `Parts` | Live WPF window and control objects; read their dependency properties only from the owning thread. |

Only one progress window can be open at a time.

## Fluence.WindowResult

`Show-FluenceWindow -PassThru` returns `Result`, the value passed to `Close-FluenceWindow -Result` or `$null`, and `Closed = $true`. Without `-PassThru`, `Show-FluenceWindow` returns the result value directly.

## Fluence.ThemeInfo

`Get-FluenceTheme` returns `CurrentTheme` (the request), `ResolvedTheme` (the displayed theme), `CurrentBackdrop`, and `IsAppInDarkMode`. Compare the backdrop enum by `.ToString()`: newer library builds use `WindowBackdropType`, while earlier builds use `BackdropType`.
