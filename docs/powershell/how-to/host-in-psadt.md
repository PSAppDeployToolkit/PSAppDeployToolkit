# How to host the module inside PSADT

This guide shows how to use the module from a PSAppDeployToolkit (PSADT) deployment script, or from a host that can execute a PowerShell runspace on its WPF application dispatcher: how to ship and import it, what the loader does with a library the host has already loaded, and which module cmdlet stands in for each toolkit dialog.

## Ship the module with the deployment

1. Stage the library into the module once, from the repository:

    ```powershell
    dotnet build Fluence.Wpf/Fluence.Wpf.csproj -c Release
    pwsh -NoProfile -File Fluence.Wpf.PowerShell.Module/build/Build-Module.ps1
    ```

    Or download the release zip, which already contains `lib/net472` and `lib/net8.0-windows10.0.26100.0`.

2. Copy the `Fluence.Wpf.PowerShell` folder (the one holding the `.psd1`) next to your deployment script, for example under `SupportFiles\`.

3. Import it by path at the top of the script, after the toolkit itself:

    ```powershell
    Import-Module "$PSScriptRoot\SupportFiles\Fluence.Wpf.PowerShell\Fluence.Wpf.PowerShell.psd1"
    ```

The module imports on Windows PowerShell 5.1 and PowerShell 7.4+ and picks the matching library build.

## Know which threading mode you are in

The module never starts a second WPF application. On every call it looks at the process:

| Situation | Mode | What happens |
| --- | --- | --- |
| The command runs on the existing WPF application dispatcher | `Inline` | The command reuses that application on its own thread. |
| The host application belongs to another foreign dispatcher | Unsupported | The command fails before dispatch; use the host's supported same-thread integration or a separate PowerShell process. |
| No application yet and the calling thread is STA (Windows PowerShell console, `pwsh` default) | `Inline` | The module creates the application on your thread; dialogs are modal to your script. |
| No application yet and the calling thread is MTA (`pwsh -MTA`, some service hosts) | `Runspace` | The module creates one STA runspace and shows everything there. |

`(Show-FluenceProgress -Message x).Mode` tells you which one applied. Importing this module does not by itself establish PSADT dispatcher integration. A deployment host must provide the supported same-thread PowerShell execution context; arbitrary cross-thread hosting is not supported.

## Handle a library the host has already loaded

The CLR loads one `Fluence.Wpf` assembly per process. If the toolkit has already loaded it, the module reuses that copy and compares versions:

```text
WARNING: The host has already loaded Fluence.Wpf 0.8.19.0 from 'C:\...\Fluence.Wpf.dll', which is older than the 0.9.0.0 build staged with this module. The module will use the host's copy; cmdlets that rely on newer library members may fail.
```

When you see this warning, either update the host's library or accept that a cmdlet may fail on a member the older build lacks. Type names that changed between library generations (`BackdropType` and `WindowBackdropType`, `CornerPreference` and `WindowCornerPreference`) are resolved at call time, so those renames alone do not break the module.

Run the import with `-Verbose` to see which assembly was reused or loaded.

## Map toolkit dialogs to module cmdlets

| You would call | Use instead | Notes |
| --- | --- | --- |
| `Show-ADTInstallationProgress`, `Close-ADTInstallationProgress` | `Show-FluenceProgress`, `Update-FluenceProgress`, `Close-FluenceProgress` | Title, message, detail, percent, topmost and position carry the same names. Close in `finally`. |
| `Show-ADTInstallationRestartPrompt` | `Show-FluenceRestartPrompt` | `-Countdown <seconds>` or `-NoCountdown`; returns `Restart`, `Later` or `TimedOut`. Restart the machine yourself. |
| `Show-ADTDialogBox` | `Show-FluenceMessage` | Presets `OK`, `OKCancel`, `YesNo`, `YesNoCancel`; `-DefaultButton`, `-Timeout`, `-Icon`. |
| `Show-ADTInstallationPrompt` | `Show-FluenceMessage` or `Show-FluenceDialog` | `-Image`, `-MessageAlignment`, `-Position`, `-Timeout -Countdown`, `-Topmost`, custom buttons through `New-FluenceButton`. |
| A list or choice dialog | `Show-FluenceListSelection`, or `New-FluencePrompt -InputType List` | `-MultiSelect` returns an array. |
| A text or credential prompt | `Get-FluenceInput`, `Show-FluenceDialog` with prompts | Validation keeps the dialog open until the input is acceptable. |
| `Show-ADTInstallationWelcome` (close running applications) | not provided | Needs process enumeration and a defer store that belong to the toolkit. |

## Run unattended

Deployments often run with no interactive desktop (session 0, or a device with nobody signed in). A dialog cannot be shown there, so guard the calls:

```powershell
if ([System.Environment]::UserInteractive)
{
    $answer = Show-FluenceMessage -Message 'Install now?' -Buttons YesNo -Timeout 60 -DefaultButton Yes
}
else
{
    $answer = 'Yes'
}
```

Give every dialog a `-Timeout` with a `-DefaultButton` (or read `TimedOut`) so an unattended run never blocks on a window nobody will click.

## Related

- [Explanation](../explanation.md) for why the module runs in-process and how the two supported modes differ
- [Show progress during long work](progress.md), [Show dialogs and messages](dialogs.md)
- [Reference](../reference/README.md) for every parameter

## End the host session deliberately

The host owns the lifetime of an existing WPF application and its dispatcher. Module removal closes module progress but preserves the application for later imports. An embedded script must not shut down its host's dispatcher.

Automatic terminal cleanup of a module-owned secondary UI runspace is limited to the primary, unpushed `ConsoleHost` session. It is not registered in child runspaces or custom hosts: closing one of those runspaces must not destroy the process's shared WPF application. See [the threading explanation](../explanation.md#where-the-ui-thread-comes-from) for the separate inline STA lifecycle requirement.
