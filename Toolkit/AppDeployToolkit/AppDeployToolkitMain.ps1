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
New-ADTSession @PSBoundParameters @sessionParams

<#
## Variables: App Deploy Script Dependency Files
[String]$appDeployRunHiddenVbsFile = Join-Path -Path $scriptRoot -ChildPath 'RunHidden.vbs'

#  App Deploy Optional Extensions File
[String]$appDeployToolkitDotSourceExtensions = 'AppDeployToolkitExtensions.ps1'

## Variables: Reset/Remove Variables
[Boolean]$instProgressRunning = $false

## Variables: Resolve Parameters. For use in a pipeline
filter Resolve-Parameters {
    <#
.SYNOPSIS

Resolve the parameters of a function call to a string.

.DESCRIPTION

Resolve the parameters of a function call to a string.

.PARAMETER Parameter

The name of the function this function is invoked from.

.INPUTS

System.Object

.OUTPUTS

System.Object

.EXAMPLE

Resolve-Parameters -Parameter $PSBoundParameters | Out-String

.NOTES

This is an internal script function and should typically not be called directly.

.LINK

https://psappdeploytoolkit.com
#>
    Param (
        [Parameter(Mandatory = $true, Position = 0, ValueFromPipeline = $true, ValueFromPipelineByPropertyName = $true)]
        [ValidateNotNullOrEmpty()]$Parameter
    )

    Switch ($Parameter) {
        {$_.Value -is [System.Management.Automation.SwitchParameter]} {
            "-$($_.Key):`$$($_.Value.ToString().ToLower())"
            break
        }
        {$_.Value -is [System.Boolean]} {
            "-$($_.Key):`$$($_.Value.ToString().ToLower())"
            break
        }
        {$_.Value -is [System.Int16]} {
            "-$($_.Key):$($_.Value)"
            break
        }
        {$_.Value -is [System.Int32]} {
            "-$($_.Key):$($_.Value)"
            break
        }
        {$_.Value -is [System.Int64]} {
            "-$($_.Key):$($_.Value)"
            break
        }
        {$_.Value -is [System.UInt16]} {
            "-$($_.Key):$($_.Value)"
            break
        }
        {$_.Value -is [System.UInt32]} {
            "-$($_.Key):$($_.Value)"
            break
        }
        {$_.Value -is [System.UInt64]} {
            "-$($_.Key):$($_.Value)"
            break
        }
        {$_.Value -is [System.Single]} {
            "-$($_.Key):$($_.Value)"
            break
        }
        {$_.Value -is [System.Double]} {
            "-$($_.Key):$($_.Value)"
            break
        }
        {$_.Value -is [System.Decimal]} {
            "-$($_.Key):$($_.Value)"
            break
        }
        {$_.Value -is [System.Collections.IDictionary]} {
            "-$($_.Key):'$(($_.Value.GetEnumerator() | Resolve-Parameters).Replace("'",'"') -join "', '")'"
            break
        }
        {$_.Value -is [System.Collections.IEnumerable]} {
            "-$($_.Key):'$($_.Value -join "', '")'"
            break
        }
        default {
            "-$($_.Key):'$($_.Value)'"
            break
        }
    }
}
#endregion
##*=============================================
##* END VARIABLE DECLARATION
##*=============================================

##*=============================================
##* SCRIPT BODY
##*=============================================
#region ScriptBody

## If the script was invoked by the Help Console, exit the script now
If ($invokingScript) {
    If ((Split-Path -Path $invokingScript -Leaf) -eq 'AppDeployToolkitHelp.ps1') {
        Return
    }
}

## Define ScriptBlocks to disable/revert script logging
[ScriptBlock]$DisableScriptLogging = { $OldDisableLoggingValue = $DisableLogging ; $DisableLogging = $true }
[ScriptBlock]$RevertScriptLogging = { $DisableLogging = $OldDisableLoggingValue }

## Disable logging until log file details are available
. $DisableScriptLogging

## Revert script logging to original setting
. $RevertScriptLogging

## Check how the script was invoked
If ($invokingScript) {
    Write-Log -Message "Script [$scriptPath] dot-source invoked by [$invokingScript]" -Source $Script:ADT.Environment.appDeployToolkitName
}
Else {
    Write-Log -Message "Script [$scriptPath] invoked directly" -Source $Script:ADT.Environment.appDeployToolkitName
}

## Evaluate non-default parameters passed to the scripts
If (Test-Path -LiteralPath 'variable:deployAppScriptParameters') {
    [String]$deployAppScriptParameters = ($deployAppScriptParameters.GetEnumerator() | Resolve-Parameters) -join ' '
}
#  Save main script parameters hashtable for async execution of the toolkit
[Hashtable]$appDeployMainScriptAsyncParameters = $appDeployMainScriptParameters
If ($appDeployMainScriptParameters) {
    [String]$appDeployMainScriptParameters = ($appDeployMainScriptParameters.GetEnumerator() | Resolve-Parameters) -join ' '
}
If ($appDeployExtScriptParameters) {
    [String]$appDeployExtScriptParameters = ($appDeployExtScriptParameters.GetEnumerator() | Resolve-Parameters) -join ' '
}

## Set the install phase to asynchronous if the script was not dot sourced, i.e. called with parameters
If ($AsyncToolkitLaunch) {
    $installPhase = 'Asynchronous'
}

## If the ShowInstallationPrompt Parameter is specified, only call that function.
If ($showInstallationPrompt) {
    Write-Log -Message "[$appDeployMainScriptFriendlyName] called with switch [-ShowInstallationPrompt]." -Source $Script:ADT.Environment.appDeployToolkitName
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
    Write-Log -Message "[$appDeployMainScriptFriendlyName] called with switch [-ShowInstallationRestartPrompt]." -Source $Script:ADT.Environment.appDeployToolkitName
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
    Write-Log -Message "[$appDeployMainScriptFriendlyName] called with switch [-CleanupBlockedApps]." -Source $Script:ADT.Environment.appDeployToolkitName
    Unblock-AppExecution
    Exit 0
}

## If the ShowBlockedAppDialog Parameter is specified, only call that function.
If ($showBlockedAppDialog) {
    Try {
        . $DisableScriptLogging
        Write-Log -Message "[$appDeployMainScriptFriendlyName] called with switch [-ShowBlockedAppDialog]." -Source $Script:ADT.Environment.appDeployToolkitName
        #  Create a mutex and specify a name without acquiring a lock on the mutex
        [Boolean]$showBlockedAppDialogMutexLocked = $false
        [String]$showBlockedAppDialogMutexName = 'Global\PSADT_ShowBlockedAppDialog_Message'
        [Threading.Mutex]$showBlockedAppDialogMutex = New-Object -TypeName 'System.Threading.Mutex' -ArgumentList ($false, $showBlockedAppDialogMutexName)
        #  Attempt to acquire an exclusive lock on the mutex, attempt will fail after 1 millisecond if unable to acquire exclusive lock
        If ((Test-IsMutexAvailable -MutexName $showBlockedAppDialogMutexName -MutexWaitTimeInMilliseconds 1) -and ($showBlockedAppDialogMutex.WaitOne(1))) {
            [Boolean]$showBlockedAppDialogMutexLocked = $true
            Show-InstallationPrompt -Title $installTitle -Message $Script:ADT.Strings.BlockExecution_Message -Icon 'Warning' -ButtonRightText 'OK'
            Exit 0
        }
        Else {
            #  If attempt to acquire an exclusive lock on the mutex failed, then exit script as another blocked app dialog window is already open
            Write-Log -Message "Unable to acquire an exclusive lock on mutex [$showBlockedAppDialogMutexName] because another blocked application dialog window is already open. Exiting script..." -Severity 2 -Source $Script:ADT.Environment.appDeployToolkitName
            Exit 0
        }
    }
    Catch {
        Write-Log -Message "There was an error in displaying the Installation Prompt. `r`n$(Resolve-Error)" -Severity 3 -Source $Script:ADT.Environment.appDeployToolkitName
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
