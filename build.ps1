[CmdletBinding()]
param
(
)

# Stop on a dime, then import our build module and get started.
$ErrorActionPreference = [System.Management.Automation.ActionPreference]::Stop
Set-StrictMode -Version 3
try
{
    Import-Module -Name $PSScriptRoot\src\PSAppDeployToolkit.Build
    Invoke-ADTModuleBuild
}
catch
{
    $PSCmdlet.ThrowTerminatingError($_)
}
