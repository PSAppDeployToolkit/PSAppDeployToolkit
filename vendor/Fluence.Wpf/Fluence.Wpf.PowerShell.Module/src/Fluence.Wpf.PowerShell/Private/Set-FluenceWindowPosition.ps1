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


function Set-FluenceWindowPosition
{
    <#
    .SYNOPSIS
        Places a window at a named screen position: Center, TopRight, or BottomRight.
    .DESCRIPTION
        Center keeps WPF's CenterScreen startup location. The corner positions switch the window to
        manual placement and, once the window has laid out (Loaded, when ActualWidth and ActualHeight
        are known even for SizeToContent windows), pin it 16 device-independent pixels inside the
        matching corner of the primary work area, so it never covers the taskbar.
    .PARAMETER Window
        The window to position. Must not have been shown yet.
    .PARAMETER Position
        Center, TopRight, or BottomRight.
    .NOTES
        Runs on the UI (STA) thread before Show or ShowDialog. Sets in-memory window properties only.
    #>
    [CmdletBinding()]
    [OutputType([void])]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '',
        Justification = 'Sets in-memory WPF window placement; changes no external system state.')]
    param
    (
        [Parameter(Mandatory = $true)]
        [System.Windows.Window]$Window,

        [Parameter(Mandatory = $true)]
        [ValidateSet('Center', 'TopRight', 'BottomRight')]
        [string]$Position
    )

    if ($Position -eq 'Center')
    {
        $Window.WindowStartupLocation = [System.Windows.WindowStartupLocation]::CenterScreen
        return
    }

    $Window.WindowStartupLocation = [System.Windows.WindowStartupLocation]::Manual
    $window = $Window
    $position = $Position
    $window.add_Loaded({
        $workArea = [System.Windows.SystemParameters]::WorkArea
        $margin = 16
        $window.Left = $workArea.Right - $window.ActualWidth - $margin
        if ($position -eq 'TopRight')
        {
            $window.Top = $workArea.Top + $margin
        }
        else
        {
            $window.Top = $workArea.Bottom - $window.ActualHeight - $margin
        }
    }.GetNewClosure())
}
