# ![PSAppDeployToolkit](https://github.com/user-attachments/assets/acfafa06-75ef-4988-aea6-5711fd9b6fc4)

![PowerShell Gallery](https://img.shields.io/powershellgallery/dt/psappdeploytoolkit?logoSize=auto&label=PowerShell%20Gallery)
![GitHub](https://img.shields.io/github/downloads/psappdeploytoolkit/psappdeploytoolkit/total?label=GitHub)
![Main Branch Status](https://img.shields.io/github/check-runs/psappdeploytoolkit/psappdeploytoolkit/main?label=main)
![Develop Branch Status](https://img.shields.io/github/check-runs/psappdeploytoolkit/psappdeploytoolkit/develop?label=develop)
![#psappdeploytoolkit Discord Chat](https://img.shields.io/discord/618712310185197588?label=Discord%20Chat)

## 🚀 Enterprise App Deployment, Simplified

PSAppDeployToolkit is a PowerShell-based, open-source framework for Windows software deployment that integrates seamlessly with existing deployment solutions (e.g. Microsoft Intune, SCCM, Tanium, BigFix etc.) to enhance the software deployment process. It achieves this by combining a battle-tested prescriptive workflow, an extensive library of functions for common deployment tasks, a customizable branded User Experience, and full-fidelity logging - to **produce consistently high deployment success rates of over 98%**.

### ✨ Key Features

- **Seamless Integration**: Works with all major deployment solutions
- **User Experience**: Beautiful, customizable UI with both Fluent and Classic interfaces
- **Flexible Deployment**: Handle complex deployment scenarios with ease
- **Reliable**: Battle-tested in enterprise environments
- **Extensible**: Rich library of functions for common deployment tasks

## 📸 Screenshots

| Light Mode | Dark Mode |
|---------------------|-----------------|
| ![LightMode](https://github.com/user-attachments/assets/d3ea4c5a-486a-48d9-86cf-c3ddf391468a) | ![DarkMode](https://github.com/user-attachments/assets/37cf1759-f211-4cf1-a686-7897a7306a27) |

| Custom Accent Light | Custom Accent Dark |
|---------------------|-----------------|
| ![CustomLightMode](https://github.com/user-attachments/assets/c092999f-46a2-43f6-bd28-bc2bdcd03b76) | ![CustomDarkMode](https://github.com/user-attachments/assets/26be16d2-f13e-491d-af86-72a169200f27) |

## 🖥️ What's New in v4.1 (Release Candidate) - 2025-07-08

**NOTE**: This is currently a release candidate for PSADT 4.1. which has not yet reached final status. While we are confident that it is rock solid, we are still testing it and may make changes before final release. As such, it is not recommended for production use at this time.

### 🎯 Major Improvements

- Up until now, it was not possible to display any user interface when deploying an application as SYSTEM using Intune (or any endpoint management tool) without using ServiceUI. Well, now it IS possible:
  - I REPEAT! **You no longer need to use ServiceUI**, EVER AGAIN! 🥳🎉🎊🪅🪩👯‍♂️
  - In fact, we strongly advise you stop using it as soon as possible. ServiceUI works by manipulating system security tokens in a way that could allow malicious actors to escalate privileges or bypass security controls.
  - We've taken a fresh approach which leverages the Windows security model and separates out user interactions onto a process running in the users' session - we never perform any user interaction or messaging of any kind within the SYSTEM context. This means a more secure and reliable deployment experience.
  - We have also removed the requirement for the 'Allow users to view and interact with the program installation' checkbox in Configuration Manager deployments.

- There is now **full feature parity** between the Fluent and Classic User Interfaces:
  - Deferral Deadline and Countdown Timer on Close Apps Dialog
  - Ability to prevent the Restart Dialog from being dismissed once a certain point in the countdown is reached
  - Ability to allow users to move dialogs
  - Ability to set the initial dialog placement to multiple locations
  - PowerShell ISE compatibility

- Furthermore, the Fluent UI has gained new features:
  - Due to the rearchitecture of how we handle user interaction with Dialogs, it is now possible to prompt the user for input using [Show-ADTInstallationPrompt](https://psappdeploytoolkit.com/docs/reference/functions/Show-ADTInstallationPrompt)'s -InputBox parameter
  - Support for formattable text (Bold, Italic & Accent) as well as URL hyperlinks in dialog messages
  - You can now set the % complete of the progress bar in the Progress Dialog (for example, if you are running a custom script that you want to show incremental progress changes for)
  - Ability to set different icons for Light / Dark mode

- The security rearchitecture required all of our process execution code to be rewritten. This has enabled us to provide a wealth of new capabilities to both [Start-ADTProcess](https://psappdeploytoolkit.com/docs/reference/functions/Start-ADTProcess) and [Start-ADTProcessAsUser](https://psappdeploytoolkit.com/docs/reference/functions/Start-ADTProcessAsUser) using the following new parameters:
  - -UseUnelevatedToken parameter to force a process run without elevation, for deploying user-context apps with Windows 11 Administrator Protection enabled
  - -WaitForChildProcesses parameter to wait for all child processes to end - useful for installers/uninstallers that hand off to another process and exit early
  - -KillChildProcessesWithParent parameter to close all started child processes once main process has ended - useful when installers start the application post-install, which is typically undesired when running as system
  - -Timeout parameter along with supporting -TimeoutAction and -NoTerminateOnTimeout parameters to control the outcome
  - -ExpandEnvironmentVariables parameter to allow variable expansion such as %AppData% when running a process as a user
  - -StreamEncoding parameter, useful for apps like Winget that write to the console using UTF8
  - -PassThru output now has a new 'interleaved' property that combines stdout/stderr in order
- It's now possible to set PSADT configuration settings via Group Policy using the included ADMX templates, which will override any settings in the config.psd1 file. This allows you to change, update or enforce settings across an organization.

### 🛠️ New and Enhanced Functions

- Added functions for managing user / machine environment variables:
  - [Get-ADTEnvironmentVariable](https://psappdeploytoolkit.com/docs/reference/functions/Get-ADTEnvironmentVariable) / [Set-ADTEnvironmentVariable](https://psappdeploytoolkit.com/docs/reference/functions/Set-ADTEnvironmentVariable) / [Remove-ADTEnvironmentVariable](https://psappdeploytoolkit.com/docs/reference/functions/Remove-ADTEnvironmentVariable)
- Added functions for managing INI file sections / values:
  - [Get-ADTIniSection](https://psappdeploytoolkit.com/docs/reference/functions/Get-ADTIniSection) / [Set-ADTIniSection](https://psappdeploytoolkit.com/docs/reference/functions/Set-ADTIniSection) / [Remove-ADTIniSection](https://psappdeploytoolkit.com/docs/reference/functions/Remove-ADTIniSection)
  - [Get-ADTIniValue](https://psappdeploytoolkit.com/docs/reference/functions/Get-ADTIniValue) / [Set-ADTIniValue](https://psappdeploytoolkit.com/docs/reference/functions/Set-ADTIniValue) / [Remove-ADTIniValue](https://psappdeploytoolkit.com/docs/reference/functions/Remove-ADTIniValue)
- Added [Start-ADTMsiProcessAsUser](https://psappdeploytoolkit.com/docs/reference/functions/Start-ADTMsiProcessAsUser) for installing / uninstalling user-context MSIs via the System account
- Added -DeferRunInterval switch to [Show-ADTInstallationWelcome](https://psappdeploytoolkit.com/docs/reference/functions/Show-ADTInstallationWelcome) to limit retry times from Intune
- Added -Path / -LiteralPath support to registry functions
- Added volatile key creation support to [Set-ADTRegistryKey](https://psappdeploytoolkit.com/docs/reference/functions/Set-ADTRegistryKey)
- Added MultiString add / remove support to [Set-ADTRegistryKey](https://psappdeploytoolkit.com/docs/reference/functions/Set-ADTRegistryKey)
- Added -MaximumElapsedTime parameter to [Invoke-ADTCommandWithRetries](https://psappdeploytoolkit.com/docs/reference/functions/Invoke-ADTCommandWithRetries)
- Added -SuccessExitCodes and -RebootExitCodes parameters to [Uninstall-ADTApplication](https://psappdeploytoolkit.com/docs/reference/functions/Uninstall-ADTApplication)

### 🛠️ Other Improvements

- [Show-ADTHelpConsole](https://psappdeploytoolkit.com/docs/reference/functions/Show-ADTHelpConsole) has been given some love and a facelift with High-DPI awareness, resizability, PowerShell 7 compatibility, and extension module display
- Added -NoWait support to [Show-ADTDialogBox](https://psappdeploytoolkit.com/docs/reference/functions/Show-ADTDialogBox)
- Added process detection code to enable automatic silent deployments when processes aren't running
- Added /Debug switch to [Invoke-AppDeployToolkit.exe](https://psappdeploytoolkit.com/docs/deployment-concepts/invoke-appdeploytoolkit) to show terminal output for debugging purposes
- Added /Core switch to [Invoke-AppDeployToolkit.exe](https://psappdeploytoolkit.com/docs/deployment-concepts/invoke-appdeploytoolkit) to allow PowerShell 7 usage

### 🛠️ Changes

- Changed default DeferExitCode from 60012 to 1602, since ConfigMgr and Intune recognize this natively as 'User cancelled the installation'
- Changed toolkit to exit with 3010 if a suppressed reboot was encountered without having to use -AllowRebootPassThru. To mask 3010 return codes and exit with 0, you can now add -SuppressRebootPassThru
- Changed default msiexec.exe parameters in interactive mode from /qb-! to /qn
- Changed UI functions to no longer minimize windows by default, -MinimizeWindows can be added to enable this
- Changed the 'Processes to close' in the Invoke-AppDeployToolkit template to the AppProcessesToClose ADTSession parameter, where they can be re-used over Install / Uninstall / Repair
- Changed installation failure to be silent as it was in v3.x; however, you can still uncomment a line to get the full detailed stack trace as used in v4.0.x, or a new minimal example using the Fluent UI

### 🛠️ Fixes

- Fixed [Start-ADTProcessAsUser](https://psappdeploytoolkit.com/docs/reference/functions/Start-ADTProcessAsUser) function to work as expected
- Fixed [Block-ADTAppExecution](https://psappdeploytoolkit.com/docs/reference/functions/Block-ADTAppExecution) to avoid triggering AV solutions
- Fixed dialogs to show correct deployment type Install / Uninstall / Repair
- Fixed SCCM pending reboot tests within [Get-ADTPendingReboot](https://psappdeploytoolkit.com/docs/reference/functions/Get-ADTPendingReboot)
- Fixed MSI repair to default to 'Reinstall' to avoid forced unavoidable reboots when running msiexec /f against an app that is in-use
- Fixed OOBE detection code to factor in User ESP phase

## 🚀 Getting Started

### Prerequisites

- Windows 10/11
- PowerShell 5.1 or later
- .NET Framework 4.7.2 or later

### Downloading

- [Getting Started Guidance](https://psappdeploytoolkit.com/docs/getting-started/download)
- [PowerShell Gallery](https://www.powershellgallery.com/packages/PSAppDeployToolkit)
- [GitHub Releases](https://github.com/psappdeploytoolkit/psappdeploytoolkit/releases)

## 📚 Documentation

For detailed documentation, examples, and advanced usage, visit our [official documentation](https://psappdeploytoolkit.com/docs/introduction)

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guide](https://github.com/PSAppDeployToolkit/PSAppDeployToolkit/blob/main/.github/CONTRIBUTING.md) for details

## 📄 License

This project is licensed under the [GNU Lesser General Public License](https://github.com/PSAppDeployToolkit/PSAppDeployToolkit/blob/main/COPYING.Lesser)

## Important Links

### PSAppDeployToolkit

- [Homepage](https://psappdeploytoolkit.com)
- [Latest News](https://psappdeploytoolkit.com/blog)
- [Documentation](https://psappdeploytoolkit.com/docs/introduction)
- [Function & Variable References](https://psappdeploytoolkit.com/docs/reference)
- [PowerShell Gallery](https://www.powershellgallery.com/packages/PSAppDeployToolkit)
- [GitHub Releases](https://github.com/PSAppDeployToolkit/PSAppDeployToolkit/releases)

### Community

- [Discourse Forum](https://discourse.psappdeploytoolkit.com/)
- [Discord Chat](https://discord.com/channels/618712310185197588/627204361545842688)
- [Reddit](https://reddit.com/r/psadt)

### GitHub

- [Issues](https://github.com/PSAppDeployToolkit/PSAppDeployToolkit/issues)
- [Security Policy](https://github.com/PSAppDeployToolkit/PSAppDeployToolkit/security)
- [Contributer Guidelines](https://github.com/PSAppDeployToolkit/PSAppDeployToolkit/blob/main/.github/CONTRIBUTING.md)
