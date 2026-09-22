# Show-FluenceProgress

Shows a non-modal themed progress window and returns a handle for updating and closing it.

## Syntax

```text
Show-FluenceProgress [-Message] <string> [-Title <string>] [-Detail <string>] [-PercentComplete <double>] [-NotTopmost] [-Position <string>] [-Width <int>] [-Theme <string>] [-Backdrop <string>] [-Accent <Color>] [<CommonParameters>]
```

## Description

Opens a fixed-width FluenceWindow with a message line, an optional detail line, and a Fluence ProgressBar, indeterminate by default. The call returns as soon as the window is shown; the script keeps running. Change the text or percentage with Update-FluenceProgress and close the window with Close-FluenceProgress. The user cannot close it: the caption buttons are hidden.

On the inline STA host (Windows PowerShell and pwsh by default) the window shares the caller's thread, so it repaints on every Show-FluenceProgress and Update-FluenceProgress call and stays static in between; update it at least every few seconds during long work. On an MTA host (pwsh -mta) the window lives on the module-owned UI runspace, stays responsive between updates, and other Fluence dialogs can still be shown while it is open. An existing host application is supported only when this command runs on its dispatcher thread; automatic dispatch to a foreign UI thread is not supported.

Parameter names mirror the PSADT installation progress dialog: title, message, detail, percent, topmost and position.

## Parameters

| Name | Type | Required | Default | Description |
| --- | --- | --- | --- | --- |
| `-Message` | String | Yes |  | The main status line. |
| `-Title` | String | No | Fluence | The window title. Defaults to 'Fluence'. |
| `-Detail` | String | No |  | An optional second line in the secondary text color, for example the current file or step. |
| `-PercentComplete` | Double | No |  | When given, the bar is determinate at this value (clamped to 0..100). Omit for an indeterminate bar. |
| `-NotTopmost` | switch | No |  | Do not keep the window above other windows. Topmost is the default, as for a deployment progress dialog. |
| `-Position` | String | No | Center | Center (default), TopRight, or BottomRight of the primary work area. Values: Center, TopRight, BottomRight. |
| `-Width` | Int32 | No | 450 | The window width in device-independent pixels. Defaults to 450. |
| `-Theme` | String | No |  | Auto, Light, Dark, or HighContrast. Omit it to keep the theme already applied to the process; the first Fluence call in a process applies Auto. Values: Auto, Light, Dark, HighContrast. |
| `-Backdrop` | String | No |  | Mica, Acrylic, Tabbed, None, or Auto. Omit it to keep the backdrop already applied to the process; the first Fluence call in a process applies Mica. Values: Mica, Acrylic, Tabbed, None, Auto. |
| `-Accent` | Color | No |  | Optional accent color (System.Windows.Media.Color or a parseable string). Defaults to system accent. |

## Outputs

- Fluence.ProgressHandle

## Examples

### Example 1

```powershell
$progress = Show-FluenceProgress -Title 'Contoso Suite' -Message 'Installing...' -Detail 'Copying files'
Update-FluenceProgress -Handle $progress -Detail 'Registering components' -PercentComplete 60
Close-FluenceProgress -Handle $progress
```

## Notes

Uses the calling STA thread or a module-owned STA runspace. An existing host application is supported only when the command already runs on its dispatcher thread. Only one progress window can be open at a time.

## See also

- [Reference index](README.md)
- [Result objects](result-objects.md)
- [Input types](input-types.md)
