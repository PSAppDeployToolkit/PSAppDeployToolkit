# Show-FluenceRestartPrompt

Shows a themed restart prompt with Restart now and Restart later buttons and an optional countdown.

## Syntax

```text
Show-FluenceRestartPrompt [[-Message] <string[]>] [-Title <string>] [-Countdown <int>] [-Icon <string>] [-NotTopmost] [-Theme <string>] [-Backdrop <string>] [<CommonParameters>]
Show-FluenceRestartPrompt [[-Message] <string[]>] -NoCountdown [-Title <string>] [-Icon <string>] [-NotTopmost] [-Theme <string>] [-Backdrop <string>] [<CommonParameters>]
```

## Description

A deployment-style prompt built on Show-FluenceDialog: a warning icon, a message, a Restart now default button and a Restart later cancel button. By default it counts down sixty seconds on the Restart now button and returns 'TimedOut' when nobody answers; -Countdown changes the seconds and -NoCountdown removes the timer. The window is topmost unless -NotTopmost is given. The prompt never restarts the machine itself: act on the returned value.

The shape mirrors the PSADT restart dialog so a deployment toolkit can map to it directly.

## Parameters

| Name | Type | Required | Default | Description |
| --- | --- | --- | --- | --- |
| `-Message` | String[] | No | @('The installation requires a restart to complete. Save your work before restarting.') | One or more message lines. Defaults to a generic "restart required" sentence. |
| `-Title` | String | No | Restart required | The window title. Defaults to 'Restart required'. |
| `-Countdown` | Int32 | No | 60 | Seconds until the prompt times out, shown on the Restart now button. Defaults to 60. |
| `-NoCountdown` | switch | Yes |  | Show the prompt without a timer; it stays open until the user answers. |
| `-Icon` | String | No | Warning | The severity icon beside the message. Warning (default), Info, Success, Error, Question, or None. Values: None, Info, Success, Warning, Error, Question. |
| `-NotTopmost` | switch | No |  | Do not keep the prompt above other windows. |
| `-Theme` | String | No |  | Auto, Light, Dark, or HighContrast. Omit it to keep the theme already applied to the process; the first Fluence call in a process applies Auto. Values: Auto, Light, Dark, HighContrast. |
| `-Backdrop` | String | No |  | Mica, Acrylic, Tabbed, None, or Auto. Omit it to keep the backdrop already applied to the process; the first Fluence call in a process applies Mica. Values: Mica, Acrylic, Tabbed, None, Auto. |

## Outputs

- System.String. 'Restart', 'Later', or 'TimedOut'.

## Examples

### Example 1

```powershell
switch (Show-FluenceRestartPrompt) {
    'Restart'  { Restart-Computer -Force }
    'TimedOut' { Restart-Computer -Force }
    'Later'    { Write-Output 'Deferred by the user.' }
}
```

### Example 2

```powershell
Show-FluenceRestartPrompt -Message 'Contoso Suite was installed.', 'Restart to finish.' -Countdown 300
```

## Notes

Uses the calling STA thread or a module-owned STA runspace. An existing host application is supported only when the command already runs on its dispatcher thread. Blocks until the prompt closes.

## See also

- [Reference index](README.md)
- [Result objects](result-objects.md)
- [Input types](input-types.md)
