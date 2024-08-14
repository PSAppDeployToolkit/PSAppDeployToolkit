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

<#[CmdletBinding()]
Param (
    ## Script Parameters: These parameters are passed to the script when it is called externally from a scheduled task or because of an Image File Execution Options registry setting
    [Switch]$ShowInstallationPrompt = $false,
    [Switch]$ShowInstallationRestartPrompt = $false,
    [Switch]$CleanupBlockedApps = $false,
    [Switch]$ShowBlockedAppDialog = $false,
    [Switch]$DisableLogging = $false,
    [String]$ReferredInstallName = '',
    [String]$ReferredInstallTitle = '',
    [String]$ReferredLogName = '',
    [String]$Title = '',
    [String]$Message = '',
    [String]$MessageAlignment = '',
    [String]$ButtonRightText = '',
    [String]$ButtonLeftText = '',
    [String]$ButtonMiddleText = '',
    [String]$Icon = '',
    [String]$Timeout = '',
    [Switch]$ExitOnTimeout = $false,
    [Boolean]$MinimizeWindows = $false,
    [Switch]$PersistPrompt = $false,
    [Int32]$CountdownSeconds = 60,
    [Int32]$CountdownNoHideSeconds = 30,
    [Switch]$NoCountdown = $false,
    [Switch]$AsyncToolkitLaunch = $false,
    [Boolean]$TopMost = $true
)#>

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
$sessionParams = @{
    Cmdlet = $PSCmdlet
    AppVendor = $(if (Test-Path -LiteralPath 'Variable:AppVendor') {$AppVendor})
    AppName = $(if (Test-Path -LiteralPath 'Variable:AppName') {$AppName})
    AppVersion = $(if (Test-Path -LiteralPath 'Variable:AppVersion') {$AppVersion})
    AppArch = $(if (Test-Path -LiteralPath 'Variable:AppArch') {$AppArch})
    AppLang = $(if (Test-Path -LiteralPath 'Variable:AppLang') {$AppLang})
    AppRevision = $(if (Test-Path -LiteralPath 'Variable:AppRevision') {$AppRevision})
    AppExitCodes = $(if (Test-Path -LiteralPath 'Variable:AppExitCodes') {$AppExitCodes})
    AppScriptVersion = $(if (Test-Path -LiteralPath 'Variable:AppScriptVersion') {$AppScriptVersion})
    AppScriptDate = $(if (Test-Path -LiteralPath 'Variable:AppScriptDate') {$AppScriptDate})
    AppScriptAuthor = $(if (Test-Path -LiteralPath 'Variable:AppScriptAuthor') {$AppScriptAuthor})
    InstallName = $(if (Test-Path -LiteralPath 'Variable:InstallName') {$InstallName})
    InstallTitle = $(if (Test-Path -LiteralPath 'Variable:InstallTitle') {$InstallTitle})
    DeployAppScriptFriendlyName = $(if (Test-Path -LiteralPath 'Variable:DeployAppScriptFriendlyName') {$DeployAppScriptFriendlyName})
    DeployAppScriptVersion = $(if (Test-Path -LiteralPath 'Variable:DeployAppScriptVersion') {$DeployAppScriptVersion})
    DeployAppScriptDate = $(if (Test-Path -LiteralPath 'Variable:DeployAppScriptDate') {$DeployAppScriptDate})
    DeployAppScriptParameters = $(if (Test-Path -LiteralPath 'Variable:DeployAppScriptParameters') {$DeployAppScriptParameters})
}
Open-ADTSession @PSBoundParameters @sessionParams


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

<#
##*=============================================
##* SCRIPT BODY
##*=============================================
#region ScriptBody

## If the ShowInstallationPrompt Parameter is specified, only call that function.
If ($showInstallationPrompt) {
    Write-ADTLogEntry -Message "[$appDeployMainScriptFriendlyName] called with switch [-ShowInstallationPrompt]." -Source $Script:ADT.Environment.appDeployToolkitName
    $appDeployMainScriptAsyncParameters.Remove('ShowInstallationPrompt')
    $appDeployMainScriptAsyncParameters.Remove('AsyncToolkitLaunch')
    $appDeployMainScriptAsyncParameters.Remove('ReferredInstallName')
    $appDeployMainScriptAsyncParameters.Remove('ReferredInstallTitle')
    $appDeployMainScriptAsyncParameters.Remove('ReferredLogName')
    Show-InstallationPrompt @appDeployMainScriptAsyncParameters
    Exit 0
}

## If the ShowInstallationRestartPrompt Parameter is specified, only call that function.
If ($showInstallationRestartPrompt) {
    Write-ADTLogEntry -Message "[$appDeployMainScriptFriendlyName] called with switch [-ShowInstallationRestartPrompt]." -Source $Script:ADT.Environment.appDeployToolkitName
    $appDeployMainScriptAsyncParameters.Remove('ShowInstallationRestartPrompt')
    $appDeployMainScriptAsyncParameters.Remove('AsyncToolkitLaunch')
    $appDeployMainScriptAsyncParameters.Remove('ReferredInstallName')
    $appDeployMainScriptAsyncParameters.Remove('ReferredInstallTitle')
    $appDeployMainScriptAsyncParameters.Remove('ReferredLogName')
    Show-InstallationRestartPrompt @appDeployMainScriptAsyncParameters
    Exit 0
}

## If the CleanupBlockedApps Parameter is specified, only call that function.
If ($cleanupBlockedApps) {
    $deployModeSilent = $true
    Write-ADTLogEntry -Message "[$appDeployMainScriptFriendlyName] called with switch [-CleanupBlockedApps]." -Source $Script:ADT.Environment.appDeployToolkitName
    Unblock-AppExecution
    Exit 0
}

## If the ShowBlockedAppDialog Parameter is specified, only call that function.
If ($showBlockedAppDialog) {
    Try {
        . $DisableScriptLogging
        Write-ADTLogEntry -Message "[$appDeployMainScriptFriendlyName] called with switch [-ShowBlockedAppDialog]." -Source $Script:ADT.Environment.appDeployToolkitName
        #  Create a mutex and specify a name without acquiring a lock on the mutex
        [Boolean]$showBlockedAppDialogMutexLocked = $false
        [String]$showBlockedAppDialogMutexName = 'Global\PSADT_ShowBlockedAppDialog_Message'
        [Threading.Mutex]$showBlockedAppDialogMutex = New-Object -TypeName 'System.Threading.Mutex' -ArgumentList ($false, $showBlockedAppDialogMutexName)
        #  Attempt to acquire an exclusive lock on the mutex, attempt will fail after 1 millisecond if unable to acquire exclusive lock
        If ((Test-IsMutexAvailable -MutexName $showBlockedAppDialogMutexName -MutexWaitTimeInMilliseconds 1) -and ($showBlockedAppDialogMutex.WaitOne(1))) {
            [Boolean]$showBlockedAppDialogMutexLocked = $true
            Show-InstallationPrompt -Title $installTitle -Message $Script:ADT.Strings.BlockExecution.Message -Icon 'Warning' -ButtonRightText 'OK'
            Exit 0
        }
        Else {
            #  If attempt to acquire an exclusive lock on the mutex failed, then exit script as another blocked app dialog window is already open
            Write-ADTLogEntry -Message "Unable to acquire an exclusive lock on mutex [$showBlockedAppDialogMutexName] because another blocked application dialog window is already open. Exiting script..." -Severity 2 -Source $Script:ADT.Environment.appDeployToolkitName
            Exit 0
        }
    }
    Catch {
        Write-ADTLogEntry -Message "There was an error in displaying the Installation Prompt. `r`n$(Resolve-Error)" -Severity 3 -Source $Script:ADT.Environment.appDeployToolkitName
        Exit 60005
    }
    Finally {
        If ($showBlockedAppDialogMutexLocked) {
            $null = $showBlockedAppDialogMutex.ReleaseMutex()
        }
        If ($showBlockedAppDialogMutex) {
            $showBlockedAppDialogMutex.Close()
        }
    }
}

#endregion
##*=============================================
##* END SCRIPT BODY
##*=============================================
#>
