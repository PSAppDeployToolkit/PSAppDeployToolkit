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


function Set-FluenceProgressState
{
    <#
    .SYNOPSIS
        Applies a progress state (message, detail, percent, mode) to the progress window's controls.
    .DESCRIPTION
        Writes the four state values onto the window's template parts: the message and detail text
        blocks (an empty detail collapses its line) and the progress bar, which switches between the
        determinate and indeterminate modes and takes a percentage clamped to 0 to 100.
    .PARAMETER Parts
        The hashtable returned by New-FluenceProgressWindow (Window, MessageText, DetailText, Bar).
    .PARAMETER State
        The progress state hashtable (Message, Detail, PercentComplete, Indeterminate).
    .NOTES
        Must run on the UI thread that owns the window. Sets in-memory control properties only.
    #>
    [CmdletBinding()]
    [OutputType([void])]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '',
        Justification = 'Sets in-memory WPF control properties; changes no external system state.')]
    param
    (
        [Parameter(Mandatory = $true)]
        [System.Collections.IDictionary]$Parts,

        [Parameter(Mandatory = $true)]
        [System.Collections.IDictionary]$State
    )

    $Parts.MessageText.Text = [string]$State.Message

    $detail = [string]$State.Detail
    $Parts.DetailText.Text = $detail
    if ([string]::IsNullOrWhiteSpace($detail))
    {
        $Parts.DetailText.Visibility = [System.Windows.Visibility]::Collapsed
    }
    else
    {
        $Parts.DetailText.Visibility = [System.Windows.Visibility]::Visible
    }

    if ([bool]$State.Indeterminate)
    {
        $Parts.Bar.ProgressMode = [Fluence.Wpf.ProgressBarMode]::Indeterminate
    }
    else
    {
        $Parts.Bar.ProgressMode = [Fluence.Wpf.ProgressBarMode]::Standard
        $Parts.Bar.Value = [double]$State.PercentComplete
    }
}
