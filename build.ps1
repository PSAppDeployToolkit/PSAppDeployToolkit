[CmdletBinding()]
param
(
)

# Stop on a dime, then import our build module and get started.
$ErrorActionPreference = [System.Management.Automation.ActionPreference]::Stop
Set-StrictMode -Version 3
try
{
    Import-Module -Name ([System.Management.Automation.WildcardPattern]::Escape("$PSScriptRoot\src\PSAppDeployToolkit.Build\PSAppDeployToolkit.Build.psd1"))
    Invoke-ADTModuleBuild
}
catch
{
    $PSCmdlet.ThrowTerminatingError($_)
}
