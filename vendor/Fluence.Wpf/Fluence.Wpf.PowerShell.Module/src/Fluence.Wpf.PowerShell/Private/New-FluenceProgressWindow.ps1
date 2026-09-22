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
