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


function New-FluenceProgressWindow
{
    <#
    .SYNOPSIS
        Builds the non-modal FluenceWindow for Show-FluenceProgress and returns its parts.
    .DESCRIPTION
        A fixed-width window with a message line, an optional detail line in the secondary text
        brush, and a Fluence ProgressBar. The caption buttons are hidden and the window cannot be
        resized or closed by the user; Close-FluenceProgress closes it. Returns a hashtable with
        Window, MessageText, DetailText and Bar, and applies the initial state.
    .PARAMETER Spec
        The normalized progress specification hashtable from Show-FluenceProgress (Title, Topmost,
        Position, Backdrop, Width).
    .PARAMETER State
        The shared progress state hashtable (Message, Detail, PercentComplete, Indeterminate).
    .OUTPUTS
        System.Collections.Hashtable
    .NOTES
        Must run on a UI (STA) thread. No hard-coded colors: text and bar resolve their own themed
        brushes from the seeded slots.
    #>
    [CmdletBinding()]
    [OutputType([hashtable])]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '',
        Justification = 'Builds a WPF window object in memory; changes no external system state.')]
    param
    (
        [Parameter(Mandatory = $true)]
        [hashtable]$Spec,

        [Parameter(Mandatory = $true)]
        [System.Collections.IDictionary]$State
    )

    $window = [Fluence.Wpf.Controls.FluenceWindow]::new()
    $window.Title = $Spec.Title
    Set-FluenceWindowIcon -Window $window -IconSource $Spec.TitleBarIcon
    $window.add_Closing({
        param($sourceWindow, $closeEvent)
        if ($sourceWindow.IsVisible -and -not $State.CloseRequested)
        {
            $closeEvent.Cancel = $true
        }
    }.GetNewClosure())
    # An absent Backdrop in the spec means the caller did not ask for one, so the window follows the
    # backdrop already applied to the process instead of overriding a Set-FluenceBackdrop pin.
    $window.SystemBackdropType = if ([string]::IsNullOrWhiteSpace($Spec.Backdrop))
    {
        [Fluence.Wpf.ApplicationThemeManager]::CurrentBackdrop
    }
    else
    {
        ConvertTo-FluenceBackdropType -Backdrop $Spec.Backdrop
    }
    $window.Width = $Spec.Width
    $window.SizeToContent = [System.Windows.SizeToContent]::Height
    $window.ResizeMode = [System.Windows.ResizeMode]::NoResize
    $window.Topmost = [bool]$Spec.Topmost
    $window.ShowInTaskbar = $true
    $window.IsMinimizeButtonVisible = [System.Windows.Visibility]::Collapsed
    $window.IsMaximizeButtonVisible = [System.Windows.Visibility]::Collapsed
    $window.IsCloseButtonVisible = [System.Windows.Visibility]::Collapsed
    Set-FluenceWindowPosition -Window $window -Position $Spec.Position

    $border = [System.Windows.Controls.Border]::new()
    $border.Padding = [System.Windows.Thickness]::new(24)

    $root = [System.Windows.Controls.StackPanel]::new()
    $root.Orientation = [System.Windows.Controls.Orientation]::Vertical
    $border.Child = $root

    $messageText = [System.Windows.Controls.TextBlock]::new()
    $messageText.TextWrapping = [System.Windows.TextWrapping]::Wrap
    $null = $root.Children.Add($messageText)

    $detailText = [System.Windows.Controls.TextBlock]::new()
    $detailText.TextWrapping = [System.Windows.TextWrapping]::Wrap
    $detailText.Margin = [System.Windows.Thickness]::new(0, 4, 0, 0)
    $detailText.SetResourceReference([System.Windows.Controls.TextBlock]::ForegroundProperty, 'TextFillColorSecondaryBrush')
    $null = $root.Children.Add($detailText)

    $bar = [Fluence.Wpf.Controls.ProgressBar]::new()
    $bar.Minimum = 0
    $bar.Maximum = 100
    $bar.Margin = [System.Windows.Thickness]::new(0, 16, 0, 0)
    $null = $root.Children.Add($bar)

    $window.Content = $border

    $parts = @{
        Window      = $window
        MessageText = $messageText
        DetailText  = $detailText
        Bar         = $bar
    }
    Set-FluenceProgressState -Parts $parts -State $State
    return $parts
}
