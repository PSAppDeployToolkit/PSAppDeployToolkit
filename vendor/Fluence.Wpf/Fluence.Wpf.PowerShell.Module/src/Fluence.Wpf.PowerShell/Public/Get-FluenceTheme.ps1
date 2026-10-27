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


function Get-FluenceTheme
{
    <#
    .SYNOPSIS
        Returns the current Fluence theme state for the process.
    .DESCRIPTION
        Reads the process-wide theme statics directly (current theme, resolved theme, current
        backdrop, and dark-mode flag) and returns them as an object. This is a cheap, side-effect
        free reader; it does not marshal onto a UI thread or create a WPF Application.
    .EXAMPLE
        Get-FluenceTheme
    .EXAMPLE
        (Get-FluenceTheme).IsAppInDarkMode
    .OUTPUTS
        Fluence.ThemeInfo
    .NOTES
        Reads current process theme state; does not require or create a host application.
    #>
    [CmdletBinding()]
    [OutputType('Fluence.ThemeInfo')]
    param()

    return [pscustomobject]@{
        PSTypeName      = 'Fluence.ThemeInfo'
        CurrentTheme    = [Fluence.Wpf.ApplicationThemeManager]::CurrentTheme
        ResolvedTheme   = [Fluence.Wpf.ApplicationThemeManager]::ResolvedTheme
        CurrentBackdrop = [Fluence.Wpf.ApplicationThemeManager]::CurrentBackdrop
        IsAppInDarkMode = [Fluence.Wpf.ApplicationThemeManager]::IsAppInDarkMode
    }
}
