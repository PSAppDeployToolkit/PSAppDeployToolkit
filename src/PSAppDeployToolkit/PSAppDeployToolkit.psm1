#-----------------------------------------------------------------------------
#
# Local psm1 file for testing the module without having to build it.
#
#-----------------------------------------------------------------------------

# Dot-source our initial imports.
. ([System.IO.Path]::Combine($PSScriptRoot, 'ImportsFirst.ps1'))

# Dot-source our imports.
if (!$Module.Compiled)
{
    try
    {
        New-Variable -Name ModuleFiles -Option Constant -Value ([System.IO.FileInfo[]]$([System.IO.Directory]::GetFiles([System.IO.Path]::Combine($PSScriptRoot, 'Private')); [System.IO.Directory]::GetFiles([System.IO.Path]::Combine($PSScriptRoot, 'Public'))))
        New-Variable -Name FunctionPaths -Option Constant -Value ($ModuleFiles | & { process { return "Microsoft.PowerShell.Core\Function::$($_.BaseName)" } })
        Remove-Item -LiteralPath $FunctionPaths -Force -ErrorAction Ignore
        $ModuleFiles.FullName | . { process { . $_ } }
    }
    catch
    {
        throw
    }
}

# Dot-source our final imports.
. ([System.IO.Path]::Combine($PSScriptRoot, 'ImportsLast.ps1'))
