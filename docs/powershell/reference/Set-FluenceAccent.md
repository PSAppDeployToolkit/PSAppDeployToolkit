# Set-FluenceAccent

Sets the Fluent accent color, either to a custom color or back to the system accent.

## Syntax

```text
Set-FluenceAccent [-Color] <Color> [<CommonParameters>]
Set-FluenceAccent -System [<CommonParameters>]
```

## Description

Runs the Fluence accent resolver on the UI (STA) thread and re-runs the theme pipeline so every accent-derived brush is recomputed. Use -Color to pin the accent ramp to a custom color, or -System to reset the accent intent to the OS palette.

## Parameters

| Name | Type | Required | Default | Description |
| --- | --- | --- | --- | --- |
| `-Color` | Color | Yes |  | The custom accent color (System.Windows.Media.Color or a parseable string). |
| `-System` | switch | Yes |  | Reset the accent intent to the system (OS) accent. |

## Examples

### Example 1

```powershell
Set-FluenceAccent -Color '#0078D4'
```

### Example 2

```powershell
Set-FluenceAccent -System
```

## Notes

Uses the calling STA thread or a module-owned STA runspace. An existing host application is supported only when the command already runs on its dispatcher thread.

## See also

- [Reference index](README.md)
- [Result objects](result-objects.md)
- [Input types](input-types.md)
