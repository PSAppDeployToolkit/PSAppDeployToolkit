<#
Copyright (c) 2026, Dan Cunningham. All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice,
   this list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

3. Neither the name of the copyright holder nor the names of its
   contributors may be used to endorse or promote products derived from
   this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
#>


function Get-FluenceLibraryPath
{
    <#
    .SYNOPSIS
        Resolves the path to Fluence.Wpf.dll for the running PowerShell edition.
    .DESCRIPTION
        Maps the edition to the staged target framework folder under the module's lib directory:
        net8.0-windows10.0.26100.0 for Core (PowerShell 7, which rolls forward onto later runtimes)
        and net472 for Desktop (Windows PowerShell 5.1). Throws when that build was never staged,
        naming the path it looked for.
    .PARAMETER ModuleRoot
        The module's own folder, the parent of lib.
    .PARAMETER Edition
        The PowerShell edition to resolve for. Defaults to the running edition ($PSEdition).
    .NOTES
        Does not require a host application.
    #>
    [CmdletBinding()]
    [OutputType([string])]
    param
    (
        [Parameter(Mandatory = $true)]
        [string]$ModuleRoot,

        [Parameter()]
        [string]$Edition = $PSEdition
    )

    if ($Edition -eq 'Core')
    {
        $tfm = 'net8.0-windows10.0.26100.0'
    }
    else
    {
        $tfm = 'net472'
    }

    $dll = [System.IO.Path]::Combine($ModuleRoot, 'lib', $tfm, 'Fluence.Wpf.dll')
    if (-not (Test-Path -LiteralPath $dll))
    {
        throw "Fluence.Wpf.dll not found for edition '$Edition' at: $dll"
    }

    return $dll
}
