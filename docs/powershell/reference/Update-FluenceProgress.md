# Update-FluenceProgress

Changes the message, detail, or percentage of an open progress window.

## Syntax

```text
Update-FluenceProgress [-Handle] <ProgressHandle> [-Message <string>] [-Detail <string>] [-PercentComplete <double>] [-Indeterminate] [<CommonParameters>]
```

## Description

Only the values you pass change; the rest keep their current text. Passing -PercentComplete makes the bar determinate (clamped to 0..100); -Indeterminate switches it back. On the inline STA host this call also lets the window repaint, so call it at least every few seconds during long work even when nothing changed.

## Parameters

| Name | Type | Required | Default | Description |
| --- | --- | --- | --- | --- |
| `-Handle` | Object | Yes |  | The Fluence.ProgressHandle returned by Show-FluenceProgress. |
| `-Message` | String | No |  | The new main status line. |
| `-Detail` | String | No |  | The new detail line. An empty string hides it. |
| `-PercentComplete` | Double | No |  | The new percentage; the bar becomes determinate. |
| `-Indeterminate` | switch | No |  | Switch the bar back to indeterminate. |

## Outputs

- System.Void

## Examples

### Example 1

```powershell
Update-FluenceProgress -Handle $progress -Message 'Installing components' -PercentComplete 40
```

### Example 2

```powershell
Update-FluenceProgress -Handle $progress -Detail 'Waiting for the service to start' -Indeterminate
```

## Notes

Runs on the UI thread that owns the window; blocks only for the duration of the update.

## See also

- [Reference index](README.md)
- [Result objects](result-objects.md)
- [Input types](input-types.md)
