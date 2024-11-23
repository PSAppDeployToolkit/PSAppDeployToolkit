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
Set-StrictMode -Version 1

# Import our local module.
Remove-Module -Name PSAppDeployToolkit* -Force
Import-Module -Name "$PSScriptRoot\PSAppDeployToolkit" -Scope Local -Force

# Open a new PSADT session.
if (Test-Path -LiteralPath Variable:PSCmdlet)
{
    # Get all relevant parameters from the targeted function, then check whether they're defined and not empty.
    $sessionProps = @{Cmdlet = $PSCmdlet}
    foreach ($param in (Get-Item -LiteralPath Function:Open-ADTSession).Parameters.Values.Where({$_.ParameterSets.Values.HelpMessage -match '^Deploy-Application\.ps1'}).Name)
    {
        # Return early if the parameter doesn't exist or its value is null.
        if (($value = Get-Variable -Name $param -ValueOnly -ErrorAction Ignore) -and ![System.String]::IsNullOrWhiteSpace((Out-String -InputObject $value)))
        {
            $sessionProps.Add($param, $value)
        }
    }

    # Initialise the module and open the session.
    Initialize-ADTModule -Cmdlet $PSCmdlet
    Open-ADTSession @sessionProps
}


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
    [CmdletBinding()]
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
        Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] has been replaced by [Write-ADTLogEntry]. Please migrate your scripts to use the new function." -Severity 2 -Source $MyInvocation.MyCommand.Name

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
            Write-Host -Object "[$([System.DateTime]::Now.ToString('O'))] [$($this.GetPropertyValue('InstallPhase'))] [$($MyInvocation.MyCommand.Name)] :: Failed to write message [$Message] to the log file [$($this.GetPropertyValue('LogName'))].`n$(Resolve-ADTError)" -ForegroundColor Red
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
    [CmdletBinding()]
    param (
        [ValidateNotNullOrEmpty()]
        [System.Int32]$ExitCode
    )

    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] has been replaced by [Close-ADTSession]. Please migrate your scripts to use the new function." -Severity 2
    Close-ADTSession @PSBoundParameters
}


#---------------------------------------------------------------------------
#
# Wrapper around Invoke-ADTAllUsersRegistryChange
#
#---------------------------------------------------------------------------

function Invoke-HKCURegistrySettingsForAllUsers
{
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [ValidateScript({ if ($_ -match '\$UserProfile\.SID') { Write-ADTLogEntry -Message "The base function [Invoke-ADTAllUsersRegistryChange] no longer supports the use of [`$UserProfile]. Please use [`$_] or [`$PSItem] instead." -Severity 2 }; ![System.String]::IsNullOrWhiteSpace($_) })]
        [System.Management.Automation.ScriptBlock]$RegistrySettings,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [PSADT.Types.UserProfile[]]$UserProfiles
    )

    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] has been replaced by [Invoke-ADTAllUsersRegistryChange]. Please migrate your scripts to use the new function." -Severity 2
    $PSBoundParameters.RegistrySettings = {$UserProfile = $_}, $PSBoundParameters.RegistrySettings
    Invoke-ADTAllUsersRegistryChange @PSBoundParameters
}


#---------------------------------------------------------------------------
#
# Replacement for Get-HardwarePlatform
#
#---------------------------------------------------------------------------

function Get-HardwarePlatform
{
    [CmdletBinding()]
    param (
        [ValidateNotNullOrEmpty()]
        [System.Boolean]$ContinueOnError = $true
    )

    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] has been replaced by [`$envHardwareType]. Please migrate your scripts to use the new function." -Severity 2
    return $envHardwareType
}


#---------------------------------------------------------------------------
#
# Wrapper around Get-ADTFreeDiskSpace
#
#---------------------------------------------------------------------------

function Get-FreeDiskSpace
{
    [CmdletBinding()]
    param (
        [ValidateNotNullOrEmpty()]
        [System.String]$Drive,

        [ValidateNotNullOrEmpty()]
        [System.Boolean]$ContinueOnError = $true
    )

    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] has been replaced by [Get-ADTFreeDiskSpace]. Please migrate your scripts to use the new function." -Severity 2
    if ($PSBoundParameters.ContainsKey('ContinueOnError'))
    {
        [System.Void]$PSBoundParameters.Remove('ContinueOnError')
    }

    try
    {
        Get-ADTFreeDiskSpace @PSBoundParameters
    }
    catch
    {
        Write-ADTLogEntry -Message "Failed to retrieve free disk space for drive [$Drive].`n$(Resolve-ADTError)" -Severity 3
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
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true, Position = 0, ValueFromPipeline = $true, ValueFromPipelineByPropertyName = $true)]
        [AllowEmptyString()]
        [System.String]$Name
    )

    begin {
        Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] has been replaced by [Remove-ADTInvalidFileNameChars]. Please migrate your scripts to use the new function." -Severity 2
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
    [CmdletBinding()]
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

    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] has been replaced by [Get-ADTInstalledApplication]. Please migrate your scripts to use the new function." -Severity 2
    Get-ADTInstalledApplication @PSBoundParameters
}


#---------------------------------------------------------------------------
#
# Wrapper around Get-ADTFileVersion
#
#---------------------------------------------------------------------------

function Get-FileVersion
{
    [CmdletBinding()]
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

    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] has been replaced by [Get-ADTFileVersion]. Please migrate your scripts to use the new function." -Severity 2
    if ($PSBoundParameters.ContainsKey('ContinueOnError'))
    {
        [System.Void]$PSBoundParameters.Remove('ContinueOnError')
    }

    try
    {
        Get-ADTFileVersion @PSBoundParameters
    }
    catch
    {
        Write-ADTLogEntry -Message "Failed to get version info.`n$(Resolve-ADTError)" -Severity 3
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
    [CmdletBinding()]
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

    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] has been replaced by [Get-ADTUserProfiles]. Please migrate your scripts to use the new function." -Severity 2
    Get-ADTUserProfiles @PSBoundParameters
}


#---------------------------------------------------------------------------
#
# Wrapper around Get-ADTDesktop
#
#---------------------------------------------------------------------------

function Update-Desktop
{
    [CmdletBinding()]
    param (
        [ValidateNotNullOrEmpty()]
        [System.Boolean]$ContinueOnError = $true
    )

    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] has been replaced by [Get-ADTDesktop]. Please migrate your scripts to use the new function." -Severity 2
    try
    {
        Get-ADTDesktop
    }
    catch
    {
        Write-ADTLogEntry -Message "Failed to refresh the Desktop and the Windows Explorer environment process block.`n$(Resolve-ADTError)" -Severity 3
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
    [CmdletBinding()]
    param (
        [System.Management.Automation.SwitchParameter]$LoadLoggedOnUserEnvironmentVariables,

        [ValidateNotNullOrEmpty()]
        [System.Boolean]$ContinueOnError = $true
    )

    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] has been replaced by [Update-ADTSessionEnvironmentVariables]. Please migrate your scripts to use the new function." -Severity 2
    try
    {
        Update-ADTSessionEnvironmentVariables
    }
    catch
    {
        Write-ADTLogEntry -Message "Failed to refresh the environment variables for this PowerShell session.`n$(Resolve-ADTError)" -Severity 3
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
    [CmdletBinding()]
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
    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] has been replaced by [Remove-ADTFile]. Please migrate your scripts to use the new function." -Severity 2
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
    [CmdletBinding()]
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
    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] has been replaced by [Show-ADTInstallationPrompt]. Please migrate your scripts to use the new function." -Severity 2

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
    [CmdletBinding()]
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

    # Announce overall deprecation before executing.
    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] has been replaced by [Show-ADTInstallationProgress]. Please migrate your scripts to use the new function." -Severity 2
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
    [CmdletBinding()]
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

    # Announce overall deprecation before executing.
    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] has been replaced by [Show-ADTDialogBox]. Please migrate your scripts to use the new function." -Severity 2
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
    [CmdletBinding()]
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
    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] has been replaced by [Show-ADTInstallationWelcome]. Please migrate your scripts to use the new function." -Severity 2

    # Tune up parameters. A lot has changed.
    if ($PSBoundParameters.ContainsKey('CloseApps'))
    {
        $PSBoundParameters.ProcessObjects = $CloseApps.Split(',').ForEach({
            $obj = @{}; $obj.Name, $obj.Description = $_.Split('=')
            return [PSADT.Types.ProcessObject]$obj
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
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true, ParameterSetName = 'SearchWinTitle')]
        [AllowEmptyString()]
        [System.String]$WindowTitle,

        [Parameter(Mandatory = $true, ParameterSetName = 'GetAllWinTitles')]
        [System.Management.Automation.SwitchParameter]$GetAllWindowTitles,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$DisableFunctionLogging
    )

    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] has been replaced by [Get-ADTWindowTitle]. Please migrate your scripts to use the new function." -Severity 2
    Get-ADTWindowTitle @PSBoundParameters
}


#---------------------------------------------------------------------------
#
# Wrapper around Show-ADTInstallationRestartPrompt
#
#---------------------------------------------------------------------------

function Show-InstallationRestartPrompt
{
    [CmdletBinding()]
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

    # Announce overall deprecation before executing.
    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] has been replaced by [Show-ADTInstallationRestartPrompt]. Please migrate your scripts to use the new function." -Severity 2
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
    [CmdletBinding()]
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

    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] has been replaced by [Show-ADTBalloonTip]. Please migrate your scripts to use the new function." -Severity 2
    Show-ADTBalloonTip @PSBoundParameters
}


#---------------------------------------------------------------------------
#
# Wrapper around Copy-ADTContentToCache
#
#---------------------------------------------------------------------------

function Copy-ContentToCache
{
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $false, Position = 0, HelpMessage = 'The path to the software cache folder')]
        [ValidateNotNullOrEmpty()]
        [System.String]$Path
    )

    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] has been replaced by [Copy-ADTContentToCache]. Please migrate your scripts to use the new function." -Severity 2
    Copy-ADTContentToCache @PSBoundParameters
}


#---------------------------------------------------------------------------
#
# Wrapper around Remove-ADTContentFromCache
#
#---------------------------------------------------------------------------

function Remove-ContentFromCache
{
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $false, Position = 0, HelpMessage = 'The path to the software cache folder')]
        [ValidateNotNullOrEmpty()]
        [System.String]$Path
    )

    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] has been replaced by [Remove-ADTContentFromCache]. Please migrate your scripts to use the new function." -Severity 2
    Remove-ADTContentFromCache @PSBoundParameters
}


#---------------------------------------------------------------------------
#
# Wrapper around Test-ADTNetworkConnection
#
#---------------------------------------------------------------------------

function Test-NetworkConnection
{
    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] has been replaced by [Test-ADTNetworkConnection]. Please migrate your scripts to use the new function." -Severity 2
    Test-ADTNetworkConnection
}


#---------------------------------------------------------------------------
#
# Wrapper around Get-ADTLoggedOnUser
#
#---------------------------------------------------------------------------

function Get-LoggedOnUser
{
    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] has been replaced by [Get-ADTLoggedOnUser]. Please migrate your scripts to use the new function." -Severity 2
    Get-ADTLoggedOnUser
}


#---------------------------------------------------------------------------
#
# Wrapper around Get-ADTIniValue
#
#---------------------------------------------------------------------------

function Get-IniValue
{
    [CmdletBinding()]
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

    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] has been replaced by [Get-ADTIniValue]. Please migrate your scripts to use the new function." -Severity 2
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
        Write-ADTLogEntry -Message "Failed to read INI file key value.`n$(Resolve-ADTError)" -Severity 3
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
    [CmdletBinding()]
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

    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] has been replaced by [Set-ADTIniValue]. Please migrate your scripts to use the new function." -Severity 2
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
        Write-ADTLogEntry -Message "Failed to write INI file key value.`n$(Resolve-ADTError)" -Severity 3
        if (!$ContinueOnError)
        {
            throw
        }
    }
}


#---------------------------------------------------------------------------
#
# Wrapper around New-ADTFolder
#
#---------------------------------------------------------------------------

function New-Folder
{
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Path,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Boolean]$ContinueOnError = $true
    )

    # Announce overall deprecation and translate $ContinueOnError to an ActionPreference before executing.
    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] has been replaced by [New-ADTFolder]. Please migrate your scripts to use the new function." -Severity 2
    if ($PSBoundParameters.ContainsKey('ContinueOnError'))
    {
        [System.Void]$PSBoundParameters.Remove('ContinueOnError')
    }
    if (!$ContinueOnError)
    {
        $PSBoundParameters.ErrorAction = [System.Management.Automation.ActionPreference]::Stop
    }
    New-ADTFolder @PSBoundParameters
}


#---------------------------------------------------------------------------
#
# Wrapper around Test-ADTPowerPoint
#
#---------------------------------------------------------------------------

function Test-PowerPoint
{
    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] has been replaced by [Test-PowerPoint]. Please migrate your scripts to use the new function." -Severity 2
    Test-ADTPowerPoint
}


#---------------------------------------------------------------------------
#
# Wrapper around Update-ADTGroupPolicy
#
#---------------------------------------------------------------------------

function Update-GroupPolicy
{
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Boolean]$ContinueOnError = $true
    )

    # Announce overall deprecation and translate $ContinueOnError to an ActionPreference before executing.
    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] has been replaced by [Update-ADTGroupPolicy]. Please migrate your scripts to use the new function." -Severity 2
    if ($PSBoundParameters.ContainsKey('ContinueOnError'))
    {
        [System.Void]$PSBoundParameters.Remove('ContinueOnError')
    }
    if (!$ContinueOnError)
    {
        $PSBoundParameters.ErrorAction = [System.Management.Automation.ActionPreference]::Stop
    }
    Update-ADTGroupPolicy @PSBoundParameters
}


#---------------------------------------------------------------------------
#
# Wrapper around Get-ADTUniversalDate
#
#---------------------------------------------------------------------------

function Get-UniversalDate
{
    [CmdletBinding()]
    param (
        [ValidateNotNullOrEmpty()]
        [System.String]$DateTime,

        [ValidateNotNullOrEmpty()]
        [System.Boolean]$ContinueOnError
    )

    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] has been replaced by [Get-ADTUniversalDate]. Please migrate your scripts to use the new function." -Severity 2
    if ($PSBoundParameters.ContainsKey('ContinueOnError'))
    {
        [System.Void]$PSBoundParameters.Remove('ContinueOnError')
    }

    try
    {
        Get-ADTUniversalDate @PSBoundParameters
    }
    catch
    {
        Write-ADTLogEntry -Message "The specified date/time [$DateTime] is not in a format recognized by the current culture [$($culture.Name)].`n$(Resolve-ADTError)" -Severity 3
        if (!$ContinueOnError)
        {
            throw
        }
    }
}


#---------------------------------------------------------------------------
#
# Wrapper around Test-ADTServiceExists
#
#---------------------------------------------------------------------------

function Test-ServiceExists
{
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Name,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$ComputerName,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$PassThru,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.SwitchParameter]$ContinueOnError = $true
    )

    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] has been replaced by [Test-ADTServiceExists]. Please migrate your scripts to use the new function." -Severity 2
    if ($PSBoundParameters.ContainsKey('ContinueOnError'))
    {
        [System.Void]$PSBoundParameters.Remove('ContinueOnError')
    }

    try
    {
        Test-ADTServiceExists @PSBoundParameters
    }
    catch
    {
        Write-ADTLogEntry -Message "The specified date/time [$DateTime] is not in a format recognized by the current culture [$($culture.Name)].`n$(Resolve-ADTError)" -Severity 3
        if (!$ContinueOnError)
        {
            throw
        }
    }
}


#---------------------------------------------------------------------------
#
# Wrapper around Disable-ADTTerminalServerInstallMode
#
#---------------------------------------------------------------------------

function Disable-TerminalServerInstallMode
{
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Boolean]$ContinueOnError = $true
    )

    # Announce overall deprecation and translate $ContinueOnError to an ActionPreference before executing.
    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] has been replaced by [Disable-ADTTerminalServerInstallMode]. Please migrate your scripts to use the new function." -Severity 2
    if ($PSBoundParameters.ContainsKey('ContinueOnError'))
    {
        [System.Void]$PSBoundParameters.Remove('ContinueOnError')
    }
    if (!$ContinueOnError)
    {
        $PSBoundParameters.ErrorAction = [System.Management.Automation.ActionPreference]::Stop
    }
    Disable-ADTTerminalServerInstallMode @PSBoundParameters
}


#---------------------------------------------------------------------------
#
# Wrapper around Disable-ADTTerminalServerInstallMode
#
#---------------------------------------------------------------------------

function Enable-TerminalServerInstallMode
{
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Boolean]$ContinueOnError = $true
    )

    # Announce overall deprecation and translate $ContinueOnError to an ActionPreference before executing.
    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] has been replaced by [Enable-ADTTerminalServerInstallMode]. Please migrate your scripts to use the new function." -Severity 2
    if ($PSBoundParameters.ContainsKey('ContinueOnError'))
    {
        [System.Void]$PSBoundParameters.Remove('ContinueOnError')
    }
    if (!$ContinueOnError)
    {
        $PSBoundParameters.ErrorAction = [System.Management.Automation.ActionPreference]::Stop
    }
    Enable-ADTTerminalServerInstallMode @PSBoundParameters
}


#---------------------------------------------------------------------------
#
# Wrapper around Add-ADTEdgeExtension and Remove-ADTEdgeExtension
#
#---------------------------------------------------------------------------

function Configure-EdgeExtension
{
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true, ParameterSetName = 'Add')]
        [System.Management.Automation.SwitchParameter]$Add,

        [Parameter(Mandatory = $true, ParameterSetName = 'Remove')]
        [System.Management.Automation.SwitchParameter]$Remove,

        [Parameter(Mandatory = $true, ParameterSetName = 'Add')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Remove')]
        [ValidateNotNullOrEmpty()]
        [System.String]$ExtensionID,

        [Parameter(Mandatory = $true, ParameterSetName = 'Add')]
        [ValidateSet('blocked', 'allowed', 'removed', 'force_installed', 'normal_installed')]
        [System.String]$InstallationMode,

        [Parameter(Mandatory = $true, ParameterSetName = 'Add')]
        [ValidateNotNullOrEmpty()]
        [System.String]$UpdateUrl,

        [Parameter(Mandatory = $false, ParameterSetName = 'Add')]
        [ValidateNotNullOrEmpty()]
        [System.String]$MinimumVersionRequired
    )

    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] has been replaced by [$($PSCmdlet.ParameterSetName)-ADTEdgeExtension]. Please migrate your scripts to use the new function." -Severity 2
    [System.Void]$PSBoundParameters.Remove($PSCmdlet.ParameterSetName)
    & "$($PSCmdlet.ParameterSetName)-ADTEdgeExtension" @PSBoundParameters
}


#---------------------------------------------------------------------------
#
# Wrapper around Resolve-ADTError
#
#---------------------------------------------------------------------------

function Resolve-Error
{
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $false, Position = 0, ValueFromPipeline = $true, ValueFromPipelineByPropertyName = $true)]
        [AllowEmptyCollection()]
        [System.Array]$ErrorRecord,

        [Parameter(Mandatory = $false, Position = 1)]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$Property,

        [Parameter(Mandatory = $false, Position = 2)]
        [System.Management.Automation.SwitchParameter]$GetErrorRecord = $true,

        [Parameter(Mandatory = $false, Position = 3)]
        [System.Management.Automation.SwitchParameter]$GetErrorInvocation = $true,

        [Parameter(Mandatory = $false, Position = 4)]
        [System.Management.Automation.SwitchParameter]$GetErrorException = $true,

        [Parameter(Mandatory = $false, Position = 5)]
        [System.Management.Automation.SwitchParameter]$GetErrorInnerException = $true
    )

    # Announce overall deprecation and translate bad switches before executing.
    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] has been replaced by [Resolve-ADTError]. Please migrate your scripts to use the new function." -Severity 2
    ('ErrorRecord', 'ErrorInvocation', 'ErrorException', 'ErrorInnerException').Where({$PSBoundParameters.ContainsKey($_)}).ForEach({
        $PSBoundParameters.Add("Exclude$_", !$PSBoundParameters["Get$_"])
        [System.Void]$PSBoundParameters.Remove("Get$_")
    })
    Resolve-ADTError @PSBoundParameters
}


#---------------------------------------------------------------------------
#
# Wrapper around Get-ADTServiceStartMode
#
#---------------------------------------------------------------------------

function Get-ServiceStartMode
{
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Name,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$ComputerName,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Boolean]$ContinueOnError = $true
    )

    # Announce overall deprecation and translate $ContinueOnError to an ActionPreference before executing.
    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] has been replaced by [Get-ADTServiceStartMode]. Please migrate your scripts to use the new function." -Severity 2
    if ($PSBoundParameters.ContainsKey('ContinueOnError'))
    {
        [System.Void]$PSBoundParameters.Remove('ContinueOnError')
    }
    if (!$ContinueOnError)
    {
        $PSBoundParameters.ErrorAction = [System.Management.Automation.ActionPreference]::Stop
    }
    Get-ADTServiceStartMode @PSBoundParameters
}


#---------------------------------------------------------------------------
#
# Wrapper around Get-ADTServiceStartMode
#
#---------------------------------------------------------------------------

function Set-ServiceStartMode
{
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Name,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$StartMode,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Boolean]$ContinueOnError = $true
    )

    # Announce overall deprecation and translate $ContinueOnError to an ActionPreference before executing.
    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] has been replaced by [Set-ADTServiceStartMode]. Please migrate your scripts to use the new function." -Severity 2
    if ($PSBoundParameters.ContainsKey('ContinueOnError'))
    {
        [System.Void]$PSBoundParameters.Remove('ContinueOnError')
    }
    if (!$ContinueOnError)
    {
        $PSBoundParameters.ErrorAction = [System.Management.Automation.ActionPreference]::Stop
    }
    Set-ADTServiceStartMode @PSBoundParameters
}


#---------------------------------------------------------------------------
#
# Wrapper around Start-ADTProcess
#
#---------------------------------------------------------------------------

function Execute-Process
{
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [Alias('FilePath')]
        [ValidateNotNullorEmpty()]
        [System.String]$Path,

        [Parameter(Mandatory = $false)]
        [Alias('Arguments')]
        [ValidateNotNullorEmpty()]
        [System.String[]]$Parameters,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$SecureParameters,

        [Parameter(Mandatory = $false)]
        [ValidateSet('Normal', 'Hidden', 'Maximized', 'Minimized')]
        [System.Diagnostics.ProcessWindowStyle]$WindowStyle,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [System.Management.Automation.SwitchParameter]$CreateNoWindow,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [System.Management.Automation.SwitchParameter]$WorkingDirectory,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$NoWait,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$PassThru,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$WaitForMsiExec,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [System.Int32]$MsiExecWaitTime = (Get-ADTConfig).MSI.MutexWaitTime,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [System.String]$IgnoreExitCodes,

        [Parameter(Mandatory = $false)]
        [ValidateSet('Idle', 'Normal', 'High', 'AboveNormal', 'BelowNormal', 'RealTime')]
        [System.Diagnostics.ProcessPriorityClass]$PriorityClass,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [System.Boolean]$ExitOnProcessFailure = $true,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [System.Boolean]$UseShellExecute,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [System.Boolean]$ContinueOnError
    )

    # Announce deprecation of this function.
    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] has been replaced by [Start-ADTProcess]. Please migrate your scripts to use the new function." -Severity 2

    # Convert out changed parameters.
    if ($PSBoundParameters.ContainsKey('IgnoreExitCodes'))
    {
        $PSBoundParameters.IgnoreExitCodes = $IgnoreExitCodes.Split(',')
    }
    if ($PSBoundParameters.ContainsKey('ExitOnProcessFailure'))
    {
        $PSBoundParameters.NoExitOnProcessFailure = !$PSBoundParameters.ExitOnProcessFailure
        [System.Void]$PSBoundParameters.Remove('ExitOnProcessFailure')
    }
    if ($PSBoundParameters.ContainsKey('ContinueOnError'))
    {
        $PSBoundParameters.ErrorAction = if ($ContinueOnError) {[System.Management.Automation.ActionPreference]::Continue} else {[System.Management.Automation.ActionPreference]::Stop}
        [System.Void]$PSBoundParameters.Remove('ContinueOnError')
    }

    # Invoke function with amended parameters.
    Start-ADTProcess @PSBoundParameters
}


#---------------------------------------------------------------------------
#
# Wrapper around Start-ADTMsiProcess
#
#---------------------------------------------------------------------------

function Execute-MSI
{
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $false)]
        [ValidateSet('Install', 'Uninstall', 'Patch', 'Repair', 'ActiveSetup')]
        [System.String]$Action,

        [Parameter(Mandatory = $true, HelpMessage = 'Please enter either the path to the MSI/MSP file or the ProductCode')]
        [ValidateScript({($_ -match (Get-ADTEnvironment).MSIProductCodeRegExPattern) -or ('.msi', '.msp' -contains [System.IO.Path]::GetExtension($_))})]
        [Alias('FilePath')]
        [System.String]$Path,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Transform,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Alias('Arguments')]
        [System.String]$Parameters,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$AddParameters,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.SwitchParameter]$SecureParameters,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Patch,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$LoggingOptions,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$LogName,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$WorkingDirectory,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$SkipMSIAlreadyInstalledCheck,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$IncludeUpdatesAndHotfixes,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$NoWait,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$PassThru,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$IgnoreExitCodes,

        [Parameter(Mandatory = $false)]
        [ValidateSet('Idle', 'Normal', 'High', 'AboveNormal', 'BelowNormal', 'RealTime')]
        [Diagnostics.ProcessPriorityClass]$PriorityClass,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Boolean]$ExitOnProcessFailure = $true,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Boolean]$RepairFromSource,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Boolean]$ContinueOnError
    )

    # Announce deprecation of this function.
    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] has been replaced by [Start-ADTMsiProcess]. Please migrate your scripts to use the new function." -Severity 2

    # Convert out changed parameters.
    if ($PSBoundParameters.ContainsKey('IgnoreExitCodes'))
    {
        $PSBoundParameters.IgnoreExitCodes = $IgnoreExitCodes.Split(',')
    }
    if ($PSBoundParameters.ContainsKey('ExitOnProcessFailure'))
    {
        $PSBoundParameters.NoExitOnProcessFailure = !$PSBoundParameters.ExitOnProcessFailure
        [System.Void]$PSBoundParameters.Remove('ExitOnProcessFailure')
    }
    if ($PSBoundParameters.ContainsKey('ContinueOnError'))
    {
        $PSBoundParameters.ErrorAction = if ($ContinueOnError) {[System.Management.Automation.ActionPreference]::Continue} else {[System.Management.Automation.ActionPreference]::Stop}
        [System.Void]$PSBoundParameters.Remove('ContinueOnError')
    }

    # Invoke function with amended parameters.
    Start-ADTMsiProcess @PSBoundParameters
}


#---------------------------------------------------------------------------
#
# Wrapper around Start-ADTMspProcess
#
#---------------------------------------------------------------------------

function Execute-MSP
{
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true, HelpMessage = 'Please enter the path to the MSP file')]
        [ValidateScript({('.msp' -contains [System.IO.Path]::GetExtension($_))})]
        [Alias('FilePath')]
        [System.String]$Path,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [System.String]$AddParameters
    )

    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] has been replaced by [Start-ADTMspProcess]. Please migrate your scripts to use the new function." -Severity 2
    Start-ADTMspProcess @PSBoundParameters
}


#---------------------------------------------------------------------------
#
# Wrapper around Unblock-ADTAppExecution
#
#---------------------------------------------------------------------------

function Unblock-AppExecution
{
    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] has been replaced by [Unblock-ADTAppExecution]. Please migrate your scripts to use the new function." -Severity 2
    Unblock-ADTAppExecution
}


#---------------------------------------------------------------------------
#
# Wrapper around Block-ADTAppExecution
#
#---------------------------------------------------------------------------

function Block-AppExecution
{
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true, HelpMessage = 'Specify process names, separated by commas.')]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$ProcessName
    )

    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] has been replaced by [Block-ADTAppExecution]. Please migrate your scripts to use the new function." -Severity 2
    Block-ADTAppExecution @PSBoundParameters
}

#---------------------------------------------------------------------------
#
# Wrapper around Test-ADTRegistryValue
#
#---------------------------------------------------------------------------

function Test-RegistryValue
{
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true, Position = 0, ValueFromPipeline = $true, ValueFromPipelineByPropertyName = $true)]
        [ValidateNotNullOrEmpty()]
        $Key,

        [Parameter(Mandatory = $true, Position = 1)]
        [ValidateNotNullOrEmpty()]
        $Value,

        [Parameter(Mandatory = $false, Position = 2)]
        [ValidateNotNullorEmpty()]
        [System.String]$SID,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$Wow6432Node
    )

    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] has been replaced by [Test-ADTRegistryValue]. Please migrate your scripts to use the new function." -Severity 2
    Test-ADTRegistryValue @PSBoundParameters
}


#---------------------------------------------------------------------------
#
# Wrapper around Convert-ADTRegistryPath
#
#---------------------------------------------------------------------------

function Convert-RegistryPath
{
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Key,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$Wow6432Node,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$SID,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [System.Boolean]$DisableFunctionLogging = $true
    )

    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] has been replaced by [Convert-ADTRegistryPath]. Please migrate your scripts to use the new function." -Severity 2
    if (!$DisableFunctionLogging)
    {
        $PSBoundParameters.Logging = [System.Management.Automation.SwitchParameter]$true
    }
    Convert-ADTRegistryPath @PSBoundParameters
}


#---------------------------------------------------------------------------
#
# Wrapper around Test-ADTMSUpdates
#
#---------------------------------------------------------------------------

function Test-MSUpdates
{
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true, Position = 0, HelpMessage = 'Enter the KB Number for the Microsoft Update')]
        [ValidateNotNullOrEmpty()]
        [System.String]$KbNumber,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Boolean]$ContinueOnError = $true
    )

    # Announce overall deprecation and translate $ContinueOnError to an ActionPreference before executing.
    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] has been replaced by [Test-ADTMSUpdates]. Please migrate your scripts to use the new function." -Severity 2
    if ($PSBoundParameters.ContainsKey('ContinueOnError'))
    {
        [System.Void]$PSBoundParameters.Remove('ContinueOnError')
    }
    if (!$ContinueOnError)
    {
        $PSBoundParameters.ErrorAction = [System.Management.Automation.ActionPreference]::Stop
    }
    Test-ADTMSUpdates @PSBoundParameters
}


#---------------------------------------------------------------------------
#
# Wrapper around Test-ADTBattery
#
#---------------------------------------------------------------------------

function Test-Battery
{
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$PassThru
    )

    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] has been replaced by [Test-ADTBattery]. Please migrate your scripts to use the new function." -Severity 2
    Test-ADTBattery @PSBoundParameters
}


#---------------------------------------------------------------------------
#
# Wrapper around Start-ADTServiceAndDependencies
#
#---------------------------------------------------------------------------

function Start-ServiceAndDependencies
{
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Name,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$SkipServiceExistsTest,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$SkipDependentServices,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.TimeSpan]$PendingStatusWait,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$PassThru,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Boolean]$ContinueOnError = $true
    )

    # Announce overall deprecation and translate $ContinueOnError to an ActionPreference before executing.
    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] has been replaced by [Start-ADTServiceAndDependencies]. Please migrate your scripts to use the new function." -Severity 2
    $PSBoundParameters.Service = $Name
    [System.Void]$PSBoundParameters.Remove('Name')
    if ($SkipServiceExistsTest)
    {
        Write-ADTLogEntry -Message "The parameter '-SkipServiceExistsTest' is discontinued and no longer has any effect." -Severity 2 -Source $MyInvocation.MyCommand.Name
        [System.Void]$PSBoundParameters.Remove('SkipServiceExistsTest')
    }
    if ($PSBoundParameters.ContainsKey('ContinueOnError'))
    {
        [System.Void]$PSBoundParameters.Remove('ContinueOnError')
    }
    if (!$ContinueOnError)
    {
        $PSBoundParameters.ErrorAction = [System.Management.Automation.ActionPreference]::Stop
    }
    Start-ADTServiceAndDependencies @PSBoundParameters
}


#---------------------------------------------------------------------------
#
# Wrapper around Stop-ADTServiceAndDependencies
#
#---------------------------------------------------------------------------

function Stop-ServiceAndDependencies
{
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Name,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$SkipServiceExistsTest,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$SkipDependentServices,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.TimeSpan]$PendingStatusWait,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$PassThru,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Boolean]$ContinueOnError = $true
    )

    # Announce overall deprecation and translate $ContinueOnError to an ActionPreference before executing.
    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] has been replaced by [Stop-ADTServiceAndDependencies]. Please migrate your scripts to use the new function." -Severity 2
    $PSBoundParameters.Service = $Name
    [System.Void]$PSBoundParameters.Remove('Name')
    if ($SkipServiceExistsTest)
    {
        Write-ADTLogEntry -Message "The parameter '-SkipServiceExistsTest' is discontinued and no longer has any effect." -Severity 2 -Source $MyInvocation.MyCommand.Name
        [System.Void]$PSBoundParameters.Remove('SkipServiceExistsTest')
    }
    if ($PSBoundParameters.ContainsKey('ContinueOnError'))
    {
        [System.Void]$PSBoundParameters.Remove('ContinueOnError')
    }
    if (!$ContinueOnError)
    {
        $PSBoundParameters.ErrorAction = [System.Management.Automation.ActionPreference]::Stop
    }
    Stop-ADTServiceAndDependencies @PSBoundParameters
}


#---------------------------------------------------------------------------
#
# Wrapper around Set-ADTRegistryKey
#
#---------------------------------------------------------------------------

function Set-RegistryKey
{
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Key,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Name,

        [Parameter(Mandatory = $false)]
        [System.Object]$Value,

        [Parameter(Mandatory = $false)]
        [ValidateSet('Binary', 'DWord', 'ExpandString', 'MultiString', 'None', 'QWord', 'String', 'Unknown')]
        [Microsoft.Win32.RegistryValueKind]$Type,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$Wow6432Node,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$SID,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Boolean]$ContinueOnError = $true
    )

    # Announce overall deprecation and translate $ContinueOnError to an ActionPreference before executing.
    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] has been replaced by [Set-ADTRegistryKey]. Please migrate your scripts to use the new function." -Severity 2
    if ($PSBoundParameters.ContainsKey('ContinueOnError'))
    {
        [System.Void]$PSBoundParameters.Remove('ContinueOnError')
    }
    if (!$ContinueOnError)
    {
        $PSBoundParameters.ErrorAction = [System.Management.Automation.ActionPreference]::Stop
    }
    Set-ADTRegistryKey @PSBoundParameters
}


#---------------------------------------------------------------------------
#
# Wrapper around Remove-ADTRegistryKey
#
#---------------------------------------------------------------------------

function Remove-RegistryKey
{
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Key,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Name,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$Recurse,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$SID,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Boolean]$ContinueOnError = $true
    )

    # Announce overall deprecation and translate $ContinueOnError to an ActionPreference before executing.
    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] has been replaced by [Remove-ADTRegistryKey]. Please migrate your scripts to use the new function." -Severity 2
    if ($PSBoundParameters.ContainsKey('ContinueOnError'))
    {
        [System.Void]$PSBoundParameters.Remove('ContinueOnError')
    }
    if (!$ContinueOnError)
    {
        $PSBoundParameters.ErrorAction = [System.Management.Automation.ActionPreference]::Stop
    }
    Remove-ADTRegistryKey @PSBoundParameters
}


#---------------------------------------------------------------------------
#
# Wrapper around Remove-ADTFileFromUserProfiles
#
#---------------------------------------------------------------------------

function Remove-FileFromUserProfiles
{
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true, Position = 0, ParameterSetName = 'Path')]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$Path,

        [Parameter(Mandatory = $true, Position = 0, ParameterSetName = 'LiteralPath')]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$LiteralPath,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$Recurse,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$ExcludeNTAccount,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Boolean]$ExcludeSystemProfiles = $true,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Boolean]$ExcludeServiceProfiles = $true,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$ExcludeDefaultUser,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Boolean]$ContinueOnError = $true
    )

    # Translate parameters.
    ('SystemProfiles', 'ServiceProfiles').Where({$PSBoundParameters.ContainsKey("Exclude$_")}).ForEach({
        if (!$PSBoundParameters["Exclude$_"])
        {
            $PSBoundParameters.Add("Include$_", [System.Management.Automation.SwitchParameter]$true)
        }
        [System.Void]$PSBoundParameters.Remove("Exclude$_")
    })
    if ($PSBoundParameters.ContainsKey('ContinueOnError'))
    {
        Write-ADTLogEntry -Message "The parameter '-ContinueOnError' is discontinued and no longer has any effect." -Severity 2 -Source $MyInvocation.MyCommand.Name
        [System.Void]$PSBoundParameters.Remove('ContinueOnError')
    }

    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] has been replaced by [Remove-ADTFileFromUserProfiles]. Please migrate your scripts to use the new function." -Severity 2
    Remove-ADTFileFromUserProfiles @PSBoundParameters
}


#---------------------------------------------------------------------------
#
# Wrapper around Get-ADTRegistryKey
#
#---------------------------------------------------------------------------

function Get-RegistryKey
{
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Key,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Value,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$Wow6432Node,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$SID,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.SwitchParameter]$ReturnEmptyKeyIfExists,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.SwitchParameter]$DoNotExpandEnvironmentNames,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Boolean]$ContinueOnError = $true
    )

    # Announce overall deprecation and translate $ContinueOnError to an ActionPreference before executing.
    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] has been replaced by [Get-ADTRegistryKey]. Please migrate your scripts to use the new function." -Severity 2
    if ($PSBoundParameters.ContainsKey('ContinueOnError'))
    {
        [System.Void]$PSBoundParameters.Remove('ContinueOnError')
    }
    if (!$ContinueOnError)
    {
        $PSBoundParameters.ErrorAction = [System.Management.Automation.ActionPreference]::Stop
    }
    Get-ADTRegistryKey @PSBoundParameters
}
