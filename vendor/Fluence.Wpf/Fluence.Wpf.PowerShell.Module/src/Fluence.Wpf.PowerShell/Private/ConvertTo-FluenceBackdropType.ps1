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


function ConvertTo-FluenceBackdropType
{
    <#
    .SYNOPSIS
        Converts a backdrop name (Mica, Acrylic, Tabbed, None, Auto) to the library's backdrop enum value.
    .DESCRIPTION
        Resolves the enum type at call time through Resolve-FluenceLibraryType so the module works
        against both the 0.9 name (WindowBackdropType) and the 0.8 name (BackdropType).
    .PARAMETER Backdrop
        The backdrop name. Matching is case-insensitive.
    .OUTPUTS
        System.Enum
    .NOTES
        Does not require a host application, but the Fluence.Wpf assembly must be loaded.
    #>
    [CmdletBinding()]
    [OutputType([System.Enum])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateSet('Mica', 'Acrylic', 'Tabbed', 'None', 'Auto')]
        [string]$Backdrop
    )

    $type = Resolve-FluenceLibraryType -Name 'WindowBackdropType', 'BackdropType'
    return [System.Enum]::Parse($type, $Backdrop, $true)
}
