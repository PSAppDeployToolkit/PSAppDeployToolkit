<#

.SYNOPSIS
PSAppDeployToolkit - This script performs the installation or uninstallation of an application(s).

.DESCRIPTION
- The script is provided as a template to perform an install or uninstall of an application(s).
- The script either performs an "Install" deployment type or an "Uninstall" deployment type.
- The install deployment type is broken down into 3 main sections/phases: Pre-Install, Install, and Post-Install.

The script dot-sources the AppDeployToolkitMain.ps1 script which contains the logic and functions required to install or uninstall an application.

PSAppDeployToolkit is licensed under the GNU LGPLv3 License - (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham and Muhammad Mashwani).

This program is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the
Free Software Foundation, either version 3 of the License, or any later version. This program is distributed in the hope that it will be useful, but
WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License
for more details. You should have received a copy of the GNU Lesser General Public License along with this program. If not, see <http://www.gnu.org/licenses/>.

.PARAMETER DeploymentType
The type of deployment to perform. Default is: Install.

.PARAMETER DeployMode
Specifies whether the installation should be run in Interactive, Silent, or NonInteractive mode. Default is: Interactive. Options: Interactive = Shows dialogs, Silent = No dialogs, NonInteractive = Very silent, i.e. no blocking apps. NonInteractive mode is automatically set if it is detected that the process is not user interactive.

.PARAMETER AllowRebootPassThru
Allows the 3010 return code (requires restart) to be passed back to the parent process (e.g. SCCM) if detected from an installation. If 3010 is passed back to SCCM, a reboot prompt will be triggered.

.PARAMETER TerminalServerMode
Changes to "user install mode" and back to "user execute mode" for installing/uninstalling applications for Remote Desktop Session Hosts/Citrix servers.

.PARAMETER DisableLogging
Disables logging to file for the script. Default is: $false.

.EXAMPLE
powershell.exe -Command "& { & '.\Deploy-Application.ps1' -DeployMode 'Silent'; Exit $LastExitCode }"

.EXAMPLE
powershell.exe -Command "& { & '.\Deploy-Application.ps1' -AllowRebootPassThru; Exit $LastExitCode }"

.EXAMPLE
powershell.exe -Command "& { & '.\Deploy-Application.ps1' -DeploymentType 'Uninstall'; Exit $LastExitCode }"

.EXAMPLE
Deploy-Application.exe -DeploymentType "Install" -DeployMode "Silent"

.INPUTS
None. You cannot pipe objects to this script.

.OUTPUTS
None. This script does not generate any output.

.NOTES
Toolkit Exit Code Ranges:
- 60000 - 68999: Reserved for built-in exit codes in Deploy-Application.ps1, Deploy-Application.exe, and AppDeployToolkitMain.ps1
- 69000 - 69999: Recommended for user customized exit codes in Deploy-Application.ps1
- 70000 - 79999: Recommended for user customized exit codes in AppDeployToolkitExtensions.ps1

.LINK
https://psappdeploytoolkit.com

#>

param (
    [Parameter(Mandatory = $false)]
    [ValidateSet('Install', 'Uninstall', 'Repair')]
    [System.String]$DeploymentType = 'Install',

    [Parameter(Mandatory = $false)]
    [ValidateSet('Interactive', 'Silent', 'NonInteractive')]
    [System.String]$DeployMode = 'Interactive',

    [Parameter(Mandatory = $false)]
    [System.Management.Automation.SwitchParameter]$AllowRebootPassThru,

    [Parameter(Mandatory = $false)]
    [System.Management.Automation.SwitchParameter]$TerminalServerMode,

    [Parameter(Mandatory = $false)]
    [System.Management.Automation.SwitchParameter]$DisableLogging
)


#---------------------------------------------------------------------------
#
# Variable definitions for you to edit.
#
#---------------------------------------------------------------------------

$sessionParams = @{
    # App variables.
    AppVendor = ''
    AppName = ''
    AppVersion = ''
    AppArch = ''
    AppLang = 'EN'
    AppRevision = '01'
    AppExitCodes = @(0, 1641, 3010)
    AppScriptVersion = '1.0.0'
    AppScriptDate = 'XX/XX/20XX'
    AppScriptAuthor = '<author name>'

    # Install Titles (Only set here to override defaults set by the toolkit).
    InstallName = ''
    InstallTitle = ''

    # Script variables.
    DeployAppScriptFriendlyName = 'Deploy Application'
    DeployAppScriptVersion = [System.Version]'3.91.0'
    DeployAppScriptDate = '05/03/2024'
}


#---------------------------------------------------------------------------
#
# Deployment type functions for you to configure.
#
#---------------------------------------------------------------------------

function Install-ADTApplication
{
    ##*===============================================
    ##* PRE-INSTALLATION
    ##*===============================================
    Update-ADTSessionInstallPhase -Value "Pre-$($DeploymentType)"

    ## Show Welcome Message, close Internet Explorer if required, allow up to 3 deferrals, verify there is enough disk space to complete the install, and persist the prompt.
    Show-InstallationWelcome -CloseApps 'iexplore' -AllowDefer -DeferTimes 50 -CheckDiskSpace -PersistPrompt

    ## Show Progress Message (with the default message).
    Show-ADTInstallationProgress

    ## <Perform Pre-Installation tasks here>


    ##*===============================================
    ##* INSTALLATION
    ##*===============================================
    Update-ADTSessionInstallPhase -Value $DeploymentType

    ## Handle Zero-Config MSI installations.
    if ($useDefaultMsi = Test-ADTSessionZeroConfigMSI)
    {
        [Hashtable]$ExecuteDefaultMSISplat = @{ Action = 'Uninstall'; Path = Get-ADTSessionZeroConfigMsiFile }
        if ($defaultMstFile = Get-ADTSessionZeroConfigMstFile)
        {
            $ExecuteDefaultMSISplat.Add('Transform', $defaultMstFile)
        }
        Execute-MSI @ExecuteDefaultMSISplat
        if ($defaultMspFiles = Get-ADTSessionZeroConfigMspFiles)
        {
            $defaultMspFiles | ForEach-Object { Execute-MSI -Action 'Patch' -Path $_ }
        }
    }

    ## <Perform Installation tasks here>


    ##*===============================================
    ##* POST-INSTALLATION
    ##*===============================================
    Update-ADTSessionInstallPhase -Value "Post-$($DeploymentType)"

    ## <Perform Post-Installation tasks here>

    ## Display a message at the end of the install.
    if (!$useDefaultMsi)
    {
        Show-ADTInstallationPrompt -Message 'You can customize text to appear at the end of an install or remove it completely for unattended installations.' -ButtonRightText 'OK' -Icon Information -NoWait
    }
}

function Uninstall-ADTApplication
{
    ##*===============================================
    ##* PRE-UNINSTALLATION
    ##*===============================================
    Update-ADTSessionInstallPhase -Value "Pre-$($DeploymentType)"

    ## Show Welcome Message, close Internet Explorer with a 60 second countdown before automatically closing.
    Show-InstallationWelcome -CloseApps 'iexplore' -CloseAppsCountdown 60

    ## Show Progress Message (with the default message).
    Show-ADTInstallationProgress

    ## <Perform Pre-Uninstallation tasks here>


    ##*===============================================
    ##* UNINSTALLATION
    ##*===============================================
    Update-ADTSessionInstallPhase -Value $DeploymentType

    ## Handle Zero-Config MSI uninstallations.
    if (Test-ADTSessionZeroConfigMSI)
    {
        [Hashtable]$ExecuteDefaultMSISplat = @{ Action = 'Uninstall'; Path = Get-ADTSessionZeroConfigMsiFile }
        if ($defaultMstFile = Get-ADTSessionZeroConfigMstFile)
        {
            $ExecuteDefaultMSISplat.Add('Transform', $defaultMstFile)
        }
        Execute-MSI @ExecuteDefaultMSISplat
    }

    ## <Perform Uninstallation tasks here>


    ##*===============================================
    ##* POST-UNINSTALLATION
    ##*===============================================
    Update-ADTSessionInstallPhase -Value "Post-$($DeploymentType)"

    ## <Perform Post-Uninstallation tasks here>
}

function Repair-ADTApplication
{
    ##*===============================================
    ##* PRE-REPAIR
    ##*===============================================
    Update-ADTSessionInstallPhase -Value "Pre-$($DeploymentType)"

    ## Show Welcome Message, close Internet Explorer with a 60 second countdown before automatically closing.
    Show-InstallationWelcome -CloseApps 'iexplore' -CloseAppsCountdown 60

    ## Show Progress Message (with the default message).
    Show-ADTInstallationProgress

    ## <Perform Pre-Repair tasks here>


    ##*===============================================
    ##* REPAIR
    ##*===============================================
    Update-ADTSessionInstallPhase -Value $DeploymentType

    ## Handle Zero-Config MSI repairs.
    if (Test-ADTSessionZeroConfigMSI)
    {
        [Hashtable]$ExecuteDefaultMSISplat = @{ Action = 'Repair'; Path = Get-ADTSessionZeroConfigMsiFile }
        if ($defaultMstFile = Get-ADTSessionZeroConfigMstFile)
        {
            $ExecuteDefaultMSISplat.Add('Transform', $defaultMstFile)
        }
        Execute-MSI @ExecuteDefaultMSISplat
    }

    ## <Perform Repair tasks here>


    ##*===============================================
    ##* POST-REPAIR
    ##*===============================================
    Update-ADTSessionInstallPhase -Value "Post-$($DeploymentType)"

    ## <Perform Post-Repair tasks here>
}


#---------------------------------------------------------------------------
#
# Module importation and session initialisation.
#
#---------------------------------------------------------------------------

try
{
    $mainExitCode = 0
    Import-Module -Name "$PSScriptRoot\AppDeployToolkit\PSAppDeployToolkit"
    Open-ADTSession -Cmdlet $PSCmdlet @PSBoundParameters @sessionParams
}
catch
{
    $mainExitCode = 60008
    throw
}
finally
{
    if ($mainExitCode)
    {
        exit $mainExitCode
    }
}


#---------------------------------------------------------------------------
#
# Deployment type invocation (where all the magic happens).
#
#---------------------------------------------------------------------------

try
{
    & "$($DeploymentType)-ADTApplication"
}
catch
{
    $mainExitCode = 60001
    $mainErrorMessage = "$(Resolve-Error)"
    Write-ADTLogEntry -Message $mainErrorMessage -Severity 3
    [System.Void](Show-ADTDialogBox -Text $mainErrorMessage -Icon Stop)
}
finally
{
    Close-ADTSession -ExitCode $mainExitCode
}
