# Show-FluenceListSelection

Shows a themed dialog with a list of items to pick from and returns the selection.

## Syntax

```text
Show-FluenceListSelection [-Items] <string[]> [-Message <string>] [-Title <string>] [-MultiSelect] [-DefaultValue <Object>] [-Timeout <int>] [-Countdown] [-Theme <string>] [-Backdrop <string>] [<CommonParameters>]
```

## Description

A single List prompt (see New-FluencePrompt -InputType List) with OK and Cancel buttons. OK requires a selection. Returns the selected item, or with -MultiSelect an array of the selected items (an array even when one item is selected). Returns $null when the user cancels or the dialog times out.

The shape mirrors the PSADT list selection dialog so a deployment toolkit can map to it directly.

## Parameters

| Name | Type | Required | Default | Description |
| --- | --- | --- | --- | --- |
| `-Items` | String[] | Yes |  | The items to list, in display order. At least one is required. |
| `-Message` | String | No | Select an item | The label shown above the list. |
| `-Title` | String | No | Fluence | The window title. Defaults to 'Fluence'. |
| `-MultiSelect` | switch | No |  | Allow more than one item to be selected; the result is then an array. |
| `-DefaultValue` | Object | No |  | The item (or, with -MultiSelect, items) selected when the dialog opens. |
| `-Timeout` | Int32 | No |  | Seconds after which the dialog closes on its own and $null is returned. Between 1 and 86400. |
| `-Countdown` | switch | No |  | Show the remaining seconds in the OK button's caption. Requires -Timeout. |
| `-Theme` | String | No |  | Auto, Light, Dark, or HighContrast. Omit it to keep the theme already applied to the process; the first Fluence call in a process applies Auto. Values: Auto, Light, Dark, HighContrast. |
| `-Backdrop` | String | No |  | Mica, Acrylic, Tabbed, None, or Auto. Omit it to keep the backdrop already applied to the process; the first Fluence call in a process applies Mica. Values: Mica, Acrylic, Tabbed, None, Auto. |

## Outputs

- System.Object

## Examples

### Example 1

```powershell
$region = Show-FluenceListSelection -Message 'Choose a region' -Items 'Europe', 'Americas', 'Asia Pacific'
```

### Example 2

```powershell
$features = Show-FluenceListSelection -Message 'Features to install' -Items 'Core', 'Docs', 'Samples' -MultiSelect -DefaultValue 'Core'
if ($null -ne $features) { "Installing: $($features -join ', ')" }
```

## Notes

Uses the calling STA thread or a module-owned STA runspace. An existing host application is supported only when the command already runs on its dispatcher thread. Blocks until the dialog closes.

## See also

- [Reference index](README.md)
- [Result objects](result-objects.md)
- [Input types](input-types.md)
