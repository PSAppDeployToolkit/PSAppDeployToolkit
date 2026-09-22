# Get-FluenceInput

Shows a themed Fluent input dialog and returns the captured value, or $null on cancel.

## Syntax

```text
Get-FluenceInput [-Message] <string> [-Title <string>] [-DefaultValue <Object>] [-AsPlainText] [-InputType <string>] [-ValidateSet <string[]>] [-As <string>] [-Timeout <int>] [-Countdown] [-Theme <string>] [-Backdrop <string>] [<CommonParameters>]
```

## Description

Wraps a single-prompt Show-FluenceDialog with an OK (default) and Cancel button. Returns the captured input value when the user clicks OK, or $null when the user cancels or the dialog times out. Password input returns SecureString unless -AsPlainText is specified. Dispose the returned SecureString when the caller no longer needs it.

## Parameters

| Name | Type | Required | Default | Description |
| --- | --- | --- | --- | --- |
| `-Message` | String | Yes |  | The prompt label shown in the dialog (Mandatory). |
| `-Title` | String | No | Fluence | The window title. Defaults to 'Fluence'. |
| `-DefaultValue` | Object | No |  | The initial value pre-filled in the input control. Password defaults must be strings and remain plaintext in the specification; omit the default when collecting a secret. |
| `-AsPlainText` | switch | No |  | Password prompts only. Return a plain string instead of the default SecureString. Assign an explicitly requested plaintext value to a variable to avoid printing it. The DialogResult default formatting does not apply to this raw string. |
| `-InputType` | String | No | Text | The input control type. One of: Text (default), Multiline, Password, Number, Checkbox, Toggle, Choice, Date, Time, FileOpen, FileSave, FolderOpen, Link. A List prompt is not offered here because it picks from a set rather than capturing a typed value; use Show-FluenceListSelection for that. Values: Text, Multiline, Password, Number, Checkbox, Toggle, Choice, Date, Time, FileOpen, FileSave, FolderOpen, Link. |
| `-ValidateSet` | String[] | No |  | The allowed values for a Choice prompt. Required when -InputType is Choice. |
| `-As` | String | No |  | How a Choice prompt renders its values: Combo (default) or Radio. Values: Combo, Radio. |
| `-Timeout` | Int32 | No |  | Seconds after which the dialog closes on its own and $null is returned. Between 1 and 86400. |
| `-Countdown` | switch | No |  | Show the remaining seconds in the OK button's caption. Requires -Timeout. |
| `-Theme` | String | No |  | Auto, Light, Dark, or HighContrast. Omit it to keep the theme already applied to the process; the first Fluence call in a process applies Auto. Values: Auto, Light, Dark, HighContrast. |
| `-Backdrop` | String | No |  | Mica, Acrylic, Tabbed, None, or Auto. Omit it to keep the backdrop already applied to the process; the first Fluence call in a process applies Mica. Values: Mica, Acrylic, Tabbed, None, Auto. |

## Outputs

- System.Object

## Examples

### Example 1

```powershell
$name = Get-FluenceInput -Message 'Enter your name'
```

### Example 2

```powershell
$age = Get-FluenceInput -Message 'Enter your age' -InputType Number -DefaultValue 25
```

### Example 3

```powershell
$edition = Get-FluenceInput -Message 'Edition' -InputType Choice -ValidateSet 'Standard', 'Pro' -As Radio
```

### Example 4

```powershell
$server = Get-FluenceInput -Message 'Server name' -DefaultValue 'localhost' -Timeout 20 -Countdown
if ($null -eq $server) { $server = 'localhost' }
```

Falls back to the default after twenty unattended seconds.

## Notes

Uses the calling STA thread or a module-owned STA runspace. An existing host application is supported only when the command already runs on its dispatcher thread. Blocks until the dialog closes.

## See also

- [Reference index](README.md)
- [Result objects](result-objects.md)
- [Input types](input-types.md)
