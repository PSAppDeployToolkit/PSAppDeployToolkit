# Show-FluenceDialog

Shows a themed Fluent dialog built from prompts and buttons, and returns the user's input.

## Syntax

```text
Show-FluenceDialog [[-Title] <string>] [[-Message] <string[]>] [[-Icon] <string>] [[-Prompts] <Object[]>] [[-Buttons] <Object[]>] [[-Timeout] <int>] [[-Image] <string>] [[-MessageAlignment] <string>] [[-Position] <string>] [[-Theme] <string>] [[-Backdrop] <string>] [[-Accent] <Color>] [[-MinWidth] <int>] [[-ParentWindow] <Window>] [-Countdown] [-Topmost] [<CommonParameters>]
```

## Description

Renders a FluenceWindow with an optional message, a stack of input prompts, and a row of buttons. Returns a Fluence.DialogResult object with a property per named prompt and a boolean per button, plus Cancelled and TimedOut flags.

## Parameters

| Name | Type | Required | Default | Description |
| --- | --- | --- | --- | --- |
| `-Title` | String | No | Fluence | The window title. |
| `-Message` | String[] | No |  | One or more message lines shown above the prompts. |
| `-Icon` | String | No | None | Optional severity icon shown to the left of the message. None (default) renders the message as plain text. Info, Success, Warning, Error, and Question each map to a Segoe Fluent glyph and themed brush (Question uses the Help glyph with the Informational brush). Values: None, Info, Success, Warning, Error, Question. |
| `-Prompts` | Object[] | No |  | Strings or Fluence.Prompt objects (see New-FluencePrompt). A bare string becomes a Text prompt. |
| `-Buttons` | Object[] | No | @('OK') | Strings or Fluence.Button objects (see New-FluenceButton). Defaults to a single OK button. A bare 'Cancel' string is treated as a cancel button (closes on Esc, no validation); any other bare string (for example 'No' or 'Close') is a plain button, so build it with New-FluenceButton -IsCancel if you want it to act as the Esc/cancel affordance. |
| `-Timeout` | Int32 | No |  | Seconds after which the dialog closes on its own with TimedOut set to $true and no button flag set. Between 1 and 86400. |
| `-Countdown` | switch | No |  | Show the remaining seconds in the caption of the default button (or the first button when none is default), refreshed every second. Requires -Timeout. |
| `-Image` | String | No |  | An image shown above the message at up to 120 device-independent pixels high: a file path, a file: URI, or a pack: URI (pack://application:,,,/Assembly;component/path). Other schemes are rejected before the dialog opens. |
| `-MessageAlignment` | String | No | Left | Left (default) or Center. Applies to the image and the message lines. Values: Left, Center. |
| `-Position` | String | No | Center | Center (default), TopRight, or BottomRight of the primary work area. With -ParentWindow, Center gives way to centering over the owner, but TopRight and BottomRight still win: they are applied from the window's Loaded handler, which runs after WPF has placed it. Values: Center, TopRight, BottomRight. |
| `-Theme` | String | No |  | Auto, Light, Dark, or HighContrast. Omit it to keep the theme already applied to the process; the first Fluence call in a process applies Auto. Values: Auto, Light, Dark, HighContrast. |
| `-Backdrop` | String | No |  | Mica, Acrylic, Tabbed, None, or Auto. Omit it to keep the backdrop already applied to the process; the first Fluence call in a process applies Mica. Values: Mica, Acrylic, Tabbed, None, Auto. |
| `-Accent` | Color | No |  | Optional accent color (System.Windows.Media.Color or a parseable string). Defaults to system accent. |
| `-MinWidth` | Int32 | No | 360 | Minimum window width (default 360). |
| `-Topmost` | switch | No |  | Show above other windows. |
| `-ParentWindow` | Window | No |  | An owning System.Windows.Window for modal parenting. |

## Outputs

- Fluence.DialogResult

## Examples

### Example 1

```powershell
Show-FluenceDialog -Title 'Setup' -Prompts 'Your name?' -Buttons OK
```

### Example 2

```powershell
$r = Show-FluenceDialog -Message 'Installation starts in one minute.' -Buttons 'Start now', 'Defer' -Timeout 60 -Countdown
if ($r.TimedOut -or $r.'Start now') { Start-Install }
```

Counts down on the first button and proceeds when the user clicks it or the minute elapses.

## Notes

Uses the calling STA thread or a module-owned STA runspace. An existing host application is supported only when the command already runs on its dispatcher thread. Blocks until the dialog closes.

## See also

- [Reference index](README.md)
- [Result objects](result-objects.md)
- [Input types](input-types.md)
