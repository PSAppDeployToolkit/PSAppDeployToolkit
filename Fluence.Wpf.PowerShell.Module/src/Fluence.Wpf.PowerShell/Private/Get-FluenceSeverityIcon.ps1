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


function Get-FluenceSeverityIcon
{
    <#
    .SYNOPSIS
        Maps a dialog -Icon value to its FontIcon glyph and themed brush key.
    .DESCRIPTION
        Returns @{ Glyph; BrushKey } for Info/Success/Warning/Error/Question, or $null when no icon
        should be shown (None or empty). The four real severities take their glyph and brush key from
        the library helper [Fluence.Wpf.Controls.InfoBar] so they never drift from the InfoBar template;
        Question (which has no InfoBarSeverity) uses the Segoe Fluent Help glyph with the neutral brush.
    .PARAMETER Icon
        The dialog -Icon value: None, Info, Success, Warning, Error, Question, or an empty string.
    .OUTPUTS
        System.Collections.Hashtable (or $null)
    .NOTES
        Does not require a host application, but the Fluence.Wpf assembly must be loaded (the module
        loads it on import) so the severity-glyph helper resolves.
    #>
    [CmdletBinding()]
    [OutputType([hashtable])]
    param
    (
        [Parameter(Mandatory = $true)]
        [AllowEmptyString()]
        [string]$Icon
    )

    if ([string]::IsNullOrWhiteSpace($Icon) -or $Icon -eq 'None')
    {
        return $null
    }

    $informational = [Fluence.Wpf.InfoBarSeverity]::Informational
    if ($Icon -eq 'Question')
    {
        # Question is a dialog affordance with no InfoBarSeverity counterpart: the Segoe Fluent Help
        # glyph with the same neutral brush as Informational.
        return @{
            Glyph    = [string][char]0xE897
            BrushKey = [Fluence.Wpf.Controls.InfoBar]::GetSeverityBrushKey($informational)
        }
    }

    $severity = switch ($Icon)
    {
        'Success' { [Fluence.Wpf.InfoBarSeverity]::Success }
        'Warning' { [Fluence.Wpf.InfoBarSeverity]::Warning }
        'Error'   { [Fluence.Wpf.InfoBarSeverity]::Error }
        default   { $informational }
    }

    return @{
        Glyph    = [Fluence.Wpf.Controls.InfoBar]::GetSeverityGlyph($severity)
        BrushKey = [Fluence.Wpf.Controls.InfoBar]::GetSeverityBrushKey($severity)
    }
}
