# Close-FluenceProgress

Closes a progress window opened by Show-FluenceProgress.

## Syntax

```text
Close-FluenceProgress [-Handle] <ProgressHandle> [<CommonParameters>]
```

## Description

Closes the window on its UI thread and marks the handle closed. Closing an already closed handle is a no-op, so a finally block can call this unconditionally. On an MTA host this also ends the UI pump that kept the window live and releases the module's UI runspace for the next dialog.

## Parameters

| Name | Type | Required | Default | Description |
| --- | --- | --- | --- | --- |
| `-Handle` | Object | Yes |  | The Fluence.ProgressHandle returned by Show-FluenceProgress. |

## Outputs

- System.Void

## Examples

### Example 1

```powershell
try { $progress = Show-FluenceProgress -Message 'Working...'; Invoke-Work } finally { Close-FluenceProgress -Handle $progress }
```

## Notes

Runs on the UI thread that owns the window. A window implies a running Application, so no application is created here.

## See also

- [Reference index](README.md)
- [Result objects](result-objects.md)
- [Input types](input-types.md)
