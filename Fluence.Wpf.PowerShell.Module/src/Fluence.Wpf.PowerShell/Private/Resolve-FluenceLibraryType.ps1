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


function Resolve-FluenceLibraryType
{
    <#
    .SYNOPSIS
        Resolves a Fluence.Wpf type by trying a list of candidate short names in order.
    .DESCRIPTION
        The library renamed a handful of public types in 0.9.0-pre (BackdropType became
        WindowBackdropType, CornerPreference became WindowCornerPreference). The module binds to
        whichever Fluence.Wpf assembly is loaded, which may be the module's own staged build or a
        host's older copy, so type names are resolved at call time rather than written as literal
        casts. Candidates are tried in the order given; the first that resolves wins.
    .PARAMETER Name
        One or more short type names inside the Fluence.Wpf namespace, newest name first.
    .OUTPUTS
        System.Type
    .NOTES
        Does not require a host application, but the Fluence.Wpf assembly must be loaded.
    #>
    [CmdletBinding()]
    [OutputType([System.Type])]
    param
    (
        [Parameter(Mandatory = $true)]
        [string[]]$Name
    )

    foreach ($candidate in $Name)
    {
        $type = ('Fluence.Wpf.' + $candidate) -as [System.Type]
        if ($null -ne $type)
        {
            return $type
        }
    }

    throw "None of the Fluence.Wpf types '$($Name -join "', '")' is available in the loaded Fluence.Wpf assembly."
}
