# Show-FluenceMessage

Shows a themed Fluent message dialog and returns the name of the button the user clicked.

## Syntax

```text
Show-FluenceMessage [-Message] <string[]> [-Title <string>] [-Icon <string>] [-Buttons <string>] [-DefaultButton <string>] [-Timeout <int>] [-Countdown] [-Image <string>] [-MessageAlignment <string>] [-Position <string>] [-Theme <string>] [-Backdrop <string>] [<CommonParameters>]
```

## Description

A thin wrapper around Show-FluenceDialog that maps a named button preset (OK, OKCancel, YesNo, YesNoCancel) to the correct button objects, renders the message as wrapping TextBlocks with an optional leading severity FontIcon, and returns the clicked button name as a string instead of the full DialogResult. For cancel-less presets (OK, YesNo), closing the dialog with the title-bar X or Esc returns the safe button name (OK or No) rather than $null, so a guard such as `if ($answer -ne 'No')` does not run the affirmative branch on a dismiss. A timeout returns -DefaultButton when it is given, and the same safe name otherwise.

## Parameters

| Name | Type | Required | Default | Description |
| --- | --- | --- | --- | --- |
| `-Message` | String[] | Yes |  | One or more message lines displayed in the dialog. |
| `-Title` | String | No | Fluence | The window title. Defaults to 'Fluence'. |
| `-Icon` | String | No | Info | The severity icon to display to the left of the message text. Info (default), Success, Warning, Error, Question, or None. Each severity maps to a distinct Segoe Fluent glyph and themed brush: Success, Warning, and Error use the InfoBar severity glyphs; Info uses the Informational glyph; Question uses the Segoe Fluent Help glyph with the neutral brush. None draws no glyph, which is what an image-led dialog wants. The message renders as wrapping TextBlocks, not inside an InfoBar. Values: None, Info, Success, Warning, Error, Question. |
| `-Buttons` | String | No | OK | Named button set: OK (default), OKCancel, YesNo, or YesNoCancel. Values: OK, OKCancel, YesNo, YesNoCancel. |
| `-DefaultButton` | String | No |  | The button (by name, for example 'No') that Enter activates, that carries the countdown caption, and that a timeout resolves to. Must be one of the names in the chosen preset. Defaults to the preset's first button. Values: OK, Cancel, Yes, No. |
| `-Timeout` | Int32 | No |  | Seconds after which the dialog closes on its own. Between 1 and 86400. |
| `-Countdown` | switch | No |  | Show the remaining seconds in the default button's caption. Requires -Timeout. |
| `-Image` | String | No |  | An image shown above the message at up to 120 device-independent pixels high: a file path, a file: URI, or a pack: URI. Other schemes are rejected before the dialog opens. |
| `-MessageAlignment` | String | No |  | Left (default) or Center. Applies to the image and the message lines. Values: Left, Center. |
| `-Position` | String | No |  | Center (default), TopRight, or BottomRight of the primary work area. Values: Center, TopRight, BottomRight. |
| `-Theme` | String | No |  | Auto, Light, Dark, or HighContrast. Omit it to keep the theme already applied to the process; the first Fluence call in a process applies Auto. Values: Auto, Light, Dark, HighContrast. |
| `-Backdrop` | String | No |  | Mica, Acrylic, Tabbed, None, or Auto. Omit it to keep the backdrop already applied to the process; the first Fluence call in a process applies Mica. Values: Mica, Acrylic, Tabbed, None, Auto. |

## Outputs

- System.String

## Examples

### Example 1

```powershell
Show-FluenceMessage -Message 'Proceed?' -Icon Question -Buttons YesNo
```

### Example 2

```powershell
$answer = Show-FluenceMessage -Message 'Save changes?' -Buttons OKCancel -Icon Warning
if ($answer -eq 'OK') { Save-Data }
```

### Example 3

```powershell
$answer = Show-FluenceMessage -Message 'Restart now?' -Buttons YesNo -DefaultButton No -Timeout 30 -Countdown
```

Counts down on the No button and returns 'No' if nobody answers within thirty seconds.

## Notes

Uses the calling STA thread or a module-owned STA runspace. An existing host application is supported only when the command already runs on its dispatcher thread. Blocks until the dialog closes.

## See also

- [Reference index](README.md)
- [Result objects](result-objects.md)
- [Input types](input-types.md)
