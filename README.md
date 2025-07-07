# ![PSAppDeployToolkit](https://github.com/user-attachments/assets/acfafa06-75ef-4988-aea6-5711fd9b6fc4)

![PowerShell Gallery](https://img.shields.io/powershellgallery/dt/psappdeploytoolkit?logoSize=auto&label=PowerShell%20Gallery)
![GitHub](https://img.shields.io/github/downloads/psappdeploytoolkit/psappdeploytoolkit/total?label=GitHub)
![Main Branch Status](https://img.shields.io/github/check-runs/psappdeploytoolkit/psappdeploytoolkit/main?label=main)
![Develop Branch Status](https://img.shields.io/github/check-runs/psappdeploytoolkit/psappdeploytoolkit/develop?label=develop)
![#psappdeploytoolkit Discord Chat](https://img.shields.io/discord/618712310185197588?label=Discord%20Chat)

PSAppDeployToolkit is a framework for deploying applications in a business / corporate environment. It provides a set of well-defined functions for common application deployment tasks, as well as user interface elements for end user interaction during a deployment. It simplifies the complex scripting challenges of deploying applications in the enterprise, provides a consistent deployment experience for your end users and as a result of this, improves the overall success rate of your deployments.

### Features

PSAppDeployToolkit allows you to encapsulate a typical Windows Installer MSI or Setup executable to provide it with enhanced capabilities.

- Validate prerequisites such as dependencies on minimum software versions
- Ensure that in-use applications are closed and prevent reopening during the deployment
- Check with the user if now is a good time to start an install and allow them to defer
- Uninstall existing applications and perform clean up operations
- Capture any important settings that may be required for an upgrade or migration
- Run the installation silently and capture logs in the event of an issue
- Run post-installation configuration tasks to customize for your environment
- Prompt the user to restart their computer if required, immediately, on a timer and with a deadline

## Screenshots

<img width="45%" alt="psadt-41-1" src="https://github.com/user-attachments/assets/d3ea4c5a-486a-48d9-86cf-c3ddf391468a" />
<img width="45%" alt="psadt-41-2" src="https://github.com/user-attachments/assets/37cf1759-f211-4cf1-a686-7897a7306a27" />
<img width="45%" alt="psadt-41-3" src="https://github.com/user-attachments/assets/c092999f-46a2-43f6-bd28-bc2bdcd03b76" />
<img width="45%" alt="psadt-41-4" src="https://github.com/user-attachments/assets/26be16d2-f13e-491d-af86-72a169200f27" />
<img width="45%" alt="psadt-41-5" src="https://github.com/user-attachments/assets/1f384898-20db-4f53-adf1-21f5e43552fa" />

## What's New in v4.1

### Highlights

- **Removed** ServiceUI requirement for Intune deployments and 'Allow users to view and interact with the program installation' checkbox for Configuration Manager deployments.

  - I REPEAT! You no longer need to use ServiceUI, EVER AGAIN! 🥳🎉🎊🪅🪩👯‍♂️
  - In fact, we strongly advise you stop using it as soon as possible. ServiceUI works by manipulating system security tokens in a way that could allow malicious actors to escalate privileges or bypass security controls.
  - We've taken a fresh approach which leverages the Windows security model and separates out user interactions onto a process running in the users' session - we never perform any user interaction or messaging of any kind within the SYSTEM context. This means a more secure and reliable deployment experience.
  - We have also removed the requirement for the 'Allow users to view and interact with the program installation' checkbox in Configuration Manager deployments.

- **Added** feature parity between Fluent UI and Classic UI:
  - Deferral Deadline and Countdown Timer on Close Apps Dialog
  - Support for moveable dialogs, and multiple dialog placement options
- **Enhanced** Fluent UI with:
  - Support for formattable text (Bold, Italic & Accent) as well as URL hyperlinks in dialog messages
  - Progress dialog now supports setting progress bar values
  - User text input prompts via Show-ADTInstallationPrompt's -InputBox parameter
- **Enhanced** Start-ADTProcess / Start-ADTProcessAsUser with multiple new parameters:
  - -UseUnelevatedToken parameter to force a process run without elevation, for deploying user-context apps with Windows 11 Administrator Protection enabled
  - -WaitForChildProcesses parameter to wait for all child processes to end - useful for installers/uninstallers that hand off to another process and exit early
  - -KillChildProcessesWithParent parameter to close all started child processes once main process has ended - useful when installers start the application post-install, which is typically undesired when running as system
  - -Timeout parameter along with supporting -TimeoutAction and -NoTerminateOnTimeout parameters to control the outcome
  - -ExpandEnvironmentVariables parameter to allow variable expansion such as %AppData% when running a process as a user
  - -StreamEncoding parameter, useful for apps like Winget that write to the console using UTF8
  - -PassThru output now has a new 'interleaved' property that combines stdout/stderr in order
- **Improved** Show-ADTHelpConsole with High-DPI awareness, resizability, PowerShell 7 compatibility, and extension module display
- **Added** ADMX templates for policy-based management

### Other User Interface Improvements

- **Added** Fluent UI support for different icons in light/dark modes
- **Added** Fluent UI support for moveable dialogs and multiple dialog placement options
- **Added** -NoWait support to Show-ADTDialogBox
- **Added** PowerShell ISE compatibility to Fluent UI
- **Added** process detection code to enable automatic silent deployments when processes aren't running

### Added

#### New Functions

- **Added** functions for managing user/machine environment variables
- **Added** functions for managing INI sections/values
- **Enhanced** Start-ADTProcess / Start-ADTProcessAsUser with multiple new parameters:
  - -UseUnelevatedToken parameter to force a process run without elevation, for deploying user-context apps with Windows 11 Administrator Protection enabled
  - -WaitForChildProcesses parameter to wait for all child processes to end - useful for installers/uninstallers that hand off to another process and exit early
  - -KillChildProcessesWithParent parameter to close all started child processes once main process has ended - useful when installers start the application post-install, which is typically undesired when running as system
  - -Timeout parameter along with supporting -TimeoutAction and -NoTerminateOnTimeout parameters to control the outcome
  - -ExpandEnvironmentVariables parameter to allow variable expansion such as %AppData% when running a process as a user
  - -StreamEncoding parameter, useful for apps like Winget that write to the console using UTF8
  - -PassThru output now has a new 'interleaved' property that combines stdout/stderr in order
- **Added** -DeferRunInterval switch to Show-ADTInstallationWelcome to limit retry times from Intune
- **Added** -Path / -LiteralPath support to registry functions
- **Added** volatile key creation support to Set-ADTRegistryKey
- **Added** MultiString add / remove support to Set-ADTRegistryKey
- **Added** -MaximumElapsedTime parameter to Invoke-ADTCommandWithRetries
- **Added** -SuccessExitCodes and -RebootExitCodes parameters to Uninstall-ADTApplication

#### Other Improvements

- **Added** /Debug switch to Invoke-AppDeployToolkit.exe to show terminal output for debugging purposes
- **Added** /Core switch to Invoke-AppDeployToolkit.exe to allow PowerShell 7 usage

### Fixed

- **Fixed** Start-ADTProcessAsUser function to work as expected
- **Fixed** Block-ADTAppExecution to avoid triggering AV solutions
- **Fixed** dialogs to show correct deployment type Install / Uninstall / Repair
- **Fixed** SCCM pending reboot tests within Get-ADTPendingReboot
- **Fixed** MSI repair to default to 'Reinstall' to avoid forced unavoidable reboots when running msiexec /f against an app that is in-use
- **Fixed** OOBE detection code to factor in User ESP phase

### Changed

- **Changed** default DeferExitCode from 60012 to 1602, since ConfigMgr and Intune recognize this natively as 'User cancelled the installation'
- **Changed** toolkit to exit with 3010 if a suppressed reboot was encountered without having to use -AllowRebootPassThru. To mask 3010 return codes and exit with 0, you can now add -SuppressRebootPassThru
- **Changed** default msiexec.exe parameters in interactive mode from /qb-! to /qn
- **Changed** UI functions to no longer minimize windows by default, -MinimizeWindows can be added to enable this
- **Changed** template: Processes to close moved to ADTSession parameters, where they can be re-used over Install / Uninstall / Repair
- **Changed** installation failure to be silent as it was in v3.x; however, you can still uncomment a line to get the full detailed stack trace as used in v4.0.x, or a new minimal example using the Fluent UI

## Getting Started / Downloading

- [Getting Started Guidance](https://psappdeploytoolkit.com/docs/getting-started/download)
- [PowerShell Gallery](https://www.powershellgallery.com/packages/PSAppDeployToolkit)
- [GitHub Latest Release](https://github.com/psappdeploytoolkit/psappdeploytoolkit/releases)

## Important Links

### PSAppDeployToolkit

- [Homepage](https://psappdeploytoolkit.com)
- [Latest News](https://psappdeploytoolkit.com/blog)
- [Documentation](https://psappdeploytoolkit.com/docs)
- [Function & Variable References](https://psappdeploytoolkit.com/docs/reference)
- [GitHub Latest Release](https://github.com/PSAppDeployToolkit/PSAppDeployToolkit/releases)

### Community

- [Discourse Forum](https://discourse.psappdeploytoolkit.com/)
- [Discord Chat](https://discord.com/channels/618712310185197588/627204361545842688)
- [Reddit](https://reddit.com/r/psadt)

### GitHub

- [Issues](https://github.com/PSAppDeployToolkit/PSAppDeployToolkit/issues)
- [Security Policy](https://github.com/PSAppDeployToolkit/PSAppDeployToolkit/security)
- [Contributer Guidelines](https://github.com/PSAppDeployToolkit/PSAppDeployToolkit/blob/main/.github/CONTRIBUTING.md)

## License

The PowerShell App Deployment Tool is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the Free Software Foundation, either version 3 of the License, or any later version.

This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Lesser General Public License for more details.
