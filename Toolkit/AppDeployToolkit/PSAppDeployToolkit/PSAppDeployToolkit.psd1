#
# Module manifest for module 'PSAppDeployToolkit'
#
# Generated on: 2024-04-13
#

@{

# Script module or binary module file associated with this manifest.
RootModule = 'PSAppDeployToolkit.psm1'

# Version number of this module.
ModuleVersion = '3.91.0'

# Supported PSEditions
# CompatiblePSEditions = @()

# ID used to uniquely identify this module
GUID = 'd64dedeb-6c11-4251-911e-a62d7e031d0f'

# Author of this module
Author = 'PSAppDeployToolkit Team'

# Company or vendor of this module
CompanyName = 'PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham and Muhammad Mashwani)'

# Copyright statement for this module
Copyright = 'Copyright © 2024 PSAppDeployToolkit Team. All rights reserved.'

# Description of the functionality provided by this module
Description = 'Enterprise App Deployment, Simplified.'

# Minimum version of the Windows PowerShell engine required by this module
PowerShellVersion = '5.1'

# Name of the Windows PowerShell host required by this module
# PowerShellHostName = ''

# Minimum version of the Windows PowerShell host required by this module
# PowerShellHostVersion = ''

# Minimum version of Microsoft .NET Framework required by this module. This prerequisite is valid for the PowerShell Desktop edition only.
# DotNetFrameworkVersion = ''

# Minimum version of the common language runtime (CLR) required by this module. This prerequisite is valid for the PowerShell Desktop edition only.
# CLRVersion = ''

# Processor architecture (None, X86, Amd64) required by this module
# ProcessorArchitecture = ''

# Modules that must be imported into the global environment prior to importing this module
RequiredModules = @(
    @{ ModuleName = 'CimCmdlets'; Guid = 'fb6cc51d-c096-4b38-b78d-0fed6277096a'; ModuleVersion = '1.0.0.0' }
    @{ ModuleName = 'Microsoft.PowerShell.Archive'; Guid = 'eb74e8da-9ae2-482a-a648-e96550fb8733'; ModuleVersion = '1.0.1.0' }
    @{ ModuleName = 'Microsoft.PowerShell.Management'; Guid = 'eefcb906-b326-4e99-9f54-8b4bb6ef3c6d'; ModuleVersion = '3.1.0.0' }
    @{ ModuleName = 'Microsoft.PowerShell.Security'; Guid = 'a94c8c7e-9810-47c0-b8af-65089c13a35a'; ModuleVersion = '3.0.0.0' }
    @{ ModuleName = 'Microsoft.PowerShell.Utility'; Guid = '1da87e53-152b-403e-98dc-74d7b4d63d59'; ModuleVersion = '3.1.0.0' }
    @{ ModuleName = 'NetAdapter'; Guid = '1042b422-63a8-4016-a6d6-293e19e8f8a6'; ModuleVersion = '2.0.0.0' }
    @{ ModuleName = 'ScheduledTasks'; Guid = '5378ee8e-e349-49bb-83b9-f3d9c396c0a6'; ModuleVersion = '1.0.0.0' }
)

# Assemblies that must be loaded prior to importing this module
# RequiredAssemblies = @()

# Script files (.ps1) that are run in the caller's environment prior to importing this module.
# ScriptsToProcess = @()

# Type files (.ps1xml) to be loaded when importing this module
# TypesToProcess = @()

# Format files (.ps1xml) to be loaded when importing this module
# FormatsToProcess = @()

# Modules to import as nested modules of the module specified in RootModule/ModuleToProcess
# NestedModules = @()

# Functions to export from this module, for best performance, do not use wildcards and do not delete the entry, use an empty array if there are no functions to export.
FunctionsToExport = @(
    'Add-ADTEdgeExtension'
    'Add-ADTSessionClosingCallback'
    'Add-ADTSessionFinishingCallback'
    'Add-ADTSessionOpeningCallback'
    'Add-ADTSessionStartingCallback'
    'Block-ADTAppExecution'
    'Close-ADTSession'
    'Complete-ADTFunction'
    'Convert-ADTRegistryPath'
    'ConvertTo-ADTNTAccountOrSID'
    'Copy-ADTContentToCache'
    'Copy-File'
    'Copy-FileToUserProfiles'
    'Disable-ADTTerminalServerInstallMode'
    'Enable-ADTTerminalServerInstallMode'
    'Execute-ProcessAsUser'
    'Get-ADTConfig'
    'Get-ADTDeferHistory'
    'Get-ADTEnvironment'
    'Get-ADTFileVersion'
    'Get-ADTFreeDiskSpace'
    'Get-ADTGuidRegexPattern'
    'Get-ADTIniValue'
    'Get-ADTInstalledApplication'
    'Get-ADTLoggedOnUser'
    'Get-ADTModulePaths'
    'Get-ADTMsiExitCodeMessage'
    'Get-ADTMsiTableProperty'
    'Get-ADTObjectProperty'
    'Get-ADTPEFileArchitecture'
    'Get-ADTPendingReboot'
    'Get-ADTPowerShellProcessPath'
    'Get-ADTRegistryKey'
    'Get-ADTRunAsActiveUser'
    'Get-ADTRunningProcesses'
    'Get-ADTSchedulerTask'
    'Get-ADTServiceStartMode'
    'Get-ADTSession'
    'Get-ADTShortcut'
    'Get-ADTStrings'
    'Get-ADTUniversalDate'
    'Get-ADTUserProfiles'
    'Get-ADTWindowTitle'
    'Initialize-ADTFunction'
    'Initialize-ADTModule'
    'Install-ADTMSUpdates'
    'Install-ADTSCCMSoftwareUpdates'
    'Invoke-ADTAllUsersRegistryChange'
    'Invoke-ADTDllFileAction'
    'Invoke-ADTFunctionErrorHandler'
    'Invoke-ADTObjectMethod'
    'Invoke-ADTSCCMTask'
    'New-ADTErrorRecord'
    'New-ADTFolder'
    'New-ADTMsiTransform'
    'New-ADTShortcut'
    'New-ADTValidateScriptErrorRecord'
    'Open-ADTSession'
    'Out-ADTPowerShellEncodedCommand'
    'Register-ADTDllFile'
    'Remove-ADTContentFromCache'
    'Remove-ADTEdgeExtension'
    'Remove-ADTFile'
    'Remove-ADTFileFromUserProfiles'
    'Remove-ADTFolder'
    'Remove-ADTInvalidFileNameChars'
    'Remove-ADTRegistryKey'
    'Remove-ADTSessionClosingCallback'
    'Remove-ADTSessionFinishingCallback'
    'Remove-ADTSessionOpeningCallback'
    'Remove-ADTSessionStartingCallback'
    'Remove-MSIApplications'
    'Resolve-ADTBoundParameters'
    'Resolve-ADTErrorRecord'
    'Send-ADTKeys'
    'Set-ADTActiveSetup'
    'Set-ADTDeferHistory'
    'Set-ADTIniValue'
    'Set-ADTItemPermission'
    'Set-ADTMsiProperty'
    'Set-ADTProcessDpiAware'
    'Set-ADTRegistryKey'
    'Set-ADTServiceStartMode'
    'Set-ADTShortcut'
    'Show-ADTDialogBox'
    'Show-ADTHelpConsole'
    'Start-ADTMsiProcess'
    'Start-ADTMspProcess'
    'Start-ADTProcess'
    'Start-ADTServiceAndDependencies'
    'Stop-ADTServiceAndDependencies'
    'Test-ADTBattery'
    'Test-ADTCallerIsAdmin'
    'Test-ADTIsMutexAvailable'
    'Test-ADTModuleInitialised'
    'Test-ADTMSUpdates'
    'Test-ADTNetworkConnection'
    'Test-ADTPowerPoint'
    'Test-ADTRegistryValue'
    'Test-ADTServiceExists'
    'Test-ADTSessionActive'
    'Unblock-ADTAppExecution'
    'Unregister-ADTDllFile'
    'Update-ADTDesktop'
    'Update-ADTGroupPolicy'
    'Update-ADTSessionEnvironmentVariables'
    'Write-ADTLogEntry'
)

# Cmdlets to export from this module, for best performance, do not use wildcards and do not delete the entry, use an empty array if there are no cmdlets to export.
CmdletsToExport = @()

# Variables to export from this module
# VariablesToExport = ''

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

    } # End of PSData hashtable

} # End of PrivateData hashtable

# HelpInfo URI of this module
# HelpInfoURI = ''

# Default prefix for commands exported from this module. Override the default prefix using Import-Module -Prefix.
# DefaultCommandPrefix = ''

}

