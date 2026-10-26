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


function Invoke-FluenceWindow
{
    <#
    .SYNOPSIS
        Ensures the WPF Application, seeds the Fluence theme slots, builds the dialog window, and shows it.
    .DESCRIPTION
        The UI-thread half of Show-FluenceDialog. Seeds the application and theming through
        Initialize-FluenceApplication, builds the window with New-FluenceDialogWindow, shows it
        modally, and returns the result hashtable the window's handlers filled in.
    .PARAMETER Spec
        The dialog specification hashtable built on the caller thread by Show-FluenceDialog.
    .NOTES
        Must run on a UI (STA) thread; call it through Invoke-OnFluenceUi. Returns the result hashtable.
    #>
    [CmdletBinding()]
    [OutputType([hashtable])]
    param
    (
        [Parameter(Mandatory = $true)]
        [hashtable]$Spec
    )

    # Ensure the Application exists and seed the three theme slots and accent. Mandatory before
    # showing a FluenceWindow; shared with the window host.
    Initialize-FluenceApplication -Theme $Spec.Theme -Backdrop $Spec.Backdrop -Accent $Spec.AccentColor

    $state = @{ Result = @{}; Window = $null }
    $window = New-FluenceDialogWindow -Spec $Spec -State $state
    $state.Window = $window

    $null = $window.ShowDialog()
    return $state.Result
}
