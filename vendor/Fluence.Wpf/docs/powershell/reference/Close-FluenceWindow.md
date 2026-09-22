# Close-FluenceWindow

Closes a Fluence-hosted window on its UI thread, optionally stashing a result for the host.

## Syntax

```text
Close-FluenceWindow [-Window] <Window> [-Result <Object>] [<CommonParameters>]
```

## Description

Runs on the UI (STA) thread that owns the window. When -Result is supplied it is stored on the window's Tag so the window host can return it, then the window is closed.

## Parameters

| Name | Type | Required | Default | Description |
| --- | --- | --- | --- | --- |
| `-Window` | Window | Yes |  | The System.Windows.Window to close. |
| `-Result` | Object | No |  | An optional value to stash on the window's Tag before closing. |

## Examples

### Example 1

```powershell
Close-FluenceWindow -Window $window
```

### Example 2

```powershell
Close-FluenceWindow -Window $window -Result 'Saved'
```

## Notes

Stashes -Result on the window's Tag for the window host to return, then closes on the UI thread. A window implies a running Application, so no application is created here.

## See also

- [Reference index](README.md)
- [Result objects](result-objects.md)
- [Input types](input-types.md)
