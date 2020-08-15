The toolkit has a number of internal exit codes for any issues that may occur.

**60000 - 68999** Reserved for built-in exit codes in Deploy-Application.ps1, Deploy-Application.exe, and AppDeployToolkitMain.ps1

**69000 - 69999** Recommended for user customized exit codes in Deploy-Application.ps1

**70000 - 79999** Recommended for user customized exit codes in AppDeployToolkitExtensions.ps1

**60001** An error occurred in Deploy-Application.ps1. Check your script syntax use.

**60002** Error when running Execute-Process function

**60003** Administrator privileges required for Execute-ProcessAsUser function

**60004** Failure when loading .NET Winforms / WPF Assemblies

**60005** Failure when displaying the Blocked Application dialog

**60006** AllowSystemInteractionFallback option was not selected in the config XML file, so toolkit will not fall back to SYSTEM context with no interaction.

**60007** Failed to export the schedule task XML file in Execute-ProcessAsUser function

**60008** Deploy-Application.ps1 failed to dot source AppDeployToolkitMain.ps1 either because it could not be found or there was an error while it was being dot sourced.

**60009** The -UserName parameter in the Execute-ProcessAsUser function has a default value that is empty because no logged in users were detected when the toolkit was launched.

**60010** Deploy-Application.exe failed before PowerShell.exe process could be launched.

**60011** Deploy-Application.exe failed to execute the PowerShell.exe process.

**60012** A UI prompt timed out or the user opted to defer the installation.

**60013** If Execute-Process function captures an exit code out of range for int32 then return this custom exit code.