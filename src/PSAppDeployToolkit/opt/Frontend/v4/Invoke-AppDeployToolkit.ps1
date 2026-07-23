<#

.SYNOPSIS
This script invokes a PSAppDeployToolkit deployment.

.DESCRIPTION
The script imports the PSAppDeployToolkit module and executes the specified install/uninstall/repair scriptblocks.

.PARAMETER DeploymentType
The type of deployment to perform, Install, Uninstall, or Repair. Default is Install.

.PARAMETER DeployMode
Specifies whether the installation should be run in Interactive (shows dialogs), Silent (no dialogs), NonInteractive (dialogs without prompts) mode, or Auto (shows dialogs if a user is logged on, device is not in the OOBE, and there's no running apps to close).

Silent mode is automatically set if no users are logged on, the device is in Autopilot mode, or there's specified processes to close that are currently running.

.PARAMETER SuppressRebootPassThru
Prevents the toolkit from exiting with a defined reboot exit code (e.g. 3010), returning 0 instead.

.NOTES
For more details on all available properties you can add to `$adtSession`, please visit https://psappdeploytoolkit.com/docs/reference/adtsession-object.

.EXAMPLE
& .\Invoke-AppDeployToolkit.ps1 -DeploymentType Install -DeployMode Silent

Invokes this script directly within PowerShell.

.EXAMPLE
& .\Invoke-AppDeployToolkit.exe -DeploymentType Install -DeployMode Silent

Invokes this script via our supplied executable.

#>

[CmdletBinding()]
param
(
    # Default is 'Install'.
    [Parameter(Mandatory = $false)]
    [ValidateSet('Install', 'Uninstall', 'Repair')]
    [System.String]$DeploymentType,

    # Default is 'Auto'. Don't hard-code this unless required.
    [Parameter(Mandatory = $false)]
    [ValidateSet('Auto', 'Interactive', 'NonInteractive', 'Silent')]
    [System.String]$DeployMode,

    [Parameter(Mandatory = $false)]
    [System.Management.Automation.SwitchParameter]$SuppressRebootPassThru
)

## MARK: Variables
$adtSession = @{
    AppVendor = ''
    AppName = ''
    AppVersion = ''
    AppArch = ''
    AppLang = 'EN'
    AppRevision = '01'
    AppSuccessExitCodes = @(0)
    AppRebootExitCodes = @(1641, 3010)
    AppProcessesToClose = @()  # Example: @('excel', @{ Name = 'winword'; Description = 'Microsoft Word' })

    AppScriptVersion = '1.0.0'
    AppScriptDate = '2000-12-31'
    AppScriptAuthor = '<author name>'

    DeployAppScriptFriendlyName = $MyInvocation.MyCommand.Name
    DeployAppScriptParameters = $PSBoundParameters
    DeployAppScriptVersion = '4.2.0'
}

## MARK: Pre-Install
New-Variable -Name Pre-Install -Force -Value {
    $saiwParams = @{
        AllowDefer = $true
        DeferTimes = 3
        CheckDiskSpace = $true
        PersistPrompt = $true
    }
    if ($adtSession.AppProcessesToClose.Count -gt 0)
    {
        $saiwParams.Add('CloseProcesses', $adtSession.AppProcessesToClose)
    }
    Show-ADTInstallationWelcome @saiwParams
    Show-ADTInstallationProgress
}

## MARK: Install
New-Variable -Name Install -Force -Value {
}

## MARK: Post-Install
New-Variable -Name Post-Install -Force -Value {
    Show-ADTInstallationPrompt -Message "$($adtSession.DeploymentType) complete." -ButtonRightText 'OK' -NoWait
}

## MARK: Pre-Uninstall
New-Variable -Name Pre-Uninstall -Force -Value {
    if ($adtSession.AppProcessesToClose.Count -gt 0)
    {
        Show-ADTInstallationWelcome -CloseProcesses $adtSession.AppProcessesToClose -CloseProcessesCountdown 60
    }
    Show-ADTInstallationProgress
}

## MARK: Uninstall
New-Variable -Name Uninstall -Force -Value {
}

## MARK: Post-Uninstall
New-Variable -Name Post-Uninstall -Force -Value {
}

## MARK: Pre-Repair
New-Variable -Name Pre-Repair -Force -Value {
    if ($adtSession.AppProcessesToClose.Count -gt 0)
    {
        Show-ADTInstallationWelcome -CloseProcesses $adtSession.AppProcessesToClose -CloseProcessesCountdown 60
    }
    Show-ADTInstallationProgress
}

## MARK: Repair
New-Variable -Name Repair -Force -Value {
}

## MARK: Post-Repair
New-Variable -Name Post-Repair -Force -Value {
    Show-ADTInstallationPrompt -Message "$($adtSession.DeploymentType) complete." -ButtonRightText 'OK' -NoWait
}

## MARK: Initialization
$ErrorActionPreference = [System.Management.Automation.ActionPreference]::Stop
$ProgressPreference = [System.Management.Automation.ActionPreference]::SilentlyContinue
Set-StrictMode -Version 1
try
{
    if (Test-Path -LiteralPath "$PSScriptRoot\..\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -PathType Leaf)
    {
        Get-ChildItem -LiteralPath "$PSScriptRoot\..\..\..\..\PSAppDeployToolkit" -Recurse -File | Unblock-File -ErrorAction Ignore
        Import-Module -FullyQualifiedName @{ ModuleName = [System.Management.Automation.WildcardPattern]::Escape("$PSScriptRoot\..\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1"); Guid = '8c3c366b-8606-4576-9f2d-4051144f7ca2'; ModuleVersion = '4.2.0' } -Force
    }
    else
    {
        Import-Module -FullyQualifiedName @{ ModuleName = 'PSAppDeployToolkit'; Guid = '8c3c366b-8606-4576-9f2d-4051144f7ca2'; ModuleVersion = '4.2.0' } -Force
    }
    $iadtParams = Get-ADTBoundParametersAndDefaultValues -Invocation $MyInvocation
    $adtSession = Remove-ADTHashtableNullOrEmptyValues -Hashtable $adtSession
    $adtSession = Open-ADTSession @adtSession @iadtParams -PassThru
    Remove-Variable -Name iadtParams -Force -Confirm:$false
}
catch
{
    $Host.UI.WriteErrorLine((Out-String -InputObject $_ -Width ([System.Int16]::MaxValue)))
    exit 60008
}

## MARK: Invocation
try
{
    # Import any PSAppDeployToolkit.* extensions
    Get-ChildItem -LiteralPath $PSScriptRoot -Directory | & {
        process
        {
            if ($_.Name -match 'PSAppDeployToolkit\..+$')
            {
                Get-ChildItem -LiteralPath $_.FullName -Recurse -File | Unblock-File -ErrorAction Ignore
                Import-Module -Name ([System.Management.Automation.WildcardPattern]::Escape("$($_.FullName)\$($_.BaseName).psd1")) -Force
            }
        }
    }
    Get-Variable -Name "Pre-$($adtSession.DeploymentType)", $adtSession.DeploymentType, "Post-$($adtSession.DeploymentType)" -ErrorAction Ignore | . {
        process
        {
            if (![System.String]::IsNullOrWhiteSpace($_.Value))
            {
                $adtSession.InstallPhase = $_.Name
                . $_.Value
            }
        }
    }
    Close-ADTSession
}
catch
{
    Write-ADTLogEntry -Message "An unhandled error within [$($MyInvocation.MyCommand.Name)] has occurred.`n$(Resolve-ADTErrorRecord -ErrorRecord $_)" -Severity Error
    # Show-ADTInstallationPrompt -Message "$($adtSession.DeploymentType) failed at line $($_.InvocationInfo.ScriptLineNumber), char $($_.InvocationInfo.OffsetInLine):`n$($_.InvocationInfo.Line.Trim())`n`nMessage:`n$($_.Exception.Message)" -ButtonRightText OK -NoWait
    Close-ADTSession -ExitCode 60001
}
