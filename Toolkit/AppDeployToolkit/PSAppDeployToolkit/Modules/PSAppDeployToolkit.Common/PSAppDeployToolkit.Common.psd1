#
# Module manifest for module 'PSAppDeployToolkit.Common'
#
# Generated on: 2024-06-05
#

@{

# Script module or binary module file associated with this manifest.
RootModule = 'PSAppDeployToolkit.Common.psm1'

# Version number of this module.
ModuleVersion = '3.91.0'

# Supported PSEditions
# CompatiblePSEditions = @()

# ID used to uniquely identify this module
GUID = '6305ffdd-8ddc-4d83-b9b6-c00986c6a495'

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
# RequiredModules = @()

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
    'Convert-ADTRegistryPath'
    'ConvertTo-ADTNTAccountOrSID'
    'Copy-File'
    'Copy-FileToUserProfiles'
    'Execute-ProcessAsUser'
    'Get-ADTFileVersion'
    'Get-ADTFreeDiskSpace'
    'Get-ADTIniValue'
    'Get-ADTInstalledApplication'
    'Get-ADTLoggedOnUser'
    'Get-ADTMsiExitCodeMessage'
    'Get-ADTObjectProperty'
    'Get-ADTPEFileArchitecture'
    'Get-ADTRunningProcesses'
    'Get-ADTServiceStartMode'
    'Get-ADTUniversalDate'
    'Get-ADTUserProfiles'
    'Get-ADTWindowTitle'
    'Get-MsiTableProperty'
    'Get-ADTPendingReboot'
    'Get-ADTRegistryKey'
    'Get-ADTSchedulerTask'
    'Get-Shortcut'
    'Install-ADTMSUpdates'
    'Install-SCCMSoftwareUpdates'
    'Invoke-ADTAllUsersRegistryChange'
    'Invoke-ADTObjectMethod'
    'Invoke-RegisterOrUnregisterDLL'
    'Invoke-SCCMTask'
    'New-ADTFolder'
    'New-MsiTransform'
    'New-Shortcut'
    'Remove-ADTEdgeExtension'
    'Remove-ADTFile'
    'Remove-ADTFileFromUserProfiles'
    'Remove-Folder'
    'Remove-MSIApplications'
    'Remove-ADTRegistryKey'
    'Send-Keys'
    'Set-ActiveSetup'
    'Set-ADTIniValue'
    'Set-ADTServiceStartMode'
    'Set-ItemPermission'
    'Set-MsiProperty'
    'Set-ADTRegistryKey'
    'Set-Shortcut'
    'Start-ADTMsiProcess'
    'Start-ADTMspProcess'
    'Start-ADTProcess'
    'Start-ADTServiceAndDependencies'
    'Stop-ADTServiceAndDependencies'
    'Test-ADTIsMutexAvailable'
    'Test-ADTNetworkConnection'
    'Test-ADTPowerPoint'
    'Test-ADTServiceExists'
    'Test-ADTBattery'
    'Test-ADTMSUpdates'
    'Test-ADTRegistryValue'
    'Update-ADTDesktop'
    'Update-ADTGroupPolicy'
    'Update-ADTSessionEnvironmentVariables'
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

