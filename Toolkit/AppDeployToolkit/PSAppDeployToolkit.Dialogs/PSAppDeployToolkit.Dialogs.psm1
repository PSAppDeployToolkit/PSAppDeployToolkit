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
New-Variable -Name ModuleManifest -Value ([System.Management.Automation.Language.Parser]::ParseFile("$PSScriptRoot\$($MyInvocation.MyCommand.ScriptBlock.Module.Name).psd1", [ref]$null, [ref]$null).EndBlock.Statements.PipelineElements.Expression.SafeGetValue()) -Option Constant -Force -Confirm:$false
New-Variable -Name ModuleFiles -Option Constant -Value ([System.IO.FileInfo[]]$([System.IO.Directory]::GetFiles("$PSScriptRoot\Private"); [System.IO.Directory]::GetFiles("$PSScriptRoot\Public")))
New-Variable -Name FunctionPaths -Option Constant -Value ($ModuleFiles.BaseName -replace '^', 'Function:')
Remove-Item -LiteralPath $FunctionPaths -Force -ErrorAction Ignore
$ModuleFiles.FullName | . { process { . $_ } }
Set-Item -LiteralPath $FunctionPaths -Options ReadOnly
Export-ModuleMember -Function $ModuleManifest.FunctionsToExport
