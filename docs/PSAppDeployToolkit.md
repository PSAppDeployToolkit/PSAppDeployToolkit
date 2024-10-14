---
Module Name: PSAppDeployToolkit
Module Guid: d64dedeb-6c11-4251-911e-a62d7e031d0f
Download Help Link: NA
Help Version: 3.91.0
Locale: en-US
---

# PSAppDeployToolkit Module
## Description
Enterprise App Deployment, Simplified.

## PSAppDeployToolkit Cmdlets
### [Add-ADTEdgeExtension](Add-ADTEdgeExtension.md)
Adds an extension for Microsoft Edge using the ExtensionSettings policy.

### [Add-ADTSessionClosingCallback](Add-ADTSessionClosingCallback.md)
Adds a callback to be executed when the ADT session is closing.

### [Add-ADTSessionFinishingCallback](Add-ADTSessionFinishingCallback.md)
Adds a callback to be executed when the ADT session is finishing.

### [Add-ADTSessionOpeningCallback](Add-ADTSessionOpeningCallback.md)
Adds a callback to be executed when the ADT session is opening.

### [Add-ADTSessionStartingCallback](Add-ADTSessionStartingCallback.md)
Adds a callback to be executed when the ADT session is starting.

### [Block-ADTAppExecution](Block-ADTAppExecution.md)
Block the execution of an application(s).

### [Close-ADTInstallationProgress](Close-ADTInstallationProgress.md)
Closes the dialog created by Show-ADTInstallationProgress.

### [Close-ADTSession](Close-ADTSession.md)
Closes the active ADT session.

### [Complete-ADTFunction](Complete-ADTFunction.md)
Completes the execution of an ADT function.

### [Convert-ADTRegistryPath](Convert-ADTRegistryPath.md)
Converts the specified registry key path to a format that is compatible with built-in PowerShell cmdlets.

### [Convert-ADTValuesFromRemainingArguments](Convert-ADTValuesFromRemainingArguments.md)
Converts the collected values from a ValueFromRemainingArguments parameter value into a dictionary or PowerShell.exe command line arguments.

### [Copy-ADTContentToCache](Copy-ADTContentToCache.md)
Copies the toolkit content to a cache folder on the local machine and sets the $dirFiles and $supportFiles directory to the cache path.

### [Copy-ADTFile](Copy-ADTFile.md)
Copies files and directories from a source to a destination.

### [Copy-ADTFileToUserProfiles](Copy-ADTFileToUserProfiles.md)
Copy one or more items to each user profile on the system.

### [Disable-ADTTerminalServerInstallMode](Disable-ADTTerminalServerInstallMode.md)
Changes to user install mode for Remote Desktop Session Host/Citrix servers.

### [Dismount-ADTWimFile](Dismount-ADTWimFile.md)
Dismounts a WIM file from the specified mount point.

### [Enable-ADTTerminalServerInstallMode](Enable-ADTTerminalServerInstallMode.md)
Changes to user install mode for Remote Desktop Session Host/Citrix servers.

### [Get-ADTApplication](Get-ADTApplication.md)
Retrieves information about installed applications.

### [Get-ADTBoundParametersAndDefaultValues](Get-ADTBoundParametersAndDefaultValues.md)
Returns a hashtable with the output of $PSBoundParameters and default-valued parameters for the given InvocationInfo.

### [Get-ADTConfig](Get-ADTConfig.md)
Retrieves the configuration data for the ADT module.

### [Get-ADTEnvironment](Get-ADTEnvironment.md)
Retrieves the environment data for the ADT module.

### [Get-ADTFileVersion](Get-ADTFileVersion.md)
Gets the version of the specified file.

### [Get-ADTFreeDiskSpace](Get-ADTFreeDiskSpace.md)
Retrieves the free disk space in MB on a particular drive (defaults to system drive).

### [Get-ADTIniValue](Get-ADTIniValue.md)
Parses an INI file and returns the value of the specified section and key.

### [Get-ADTLoggedOnUser](Get-ADTLoggedOnUser.md)
Retrieves session details for all local and RDP logged on users.

### [Get-ADTMsiProductCodeRegexPattern](Get-ADTMsiProductCodeRegexPattern.md)
Returns a regex pattern to use for MSI ProductCode matching, or matching any UUID.

### [Get-ADTPendingReboot](Get-ADTPendingReboot.md)
Get the pending reboot status on a local computer.

### [Get-ADTPowerShellProcessPath](Get-ADTPowerShellProcessPath.md)
Retrieves the path to the PowerShell executable.

### [Get-ADTRedirectedUri](Get-ADTRedirectedUri.md)
Returns the resolved URI from the provided permalink.

### [Get-ADTRegistryKey](Get-ADTRegistryKey.md)
Retrieves value names and value data for a specified registry key or optionally, a specific value.

### [Get-ADTRunAsActiveUser](Get-ADTRunAsActiveUser.md)
Retrieves the active user session information.

### [Get-ADTSchedulerTask](Get-ADTSchedulerTask.md)
Retrieve all details for scheduled tasks on the local computer.

### [Get-ADTServiceStartMode](Get-ADTServiceStartMode.md)
Retrieves the startup mode of a specified service.

### [Get-ADTSession](Get-ADTSession.md)
Retrieves the most recent ADT session.

### [Get-ADTShortcut](Get-ADTShortcut.md)
Get information from a .lnk or .url type shortcut.

### [Get-ADTStringTable](Get-ADTStringTable.md)
Retrieves the string database from the ADT module.

### [Get-ADTUniversalDate](Get-ADTUniversalDate.md)
Returns the date/time for the local culture in a universal sortable date time pattern.

### [Get-ADTUriFileName](Get-ADTUriFileName.md)
Returns the filename of the provided URI.

### [Get-ADTUserProfiles](Get-ADTUserProfiles.md)
Get the User Profile Path, User Account SID, and the User Account Name for all users that log onto the machine and also the Default User.

### [Get-ADTWindowTitle](Get-ADTWindowTitle.md)
Search for an open window title and return details about the window.

### [Initialize-ADTFunction](Initialize-ADTFunction.md)
Initializes the ADT function environment.

### [Initialize-ADTModule](Initialize-ADTModule.md)
Initializes the ADT module by setting up necessary configurations and environment.

### [Install-ADTMSUpdates](Install-ADTMSUpdates.md)
Install all Microsoft Updates in a given directory.

### [Install-ADTSCCMSoftwareUpdates](Install-ADTSCCMSoftwareUpdates.md)
Scans for outstanding SCCM updates to be installed and installs the pending updates.

### [Invoke-ADTAllUsersRegistryAction](Invoke-ADTAllUsersRegistryAction.md)
Set current user registry settings for all current users and any new users in the future.

### [Invoke-ADTCommandWithRetries](Invoke-ADTCommandWithRetries.md)
Drop-in replacement for any cmdlet/function where a retry is desirable due to transient issues.

### [Invoke-ADTFunctionErrorHandler](Invoke-ADTFunctionErrorHandler.md)
Handles errors within ADT functions by logging and optionally passing through the error.

### [Invoke-ADTRegSvr32](Invoke-ADTRegSvr32.md)
Register or unregister a DLL file.

### [Invoke-ADTSCCMTask](Invoke-ADTSCCMTask.md)
Triggers SCCM to invoke the requested schedule task ID.

### [Invoke-ADTWebDownload](Invoke-ADTWebDownload.md)
Wraps around Invoke-WebRequest to provide logging and retry support.

### [Mount-ADTWimFile](Mount-ADTWimFile.md)
Mounts a WIM file to a specified directory.

### [New-ADTErrorRecord](New-ADTErrorRecord.md)
Creates a new ErrorRecord object.

### [New-ADTFolder](New-ADTFolder.md)
Create a new folder.

### [New-ADTMsiTransform](New-ADTMsiTransform.md)
Create a transform file for an MSI database.

### [New-ADTShortcut](New-ADTShortcut.md)
Creates a new .lnk or .url type shortcut.

### [New-ADTTemplate](New-ADTTemplate.md)
Creates a new folder containing a template front end and module folder, ready to customise.

### [New-ADTValidateScriptErrorRecord](New-ADTValidateScriptErrorRecord.md)
Creates a new ErrorRecord for script validation errors.

### [Open-ADTSession](Open-ADTSession.md)
Opens a new ADT session.

### [Out-ADTPowerShellEncodedCommand](Out-ADTPowerShellEncodedCommand.md)
Encodes a PowerShell command into a Base64 string.

### [Register-ADTDll](Register-ADTDll.md)
Register a DLL file.

### [Remove-ADTContentFromCache](Remove-ADTContentFromCache.md)
Removes the toolkit content from the cache folder on the local machine and reverts the $dirFiles and $supportFiles directory.

### [Remove-ADTEdgeExtension](Remove-ADTEdgeExtension.md)
Removes an extension for Microsoft Edge using the ExtensionSettings policy.

### [Remove-ADTFile](Remove-ADTFile.md)
Removes one or more items from a given path on the filesystem.

### [Remove-ADTFileFromUserProfiles](Remove-ADTFileFromUserProfiles.md)
Removes one or more items from each user profile on the system.

### [Remove-ADTFolder](Remove-ADTFolder.md)
Remove folder and files if they exist.

### [Remove-ADTInvalidFileNameChars](Remove-ADTInvalidFileNameChars.md)
Remove invalid characters from the supplied string.

### [Remove-ADTRegistryKey](Remove-ADTRegistryKey.md)
Deletes the specified registry key or value.

### [Remove-ADTSessionClosingCallback](Remove-ADTSessionClosingCallback.md)
Removes a callback function from the ADT session closing event.

### [Remove-ADTSessionFinishingCallback](Remove-ADTSessionFinishingCallback.md)
Removes a callback function from the ADT session finishing event.

### [Remove-ADTSessionOpeningCallback](Remove-ADTSessionOpeningCallback.md)
Removes a callback function from the ADT session opening event.

### [Remove-ADTSessionStartingCallback](Remove-ADTSessionStartingCallback.md)
Removes a callback function from the ADT session starting event.

### [Resolve-ADTErrorRecord](Resolve-ADTErrorRecord.md)
Enumerates error record details.

### [Send-ADTKeys](Send-ADTKeys.md)
Send a sequence of keys to one or more application windows.

### [Set-ADTActiveSetup](Set-ADTActiveSetup.md)
Creates an Active Setup entry in the registry to execute a file for each user upon login.

### [Set-ADTIniValue](Set-ADTIniValue.md)
Opens an INI file and sets the value of the specified section and key.

### [Set-ADTItemPermission](Set-ADTItemPermission.md)
Allows you to easily change permissions on files or folders.

### [Set-ADTPowerShellCulture](Set-ADTPowerShellCulture.md)
Changes the current thread's Culture and UICulture to the specified culture.

### [Set-ADTRegistryKey](Set-ADTRegistryKey.md)
Creates or sets a registry key name, value, and value data.

### [Set-ADTServiceStartMode](Set-ADTServiceStartMode.md)
Set the service startup mode.

### [Set-ADTShortcut](Set-ADTShortcut.md)
Modifies a .lnk or .url type shortcut.

### [Show-ADTBalloonTip](Show-ADTBalloonTip.md)
Displays a balloon tip notification in the system tray.

### [Show-ADTBlockedAppDialog](Show-ADTBlockedAppDialog.md)
Displays a dialog to inform the user about a blocked application.

### [Show-ADTDialogBox](Show-ADTDialogBox.md)
Display a custom dialog box with optional title, buttons, icon, and timeout.

### [Show-ADTHelpConsole](Show-ADTHelpConsole.md)
Displays a help console for the ADT module.

### [Show-ADTInstallationProgress](Show-ADTInstallationProgress.md)
Displays a progress dialog in a separate thread with an updateable custom message.

### [Show-ADTInstallationPrompt](Show-ADTInstallationPrompt.md)
Displays a custom installation prompt with the toolkit branding and optional buttons.

### [Show-ADTInstallationRestartPrompt](Show-ADTInstallationRestartPrompt.md)
Displays a restart prompt with a countdown to a forced restart.

### [Show-ADTInstallationWelcome](Show-ADTInstallationWelcome.md)
Show a welcome dialog prompting the user with information about the installation and actions to be performed before the installation can begin.

### [Start-ADTMsiProcess](Start-ADTMsiProcess.md)
Executes msiexec.exe to perform actions such as install, uninstall, patch, repair, or active setup for MSI and MSP files or MSI product codes.

### [Start-ADTMspProcess](Start-ADTMspProcess.md)
Executes an MSP file using the same logic as Start-ADTMsiProcess.

### [Start-ADTProcess](Start-ADTProcess.md)
Execute a process with optional arguments, working directory, window style.

### [Start-ADTProcessAsUser](Start-ADTProcessAsUser.md)
Invokes a process in another user's session.

### [Start-ADTServiceAndDependencies](Start-ADTServiceAndDependencies.md)
Start a Windows service and its dependencies.

### [Stop-ADTServiceAndDependencies](Stop-ADTServiceAndDependencies.md)
Stop a Windows service and its dependencies.

### [Test-ADTBattery](Test-ADTBattery.md)
Tests whether the local machine is running on AC power or not.

### [Test-ADTCallerIsAdmin](Test-ADTCallerIsAdmin.md)
Checks if the current user has administrative privileges.

### [Test-ADTModuleInitialized](Test-ADTModuleInitialized.md)
Checks if the ADT (PSAppDeployToolkit) module is initialized.

### [Test-ADTMSUpdates](Test-ADTMSUpdates.md)
Test whether a Microsoft Windows update is installed.

### [Test-ADTNetworkConnection](Test-ADTNetworkConnection.md)
Tests for an active local network connection, excluding wireless and virtual network adapters.

### [Test-ADTOobeCompleted](Test-ADTOobeCompleted.md)
Checks if the device's Out-of-Box Experience (OOBE) has completed or not.

### [Test-ADTPowerPoint](Test-ADTPowerPoint.md)
Tests whether PowerPoint is running in either fullscreen slideshow mode or presentation mode.

### [Test-ADTRegistryValue](Test-ADTRegistryValue.md)
Test if a registry value exists.

### [Test-ADTServiceExists](Test-ADTServiceExists.md)
Check to see if a service exists.

### [Test-ADTSessionActive](Test-ADTSessionActive.md)
Checks if there is an active ADT session.

### [Unblock-ADTAppExecution](Unblock-ADTAppExecution.md)
Unblocks the execution of applications performed by the Block-ADTAppExecution function.

### [Uninstall-ADTApplication](Uninstall-ADTApplication.md)
Removes all MSI applications matching the specified application name.

### [Unregister-ADTDll](Unregister-ADTDll.md)
Unregister a DLL file.

### [Update-ADTDesktop](Update-ADTDesktop.md)
Refresh the Windows Explorer Shell, which causes the desktop icons and the environment variables to be reloaded.

### [Update-ADTEnvironmentPsProvider](Update-ADTEnvironmentPsProvider.md)
Updates the environment variables for the current PowerShell session with any environment variable changes that may have occurred during script execution.

### [Update-ADTGroupPolicy](Update-ADTGroupPolicy.md)
Performs a gpupdate command to refresh Group Policies on the local machine.

### [Write-ADTLogEntry](Write-ADTLogEntry.md)
Write messages to a log file in CMTrace.exe compatible format or Legacy text file format.


