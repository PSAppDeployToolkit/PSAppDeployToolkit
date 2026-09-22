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
