#---------------------------------------------------------------------------
#
# Local psm1 file for testing the module without having to build it.
#
#---------------------------------------------------------------------------

# Dot-source our initial imports.
. "$PSScriptRoot\ImportsFirst.ps1"

# Dot-source our imports.
if (!(& $CommandTable.'Test-Path' -LiteralPath Microsoft.PowerShell.Core\Variable::FunctionPaths))
{
    & $CommandTable.'New-Variable' -Name ModuleFiles -Option Constant -Value ([System.IO.FileInfo[]]$([System.IO.Directory]::GetFiles("$PSScriptRoot\Private"); [System.IO.Directory]::GetFiles("$PSScriptRoot\Public")))
    & $CommandTable.'New-Variable' -Name FunctionPaths -Option Constant -Value ($ModuleFiles.BaseName -replace '^', 'Microsoft.PowerShell.Core\Function::')
    & $CommandTable.'Remove-Item' -LiteralPath $FunctionPaths -Force -ErrorAction Ignore
    $ModuleFiles.FullName | . { process { . $_ } }
}

# Dot-source our final imports.
. "$PSScriptRoot\ImportsLast.ps1"
