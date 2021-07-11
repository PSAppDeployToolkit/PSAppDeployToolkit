#
# Module manifest for module 'PSAppDeployToolkit'
#
# Generated on: 10.07.2021
#

@{

# Script module or binary module file associated with this manifest.
# RootModule = ''

# Version number of this module.
ModuleVersion = '0.0.1'

# Supported PSEditions
# CompatiblePSEditions = @()

# ID used to uniquely identify this module
GUID = '23c5ec14-5d7b-4975-875e-e54b9e00966a'

# Author of this module
Author = ''

# Company or vendor of this module
CompanyName = ''

# Copyright statement for this module
Copyright = ''

# Description of the functionality provided by this module
# Description = ''

# Minimum version of the PowerShell engine required by this module
# PowerShellVersion = ''

# Name of the PowerShell host required by this module
# PowerShellHostName = ''

# Minimum version of the PowerShell host required by this module
# PowerShellHostVersion = ''

# Minimum version of Microsoft .NET Framework required by this module. This prerequisite is valid for the PowerShell Desktop edition only.
# DotNetFrameworkVersion = ''

# Minimum version of the common language runtime (CLR) required by this module. This prerequisite is valid for the PowerShell Desktop edition only.
# ClrVersion = ''

# Processor architecture (None, X86, Amd64) required by this module
# ProcessorArchitecture = ''

# Modules that must be imported into the global environment prior to importing this module
# RequiredModules = @()

# Assemblies that must be loaded prior to importing this module
# RequiredAssemblies = @()

# Script files (.ps1) that are run in the caller's environment prior to importing this module.
ScriptsToProcess = @(
    ".\Scripts\Main\AppDeployToolkitMain.ps1"
)

# Type files (.ps1xml) to be loaded when importing this module
# TypesToProcess = @()

# Format files (.ps1xml) to be loaded when importing this module
# FormatsToProcess = @()

# Modules to import as nested modules of the module specified in RootModule/ModuleToProcess
# NestedModules = @()

# Functions to export from this module, for best performance, do not use wildcards and do not delete the entry, use an empty array if there are no functions to export.
FunctionsToExport = @(
    ".\Functions\Main\Block-AppExecution.ps1",
    ".\Functions\Main\Close-InstallationProgress.ps1",
    ".\Functions\Main\Convert-RegistryPath.ps1",
    ".\Functions\Main\ConvertTo-NTAccountOrSID.ps1",
    ".\Functions\Main\Copy-File.ps1",
    ".\Functions\Main\Disable-TerminalServerInstallMode.ps1",
    ".\Functions\Main\Enable-TerminalServerInstallMode.ps1",
    ".\Functions\Main\Execute-MSI.ps1",
    ".\Functions\Main\Execute-MSP.ps1",
    ".\Functions\Main\Execute-Process.ps1",
    ".\Functions\Main\Execute-ProcessAsUser.ps1",
    ".\Functions\Main\Exit-Script.ps1",
    ".\Functions\Main\Get-DeferHistory.ps1",
    ".\Functions\Main\Get-FileVersion.ps1",
    ".\Functions\Main\Get-FreeDiskSpace.ps1",
    ".\Functions\Main\Get-HardwarePlatform.ps1",
    ".\Functions\Main\Get-IniValue.ps1",
    ".\Functions\Main\Get-InstalledApplication.ps1",
    ".\Functions\Main\Get-LoggedOnUser.ps1",
    ".\Functions\Main\Get-MsiExitCodeMessage.ps1",
    ".\Functions\Main\Get-MsiTableProperty.ps1",
    ".\Functions\Main\Get-ObjectProperty.ps1",
    ".\Functions\Main\Get-PEFileArchitecture.ps1",
    ".\Functions\Main\Get-PendingReboot.ps1",
    ".\Functions\Main\Get-RegistryKey.ps1",
    ".\Functions\Main\Get-RunningProcesses.ps1",
    ".\Functions\Main\Get-SchedulerTask.ps1",
    ".\Functions\Main\Get-ServiceStartMode.ps1",
    ".\Functions\Main\Get-Shortcut.ps1",
    ".\Functions\Main\Get-UniversalDate.ps1",
    ".\Functions\Main\Get-UserProfiles.ps1",
    ".\Functions\Main\Get-WindowTitle.ps1",
    ".\Functions\Main\Install-MSUpdates.ps1",
    ".\Functions\Main\Install-SCCMSoftwareUpdates.ps1",
    ".\Functions\Main\Invoke-HKCURegistrySettingsForAllUsers.ps1",
    ".\Functions\Main\Invoke-ObjectMethod.ps1",
    ".\Functions\Main\Invoke-RegisterOrUnregisterDLL.ps1",
    ".\Functions\Main\Invoke-SCCMTask.ps1",
    ".\Functions\Main\New-Folder.ps1",
    ".\Functions\Main\New-MsiTransform.ps1",
    ".\Functions\Main\New-Shortcut.ps1",
    ".\Functions\Main\New-ZipFile.ps1",
    ".\Functions\Main\Remove-File.ps1",
    ".\Functions\Main\Remove-Folder.ps1",
    ".\Functions\Main\Remove-InvalidFileNameChars.ps1",
    ".\Functions\Main\Remove-MSIApplications.ps1",
    ".\Functions\Main\Remove-RegistryKey.ps1",
    ".\Functions\Main\Resove-Error.ps1",
    ".\Functions\Main\Send-Keys.ps1",
    ".\Functions\Main\Set-ActiveSetup.ps1",
    ".\Functions\Main\Set-DeferHistory.ps1",
    ".\Functions\Main\Set-IniValue.ps1",
    ".\Functions\Main\Set-ItemPermission.ps1",
    ".\Functions\Main\Set-MsiProperty.ps1",
    ".\Functions\Main\Set-PinnedApplication.ps1",
    ".\Functions\Main\Set-RegistryKey.ps1",
    ".\Functions\Main\Set-ServiceStartMode.ps1",
    ".\Functions\Main\Set-Shortcut.ps1",
    ".\Functions\Main\Show-BalloonTip.ps1",
    ".\Functions\Main\Show-DialogBox.ps1",
    ".\Functions\Main\Show-InstallationProgress.ps1",
    ".\Functions\Main\Show-InstallationPrompt.ps1",
    ".\Functions\Main\Show-InstallationRestartPrompt.ps1",
    ".\Functions\Main\Show-InstallationWelcome.ps1",
    ".\Functions\Main\Show-WelcomePrompt.ps1",
    ".\Functions\Main\Start-ServiceAndDependencies.ps1",
    ".\Functions\Main\Stop-ServiceAndDependencies.ps1",
    ".\Functions\Main\Test-Battery.ps1",
    ".\Functions\Main\Test-IsMutexAvailable.ps1",
    ".\Functions\Main\Test-MSUpdates.ps1",
    ".\Functions\Main\Test-NetworkConnection.ps1",
    ".\Functions\Main\Test-PowerPoint.ps1",
    ".\Functions\Main\Test-RegistryValue.ps1",
    ".\Functions\Main\Test-ServiceExists.ps1",
    ".\Functions\Main\Unblock-AppExecution.ps1",
    ".\Functions\Main\Update-Desktop.ps1",
    ".\Functions\Main\Update-GroupPolicy.ps1",
    ".\Functions\Main\Update-SessionEnvironmentVariables.ps1",
    ".\Functions\Main\Write-FunctionInfo.ps1",
    ".\Functions\Main\Write-Log.ps1"
)

# Cmdlets to export from this module, for best performance, do not use wildcards and do not delete the entry, use an empty array if there are no cmdlets to export.
CmdletsToExport = @()

# Variables to export from this module
VariablesToExport = '*'

# Aliases to export from this module, for best performance, do not use wildcards and do not delete the entry, use an empty array if there are no aliases to export.
AliasesToExport = @()

# DSC resources to export from this module
# DscResourcesToExport = @()

# List of all modules packaged with this module
# ModuleList = @()

# List of all files packaged with this module
# FileList = @()

# Private data to pass to the module specified in RootModule/ModuleToProcess. This may also contain a PSData hashtable with additional module metadata used by PowerShell.
PrivateData = @{

    PSData = @{

        # Tags applied to this module. These help with module discovery in online galleries.
        # Tags = @()

        # A URL to the license for this module.
        # LicenseUri = ''

        # A URL to the main website for this project.
        # ProjectUri = ''

        # A URL to an icon representing this module.
        # IconUri = ''

        # ReleaseNotes of this module
        # ReleaseNotes = ''

        # Prerelease string of this module
        # Prerelease = ''

        # Flag to indicate whether the module requires explicit user acceptance for install/update/save
        # RequireLicenseAcceptance = $false

        # External dependent modules of this module
        # ExternalModuleDependencies = @()

    } # End of PSData hashtable

} # End of PrivateData hashtable

# HelpInfo URI of this module
# HelpInfoURI = ''

# Default prefix for commands exported from this module. Override the default prefix using Import-Module -Prefix.
# DefaultCommandPrefix = ''

}

