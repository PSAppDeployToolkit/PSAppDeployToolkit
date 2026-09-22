# Set-FluenceTheme

Applies a Fluent theme (and optionally a backdrop) to the process-wide WPF resources.

## Syntax

```text
Set-FluenceTheme [-Theme] <string> [-Backdrop <string>] [-UpdateAccent] [<CommonParameters>]
```

## Description

Runs the Fluence theme engine on the UI (STA) thread, rebuilding the computed color and brush dictionary for the requested theme. When -Backdrop is omitted the current backdrop is preserved. The accent intent is always preserved by the engine.

## Parameters

| Name | Type | Required | Default | Description |
| --- | --- | --- | --- | --- |
| `-Theme` | String | Yes |  | Auto, Light, Dark, or HighContrast. Values: Auto, Light, Dark, HighContrast. |
| `-Backdrop` | String | No |  | Mica, Acrylic, Tabbed, None, or Auto. When omitted, the current backdrop is kept. Values: Mica, Acrylic, Tabbed, None, Auto. |
| `-UpdateAccent` | switch | No |  | Accepted and ignored. Earlier library versions took an updateAccent flag on Apply; the 0.9 engine always preserves the accent intent, so the switch stays only for contract stability. |

## Examples

### Example 1

```powershell
Set-FluenceTheme -Theme Dark
```

### Example 2

```powershell
Set-FluenceTheme -Theme Light -Backdrop Acrylic
```

## Notes

Uses the calling STA thread or a module-owned STA runspace. An existing host application is supported only when the command already runs on its dispatcher thread. Omitting -Backdrop keeps the current backdrop, and the accent intent is preserved.

## See also

- [Reference index](README.md)
- [Result objects](result-objects.md)
- [Input types](input-types.md)
