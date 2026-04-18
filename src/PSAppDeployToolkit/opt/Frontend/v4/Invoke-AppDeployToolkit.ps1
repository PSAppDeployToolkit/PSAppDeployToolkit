<#

.SYNOPSIS
This script invokes a PSAppDeployToolkit deployment.

.DESCRIPTION
The script imports the PSAppDeployToolkit module and executes the specified install/uninstall/repair scriptblocks.

.PARAMETER DeploymentType
The type of deployment to perform, Install, Uninstall, or Repair. Default is Install.

.PARAMETER DeployMode
Specifies whether the installation should be run in Interactive (shows dialogs), Silent (no dialogs), NonInteractive (dialogs without prompts) mode, or Auto (shows dialogs if a user is logged on, device is not in the OOBE, and there's no running apps to close).
Silent mode is automatically set if it is detected that the process is not user interactive, no users are logged on, the device is in Autopilot mode, or there's specified processes to close that are currently running.

.PARAMETER SuppressRebootPassThru
Suppresses the 3010 return code (requires restart) from being passed back to the parent process (e.g. Intune) if detected from an installation.

.PARAMETER TerminalServerMode
Changes to "user install mode" and back to "user execute mode" for installing/uninstalling applications for Remote Desktop Session Hosts/Citrix servers.

.PARAMETER DisableLogging
Disables logging to file for the script.

.EXAMPLE
Invoke-AppDeployToolkit.exe -DeploymentType Install -DeployMode Silent

.LINK
https://psappdeploytoolkit.com

#>

[CmdletBinding()]
[System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'PreInstall', Justification = 'This variable is accessed dynamically via Get-Variable and therefore cannot be seen by PSScriptAnalyzer.')]
[System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'Install', Justification = 'This variable is accessed dynamically via Get-Variable and therefore cannot be seen by PSScriptAnalyzer.')]
[System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'PostInstall', Justification = 'This variable is accessed dynamically via Get-Variable and therefore cannot be seen by PSScriptAnalyzer.')]
[System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'PreUninstall', Justification = 'This variable is accessed dynamically via Get-Variable and therefore cannot be seen by PSScriptAnalyzer.')]
[System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'Uninstall', Justification = 'This variable is accessed dynamically via Get-Variable and therefore cannot be seen by PSScriptAnalyzer.')]
[System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'PostUninstall', Justification = 'This variable is accessed dynamically via Get-Variable and therefore cannot be seen by PSScriptAnalyzer.')]
[System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'PreRepair', Justification = 'This variable is accessed dynamically via Get-Variable and therefore cannot be seen by PSScriptAnalyzer.')]
[System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'Repair', Justification = 'This variable is accessed dynamically via Get-Variable and therefore cannot be seen by PSScriptAnalyzer.')]
[System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'PostRepair', Justification = 'This variable is accessed dynamically via Get-Variable and therefore cannot be seen by PSScriptAnalyzer.')]
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
    [System.Management.Automation.SwitchParameter]$SuppressRebootPassThru,

    [Parameter(Mandatory = $false)]
    [System.Management.Automation.SwitchParameter]$TerminalServerMode,

    [Parameter(Mandatory = $false)]
    [System.Management.Automation.SwitchParameter]$DisableLogging
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
    AppProcessesToClose = @()
    AppScriptVersion = '1.0.0'
    AppScriptDate = '2000-12-31'
    AppScriptAuthor = '<author name>'

    InstallName = ''
    InstallTitle = ''

    DeployAppScriptFriendlyName = $MyInvocation.MyCommand.Name
    DeployAppScriptParameters = $PSBoundParameters
    DeployAppScriptVersion = '4.2.0'
}


## MARK: Pre-Install
$PreInstall = {
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
$Install = {

}


## MARK: Post-Install
$PostInstall = {
    Show-ADTInstallationPrompt -Message "$($adtSession.DeploymentType) complete." -ButtonRightText 'OK' -NoWait
}


## MARK: Pre-Uninstall
$PreUninstall = {
    if ($adtSession.AppProcessesToClose.Count -gt 0)
    {
        Show-ADTInstallationWelcome -CloseProcesses $adtSession.AppProcessesToClose -CloseProcessesCountdown 60
    }

    Show-ADTInstallationProgress
}


## MARK: Uninstall
$Uninstall = {

}


## MARK: Post-Uninstall
$PostUninstall = {

}


## MARK: Pre-Repair
$PreRepair = {
    if ($adtSession.AppProcessesToClose.Count -gt 0)
    {
        Show-ADTInstallationWelcome -CloseProcesses $adtSession.AppProcessesToClose -CloseProcessesCountdown 60
    }

    Show-ADTInstallationProgress
}


## MARK: Repair
$Repair = {

}


## MARK: Post-Repair
$PostRepair = {
    Show-ADTInstallationPrompt -Message "$($adtSession.DeploymentType) complete." -ButtonRightText 'OK' -NoWait
}


## MARK: Initialization
$ErrorActionPreference = [System.Management.Automation.ActionPreference]::Stop
$ProgressPreference = [System.Management.Automation.ActionPreference]::SilentlyContinue
Set-StrictMode -Version 1

try
{
    $moduleBase = "$PSScriptRoot\..\..\..\..\PSAppDeployToolkit"
    if (Test-Path -LiteralPath "$moduleBase\PSAppDeployToolkit.psd1" -PathType Leaf)
    {
        Get-ChildItem -LiteralPath $moduleBase -Recurse -File | Unblock-File -ErrorAction Ignore
        Import-Module -FullyQualifiedName @{ ModuleName = [System.Management.Automation.WildcardPattern]::Escape("$moduleBase\PSAppDeployToolkit.psd1"); Guid = '8c3c366b-8606-4576-9f2d-4051144f7ca2'; ModuleVersion = '4.2.0' } -Force
    }
    else
    {
        Import-Module -FullyQualifiedName @{ ModuleName = 'PSAppDeployToolkit'; Guid = '8c3c366b-8606-4576-9f2d-4051144f7ca2'; ModuleVersion = '4.2.0' } -Force
    }

    $iadtParams = Get-ADTBoundParametersAndDefaultValues -Invocation $MyInvocation
    $adtSession = Remove-ADTHashtableNullOrEmptyValues -Hashtable $adtSession
    $adtSession = Open-ADTSession @adtSession @iadtParams -PassThru
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

    foreach ($prefix in 'Pre-', '', 'Post-')
    {
        $installPhase = "$prefix$($adtSession.DeploymentType)"
        $scriptBlock = Get-Variable -Name $installPhase.Replace('-', '') -ValueOnly -ErrorAction Ignore
        if (![System.String]::IsNullOrWhiteSpace($scriptBlock))
        {
            $adtSession.InstallPhase = $installPhase
            . $scriptBlock
        }
    }

    Close-ADTSession
}
catch
{
    $mainErrorMessage = "An unhandled error within [$($MyInvocation.MyCommand.Name)] has occurred.`n$(Resolve-ADTErrorRecord -ErrorRecord $_)"
    Write-ADTLogEntry -Message $mainErrorMessage -Severity Error

    ## Error details hidden from the user by default. Show a simple dialog with full stack trace:
    # Show-ADTDialogBox -Text $mainErrorMessage -Icon Stop -NoWait

    ## Or, a themed dialog with basic error message:
    # Show-ADTInstallationPrompt -Message "$($adtSession.DeploymentType) failed at line $($_.InvocationInfo.ScriptLineNumber), char $($_.InvocationInfo.OffsetInLine):`n$($_.InvocationInfo.Line.Trim())`n`nMessage:`n$($_.Exception.Message)" -ButtonRightText OK -NoWait

    Close-ADTSession -ExitCode 60001
}
