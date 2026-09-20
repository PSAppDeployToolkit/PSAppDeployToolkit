#-----------------------------------------------------------------------------
#
# Local psm1 file for testing the module without having to build it.
#
#-----------------------------------------------------------------------------

# Rethrowing caught exceptions makes the error output from Import-Module look better.
try
{
    # Dot-source our initial imports.
    . (Join-Path -Path $PSScriptRoot -ChildPath ImportsFirst.ps1)

    # Dot-source our imports.
    $PrivateFuncs = [System.Collections.Generic.HashSet[System.String]]::new()
    New-Variable -Name ModuleFiles -Option Constant -Value ([System.Collections.ObjectModel.ReadOnlyCollection[System.IO.FileInfo]]$([System.IO.Directory]::GetFiles((Join-Path -Path $PSScriptRoot -ChildPath Private)); [System.IO.Directory]::GetFiles((Join-Path -Path $PSScriptRoot -ChildPath Public))))
    New-Variable -Name FunctionPaths -Option Constant -Force -Value ([System.Collections.ObjectModel.ReadOnlyCollection[System.String]]($ModuleFiles | & {
                process
                {
                    if ([System.IO.Path]::GetDirectoryName($_.FullName).EndsWith('Private'))
                    {
                        $null = $PrivateFuncs.Add($_.BaseName)
                    }
                    return "Microsoft.PowerShell.Core\Function::$($_.BaseName)"
                }
            }))
    New-Variable -Name PrivateFuncs -Option Constant -Value ([System.Collections.Frozen.FrozenSet]::ToFrozenSet($PrivateFuncs, [System.StringComparer]::Ordinal)) -Force
    Remove-Item -LiteralPath $FunctionPaths -Force -ErrorAction Ignore
    $ModuleFiles.FullName | . { process { . $_ } }

    # Dot-source our final imports.
    . (Join-Path -Path $PSScriptRoot -ChildPath 'ImportsLast.ps1')
}
catch
{
    throw
}
