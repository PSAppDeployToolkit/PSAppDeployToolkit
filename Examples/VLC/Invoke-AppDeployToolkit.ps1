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
    [ValidateSet('Install', 'Uninstall', 'Repair')]
    [System.String]$DeploymentType = 'Install',

    [ValidateSet('Interactive', 'Silent', 'NonInteractive')]
    [System.String]$DeployMode = 'Interactive',

    [System.Management.Automation.SwitchParameter]$AllowRebootPassThru,
    [System.Management.Automation.SwitchParameter]$TerminalServerMode,
    [System.Management.Automation.SwitchParameter]$DisableLogging
)


#---------------------------------------------------------------------------
#
# Variable definitions for you to edit.
#
#---------------------------------------------------------------------------

$adtSession = @{
    # App variables.
    AppVendor = 'VideoLAN'
    AppName = 'VLC media player'
    AppVersion = '3.0.21'
    AppArch = 'x64'
    AppLang = 'EN'
    AppRevision = '01'
    AppExitCodes = @(0)
    AppRebootCodes = @(1641, 3010)
    AppScriptVersion = [System.Version]'1.0.0'
    AppScriptDate = '13/06/2024'
    AppScriptAuthor = 'PSAppDeployToolkit'

    # Install Titles (Only set here to override defaults set by the toolkit).
    InstallName = ''
    InstallTitle = ''

    # Script variables.
    DeployAppScriptFriendlyName = $MyInvocation.MyCommand.Name
    DeployAppScriptVersion = [System.Version]'3.91.0'
    DeployAppScriptDate = '05/03/2024'
    DeployAppScriptParameters = $PSBoundParameters
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
    $adtSession.InstallPhase = "Pre-$($DeploymentType)"

    ## Show Welcome Message, close VLC if required, allow up to 3 deferrals, and persist the prompt
    Show-ADTInstallationWelcome -ProcessObjects @{Name = 'vlc'; Description = $adtSession.AppName} -AllowDeferCloseApps -DeferTimes 3 -PersistPrompt -NoMinimizeWindows

    ## Show Progress Message (with the default message).
    Show-ADTInstallationProgress

    ## <Perform Pre-Installation tasks here>


    ##*===============================================
    ##* INSTALLATION
    ##*===============================================
    $adtSession.InstallPhase = $DeploymentType

    ## Handle Zero-Config MSI installations.
    if ($adtSession.UseDefaultMsi)
    {
        [Hashtable]$ExecuteDefaultMSISplat = @{ Action = 'Install'; Path = $adtSession.DefaultMsiFile }
        if ($defaultMstFile = $adtSession.DefaultMstFile)
        {
            $ExecuteDefaultMSISplat.Add('Transform', $defaultMstFile)
        }
        Start-ADTMsiProcess @ExecuteDefaultMSISplat
        if ($defaultMspFiles = $adtSession.DefaultMspFiles)
        {
            $defaultMspFiles | ForEach-Object { Start-ADTMsiProcess -Action 'Patch' -Path $_ }
        }
    }

    ## <Perform Installation tasks here>

    Start-ADTProcess -Path 'vlc-3.0.21-win64.exe' -Parameters '/L=1033 /S'

    ##*===============================================
    ##* POST-INSTALLATION
    ##*===============================================
    $adtSession.InstallPhase = "Post-$($DeploymentType)"

    ## <Perform Post-Installation tasks here>

    Remove-ADTFile -Path "$envCommonDesktop\VLC media player.lnk","$envCommonStartMenuPrograms\VideoLAN\Release Notes.lnk","$envCommonStartMenuPrograms\VideoLAN\Documentation.lnk","$envCommonStartMenuPrograms\VideoLAN\VideoLAN Website.lnk"

    #Copy-FileToUserProfiles -Path "$dirSupportFiles\vlc" -Destination 'AppData\Roaming' -Recurse

    ## Display a message at the end of the install.
    if (!$adtSession.UseDefaultMsi)
    {
        Show-ADTInstallationPrompt -Message "$($adtSession.AppName) installation complete." -ButtonRightText 'OK' -Icon Information -NoWait
    }
}

function Uninstall-ADTApplication
{
    ##*===============================================
    ##* PRE-UNINSTALLATION
    ##*===============================================
    $adtSession.InstallPhase = "Pre-$($DeploymentType)"

    ## Show Welcome Message, close VLC with a 60 second countdown before automatically closing
    Show-ADTInstallationWelcome -ProcessObjects @{Name = 'vlc'; Description = $adtSession.AppName} -CloseAppsCountdown 60

    ## Show Progress Message (with the default message).
    Show-ADTInstallationProgress

    ## <Perform Pre-Uninstallation tasks here>

    ##*===============================================
    ##* UNINSTALLATION
    ##*===============================================
    $adtSession.InstallPhase = $DeploymentType

    ## Handle Zero-Config MSI uninstallations.
    if ($adtSession.UseDefaultMsi)
    {
        [Hashtable]$ExecuteDefaultMSISplat = @{ Action = 'Uninstall'; Path = $adtSession.DefaultMsiFile }
        if ($defaultMstFile = $adtSession.DefaultMstFile)
        {
            $ExecuteDefaultMSISplat.Add('Transform', $defaultMstFile)
        }
        Start-ADTMsiProcess @ExecuteDefaultMSISplat
    }

    ## <Perform Uninstallation tasks here>

    Start-ADTProcess -Path "$envProgramFiles\VideoLAN\VLC\uninstall.exe" -Parameters '/S' -ErrorAction Continue
    
    ##*===============================================
    ##* POST-UNINSTALLATION
    ##*===============================================
    $adtSession.InstallPhase = "Post-$($DeploymentType)"

    ## <Perform Post-Uninstallation tasks here>
}

function Repair-ADTApplication
{
    ##*===============================================
    ##* PRE-REPAIR
    ##*===============================================
    $adtSession.InstallPhase = "Pre-$($DeploymentType)"

    ## Show Welcome Message, close VLC with a 60 second countdown before automatically closing.
    Show-ADTInstallationWelcome -ProcessObjects @{Name = 'vlc'; Description = $adtSession.AppName} -CloseAppsCountdown 60

    ## Show Progress Message (with the default message).
    Show-ADTInstallationProgress

    ## <Perform Pre-Repair tasks here>


    ##*===============================================
    ##* REPAIR
    ##*===============================================
    $adtSession.InstallPhase = $DeploymentType

    ## Handle Zero-Config MSI repairs.
    if ($adtSession.UseDefaultMsi)
    {
        [Hashtable]$ExecuteDefaultMSISplat = @{ Action = 'Repair'; Path = $adtSession.DefaultMsiFile }
        if ($defaultMstFile = $adtSession.DefaultMstFile)
        {
            $ExecuteDefaultMSISplat.Add('Transform', $defaultMstFile)
        }
        Start-ADTMsiProcess @ExecuteDefaultMSISplat
    }

    ## <Perform Repair tasks here>

    Start-ADTProcess -Path "$envProgramFiles\VideoLAN\VLC\uninstall.exe" -Parameters '/S' -ErrorAction Continue
    Start-ADTProcess -Path 'vlc-3.0.21-win64.exe' -Parameters '/L=1033 /S'

    ##*===============================================
    ##* POST-REPAIR
    ##*===============================================
    $adtSession.InstallPhase = "Post-$($DeploymentType)"

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

# Import the module.
try
{
    Import-Module -Name "$PSScriptRoot\AppDeployToolkit\PSAppDeployToolkit" -Scope Local -Force
}
catch
{
    $Host.UI.WriteErrorLine(($_ | Out-String))
    exit 60008
}

# Instantiate a new session.
try
{
    $adtSession = Open-ADTSession -SessionState $ExecutionContext.SessionState @PSBoundParameters @adtSession -PassThru
}
catch
{
    $Host.UI.WriteErrorLine(($_ | Out-String))
    Remove-Module -Name PSAppDeployToolkit* -Force
    exit 60008
}


#---------------------------------------------------------------------------
#
# Deployment type invocation (where all the magic happens).
#
#---------------------------------------------------------------------------

try
{
    if ($TerminalServerMode) {Enable-ADTTerminalServerInstallMode}
    & "$($DeploymentType)-ADTApplication"
    Close-ADTSession
}
catch
{
    $mainErrorMessage = "$($adtSession.DeployAppScriptFriendlyName) received a terminating error and could not complete its operations.`n`n`n$(Resolve-ADTError -ErrorRecord $_)"
    Write-ADTLogEntry -Message $mainErrorMessage -Severity 3
    Show-ADTDialogBox -Text $mainErrorMessage -Icon Stop | Out-Null
    Close-ADTSession -ExitCode 60001
}
finally
{
    Remove-Module -Name PSAppDeployToolkit* -Force
}
