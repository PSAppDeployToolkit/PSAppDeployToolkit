<#

.SYNOPSIS
PSAppDeployToolkit - This script contains the PSADT core runtime and functions using by a Deploy-Application.ps1 script.

.DESCRIPTION
The script can be called directly to dot-source the toolkit functions for testing, but it is usually called by the Deploy-Application.ps1 script.

The script can usually be updated to the latest version without impacting your per-application Deploy-Application scripts. Please check release notes before upgrading.

PSAppDeployToolkit is licensed under the GNU LGPLv3 License - (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham and Muhammad Mashwani).

This program is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the
Free Software Foundation, either version 3 of the License, or any later version. This program is distributed in the hope that it will be useful, but
WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License
for more details. You should have received a copy of the GNU Lesser General Public License along with this program. If not, see <http://www.gnu.org/licenses/>.

.INPUTS
None. You cannot pipe objects to this script.

.OUTPUTS
None. This script does not generate any output.

.LINK
https://psappdeploytoolkit.com

#>

#---------------------------------------------------------------------------
#
# Initialisation code.
#
#---------------------------------------------------------------------------

# Set required variables to ensure module functionality.
$ErrorActionPreference = [System.Management.Automation.ActionPreference]::Stop
$ProgressPreference = [System.Management.Automation.ActionPreference]::SilentlyContinue
Set-PSDebug -Strict
Set-StrictMode -Version Latest

# Import our local module.
Import-Module -Name "$PSScriptRoot\PSAppDeployToolkit"

# Open a new PSADT session.
$sessionParams = Get-ADTDeployApplicationParameters -Cmdlet $PSCmdlet
Open-ADTSession @sessionParams


#---------------------------------------------------------------------------
#
# Legacy aliases that need to be refactored.
#
#---------------------------------------------------------------------------

Set-Alias -Name 'Register-DLL' -Value 'Invoke-RegisterOrUnregisterDLL'
Set-Alias -Name 'Unregister-DLL' -Value 'Invoke-RegisterOrUnregisterDLL'
if (!(Get-Command -Name 'Get-ScheduledTask')) {New-Alias -Name 'Get-ScheduledTask' -Value 'Get-SchedulerTask'}


#---------------------------------------------------------------------------
#
# Wrapper around Write-ADTLogEntry
#
#---------------------------------------------------------------------------

function Write-Log
{
    param (
        [Parameter(Mandatory = $true, Position = 0, ValueFromPipeline = $true, ValueFromPipelineByPropertyName = $true)]
        [AllowEmptyCollection()]
        [Alias('Text')]
        [System.String[]]$Message,

        [Parameter(Mandatory = $false, Position = 1)]
        [ValidateRange(0, 3)]
        [System.Int16]$Severity,

        [Parameter(Mandatory = $false, Position = 2)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Source,

        [Parameter(Mandatory = $false, Position = 3)]
        [ValidateNotNullOrEmpty()]
        [System.String]$ScriptSection,

        [Parameter(Mandatory = $false, Position = 4)]
        [ValidateSet('CMTrace', 'Legacy')]
        [System.String]$LogType,

        [Parameter(Mandatory = $false, Position = 5)]
        [ValidateNotNullOrEmpty()]
        [System.String]$LogFileDirectory,

        [Parameter(Mandatory = $false, Position = 6)]
        [ValidateNotNullOrEmpty()]
        [System.String]$LogFileName,

        [Parameter(Mandatory = $false, Position = 7)]
        [ValidateNotNullOrEmpty()]
        [System.Boolean]$AppendToLogFile,

        [Parameter(Mandatory = $false, Position = 8)]
        [ValidateNotNullOrEmpty()]
        [System.Int32]$MaxLogHistory,

        [Parameter(Mandatory = $false, Position = 9)]
        [ValidateNotNullOrEmpty()]
        [System.Decimal]$MaxLogFileSizeMB,

        [Parameter(Mandatory = $false, Position = 10)]
        [ValidateNotNullOrEmpty()]
        [System.Boolean]$ContinueOnError = $true,

        [Parameter(Mandatory = $false, Position = 11)]
        [ValidateNotNullOrEmpty()]
        [System.Boolean]$WriteHost,

        [Parameter(Mandatory = $false, Position = 12)]
        [System.Management.Automation.SwitchParameter]$PassThru,

        [Parameter(Mandatory = $false, Position = 13)]
        [System.Management.Automation.SwitchParameter]$DebugMessage,

        [Parameter(Mandatory = $false, Position = 14)]
        [ValidateNotNullOrEmpty()]
        [System.Boolean]$LogDebugMessage
    )

    begin {
        # Announce overall deprecation.
        Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] is deprecated. Please migrate your scripts to use [Write-ADTLogEntry] instead." -Severity 2 -Source $MyInvocation.MyCommand.Name

        # Announce dead parameters.
        if ($LogType)
        {
            Write-ADTLogEntry -Message "The parameter '-LogType' is discontinued and no longer has any effect." -Severity 2 -Source $MyInvocation.MyCommand.Name
            [System.Void]$PSBoundParameters.Remove('LogType')
        }
        if ($LogFileDirectory)
        {
            Write-ADTLogEntry -Message "The parameter '-LogFileDirectory' is discontinued and no longer has any effect." -Severity 2 -Source $MyInvocation.MyCommand.Name
            [System.Void]$PSBoundParameters.Remove('LogFileDirectory')
        }
        if ($LogFileName)
        {
            Write-ADTLogEntry -Message "The parameter '-LogFileName' is discontinued and no longer has any effect." -Severity 2 -Source $MyInvocation.MyCommand.Name
            [System.Void]$PSBoundParameters.Remove('LogFileName')
        }
        if ($AppendToLogFile)
        {
            Write-ADTLogEntry -Message "The parameter '-AppendToLogFile' is discontinued and no longer has any effect." -Severity 2 -Source $MyInvocation.MyCommand.Name
            [System.Void]$PSBoundParameters.Remove('AppendToLogFile')
        }
        if ($MaxLogHistory)
        {
            Write-ADTLogEntry -Message "The parameter '-MaxLogHistory' is discontinued and no longer has any effect." -Severity 2 -Source $MyInvocation.MyCommand.Name
            [System.Void]$PSBoundParameters.Remove('MaxLogHistory')
        }
        if ($MaxLogFileSizeMB)
        {
            Write-ADTLogEntry -Message "The parameter '-MaxLogFileSizeMB' is discontinued and no longer has any effect." -Severity 2 -Source $MyInvocation.MyCommand.Name
            [System.Void]$PSBoundParameters.Remove('MaxLogFileSizeMB')
        }
        if ($WriteHost)
        {
            Write-ADTLogEntry -Message "The parameter '-WriteHost' is discontinued and no longer has any effect." -Severity 2 -Source $MyInvocation.MyCommand.Name
            [System.Void]$PSBoundParameters.Remove('WriteHost')
        }
        if ($LogDebugMessage)
        {
            Write-ADTLogEntry -Message "The parameter '-LogDebugMessage' is discontinued and no longer has any effect." -Severity 2 -Source $MyInvocation.MyCommand.Name
            [System.Void]$PSBoundParameters.Remove('LogDebugMessage')
        }
        if ($PSBoundParameters.ContainsKey('ContinueOnError'))
        {
            [System.Void]$PSBoundParameters.Remove('ContinueOnError')
        }
    }

    process {
        try
        {
            Write-ADTLogEntry @PSBoundParameters
        }
        catch
        {
            Write-Host -Object "[$([System.DateTime]::Now.ToString('O'))] [$($this.GetPropertyValue('InstallPhase'))] [$($MyInvocation.MyCommand.Name)] :: Failed to write message [$Message] to the log file [$($this.GetPropertyValue('LogName'))].`n$(Resolve-Error)" -ForegroundColor Red
            if (!$ContinueOnError)
            {
                throw
            }
        }
    }
}


#---------------------------------------------------------------------------
#
# Wrapper around Close-ADTSession
#
#---------------------------------------------------------------------------

function Exit-Script
{
    param (
        [ValidateNotNullOrEmpty()]
        [System.Int32]$ExitCode
    )

    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] is deprecated. Please migrate your scripts to use [Close-ADTSession] instead." -Severity 2
    Close-ADTSession @PSBoundParameters
}


#---------------------------------------------------------------------------
#
# Wrapper around Invoke-ADTAllUsersRegistryChange
#
#---------------------------------------------------------------------------

function Invoke-HKCURegistrySettingsForAllUsers
{
    param (
        [Parameter(Mandatory = $true)]
        [ValidateScript({if ($_ -match '\$UserProfile\.SID') {throw "The function [Invoke-HKCURegistrySettingsForAllUsers] no longer supports the use of [`$UserProfile]. Please use [`$_] or [`$PSItem] instead."}; ![System.String]::IsNullOrWhiteSpace($_)})]
        [System.Management.Automation.ScriptBlock]$RegistrySettings,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.PSObject[]]$UserProfiles
    )

    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] is deprecated. Please migrate your scripts to use [Invoke-ADTAllUsersRegistryChange] instead." -Severity 2
    Invoke-ADTAllUsersRegistryChange @PSBoundParameters
}


#---------------------------------------------------------------------------
#
# Replacement for Get-HardwarePlatform
#
#---------------------------------------------------------------------------

function Get-HardwarePlatform
{
    param (
        [ValidateNotNullOrEmpty()]
        [System.Boolean]$ContinueOnError = $true
    )

    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] is deprecated. Please migrate your scripts to use [`$envHardwareType] instead." -Severity 2
    return $envHardwareType
}


#---------------------------------------------------------------------------
#
# Wrapper around Get-ADTFreeDiskSpace
#
#---------------------------------------------------------------------------

function Get-FreeDiskSpace
{
    param (
        [ValidateNotNullOrEmpty()]
        [System.String]$Drive,

        [ValidateNotNullOrEmpty()]
        [System.Boolean]$ContinueOnError = $true
    )

    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] is deprecated. Please migrate your scripts to use [Get-ADTFreeDiskSpace] instead." -Severity 2
    try
    {
        Get-ADTFreeDiskSpace @PSBoundParameters
    }
    catch
    {
        Write-ADTLogEntry -Message "Failed to retrieve free disk space for drive [$Drive].`n$(Resolve-Error)" -Severity 3
        if (!$ContinueOnError)
        {
            throw
        }
    }
}


#---------------------------------------------------------------------------
#
# Wrapper around Remove-ADTInvalidFileNameChars
#
#---------------------------------------------------------------------------

function Remove-InvalidFileNameChars
{
    param (
        [Parameter(Mandatory = $true, Position = 0, ValueFromPipeline = $true, ValueFromPipelineByPropertyName = $true)]
        [AllowEmptyString()]
        [System.String]$Name
    )

    begin {
        Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] is deprecated. Please migrate your scripts to use [Remove-ADTInvalidFileNameChars] instead." -Severity 2
    }

    end {
        $input.Where({$null -ne $_}) | Remove-ADTInvalidFileNameChars
    }
}


#---------------------------------------------------------------------------
#
# Wrapper around Get-ADTInstalledApplication
#
#---------------------------------------------------------------------------

function Get-InstalledApplication
{
    param (
        [ValidateNotNullOrEmpty()]
        [System.String[]]$Name,

        [ValidateNotNullOrEmpty()]
        [System.String]$ProductCode,

        [System.Management.Automation.SwitchParameter]$Exact,
        [System.Management.Automation.SwitchParameter]$WildCard,
        [System.Management.Automation.SwitchParameter]$RegEx,
        [System.Management.Automation.SwitchParameter]$IncludeUpdatesAndHotfixes
    )

    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] is deprecated. Please migrate your scripts to use [Get-ADTInstalledApplication] instead." -Severity 2
    Get-ADTInstalledApplication @PSBoundParameters
}


#---------------------------------------------------------------------------
#
# Wrapper around Get-ADTFileVersion
#
#---------------------------------------------------------------------------

function Get-FileVersion
{
    param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$File,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$ProductVersion,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Boolean]$ContinueOnError = $true
    )

    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] is deprecated. Please migrate your scripts to use [Get-ADTFileVersion] instead." -Severity 2
    try
    {
        Get-ADTFileVersion @PSBoundParameters
    }
    catch
    {
        Write-ADTLogEntry -Message "Failed to get version info.`n$(Resolve-Error)" -Severity 3
        if (!$ContinueOnError)
        {
            throw
        }
    }
}


#---------------------------------------------------------------------------
#
# Wrapper around Get-ADTUserProfiles
#
#---------------------------------------------------------------------------

function Get-UserProfiles
{
    param (
        [ValidateNotNullOrEmpty()]
        [System.String[]]$ExcludeNTAccount,

        [ValidateNotNullOrEmpty()]
        [System.Boolean]$ExcludeSystemProfiles = $true,

        [ValidateNotNullOrEmpty()]
        [System.Boolean]$ExcludeServiceProfiles = $true,

        [System.Management.Automation.SwitchParameter]$ExcludeDefaultUser
    )

    # Translate parameters.
    ('SystemProfiles', 'ServiceProfiles').Where({$PSBoundParameters.ContainsKey("Exclude$_")}).ForEach({
        if (!$PSBoundParameters["Exclude$_"])
        {
            $PSBoundParameters.Add("Include$_", [System.Management.Automation.SwitchParameter]$true)
        }
        [System.Void]$PSBoundParameters.Remove("Exclude$_")
    })

    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] is deprecated. Please migrate your scripts to use [Get-ADTUserProfiles] instead." -Severity 2
    Get-ADTUserProfiles @PSBoundParameters
}


#---------------------------------------------------------------------------
#
# Wrapper around Get-ADTDesktop
#
#---------------------------------------------------------------------------

function Update-Desktop
{
    param (
        [ValidateNotNullOrEmpty()]
        [System.Boolean]$ContinueOnError = $true
    )

    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] is deprecated. Please migrate your scripts to use [Get-ADTDesktop] instead." -Severity 2
    try
    {
        Get-ADTDesktop
    }
    catch
    {
        Write-ADTLogEntry -Message "Failed to refresh the Desktop and the Windows Explorer environment process block.`n$(Resolve-Error)" -Severity 3
        if (!$ContinueOnError)
        {
            throw
        }
    }
}

Set-Alias -Name Refresh-Desktop -Value Update-Desktop


#---------------------------------------------------------------------------
#
# Wrapper around Update-ADTSessionEnvironmentVariables
#
#---------------------------------------------------------------------------

function Update-SessionEnvironmentVariables
{
    param (
        [System.Management.Automation.SwitchParameter]$LoadLoggedOnUserEnvironmentVariables,

        [ValidateNotNullOrEmpty()]
        [System.Boolean]$ContinueOnError = $true
    )

    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] is deprecated. Please migrate your scripts to use [Update-ADTSessionEnvironmentVariables] instead." -Severity 2
    try
    {
        Update-ADTSessionEnvironmentVariables
    }
    catch
    {
        Write-ADTLogEntry -Message "Failed to refresh the environment variables for this PowerShell session.`n$(Resolve-Error)" -Severity 3
        if (!$ContinueOnError)
        {
            throw
        }
    }
}

Set-Alias -Name Refresh-SessionEnvironmentVariables -Value Update-ADTSessionEnvironmentVariables


#---------------------------------------------------------------------------
#
# Wrapper around Remove-ADTFile
#
#---------------------------------------------------------------------------

function Remove-File
{
    param (
        [Parameter(Mandatory = $true, ParameterSetName = 'Path')]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$Path,

        [Parameter(Mandatory = $true, ParameterSetName = 'LiteralPath')]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$LiteralPath,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$Recurse,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Boolean]$ContinueOnError = $true
    )

    # Announce overall deprecation and translate $ContinueOnError to an ActionPreference before executing.
    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] is deprecated. Please migrate your scripts to use [Remove-ADTFile] instead." -Severity 2
    if ($PSBoundParameters.ContainsKey('ContinueOnError'))
    {
        [System.Void]$PSBoundParameters.Remove('ContinueOnError')
    }
    if (!$ContinueOnError)
    {
        $PSBoundParameters.ErrorAction = [System.Management.Automation.ActionPreference]::Stop
    }
    Remove-ADTFile @PSBoundParameters
}


#---------------------------------------------------------------------------
#
# Wrapper around Show-ADTInstallationPrompt
#
#---------------------------------------------------------------------------

function Show-InstallationPrompt
{
    param (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Title,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Message,

        [Parameter(Mandatory = $false)]
        [ValidateSet('Left', 'Center', 'Right')]
        [System.String]$MessageAlignment,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$ButtonRightText,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$ButtonLeftText,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$ButtonMiddleText,

        [Parameter(Mandatory = $false)]
        [ValidateSet('Application', 'Asterisk', 'Error', 'Exclamation', 'Hand', 'Information', 'None', 'Question', 'Shield', 'Warning', 'WinLogo')]
        [System.String]$Icon,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$NoWait,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$PersistPrompt,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$MinimizeWindows,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.UInt32]$Timeout,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Boolean]$ExitOnTimeout,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Boolean]$TopMost
    )

    # Announce overall deprecation.
    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] is deprecated. Please migrate your scripts to use [Show-ADTInstallationPrompt] instead." -Severity 2

    # Tune up parameters. A lot has changed.
    if ($PSBoundParameters.ContainsKey('MessageAlignment'))
    {
        $PSBoundParameters.MessageAlignment = [System.Drawing.ContentAlignment]"Middle$($PSBoundParameters.MessageAlignment)"
    }
    if ($PSBoundParameters.ContainsKey('Icon') -and ($PSBoundParameters.Icon -eq 'None'))
    {
        [System.Void]$PSBoundParameters.Remove('Icon')
    }
    if ($PSBoundParameters.ContainsKey('ExitOnTimeout'))
    {
        $PSBoundParameters.NoExitOnTimeout = !$PSBoundParameters.ExitOnTimeout
        [System.Void]$PSBoundParameters.Remove('ExitOnTimeout')
    }
    if ($PSBoundParameters.ContainsKey('TopMost'))
    {
        $PSBoundParameters.NotTopMost = !$PSBoundParameters.TopMost
        [System.Void]$PSBoundParameters.Remove('TopMost')
    }

    # Invoke function with amended parameters.
    Show-ADTInstallationPrompt @PSBoundParameters
}


#---------------------------------------------------------------------------
#
# Wrapper around Show-ADTInstallationProgress
#
#---------------------------------------------------------------------------

function Show-InstallationProgress
{
    param (
        [ValidateNotNullOrEmpty()]
        [System.String]$StatusMessage,

        [ValidateSet('Default', 'TopLeft', 'Top', 'TopRight', 'TopCenter', 'BottomLeft', 'Bottom', 'BottomRight')]
        [System.String]$WindowLocation,

        [ValidateNotNullOrEmpty()]
        [System.Boolean]$TopMost = $true,

        [System.Management.Automation.SwitchParameter]$Quiet,
        [System.Management.Automation.SwitchParameter]$NoRelocation
    )

    # Announce overall deprecation and translate $ContinueOnError to an ActionPreference before executing.
    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] is deprecated. Please migrate your scripts to use [Show-ADTInstallationProgress] instead." -Severity 2
    if ($PSBoundParameters.ContainsKey('TopMost'))
    {
        $PSBoundParameters.NotTopMost = !$PSBoundParameters.TopMost
        [System.Void]$PSBoundParameters.Remove('TopMost')
    }
    Show-ADTInstallationProgress @PSBoundParameters
}


#---------------------------------------------------------------------------
#
# Wrapper around Show-ADTDialogBox
#
#---------------------------------------------------------------------------

function Show-DialogBox
{
    param (
        [Parameter(Mandatory = $true, Position = 0, HelpMessage = 'Enter a message for the dialog box.')]
        [ValidateNotNullOrEmpty()]
        [System.String]$Text,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Title,

        [Parameter(Mandatory = $false)]
        [ValidateSet('OK', 'OKCancel', 'AbortRetryIgnore', 'YesNoCancel', 'YesNo', 'RetryCancel', 'CancelTryAgainContinue')]
        [System.String]$Buttons,

        [Parameter(Mandatory = $false)]
        [ValidateSet('First', 'Second', 'Third')]
        [System.String]$DefaultButton,

        [Parameter(Mandatory = $false)]
        [ValidateSet('Exclamation', 'Information', 'None', 'Stop', 'Question')]
        [System.String]$Icon,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Timeout,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Boolean]$TopMost
    )

    # Announce overall deprecation and translate $ContinueOnError to an ActionPreference before executing.
    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] is deprecated. Please migrate your scripts to use [Show-ADTDialogBox] instead." -Severity 2
    if ($PSBoundParameters.ContainsKey('TopMost'))
    {
        $PSBoundParameters.NotTopMost = !$PSBoundParameters.TopMost
        [System.Void]$PSBoundParameters.Remove('TopMost')
    }
    Show-ADTDialogBox @PSBoundParameters
}


#---------------------------------------------------------------------------
#
# Wrapper around Show-ADTInstallationWelcome
#
#---------------------------------------------------------------------------

function Show-InstallationWelcome
{
    [CmdletBinding(DefaultParameterSetName = 'None')]
    param (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$CloseApps,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$Silent,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Int32]$CloseAppsCountdown,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Int32]$ForceCloseAppsCountdown,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$PromptToSave,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$PersistPrompt,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$BlockExecution,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$AllowDefer,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$AllowDeferCloseApps,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Int32]$DeferTimes,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Int32]$DeferDays,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$DeferDeadline,

        [Parameter(ParameterSetName = 'CheckDiskSpaceParameterSet', Mandatory = $true)]
        [System.Management.Automation.SwitchParameter]$CheckDiskSpace,

        [Parameter(ParameterSetName = 'CheckDiskSpaceParameterSet', Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Int32]$RequiredDiskSpace,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Boolean]$MinimizeWindows = $true,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Boolean]$TopMost = $true,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Int32]$ForceCountdown,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$CustomText
    )

    # Announce overall deprecation.
    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] is deprecated. Please migrate your scripts to use [Show-ADTInstallationWelcome] instead." -Severity 2

    # Tune up parameters. A lot has changed.
    if ($PSBoundParameters.ContainsKey('CloseApps'))
    {
        $PSBoundParameters.ProcessObjects = $CloseApps.Split(',').ForEach({
            $obj = @{}
            $obj.ProcessName, $obj.ProcessDescription = $_.Split('=')
            return [pscustomobject]$obj
        })
        [System.Void]$PSBoundParameters.Remove('CloseApps')
    }
    if ($PSBoundParameters.ContainsKey('MinimizeWindows'))
    {
        $PSBoundParameters.NoMinimizeWindows = !$PSBoundParameters.MinimizeWindows
        [System.Void]$PSBoundParameters.Remove('MinimizeWindows')
    }
    if ($PSBoundParameters.ContainsKey('TopMost'))
    {
        $PSBoundParameters.NotTopMost = !$PSBoundParameters.TopMost
        [System.Void]$PSBoundParameters.Remove('TopMost')
    }

    # Invoke function with amended parameters.
    Show-ADTInstallationWelcome @PSBoundParameters
}


#---------------------------------------------------------------------------
#
# Wrapper around Get-ADTWindowTitle
#
#---------------------------------------------------------------------------

function Get-WindowTitle
{
    param (
        [Parameter(Mandatory = $true, ParameterSetName = 'SearchWinTitle')]
        [AllowEmptyString()]
        [System.String]$WindowTitle,

        [Parameter(Mandatory = $true, ParameterSetName = 'GetAllWinTitles')]
        [System.Management.Automation.SwitchParameter]$GetAllWindowTitles,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$DisableFunctionLogging
    )

    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] is deprecated. Please migrate your scripts to use [Get-ADTWindowTitle] instead." -Severity 2
    Get-ADTWindowTitle @PSBoundParameters
}


#---------------------------------------------------------------------------
#
# Wrapper around Show-ADTInstallationRestartPrompt
#
#---------------------------------------------------------------------------

function Show-InstallationRestartPrompt
{
    param (
        [ValidateNotNullOrEmpty()]
        [System.Int32]$CountdownSeconds,

        [ValidateNotNullOrEmpty()]
        [System.Int32]$CountdownNoHideSeconds,

        [ValidateNotNullOrEmpty()]
        [System.Boolean]$NoSilentRestart = $true,

        [ValidateNotNullOrEmpty()]
        [System.Int32]$SilentCountdownSeconds,

        [ValidateNotNullOrEmpty()]
        [System.Boolean]$TopMost = $true,

        [System.Management.Automation.SwitchParameter]$NoCountdown
    )

    # Announce overall deprecation and translate $ContinueOnError to an ActionPreference before executing.
    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] is deprecated. Please migrate your scripts to use [Show-ADTInstallationRestartPrompt] instead." -Severity 2
    if ($PSBoundParameters.ContainsKey('NoSilentRestart'))
    {
        $PSBoundParameters.SilentRestart = !$PSBoundParameters.NoSilentRestart
        [System.Void]$PSBoundParameters.Remove('NoSilentRestart')
    }
    if ($PSBoundParameters.ContainsKey('TopMost'))
    {
        $PSBoundParameters.NotTopMost = !$PSBoundParameters.TopMost
        [System.Void]$PSBoundParameters.Remove('TopMost')
    }
    Show-ADTInstallationRestartPrompt @PSBoundParameters
}


#---------------------------------------------------------------------------
#
# Wrapper around Show-ADTBalloonTip
#
#---------------------------------------------------------------------------

function Show-BalloonTip
{
    param (
        [Parameter(Mandatory = $true, Position = 0)]
        [ValidateNotNullOrEmpty()]
        [System.String]$BalloonTipText,

        [Parameter(Mandatory = $false, Position = 1)]
        [ValidateNotNullOrEmpty()]
        [System.String]$BalloonTipTitle,

        [Parameter(Mandatory = $false, Position = 2)]
        [ValidateSet('Error', 'Info', 'None', 'Warning')]
        [System.Windows.Forms.ToolTipIcon]$BalloonTipIcon,

        [Parameter(Mandatory = $false, Position = 3)]
        [ValidateNotNullOrEmpty()]
        [System.Int32]$BalloonTipTime,

        [Parameter(Mandatory = $false, Position = 4)]
        [System.Management.Automation.SwitchParameter]$NoWait
    )

    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] is deprecated. Please migrate your scripts to use [Show-ADTBalloonTip] instead." -Severity 2
    Show-ADTBalloonTip @PSBoundParameters
}


#---------------------------------------------------------------------------
#
# Wrapper around Copy-ADTContentToCache
#
#---------------------------------------------------------------------------

function Copy-ContentToCache
{
    param (
        [Parameter(Mandatory = $false, Position = 0, HelpMessage = 'The path to the software cache folder')]
        [ValidateNotNullOrEmpty()]
        [System.String]$Path
    )

    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] is deprecated. Please migrate your scripts to use [Copy-ADTContentToCache] instead." -Severity 2
    Copy-ADTContentToCache @PSBoundParameters
}


#---------------------------------------------------------------------------
#
# Wrapper around Remove-ADTContentFromCache
#
#---------------------------------------------------------------------------

function Remove-ContentFromCache
{
    param (
        [Parameter(Mandatory = $false, Position = 0, HelpMessage = 'The path to the software cache folder')]
        [ValidateNotNullOrEmpty()]
        [System.String]$Path
    )

    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] is deprecated. Please migrate your scripts to use [Remove-ADTContentFromCache] instead." -Severity 2
    Remove-ADTContentFromCache @PSBoundParameters
}


#---------------------------------------------------------------------------
#
# Wrapper around Test-ADTNetworkConnection
#
#---------------------------------------------------------------------------

function Test-NetworkConnection
{
    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] is deprecated. Please migrate your scripts to use [Test-ADTNetworkConnection] instead." -Severity 2
    Test-ADTNetworkConnection
}


#---------------------------------------------------------------------------
#
# Wrapper around Get-ADTLoggedOnUser
#
#---------------------------------------------------------------------------

function Get-LoggedOnUser
{
    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] is deprecated. Please migrate your scripts to use [Get-ADTLoggedOnUser] instead." -Severity 2
    Get-ADTLoggedOnUser
}


#---------------------------------------------------------------------------
#
# Wrapper around Get-ADTIniValue
#
#---------------------------------------------------------------------------

function Get-IniValue
{
    param (
        [Parameter(Mandatory = $true)]
        [ValidateScript({if (![System.IO.File]::Exists($_)) {throw "The specified file does not exist."}; $_})]
        [System.String]$FilePath,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Section,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Key,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Boolean]$ContinueOnError = $true
    )

    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] is deprecated. Please migrate your scripts to use [Get-ADTIniValue] instead." -Severity 2
    if ($PSBoundParameters.ContainsKey('ContinueOnError'))
    {
        [System.Void]$PSBoundParameters.Remove('ContinueOnError')
    }

    try
    {
        Get-ADTIniValue @PSBoundParameters
    }
    catch
    {
        Write-ADTLogEntry -Message "Failed to read INI file key value.`n$(Resolve-Error)" -Severity 3
        if (!$ContinueOnError)
        {
            throw
        }
    }
}


#---------------------------------------------------------------------------
#
# Wrapper around Set-ADTIniValue
#
#---------------------------------------------------------------------------

function Set-IniValue
{
    param (
        [Parameter(Mandatory = $true)]
        [ValidateScript({if (![System.IO.File]::Exists($_)) {throw "The specified file does not exist."}; $_})]
        [System.String]$FilePath,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Section,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Key,

        [Parameter(Mandatory = $true)]
        [AllowNull()]
        [System.Object]$Value,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Boolean]$ContinueOnError = $true
    )

    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] is deprecated. Please migrate your scripts to use [Set-ADTIniValue] instead." -Severity 2
    if ($PSBoundParameters.ContainsKey('ContinueOnError'))
    {
        [System.Void]$PSBoundParameters.Remove('ContinueOnError')
    }

    try
    {
        Set-ADTIniValue @PSBoundParameters
    }
    catch
    {
        Write-ADTLogEntry -Message "Failed to write INI file key value.`n$(Resolve-Error)" -Severity 3
        if (!$ContinueOnError)
        {
            throw
        }
    }
}
