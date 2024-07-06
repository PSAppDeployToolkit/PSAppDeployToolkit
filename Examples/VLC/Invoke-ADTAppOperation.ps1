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

$sessionProps = @{
    # App variables.
    AppVendor = 'VideoLAN'
    AppName = 'VLC Media Player'
    AppVersion = '3.0.21'
    AppArch = 'x64'
    AppLang = 'EN'
    AppRevision = '01'
    AppExitCodes = @(0)
    AppRebootCodes = @(1641, 3010)
    AppScriptVersion = '1.0.0'
    AppScriptDate = '13/06/2024'
    AppScriptAuthor = 'PsAppDeployToolkit'

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

    ## Show Welcome Message, close VLC if required, allow up to 3 deferrals, and persist the prompt
    Show-ADTInstallationWelcome -ProcessObjects @{ProcessName = 'vlc'} -AllowDeferCloseApps -DeferTimes 3 -PersistPrompt -NoMinimizeWindows

    ## Show Progress Message (with the default message).
    Show-ADTInstallationProgress

    ## <Perform Pre-Installation tasks here>


    ##*===============================================
    ##* INSTALLATION
    ##*===============================================
    Update-ADTSessionInstallPhase -Value $DeploymentType

    ## Handle Zero-Config MSI installations.
    if ($sessionProps.UseDefaultMsi)
    {
        [Hashtable]$ExecuteDefaultMSISplat = @{ Action = 'Install'; Path = $sessionProps.DefaultMsiFile }
        if ($defaultMstFile = $sessionProps.DefaultMstFile)
        {
            $ExecuteDefaultMSISplat.Add('Transform', $defaultMstFile)
        }
        $mainExitCode = (Start-ADTMsiProcess @ExecuteDefaultMSISplat -PassThru).ExitCode
        if ($defaultMspFiles = $sessionProps.DefaultMspFiles)
        {
            $defaultMspFiles | ForEach-Object { Start-ADTMsiProcess -Action 'Patch' -Path $_ }
        }
    }

    ## <Perform Installation tasks here>

    Start-ADTProcess -Path 'vlc-3.0.21-win64.exe' -Parameters '/L=1033 /S'

    ##*===============================================
    ##* POST-INSTALLATION
    ##*===============================================
    Update-ADTSessionInstallPhase -Value "Post-$($DeploymentType)"

    ## <Perform Post-Installation tasks here>

    Remove-ADTFile -Path "$envCommonDesktop\VLC media player.lnk","$envCommonStartMenuPrograms\VideoLAN\Release Notes.lnk","$envCommonStartMenuPrograms\VideoLAN\Documentation.lnk","$envCommonStartMenuPrograms\VideoLAN\VideoLAN Website.lnk"

    #Copy-FileToUserProfiles -Path "$dirSupportFiles\vlc" -Destination 'AppData\Roaming' -Recurse

    ## Display a message at the end of the install.
    if (!$sessionProps.UseDefaultMsi)
    {
        Show-ADTInstallationPrompt -Message "$appName installation complete." -ButtonRightText 'OK' -Icon Information -NoWait
    }
}

function Uninstall-ADTApplication
{
    ##*===============================================
    ##* PRE-UNINSTALLATION
    ##*===============================================
    Update-ADTSessionInstallPhase -Value "Pre-$($DeploymentType)"

    ## Show Welcome Message, close VLC with a 60 second countdown before automatically closing
    Show-ADTInstallationWelcome -ProcessObjects @{ProcessName = 'vlc'} -CloseAppsCountdown 60

    ## Show Progress Message (with the default message).
    Show-ADTInstallationProgress

    ## <Perform Pre-Uninstallation tasks here>

    ##*===============================================
    ##* UNINSTALLATION
    ##*===============================================
    Update-ADTSessionInstallPhase -Value $DeploymentType

    ## Handle Zero-Config MSI uninstallations.
    if ($sessionProps.UseDefaultMsi)
    {
        [Hashtable]$ExecuteDefaultMSISplat = @{ Action = 'Uninstall'; Path = $sessionProps.DefaultMsiFile }
        if ($defaultMstFile = $sessionProps.DefaultMstFile)
        {
            $ExecuteDefaultMSISplat.Add('Transform', $defaultMstFile)
        }
        $mainExitCode = (Start-ADTMsiProcess @ExecuteDefaultMSISplat -PassThru).ExitCode
    }

    ## <Perform Uninstallation tasks here>

    Start-ADTProcess -Path "$envProgramFiles\VideoLAN\VLC\uninstall.exe" -Parameters '/S' -ErrorAction Continue
    
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

    ## Show Welcome Message, close VLC with a 60 second countdown before automatically closing.
    Show-ADTInstallationWelcome -ProcessObjects @{ProcessName = 'vlc'} -CloseAppsCountdown 60

    ## Show Progress Message (with the default message).
    Show-ADTInstallationProgress

    ## <Perform Pre-Repair tasks here>


    ##*===============================================
    ##* REPAIR
    ##*===============================================
    Update-ADTSessionInstallPhase -Value $DeploymentType

    ## Handle Zero-Config MSI repairs.
    if ($sessionProps.UseDefaultMsi)
    {
        [Hashtable]$ExecuteDefaultMSISplat = @{ Action = 'Repair'; Path = $sessionProps.DefaultMsiFile }
        if ($defaultMstFile = $sessionProps.DefaultMstFile)
        {
            $ExecuteDefaultMSISplat.Add('Transform', $defaultMstFile)
        }
        $mainExitCode = (Start-ADTMsiProcess @ExecuteDefaultMSISplat -PassThru).ExitCode
    }

    ## <Perform Repair tasks here>

    Start-ADTProcess -Path "$envProgramFiles\VideoLAN\VLC\uninstall.exe" -Parameters '/S' -ErrorAction Continue
    Start-ADTProcess -Path 'vlc-3.0.21-win64.exe' -Parameters '/L=1033 /S'

    ##*===============================================
    ##* POST-REPAIR
    ##*===============================================
    Update-ADTSessionInstallPhase -Value "Post-$($DeploymentType)"

    ## <Perform Post-Repair tasks here>

    Remove-ADTFile -Path "$envCommonDesktop\VLC media player.lnk","$envCommonStartMenuPrograms\VideoLAN\Release Notes.lnk","$envCommonStartMenuPrograms\VideoLAN\Documentation.lnk","$envCommonStartMenuPrograms\VideoLAN\VideoLAN Website.lnk"

    #Copy-FileToUserProfiles -Path "$dirSupportFiles\vlc" -Destination 'AppData\Roaming' -Recurse

}


#---------------------------------------------------------------------------
#
# Module importation and session initialisation.
#
#---------------------------------------------------------------------------

# Set strict error handling across entire operation.
$ErrorActionPreference = [System.Management.Automation.ActionPreference]::Stop
$ProgressPreference = [System.Management.Automation.ActionPreference]::SilentlyContinue
Set-StrictMode -Version 1
$mainExitCode = 0

# Import the module.
try
{
    Import-Module -Name "$PSScriptRoot\AppDeployToolkit\PSAppDeployToolkit" -Scope Local -Force
}
catch
{
    $Host.UI.WriteErrorLine(($_ | Out-String))
    exit ($mainExitCode = 60008)
}

# Instantiate a new session.
try
{
    Initialize-ADTModule
    Open-ADTSession -Cmdlet $PSCmdlet @PSBoundParameters @sessionProps
    $sessionProps = Get-ADTSessionProperties
}
catch
{
    $Host.UI.WriteErrorLine(($_ | Out-String))
    Remove-Module -Name PSAppDeployToolkit* -Force
    exit ($mainExitCode = 60008)
}


#---------------------------------------------------------------------------
#
# Deployment type invocation (where all the magic happens).
#
#---------------------------------------------------------------------------

try
{
    & "$($DeploymentType)-ADTApplication"
    Close-ADTSession -ExitCode $mainExitCode
}
catch
{
    Write-ADTLogEntry -Message ($mainErrorMessage = Resolve-ADTError) -Severity 3
    Show-ADTDialogBox -Text $mainErrorMessage -Icon Stop | Out-Null
    Close-ADTSession -ExitCode ($mainExitCode = 60001)
}
finally
{
    Remove-Module -Name PSAppDeployToolkit* -Force
}
