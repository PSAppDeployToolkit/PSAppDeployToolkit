function New-FluenceDialogWindow
{
    <#
    .SYNOPSIS
        Builds the FluenceWindow for a dialog specification and wires its buttons and validation.
    .DESCRIPTION
        Creates a FluenceWindow sized to its content, composes the optional message lines, each
        prompt's label and input control (via New-FluenceInputControl), an inline validation InfoBar,
        and a row of buttons. A non-cancel button validates the captured values with Test-FluenceInput
        and either opens the InfoBar (keeping the window open) or records its result and closes. A
        cancel button closes without validating. The Closed handler marks the result Cancelled when no
        button result is true. Returns the window; the caller assigns $State.Window before ShowDialog.
    .PARAMETER Spec
        The normalized dialog specification hashtable from Show-FluenceDialog.
    .PARAMETER State
        The shared @{ Result = @{}; Window = $null } hashtable that accumulates captured values.
    .OUTPUTS
        Fluence.Wpf.Controls.FluenceWindow
    .NOTES
        Must run on a UI (STA) thread; call it through Invoke-FluenceWindow. No hard-coded colors:
        every control resolves its own themed brushes from the seeded slots.
    #>
    [CmdletBinding()]
    [OutputType([Fluence.Wpf.Controls.FluenceWindow])]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '',
        Justification = 'Builds a WPF window object in memory; changes no external system state.')]
    param
    (
        [Parameter(Mandatory = $true)]
        [hashtable]$Spec,

        [Parameter(Mandatory = $true)]
        [hashtable]$State
    )

    # User validators follow the same scope contract as window callbacks. Clone each prompt so
    # rebinding a validator to this UI runspace never changes the caller's specification object.
    if ($Spec.ContainsKey('CallerRunspaceId'))
    {
        $resolvedPrompts = @()
        foreach ($prompt in $Spec.Prompts)
        {
            $copy = $prompt.PSObject.Copy()
            if ($null -ne $copy.ValidateScript)
            {
                $copy.ValidateScript = Resolve-FluenceUserBlock -LiveBlock $copy.ValidateScript -BlockText $copy.ValidateScript.ToString() -CallerRunspaceId $Spec.CallerRunspaceId
            }
            $resolvedPrompts += $copy
        }
        $Spec.Prompts = $resolvedPrompts
    }

    $window = [Fluence.Wpf.Controls.FluenceWindow]::new()
    $window.Title = $Spec.Title
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
    $window.SizeToContent = [System.Windows.SizeToContent]::WidthAndHeight
    $window.MinWidth = $Spec.MinWidth
    $window.MinHeight = 120
    $position = 'Center'
    if (-not [string]::IsNullOrWhiteSpace([string]$Spec.Position))
    {
        $position = [string]$Spec.Position
    }
    Set-FluenceWindowPosition -Window $window -Position $position
    $window.Topmost = [bool]$Spec.Topmost

    # Left (default) or Center: applied to the image and every message line.
    $contentAlignment = [System.Windows.HorizontalAlignment]::Left
    $textAlignment = [System.Windows.TextAlignment]::Left
    if ([string]$Spec.MessageAlignment -eq 'Center')
    {
        $contentAlignment = [System.Windows.HorizontalAlignment]::Center
        $textAlignment = [System.Windows.TextAlignment]::Center
    }
    if ($null -ne $Spec.ParentWindow)
    {
        # A WPF window's Owner must live on the same thread as the window (built on the Fluence UI
        # thread). A ParentWindow from another thread cannot parent it; parent only when they share a
        # thread, otherwise show the dialog unparented rather than throwing a cross-thread error.
        if ($Spec.ParentWindow.Dispatcher.CheckAccess())
        {
            $window.Owner = $Spec.ParentWindow
            $window.WindowStartupLocation = [System.Windows.WindowStartupLocation]::CenterOwner
        }
        else
        {
            Write-Warning "The -ParentWindow belongs to a different thread than the Fluence UI thread; modal owner parenting is not available here. Showing the dialog without an owner."
        }
    }

    # Root stack inside a padded border. Controls resolve their own themed brushes.
    $border = [System.Windows.Controls.Border]::new()
    $border.Padding = [System.Windows.Thickness]::new(24)

    $root = [System.Windows.Controls.StackPanel]::new()
    $root.Orientation = [System.Windows.Controls.Orientation]::Vertical
    $border.Child = $root

    # Optional image above the message, rendered with the Fluence Image control at a capped height so
    # a large branding asset never dominates the dialog. The source was validated at spec-build time
    # (file: or pack: only) by Resolve-FluenceImageSource.
    if (-not [string]::IsNullOrWhiteSpace([string]$Spec.Image))
    {
        $image = [Fluence.Wpf.Controls.Image]::new()
        $bitmap = [System.Windows.Media.Imaging.BitmapImage]::new()
        $bitmap.BeginInit()
        $bitmap.UriSource = [System.Uri]$Spec.Image
        $bitmap.CacheOption = [System.Windows.Media.Imaging.BitmapCacheOption]::OnLoad
        $bitmap.EndInit()
        $bitmap.Freeze()
        $image.Source = $bitmap
        $image.Stretch = [System.Windows.Media.Stretch]::Uniform
        $image.MaxHeight = 120
        $image.HorizontalAlignment = $contentAlignment
        $image.Margin = [System.Windows.Thickness]::new(0, 0, 0, 12)
        $null = $root.Children.Add($image)
    }

    # Message lines render as wrapping TextBlocks. When an icon severity is set, a leading colored
    # FontIcon (the same glyph and brush the InfoBar uses) sits to their left. The message no longer
    # renders inside an InfoBar.
    if ($null -ne $Spec.Message)
    {
        $iconValue = $null
        if ($Spec.ContainsKey('Icon'))
        {
            $iconValue = $Spec.Icon
        }
        $severityIcon = Get-FluenceSeverityIcon -Icon ([string]$iconValue)

        $messageStack = [System.Windows.Controls.StackPanel]::new()
        $messageStack.Orientation = [System.Windows.Controls.Orientation]::Vertical
        $messageStack.HorizontalAlignment = $contentAlignment
        foreach ($line in $Spec.Message)
        {
            $text = [System.Windows.Controls.TextBlock]::new()
            $text.Text = $line
            $text.TextWrapping = [System.Windows.TextWrapping]::Wrap
            $text.TextAlignment = $textAlignment
            $text.Margin = [System.Windows.Thickness]::new(0, 0, 0, 4)
            $null = $messageStack.Children.Add($text)
        }

        if ($null -ne $severityIcon)
        {
            $messageRow = [System.Windows.Controls.Grid]::new()
            $messageRow.Margin = [System.Windows.Thickness]::new(0, 0, 0, 8)
            $iconColumn = [System.Windows.Controls.ColumnDefinition]::new()
            $iconColumn.Width = [System.Windows.GridLength]::Auto
            $textColumn = [System.Windows.Controls.ColumnDefinition]::new()
            $textColumn.Width = [System.Windows.GridLength]::new(1, [System.Windows.GridUnitType]::Star)
            $null = $messageRow.ColumnDefinitions.Add($iconColumn)
            $null = $messageRow.ColumnDefinitions.Add($textColumn)

            $fontIcon = [Fluence.Wpf.Controls.FontIcon]::new()
            $fontIcon.Glyph = $severityIcon.Glyph
            $fontIcon.IconFontSize = 20
            $fontIcon.VerticalAlignment = [System.Windows.VerticalAlignment]::Top
            $fontIcon.Margin = [System.Windows.Thickness]::new(0, 0, 12, 0)
            $fontIcon.SetResourceReference([System.Windows.Controls.Control]::ForegroundProperty, $severityIcon.BrushKey)

            [System.Windows.Controls.Grid]::SetColumn($fontIcon, 0)
            [System.Windows.Controls.Grid]::SetColumn($messageStack, 1)
            $null = $messageRow.Children.Add($fontIcon)
            $null = $messageRow.Children.Add($messageStack)
            $null = $root.Children.Add($messageRow)
        }
        else
        {
            $messageStack.Margin = [System.Windows.Thickness]::new(0, 0, 0, 8)
            $null = $root.Children.Add($messageStack)
        }
    }

    # Prompts: optional label + control.
    foreach ($prompt in $Spec.Prompts)
    {
        if (-not [string]::IsNullOrWhiteSpace($prompt.Message) -and $prompt.InputType -ne 'Checkbox' -and $prompt.InputType -ne 'Link')
        {
            $label = [System.Windows.Controls.TextBlock]::new()
            $label.Text = $prompt.Message
            $label.TextWrapping = [System.Windows.TextWrapping]::Wrap
            $label.Margin = [System.Windows.Thickness]::new(0, 8, 0, 4)
            $null = $root.Children.Add($label)
        }

        $control = New-FluenceInputControl -Prompt $prompt -State $State
        $control.HorizontalAlignment = [System.Windows.HorizontalAlignment]::Stretch
        $control.Margin = [System.Windows.Thickness]::new(0, 0, 0, 4)
        $null = $root.Children.Add($control)
    }

    # Inline validation InfoBar (hidden until a validation failure). Stashed on $State so the
    # button closures can open it.
    $infoBar = [Fluence.Wpf.Controls.InfoBar]::new()
    $infoBar.Severity = [Fluence.Wpf.InfoBarSeverity]::Error
    $infoBar.IsOpen = $false
    $infoBar.Margin = [System.Windows.Thickness]::new(0, 8, 0, 0)
    $State.InfoBar = $infoBar
    $null = $root.Children.Add($infoBar)

    # Initialize the result: Cancelled and TimedOut flags plus a false entry per button, before
    # wiring. TimedOut is set by the timeout timer below when -Timeout elapses.
    $State.Result['Cancelled'] = $false
    $State.Result['TimedOut'] = $false
    foreach ($button in $Spec.Buttons)
    {
        $State.Result[$button.Name] = $false
    }

    # Capture the validator as a variable so the button closures invoke it through a closed-over
    # reference. A GetNewClosure closure runs in a fresh anonymous scope that cannot resolve a
    # module-private function by name when the deferred WPF Click event fires, but it can call a
    # captured scriptblock.
    $validateInput = ${function:Test-FluenceInput}

    # Button row: equal-fill Grid mirroring ContentDialog CommandSpace.
    # Layout order: IsDefault left, neither-default-nor-cancel in the middle (preserving $Spec.Buttons
    # order among them), IsCancel right. $Spec.Buttons itself is not reordered; the Closed handler and
    # Result init above continue to iterate it. The ordered list is layout-only.
    $orderedButtons = @($Spec.Buttons | Where-Object { $_.IsDefault }) `
        + @($Spec.Buttons | Where-Object { -not $_.IsDefault -and -not $_.IsCancel }) `
        + @($Spec.Buttons | Where-Object { $_.IsCancel -and -not $_.IsDefault })

    $grid = [System.Windows.Controls.Grid]::new()
    $grid.HorizontalAlignment = [System.Windows.HorizontalAlignment]::Stretch
    $grid.Margin = [System.Windows.Thickness]::new(0, 16, 0, 0)

    $n = $orderedButtons.Count

    # The countdown caption (when -Countdown is set) goes on the first laid-out button, which is the
    # IsDefault button when there is one and the left-most button otherwise.
    $countdownButton = $null

    # A single button is sized as if there were two (half width) and pinned to the right column,
    # leaving the left column empty. For 2+ buttons, one equal Star column per button. The single
    # button therefore needs an extra empty leading column.
    $columnCount = if ($n -eq 1) { 2 } else { $n }
    $starWidth = [System.Windows.GridLength]::new(1, [System.Windows.GridUnitType]::Star)
    for ($c = 0; $c -lt $columnCount; $c++)
    {
        $col = [System.Windows.Controls.ColumnDefinition]::new()
        $col.Width = $starWidth
        $null = $grid.ColumnDefinitions.Add($col)
    }

    for ($i = 0; $i -lt $n; $i++)
    {
        $button = $orderedButtons[$i]
        $wpfButton = [Fluence.Wpf.Controls.Button]::new()
        $wpfButton.Content = $button.Text
        $wpfButton.HorizontalAlignment = [System.Windows.HorizontalAlignment]::Stretch
        $wpfButton.MinWidth = 0

        # 4px half-margins produce 8px gaps between adjacent buttons, 0 at the outer edges,
        # matching ContentDialog CommandSpace: left (0,0,4,0), middle (4,0,4,0), right (4,0,0,0).
        # A lone button sits in the right column, so it takes the right-most margin (4,0,0,0).
        if ($n -eq 1)
        {
            $wpfButton.Margin = [System.Windows.Thickness]::new(4, 0, 0, 0)
        }
        elseif ($i -eq 0)
        {
            $wpfButton.Margin = [System.Windows.Thickness]::new(0, 0, 4, 0)
        }
        elseif ($i -eq ($n - 1))
        {
            $wpfButton.Margin = [System.Windows.Thickness]::new(4, 0, 0, 0)
        }
        else
        {
            $wpfButton.Margin = [System.Windows.Thickness]::new(4, 0, 4, 0)
        }

        $wpfButton.IsDefault = [bool]$button.IsDefault
        $wpfButton.IsCancel = [bool]$button.IsCancel

        if ($button.IsDefault)
        {
            $wpfButton.Appearance = [Fluence.Wpf.ControlAppearance]::Accent
        }

        # Lone button -> right column (index 1 of 2); otherwise its position in the ordered list.
        $targetColumn = if ($n -eq 1) { 1 } else { $i }
        [System.Windows.Controls.Grid]::SetColumn($wpfButton, $targetColumn)

        if ($button.IsCancel)
        {
            $wpfButton.add_Click({
                # Cancel closes without validating; the Closed handler marks Cancelled.
                $State.Window.Close()
            }.GetNewClosure())
        }
        else
        {
            $wpfButton.add_Click({
                $validation = @{ IsValid = $true; Message = '' }
                if ($Spec.Prompts.Count -gt 0)
                {
                    $validation = & $validateInput -Prompts $Spec.Prompts -Values $State.Result
                }
                if (-not $validation.IsValid)
                {
                    $State.InfoBar.Message = $validation.Message
                    $State.InfoBar.Severity = [Fluence.Wpf.InfoBarSeverity]::Error
                    $State.InfoBar.IsOpen = $true
                    return
                }
                $State.Result[$button.Name] = $true
                $State.Window.Close()
            }.GetNewClosure())
        }

        if ($i -eq 0)
        {
            $countdownButton = $wpfButton
        }

        $null = $grid.Children.Add($wpfButton)
    }

    $null = $root.Children.Add($grid)
    $window.Content = $border
    if (@($Spec.Buttons | Where-Object { $_.IsCancel }).Count -eq 0)
    {
        $window.add_KeyDown({
            param($sourceWindow, $closeEvent)
            if ($closeEvent.Key -eq [System.Windows.Input.Key]::Escape)
            {
                $closeEvent.Handled = $true
                $sourceWindow.Close()
            }
        })
    }

    # Optional timeout. A DispatcherTimer created here, on the UI thread, ticks once a second, so it
    # works on every host shape (inline STA and the module-owned STA runspace). It starts on Loaded
    # rather than immediately: closing a window that has not been shown yet would make the later
    # ShowDialog throw. When the countdown reaches zero the result is flagged TimedOut and the window
    # closes with no button flag set; the Closed handler leaves Cancelled false for that path. With
    # -Countdown the remaining seconds are appended to the first button's caption, refreshed each tick.
    if ($Spec.ContainsKey('Timeout') -and [int]$Spec.Timeout -gt 0)
    {
        $State.Remaining = [int]$Spec.Timeout
        $captionBase = $null
        if ($Spec.Countdown -and $null -ne $countdownButton)
        {
            $captionBase = [string]$countdownButton.Content
            $countdownButton.Content = "$captionBase ($($State.Remaining))"
        }

        $timer = [System.Windows.Threading.DispatcherTimer]::new()
        $timer.Interval = [timespan]::FromSeconds(1)
        $State.Timer = $timer
        $timer.add_Tick({
            $State.Remaining = $State.Remaining - 1
            if ($State.Remaining -le 0)
            {
                $timer.Stop()
                $State.Result['TimedOut'] = $true
                if ($State.Window.IsVisible)
                {
                    $State.Window.Close()
                }
                return
            }
            if ($null -ne $captionBase)
            {
                $countdownButton.Content = "$captionBase ($($State.Remaining))"
            }
        }.GetNewClosure())
        $window.add_Loaded({ $timer.Start() }.GetNewClosure())
    }

    # When the window closes by any path, stop the timeout timer and mark Cancelled unless a button
    # reported success or the timeout elapsed (a timeout is neither a click nor a user dismissal).
    $window.add_Closed({
        if ($null -ne $State.Timer)
        {
            $State.Timer.Stop()
        }
        $anySuccess = $false
        foreach ($button in $Spec.Buttons)
        {
            if ($State.Result[$button.Name] -eq $true)
            {
                $anySuccess = $true
            }
        }
        if (-not $anySuccess -and $State.Result['TimedOut'] -ne $true)
        {
            $State.Result['Cancelled'] = $true
        }
    }.GetNewClosure())

    return $window
}
