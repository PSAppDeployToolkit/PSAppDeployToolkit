# Invoke-ServiceUI.ps1

## Description

This will launch the toolkit silently if the chosen process (explorer.exe by default) is not running. If it is running, then it will launch the toolkit interactively, and use ServiceUI to do so if the current process is non-interactive.

An alternate ProcessName can be specified if you only want the toolkit to be visible when a specific application is running.

Download MDT here: <https://www.microsoft.com/en-us/download/details.aspx?id=54259>

There are x86 and x64 builds of ServiceUI available in MDT under 'Microsoft Deployment Toolkit\Templates\Distribution\Tools'. Rename these to ServiceUI_x86.exe and ServiceUI_x64.exe and place them with this script in the root of the toolkit next to Deploy-Application.exe.

## Parameters

- ProcessName
  - Specifies the name of the process check for to trigger the interactive installation. Default value is 'explorer'. Multiple values can be supplied such as 'app1','app2'. The .exe extension must be omitted.
- DeploymentType
  - Specifies the type of deployment. Valid values are 'Install', 'Uninstall', or 'Repair'. Default value is 'Install'.
- AllowRebootPassThru
  - Passthru of switch to Deploy-Application.exe, will instruct the toolkit to not to mask a 3010 return code with a 0.
- TerminalServerMode
  - Passthru of switch to Deploy-Application.exe to enable terminal server mode.
- DisableLogging
  - Passthru of switch to Deploy-Application.exe to disable logging.

## Examples

Invoking the script from the command line:

```ps
.\Invoke-ServiceUI.ps1 -ProcessName 'WinSCP' -DeploymentType 'Install' -AllowRebootPassThru
```

An example command line to use in Intune:

```bat
%SystemRoot%\System32\WindowsPowerShell\v1.0\PowerShell.exe -ExecutionPolicy Bypass -NoProfile -File Invoke-ServiceUI.ps1 -DeploymentType Install -AllowRebootPassThru
```
