# Set-FluenceBackdrop

Sets the Fluent system backdrop for the process (and optionally a specific window).

## Syntax

```text
Set-FluenceBackdrop [-Backdrop] <string> [-Window <Window>] [<CommonParameters>]
```

## Description

Runs on the UI (STA) thread. When -Window is supplied its SystemBackdropType is set, then the theme pipeline is re-applied with the requested backdrop and the current theme so the computed resources match. The accent intent is preserved.

## Parameters

| Name | Type | Required | Default | Description |
| --- | --- | --- | --- | --- |
| `-Backdrop` | String | Yes |  | Mica, Acrylic, Tabbed, None, or Auto. Auto lets the library pick the backdrop the running Windows build supports. Values: Mica, Acrylic, Tabbed, None, Auto. |
| `-Window` | Window | No |  | An optional System.Windows.Window whose SystemBackdropType is updated. When supplied it must be a window owned by the Fluence UI thread. |

## Examples

### Example 1

```powershell
Set-FluenceBackdrop -Backdrop Acrylic
```

### Example 2

```powershell
Set-FluenceBackdrop -Backdrop Mica -Window $window
```

## Notes

Uses the calling STA thread or a module-owned STA runspace. An existing host application is supported only when the command already runs on its dispatcher thread. When -Window is supplied it must be a window owned by the Fluence UI thread.

## See also

- [Reference index](README.md)
- [Result objects](result-objects.md)
- [Input types](input-types.md)
