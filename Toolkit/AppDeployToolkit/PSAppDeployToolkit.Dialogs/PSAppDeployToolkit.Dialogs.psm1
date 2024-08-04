#---------------------------------------------------------------------------
#
# Module setup to ensure expected functionality.
#
#---------------------------------------------------------------------------

# Set required variables to ensure module functionality.
$ErrorActionPreference = [System.Management.Automation.ActionPreference]::Stop
$ProgressPreference = [System.Management.Automation.ActionPreference]::SilentlyContinue
Set-StrictMode -Version 3

# Dot-source our imports and perform exports.
(Get-ChildItem -Path $PSScriptRoot\Private\*.ps1, $PSScriptRoot\Public\*.ps1).FullName.ForEach({. $_})
Export-ModuleMember -Function (Get-ChildItem -LiteralPath $PSScriptRoot\Public).BaseName
