#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }

# Opt-in render test for the Show-FluenceDialog UI path. Every It is tagged UI and skips unless
# FLUENCE_PS_UI=1, mirroring the library's opt-in Screenshots pattern. The dialog is driven and
# closed by a DispatcherTimer created ON the UI thread (inside the Invoke-OnFluenceUi scriptblock),
# which is the only pattern that works on every host shape: inline-STA (powershell, pwsh) and the
# module-owned STA runspace used by an MTA host (pwsh -mta).
#
# The harness scriptblock is fully self-contained: its only inputs are the spec hashtable and a
# bool, and it resolves the private renderer helpers through the module object (& $module { ... }),
# so it behaves identically whether Invoke-OnFluenceUi runs it inline or transports it by text to
# the STA runspace (where only public functions sit at top level).

# PSScriptAnalyzer cannot trace parameter usage inside a nested scriptblock assigned to a variable;
# $clickFirstButton is read at runtime inside $script:RenderHarness via the timer Tick closure.
[Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'clickFirstButton',
    Justification = 'Parameter is used inside a scriptblock that PSScriptAnalyzer cannot statically trace.')]
param()

BeforeAll {
    $script:ModulePath = Join-Path $PSScriptRoot '..\src\Fluence.Wpf.PowerShell\Fluence.Wpf.PowerShell.psd1'
    Import-Module $script:ModulePath -Force

    # Self-contained UI-thread harness. Ensures the Application + theme slots, builds the dialog
    # window via the private builder (through the module object), and attaches a DispatcherTimer
    # that optionally clicks the first button then closes. Returns the collected result hashtable.
    $script:RenderHarness = {
        param($spec, $clickFirstButton)

        # Seed the Application and the theme slots through the module's own initializer (via the module

        # object) so the harness never re-implements the library call and follows any API change once.
        $module = Get-Module Fluence.Wpf.PowerShell
        & $module {
            param($s)
            Initialize-FluenceApplication -Theme $s.Theme -Backdrop $s.Backdrop -Accent $s.AccentColor
        } $spec
        $state = @{ Result = @{}; Window = $null }
        $window = & $module {
            param($s, $st)
            New-FluenceDialogWindow -Spec $s -State $st
        } $spec $state
        $state.Window = $window

        $timer = [System.Windows.Threading.DispatcherTimer]::new()
        $timer.Interval = [timespan]::FromSeconds(2)
        $timer.add_Tick({
            $timer.Stop()
            try
            {
                if ($clickFirstButton)
                {
                    # Iterative depth-first walk of the visual tree (no nested function, which a
                    # deferred dispatcher callback cannot resolve) to find the first Fluence Button.
                    $button = $null
                    $stack = [System.Collections.Generic.Stack[System.Windows.DependencyObject]]::new()
                    $stack.Push($window)
                    while ($stack.Count -gt 0 -and $null -eq $button)
                    {
                        $node = $stack.Pop()
                        $childCount = [System.Windows.Media.VisualTreeHelper]::GetChildrenCount($node)
                        for ($i = 0; $i -lt $childCount; $i++)
                        {
                            $child = [System.Windows.Media.VisualTreeHelper]::GetChild($node, $i)
                            if ($child -is [Fluence.Wpf.Controls.Button])
                            {
                                $button = $child
                                break
                            }
                            $stack.Push($child)
                        }
                    }
                    if ($null -ne $button)
                    {
                        $button.RaiseEvent([System.Windows.RoutedEventArgs]::new([System.Windows.Controls.Primitives.ButtonBase]::ClickEvent))
                    }
                }
            }
            finally
            {
                if ($window.IsVisible)
                {
                    $window.Close()
                }
            }
        }.GetNewClosure())
        $timer.Start()

        $null = $window.ShowDialog()
        return $state.Result
    }

    # Runs the harness on the UI thread through the private Invoke-OnFluenceUi router (via the module
    # object), so the test body does not need module-private functions in its own scope.
    function script:RunHarness
    {
        param([scriptblock]$Harness, [object[]]$Arguments)
        return (& (Get-Module Fluence.Wpf.PowerShell) {
                param($h, $a)
                Invoke-OnFluenceUi -Script $h -ArgumentList $a
            } $Harness $Arguments)
    }

    # Projects the raw result hashtable through the private converter (via the module object).
    function script:ConvertResult
    {
        param([hashtable]$Hash)
        return (& (Get-Module Fluence.Wpf.PowerShell) {
                param($h)
                ConvertTo-FluenceResult -Result $h
            } $Hash)
    }

    # Unwraps the Invoke-OnFluenceUi return (a collection from the STA runspace) to the hashtable.
    function script:Unwrap
    {
        param($Raw)
        if ($Raw -is [System.Collections.IList] -and $Raw.Count -ge 1)
        {
            return $Raw[$Raw.Count - 1]
        }
        return $Raw
    }
}

Describe 'Show-FluenceDialog render' -Tag UI {

    It 'returns the prompt value and OK flag on the success path' -Skip:($env:FLUENCE_PS_UI -ne '1') {
        $spec = @{
            Title        = 'Render Success'
            Message      = @('Enter your city.')
            Prompts      = @(New-FluencePrompt -Name City -Message 'City' -DefaultValue 'Leeds')
            Buttons      = @(New-FluenceButton -Text 'OK' -IsDefault)
            Theme        = 'Light'
            Backdrop     = 'None'
            AccentColor  = $null
            MinWidth     = 360
            Topmost      = $false
            ParentWindow = $null
        }

        $raw = script:RunHarness -Harness $script:RenderHarness -Arguments @($spec, $true)
        $result = script:ConvertResult -Hash (script:Unwrap -Raw $raw)

        $result.City | Should -Be 'Leeds'
        $result.OK | Should -BeTrue
        $result.Cancelled | Should -BeFalse
    }

    It 'renders a leading severity FontIcon when Icon is Success' -Skip:($env:FLUENCE_PS_UI -ne '1') {
        # Custom harness that builds the window, walks the root panel for the leading severity
        # FontIcon (the message no longer renders inside an InfoBar), then closes.
        # Returns @{ NoException=$true; FoundFontIcon=$bool; Glyph=$string }.
        $iconHarness = {
            param($spec)

            # Seed the Application and the theme slots through the module's own initializer (via the module

            # object) so the harness never re-implements the library call and follows any API change once.
            $module = Get-Module Fluence.Wpf.PowerShell
            & $module {
                param($s)
                Initialize-FluenceApplication -Theme $s.Theme -Backdrop $s.Backdrop -Accent $s.AccentColor
            } $spec
            $state = @{ Result = @{}; Window = $null }
            $window = & $module {
                param($s, $st)
                New-FluenceDialogWindow -Spec $s -State $st
            } $spec $state
            $state.Window = $window

            # The message row is a Grid (leading FontIcon + message StackPanel) added to the root
            # StackPanel. Walk the root panel's children for that Grid and find the FontIcon inside.
            # Do this before ShowDialog so the check is synchronous and reliable.
            $foundFontIcon = $false
            $glyph = $null
            $border = $window.Content
            if ($null -ne $border)
            {
                $panel = $border.Child
                if ($null -ne $panel)
                {
                    foreach ($child in $panel.Children)
                    {
                        if ($child -is [System.Windows.Controls.Grid])
                        {
                            foreach ($gridChild in $child.Children)
                            {
                                if ($gridChild -is [Fluence.Wpf.Controls.FontIcon])
                                {
                                    $foundFontIcon = $true
                                    $glyph = $gridChild.Glyph
                                }
                            }
                        }
                    }
                }
            }

            $timer = [System.Windows.Threading.DispatcherTimer]::new()
            $timer.Interval = [timespan]::FromMilliseconds(400)
            $timer.add_Tick({
                $timer.Stop()
                if ($window.IsVisible)
                {
                    $window.Close()
                }
            }.GetNewClosure())
            $timer.Start()

            $null = $window.ShowDialog()
            return @{ NoException = $true; FoundFontIcon = $foundFontIcon; Glyph = $glyph }
        }

        $spec = @{
            Title        = 'Icon FontIcon'
            Message      = @('Operation completed successfully.')
            Icon         = 'Success'
            Prompts      = @()
            Buttons      = @(New-FluenceButton -Text 'OK' -IsDefault)
            Theme        = 'Light'
            Backdrop     = 'None'
            AccentColor  = $null
            MinWidth     = 360
            Topmost      = $false
            ParentWindow = $null
        }

        $raw = script:RunHarness -Harness $iconHarness -Arguments @($spec)
        $result = script:Unwrap -Raw $raw

        $result.NoException | Should -BeTrue
        $result.FoundFontIcon | Should -BeTrue
        $result.Glyph | Should -Not -BeNullOrEmpty
    }

    It 'constructs every input type and returns a key per prompt' -Skip:($env:FLUENCE_PS_UI -ne '1') {
        $prompts = @(
            New-FluencePrompt -Name P_Text -Message 'Text' -InputType Text -DefaultValue 'abc'
            New-FluencePrompt -Name P_Multiline -Message 'Multiline' -InputType Multiline -DefaultValue "a`nb"
            New-FluencePrompt -Name P_Password -Message 'Password' -InputType Password -DefaultValue 'secret'
            New-FluencePrompt -Name P_Number -Message 'Number' -InputType Number -DefaultValue 42
            New-FluencePrompt -Name P_Checkbox -Message 'Checkbox' -InputType Checkbox -DefaultValue $true
            New-FluencePrompt -Name P_Toggle -Message 'Toggle' -InputType Toggle -DefaultValue $true
            New-FluencePrompt -Name P_Combo -Message 'Combo' -InputType Choice -ValidateSet 'One', 'Two' -As Combo -DefaultValue 'One'
            New-FluencePrompt -Name P_Radio -Message 'Radio' -InputType Choice -ValidateSet 'Red', 'Green' -As Radio -DefaultValue 'Red'
            New-FluencePrompt -Name P_List -Message 'List' -InputType List -ValidateSet 'Alpha', 'Beta' -DefaultValue 'Beta'
            New-FluencePrompt -Name P_MultiList -Message 'MultiList' -InputType List -ValidateSet 'Alpha', 'Beta' -MultiSelect -DefaultValue 'Alpha'
            New-FluencePrompt -Name P_Date -Message 'Date' -InputType Date -DefaultValue ([datetime]'2026-01-01')
            New-FluencePrompt -Name P_Time -Message 'Time' -InputType Time -DefaultValue ([timespan]'09:30:00')
            New-FluencePrompt -Name P_FileOpen -Message 'FileOpen' -InputType FileOpen -DefaultValue 'C:\file.txt'
            New-FluencePrompt -Name P_FileSave -Message 'FileSave' -InputType FileSave -DefaultValue 'C:\out.txt'
            New-FluencePrompt -Name P_FolderOpen -Message 'FolderOpen' -InputType FolderOpen -DefaultValue 'C:\folder'
            New-FluencePrompt -Name P_Link -Message 'Link' -InputType Link -DefaultValue 'https://example.com'
        )

        $spec = @{
            Title        = 'Render All Types'
            Message      = @('Every input type.')
            Prompts      = $prompts
            Buttons      = @(New-FluenceButton -Text 'OK' -IsDefault)
            Theme        = 'Light'
            Backdrop     = 'None'
            AccentColor  = $null
            MinWidth     = 420
            Topmost      = $false
            ParentWindow = $null
        }

        # Timer just closes the window (no button click) so no value capture is exercised.
        $raw = script:RunHarness -Harness $script:RenderHarness -Arguments @($spec, $false)
        $result = script:ConvertResult -Hash (script:Unwrap -Raw $raw)

        foreach ($prompt in $prompts)
        {
            $result.PSObject.Properties.Name | Should -Contain $prompt.Name
        }
        $result.Cancelled | Should -BeTrue
    }

    It 'button Grid: 2-button layout has equal columns, Stretch, Accent on default, correct order' -Skip:($env:FLUENCE_PS_UI -ne '1') {
        # Custom harness: builds the window and inspects the button Grid before ShowDialog.
        # Returns a hashtable of observable layout properties without requiring a live render cycle.
        $layoutHarness2 = {
            param($spec)

            # Seed the Application and the theme slots through the module's own initializer (via the module

            # object) so the harness never re-implements the library call and follows any API change once.
            $module = Get-Module Fluence.Wpf.PowerShell
            & $module {
                param($s)
                Initialize-FluenceApplication -Theme $s.Theme -Backdrop $s.Backdrop -Accent $s.AccentColor
            } $spec
            $state = @{ Result = @{}; Window = $null }
            $window = & $module {
                param($s, $st)
                New-FluenceDialogWindow -Spec $s -State $st
            } $spec $state
            $state.Window = $window

            # Locate the button Grid: last direct child of the root StackPanel (the Border's child).
            $border = $window.Content
            $panel = $border.Child
            $buttonGrid = $panel.Children[$panel.Children.Count - 1]

            # Collect per-button observable properties by walking Grid.Children.
            $buttonProps = @()
            foreach ($child in $buttonGrid.Children)
            {
                if ($child -is [Fluence.Wpf.Controls.Button])
                {
                    $buttonProps += @{
                        Col         = [System.Windows.Controls.Grid]::GetColumn($child)
                        IsDefault   = $child.IsDefault
                        IsCancel    = $child.IsCancel
                        Appearance  = $child.Appearance
                        HAlign      = $child.HorizontalAlignment
                        MinWidth    = $child.MinWidth
                    }
                }
            }

            $timer = [System.Windows.Threading.DispatcherTimer]::new()
            $timer.Interval = [timespan]::FromMilliseconds(400)
            $timer.add_Tick({
                $timer.Stop()
                if ($window.IsVisible)
                {
                    $window.Close()
                }
            }.GetNewClosure())
            $timer.Start()

            $null = $window.ShowDialog()
            return @{
                ColumnCount = $buttonGrid.ColumnDefinitions.Count
                Buttons     = $buttonProps
            }
        }

        $spec2 = @{
            Title        = 'Layout 2'
            Message      = @()
            Prompts      = @()
            Buttons      = @(
                New-FluenceButton -Text 'OK' -IsDefault
                New-FluenceButton -Text 'Cancel' -IsCancel
            )
            Theme        = 'Light'
            Backdrop     = 'None'
            AccentColor  = $null
            MinWidth     = 360
            Topmost      = $false
            ParentWindow = $null
        }

        $raw2 = script:RunHarness -Harness $layoutHarness2 -Arguments @($spec2)
        $r2 = script:Unwrap -Raw $raw2

        # Grid must have exactly as many columns as buttons.
        $r2.ColumnCount | Should -Be 2

        # Every button must be Stretch / MinWidth 0.
        foreach ($bp in $r2.Buttons)
        {
            $bp.HAlign | Should -Be ([System.Windows.HorizontalAlignment]::Stretch)
            $bp.MinWidth | Should -Be 0
        }

        # IsDefault button occupies column 0 and is Accent.
        $defaultBtn = $r2.Buttons | Where-Object { $_.IsDefault }
        $defaultBtn.Col | Should -Be 0
        $defaultBtn.Appearance | Should -Be ([Fluence.Wpf.ControlAppearance]::Accent)

        # IsCancel button occupies the last column and is not Accent.
        $cancelBtn = $r2.Buttons | Where-Object { $_.IsCancel }
        $cancelBtn.Col | Should -Be ($r2.ColumnCount - 1)
        $cancelBtn.Appearance | Should -Not -Be ([Fluence.Wpf.ControlAppearance]::Accent)
    }

    It 'button Grid: 3-button layout has equal columns, default col 0, cancel col 2, middle not Accent' -Skip:($env:FLUENCE_PS_UI -ne '1') {
        $layoutHarness3 = {
            param($spec)

            # Seed the Application and the theme slots through the module's own initializer (via the module

            # object) so the harness never re-implements the library call and follows any API change once.
            $module = Get-Module Fluence.Wpf.PowerShell
            & $module {
                param($s)
                Initialize-FluenceApplication -Theme $s.Theme -Backdrop $s.Backdrop -Accent $s.AccentColor
            } $spec
            $state = @{ Result = @{}; Window = $null }
            $window = & $module {
                param($s, $st)
                New-FluenceDialogWindow -Spec $s -State $st
            } $spec $state
            $state.Window = $window

            $border = $window.Content
            $panel = $border.Child
            $buttonGrid = $panel.Children[$panel.Children.Count - 1]

            $buttonProps = @()
            foreach ($child in $buttonGrid.Children)
            {
                if ($child -is [Fluence.Wpf.Controls.Button])
                {
                    $buttonProps += @{
                        Col        = [System.Windows.Controls.Grid]::GetColumn($child)
                        IsDefault  = $child.IsDefault
                        IsCancel   = $child.IsCancel
                        Appearance = $child.Appearance
                        HAlign     = $child.HorizontalAlignment
                        MinWidth   = $child.MinWidth
                    }
                }
            }

            $timer = [System.Windows.Threading.DispatcherTimer]::new()
            $timer.Interval = [timespan]::FromMilliseconds(400)
            $timer.add_Tick({
                $timer.Stop()
                if ($window.IsVisible)
                {
                    $window.Close()
                }
            }.GetNewClosure())
            $timer.Start()

            $null = $window.ShowDialog()
            return @{
                ColumnCount = $buttonGrid.ColumnDefinitions.Count
                Buttons     = $buttonProps
            }
        }

        $spec3 = @{
            Title        = 'Layout 3'
            Message      = @()
            Prompts      = @()
            Buttons      = @(
                New-FluenceButton -Text 'Apply' -IsDefault
                New-FluenceButton -Text 'More'
                New-FluenceButton -Text 'Cancel' -IsCancel
            )
            Theme        = 'Light'
            Backdrop     = 'None'
            AccentColor  = $null
            MinWidth     = 360
            Topmost      = $false
            ParentWindow = $null
        }

        $raw3 = script:RunHarness -Harness $layoutHarness3 -Arguments @($spec3)
        $r3 = script:Unwrap -Raw $raw3

        $r3.ColumnCount | Should -Be 3

        foreach ($bp in $r3.Buttons)
        {
            $bp.HAlign | Should -Be ([System.Windows.HorizontalAlignment]::Stretch)
            $bp.MinWidth | Should -Be 0
        }

        $defaultBtn = $r3.Buttons | Where-Object { $_.IsDefault }
        $defaultBtn.Col | Should -Be 0
        $defaultBtn.Appearance | Should -Be ([Fluence.Wpf.ControlAppearance]::Accent)

        $cancelBtn = $r3.Buttons | Where-Object { $_.IsCancel }
        $cancelBtn.Col | Should -Be ($r3.ColumnCount - 1)
        $cancelBtn.Appearance | Should -Not -Be ([Fluence.Wpf.ControlAppearance]::Accent)

        # The middle button (neither default nor cancel) must be in column 1 and not Accent.
        $middleBtn = $r3.Buttons | Where-Object { -not $_.IsDefault -and -not $_.IsCancel }
        $middleBtn.Col | Should -Be 1
        $middleBtn.Appearance | Should -Not -Be ([Fluence.Wpf.ControlAppearance]::Accent)
    }

    It 'button Grid: 1-button layout has 2 columns, button in right column sized as half the grid' -Skip:($env:FLUENCE_PS_UI -ne '1') {
        # Custom harness that measures rendered widths in the timer tick (so layout has run), then
        # closes. Width facts are carried back through the $state hashtable (a reference shared with
        # the timer closure), since assignment to a $script: variable inside GetNewClosure does not
        # surface to the harness return value.
        $layoutHarness1 = {
            param($spec)

            # Seed the Application and the theme slots through the module's own initializer (via the module

            # object) so the harness never re-implements the library call and follows any API change once.
            $module = Get-Module Fluence.Wpf.PowerShell
            & $module {
                param($s)
                Initialize-FluenceApplication -Theme $s.Theme -Backdrop $s.Backdrop -Accent $s.AccentColor
            } $spec
            $state = @{ Result = @{}; Window = $null; Facts = $null }
            $window = & $module {
                param($s, $st)
                New-FluenceDialogWindow -Spec $s -State $st
            } $spec $state
            $state.Window = $window

            $border = $window.Content
            $panel = $border.Child
            $buttonGrid = $panel.Children[$panel.Children.Count - 1]

            $timer = [System.Windows.Threading.DispatcherTimer]::new()
            $timer.Interval = [timespan]::FromMilliseconds(500)
            $timer.add_Tick({
                $timer.Stop()
                try
                {
                    $theButton = $null
                    foreach ($child in $buttonGrid.Children)
                    {
                        if ($child -is [Fluence.Wpf.Controls.Button])
                        {
                            $theButton = $child
                        }
                    }
                    $state.Facts = @{
                        ColumnCount = $buttonGrid.ColumnDefinitions.Count
                        Col         = [System.Windows.Controls.Grid]::GetColumn($theButton)
                        HAlign      = $theButton.HorizontalAlignment
                        MinWidth    = $theButton.MinWidth
                        IsDefault   = $theButton.IsDefault
                        Appearance  = $theButton.Appearance
                        GridWidth   = $buttonGrid.ActualWidth
                        ButtonWidth = $theButton.ActualWidth
                    }
                }
                finally
                {
                    if ($window.IsVisible)
                    {
                        $window.Close()
                    }
                }
            }.GetNewClosure())
            $timer.Start()

            $null = $window.ShowDialog()
            return $state.Facts
        }

        $spec1 = @{
            Title        = 'Layout 1'
            Message      = @('A message line giving the dialog a stable width to measure against.')
            Prompts      = @()
            Buttons      = @(New-FluenceButton -Text 'OK' -IsDefault)
            Theme        = 'Light'
            Backdrop     = 'None'
            AccentColor  = $null
            MinWidth     = 360
            Topmost      = $false
            ParentWindow = $null
        }

        $raw1 = script:RunHarness -Harness $layoutHarness1 -Arguments @($spec1)
        $f1 = script:Unwrap -Raw $raw1

        # Two columns so the lone button occupies half the width; it sits in the right column.
        $f1.ColumnCount | Should -Be 2
        $f1.Col | Should -Be 1
        $f1.HAlign | Should -Be ([System.Windows.HorizontalAlignment]::Stretch)
        $f1.MinWidth | Should -Be 0
        $f1.IsDefault | Should -BeTrue
        $f1.Appearance | Should -Be ([Fluence.Wpf.ControlAppearance]::Accent)

        # "Sized as if there were two buttons": its rendered width is about half the grid.
        # (A lone button in a half column carries a 4px left margin, so it is just under half.)
        $f1.GridWidth | Should -BeGreaterThan 0
        $half = $f1.GridWidth / 2
        $f1.ButtonWidth | Should -BeGreaterThan ($half - 12)
        $f1.ButtonWidth | Should -BeLessThan ($half + 1)
    }

    It 'closes on its own after -Timeout and flags TimedOut without Cancelled' -Skip:($env:FLUENCE_PS_UI -ne '1') {
        # No harness timer: the dialog's own timeout must close it. A 10 second fallback closes the
        # window if the timeout never fires, so a regression fails the test instead of hanging it.
        $timeoutHarness = {
            param($spec)

            $module = Get-Module Fluence.Wpf.PowerShell
            & $module {
                param($s)
                Initialize-FluenceApplication -Theme $s.Theme -Backdrop $s.Backdrop -Accent $s.AccentColor
            } $spec

            $state = @{ Result = @{}; Window = $null }
            $window = & $module {
                param($s, $st)
                New-FluenceDialogWindow -Spec $s -State $st
            } $spec $state
            $state.Window = $window

            $fallback = [System.Windows.Threading.DispatcherTimer]::new()
            $fallback.Interval = [timespan]::FromSeconds(10)
            $fallback.add_Tick({
                $fallback.Stop()
                if ($window.IsVisible)
                {
                    $window.Close()
                }
            }.GetNewClosure())
            $fallback.Start()

            $started = [System.Diagnostics.Stopwatch]::StartNew()
            $null = $window.ShowDialog()
            $fallback.Stop()
            $state.Result['ElapsedMs'] = $started.ElapsedMilliseconds
            return $state.Result
        }

        $spec = @{
            Title        = 'Render Timeout'
            Message      = @('Closes in one second.')
            Prompts      = @()
            Buttons      = @(New-FluenceButton -Text 'OK' -IsDefault)
            Theme        = 'Light'
            Backdrop     = 'None'
            AccentColor  = $null
            MinWidth     = 360
            Topmost      = $false
            ParentWindow = $null
            Timeout      = 1
            Countdown    = $false
        }

        $raw = script:RunHarness -Harness $timeoutHarness -Arguments @($spec)
        $result = script:Unwrap -Raw $raw

        $result.TimedOut | Should -BeTrue
        $result.Cancelled | Should -BeFalse
        $result.OK | Should -BeFalse
        $result.ElapsedMs | Should -BeLessThan 9000
    }

    It 'shows the remaining seconds on the default button caption when -Countdown is set' -Skip:($env:FLUENCE_PS_UI -ne '1') {
        # The caption is stamped at build time, so it is read before ShowDialog; the dialog then
        # closes on its own two-second timeout.
        $captionHarness = {
            param($spec)

            $module = Get-Module Fluence.Wpf.PowerShell
            & $module {
                param($s)
                Initialize-FluenceApplication -Theme $s.Theme -Backdrop $s.Backdrop -Accent $s.AccentColor
            } $spec

            $state = @{ Result = @{}; Window = $null }
            $window = & $module {
                param($s, $st)
                New-FluenceDialogWindow -Spec $s -State $st
            } $spec $state
            $state.Window = $window

            $border = $window.Content
            $panel = $border.Child
            $buttonGrid = $panel.Children[$panel.Children.Count - 1]
            $caption = $null
            foreach ($child in $buttonGrid.Children)
            {
                if ($child -is [Fluence.Wpf.Controls.Button] -and $child.IsDefault)
                {
                    $caption = [string]$child.Content
                }
            }

            $fallback = [System.Windows.Threading.DispatcherTimer]::new()
            $fallback.Interval = [timespan]::FromSeconds(10)
            $fallback.add_Tick({
                $fallback.Stop()
                if ($window.IsVisible)
                {
                    $window.Close()
                }
            }.GetNewClosure())
            $fallback.Start()

            $null = $window.ShowDialog()
            $fallback.Stop()
            return @{ Caption = $caption; TimedOut = $state.Result['TimedOut'] }
        }

        $spec = @{
            Title        = 'Render Countdown'
            Message      = @('Counting down.')
            Prompts      = @()
            Buttons      = @(
                New-FluenceButton -Text 'Restart now' -IsDefault
                New-FluenceButton -Text 'Later'
            )
            Theme        = 'Light'
            Backdrop     = 'None'
            AccentColor  = $null
            MinWidth     = 360
            Topmost      = $false
            ParentWindow = $null
            Timeout      = 2
            Countdown    = $true
        }

        $raw = script:RunHarness -Harness $captionHarness -Arguments @($spec)
        $result = script:Unwrap -Raw $raw

        $result.Caption | Should -Be 'Restart now (2)'
        $result.TimedOut | Should -BeTrue
    }

    It 'renders an -Image above the message, honouring MessageAlignment and Position' -Skip:($env:FLUENCE_PS_UI -ne '1') {
        # A real PNG written to $TestDrive, rendered through the Fluence Image control. The window is
        # positioned bottom-right; its Loaded handler pins it inside the work area, which is checked
        # once the window has laid out (in the timer tick) before it closes.
        Add-Type -AssemblyName System.Drawing
        $png = Join-Path $TestDrive 'render-logo.png'
        $bitmap = [System.Drawing.Bitmap]::new(200, 60)
        try
        {
            $bitmap.Save($png, [System.Drawing.Imaging.ImageFormat]::Png)
        }
        finally
        {
            $bitmap.Dispose()
        }

        $imageHarness = {
            param($spec)

            $module = Get-Module Fluence.Wpf.PowerShell
            & $module {
                param($s)
                Initialize-FluenceApplication -Theme $s.Theme -Backdrop $s.Backdrop -Accent $s.AccentColor
            } $spec

            $state = @{ Result = @{}; Window = $null; Facts = $null }
            $window = & $module {
                param($s, $st)
                New-FluenceDialogWindow -Spec $s -State $st
            } $spec $state
            $state.Window = $window

            $image = $null
            $messageAlignment = $null
            $panel = $window.Content.Child
            foreach ($child in $panel.Children)
            {
                if ($child -is [Fluence.Wpf.Controls.Image])
                {
                    $image = $child
                }
                if ($child -is [System.Windows.Controls.StackPanel] -and $child.Children.Count -gt 0 -and $child.Children[0] -is [System.Windows.Controls.TextBlock])
                {
                    $messageAlignment = $child.Children[0].TextAlignment
                }
            }

            $timer = [System.Windows.Threading.DispatcherTimer]::new()
            $timer.Interval = [timespan]::FromMilliseconds(600)
            $timer.add_Tick({
                $timer.Stop()
                try
                {
                    $workArea = [System.Windows.SystemParameters]::WorkArea
                    $state.Facts = @{
                        HasImage         = ($null -ne $image)
                        HasSource        = ($null -ne $image -and $null -ne $image.Source)
                        ImageMaxHeight   = $image.MaxHeight
                        ImageAlignment   = $image.HorizontalAlignment.ToString()
                        MessageAlignment = [string]$messageAlignment
                        StartupLocation  = $window.WindowStartupLocation.ToString()
                        RightGap         = $workArea.Right - ($window.Left + $window.ActualWidth)
                        BottomGap        = $workArea.Bottom - ($window.Top + $window.ActualHeight)
                    }
                }
                finally
                {
                    if ($window.IsVisible)
                    {
                        $window.Close()
                    }
                }
            }.GetNewClosure())
            $timer.Start()

            $null = $window.ShowDialog()
            return $state.Facts
        }

        $spec = @{
            Title            = 'Render Image'
            Message          = @('Branded message.')
            Prompts          = @()
            Buttons          = @(New-FluenceButton -Text 'OK' -IsDefault)
            Theme            = 'Light'
            Backdrop         = 'None'
            AccentColor      = $null
            MinWidth         = 360
            Topmost          = $false
            ParentWindow     = $null
            Image            = ([System.Uri]$png).AbsoluteUri
            MessageAlignment = 'Center'
            Position         = 'BottomRight'
        }

        $raw = script:RunHarness -Harness $imageHarness -Arguments @($spec)
        $facts = script:Unwrap -Raw $raw

        $facts.HasImage | Should -BeTrue
        $facts.HasSource | Should -BeTrue
        $facts.ImageMaxHeight | Should -Be 120
        $facts.ImageAlignment | Should -Be 'Center'
        $facts.MessageAlignment | Should -Be 'Center'
        $facts.StartupLocation | Should -Be 'Manual'
        [math]::Round($facts.RightGap) | Should -Be 16
        [math]::Round($facts.BottomGap) | Should -Be 16
    }
}
