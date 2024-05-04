# Example - WinSCP

## Description

This is an example script to deploy WinSCP. You will need to add the rest of the toolkit files, as well as the latest WinSCP MSI in the Files folder.

This application requires registry keys to be set for every user to disable automatic updates. This is handled with the `Invoke-HKCURegistrySettingsForAllUsers` command.

## Pre-Installation

```ps
## Show Welcome Message, close WinSCP if required, allow up to 3 deferrals, and persist the prompt
Show-InstallationWelcome -CloseApps 'WinSCP=WinSCP' -AllowDeferCloseApps -DeferTimes 3 -PersistPrompt -MinimizeWindows $false
```

If WinSCP is running, the user will be prompted to either close the app or defer the installation.

## Installation

```ps
Execute-MSI -Action Install -Path 'WinSCP-6.3.2.msi'
```

Installs the MSI.

## Post-Installation

```ps
Remove-File -Path "$envCommonDesktop\WinSCP.lnk"

[scriptblock]$HKCURegistrySettings = {
    Set-RegistryKey -Key 'HKCU\Software\Martin Prikryl\WinSCP 2\Configuration\Interface' -Name 'CollectUsage' -Value 0 -Type DWord -SID $UserProfile.SID
    Set-RegistryKey -Key 'HKCU\Software\Martin Prikryl\WinSCP 2\Configuration\Interface\Updates' -Name 'Period' -Value 0 -Type DWord -SID $UserProfile.SID
    Set-RegistryKey -Key 'HKCU\Software\Martin Prikryl\WinSCP 2\Configuration\Interface\Updates' -Name 'BetaVersions' -Value 1 -Type DWord -SID $UserProfile.SID
    Set-RegistryKey -Key 'HKCU\Software\Martin Prikryl\WinSCP 2\Configuration\Interface\Updates' -Name 'ShowOnStartup' -Value 0 -Type DWord -SID $UserProfile.SID
}
Invoke-HKCURegistrySettingsForAllUsers -RegistrySettings $HKCURegistrySettings
```

This deletes the desktop shortcut, then applies some HKCU registry keys for every user.
