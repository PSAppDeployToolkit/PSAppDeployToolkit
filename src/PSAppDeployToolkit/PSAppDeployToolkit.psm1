#-----------------------------------------------------------------------------
#
# Local psm1 file for testing the module without having to build it.
#
#-----------------------------------------------------------------------------

# Store the module info in a variable for further usage.
New-Variable -Name ModuleInfo -Option Constant -Value $MyInvocation.MyCommand.ScriptBlock.Module -Force

# Dot-source our initial imports.
. ([System.IO.Path]::Combine($PSScriptRoot, 'ImportsFirst.ps1'))

# Dot-source our imports.
if (!$Module.Compiled)
{
    try
    {
        New-Variable -Name ModuleFiles -Option Constant -Value ([System.IO.FileInfo[]]$([System.IO.Directory]::GetFiles([System.IO.Path]::Combine($PSScriptRoot, 'Private')); [System.IO.Directory]::GetFiles([System.IO.Path]::Combine($PSScriptRoot, 'Public'))))
        $ModuleFiles | . {
            begin
            {
                $FunctionPaths = [System.Collections.Generic.List[System.String]]::new()
                $PrivateFuncs = [System.Collections.Generic.List[System.String]]::new()
            }
            process
            {
                if ([System.IO.Path]::GetDirectoryName($_.FullName).EndsWith('Private'))
                {
                    $PrivateFuncs.Add($_.BaseName)
                }
                $FunctionPaths.Add("Microsoft.PowerShell.Core\Function::$($_.BaseName)")
            }
            end
            {
                New-Variable -Name FunctionPaths -Option Constant -Value $FunctionPaths.AsReadOnly() -Force
                New-Variable -Name PrivateFuncs -Option Constant -Value $PrivateFuncs.AsReadOnly() -Force
                Remove-Item -LiteralPath $FunctionPaths -Force -ErrorAction Ignore
            }
        }
        $ModuleFiles.FullName | . { process { . $_ } }
    }
    catch
    {
        throw
    }
}

# Dot-source our final imports.
. ([System.IO.Path]::Combine($PSScriptRoot, 'ImportsLast.ps1'))
