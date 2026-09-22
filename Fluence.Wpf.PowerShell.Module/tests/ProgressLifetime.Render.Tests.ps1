#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }
BeforeAll {
    $script:ModulePath = Join-Path $PSScriptRoot '../src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1'
    Import-Module $script:ModulePath -Force
}

Describe 'Progress lifetime regressions' -Tag UI {
    It 'vetoes a window-close request until Close-FluenceProgress permits it' -Skip:($env:FLUENCE_PS_UI -ne '1') {
        $handle = Show-FluenceProgress -Message 'Close guard' -Backdrop None
        try
        {
            $visible = & (Get-Module Fluence.Wpf.PowerShell) {
                param($h)
                Invoke-OnFluenceUi -Script {
                    param($progress)
                    $progress.Parts.Window.Close()
                    return $progress.Parts.Window.IsVisible
                } -ArgumentList @($h)
            } $handle
            $visible | Should -BeTrue
        }
        finally
        {
            Close-FluenceProgress -Handle $handle
        }
        $handle.IsOpen | Should -BeFalse
    }
    It 'closes progress during reimport and allows the next progress window' -Skip:($env:FLUENCE_PS_UI -ne '1') {
        $handle = Show-FluenceProgress -Message 'Before reimport' -Backdrop None
        try
        {
            Import-Module $script:ModulePath -Force
            $handle.IsOpen | Should -BeFalse
            $next = Show-FluenceProgress -Message 'After reimport' -Backdrop None
            Close-FluenceProgress -Handle $handle
            $next.IsOpen | Should -BeTrue
            Close-FluenceProgress -Handle $next
            $next.IsOpen | Should -BeFalse
        }
        finally
        {
            if ($handle.IsOpen) { Close-FluenceProgress -Handle $handle }
        }
    }
}

Describe 'Dialog keyboard regressions' -Tag UI {
    It 'cancels a dialog without a cancel button on Escape' -Skip:($env:FLUENCE_PS_UI -ne '1') {
        $result = & (Get-Module Fluence.Wpf.PowerShell) {
            Invoke-OnFluenceUi -Script {
                Initialize-FluenceApplication -Theme Light -Backdrop None
                $spec = @{ Title = 'Escape regression'; Message = @('Escape closes this dialog'); Prompts = @(); Buttons = @(New-FluenceButton -Text OK -IsDefault); MinWidth = 320; Backdrop = 'None'; Topmost = $false }
                $state = @{ Result = @{}; Window = $null }
                $window = New-FluenceDialogWindow -Spec $spec -State $state
                $state.Window = $window
                $observed = @{ ClosedByEscape = $false; Error = $null }
                $timer = [System.Windows.Threading.DispatcherTimer]::new()
                $timer.Interval = [timespan]::FromMilliseconds(200)
                $timer.add_Tick({
                    $timer.Stop()
                    try
                    {
                        $source = [System.Windows.PresentationSource]::FromVisual($window)
                        $key = [System.Windows.Input.KeyEventArgs]::new([System.Windows.Input.Keyboard]::PrimaryDevice, $source, 0, [System.Windows.Input.Key]::Escape)
                        $key.RoutedEvent = [System.Windows.Input.Keyboard]::KeyDownEvent
                        $window.RaiseEvent($key)
                        $observed.ClosedByEscape = -not $window.IsVisible
                    }
                    catch
                    {
                        $observed.Error = $_.ToString()
                    }
                    finally
                    {
                        if ($window.IsVisible) { $window.Close() }
                    }
                }.GetNewClosure())
                $timer.Start()
                try
                {
                    $null = $window.ShowDialog()
                    return @{ Cancelled = $state.Result.Cancelled; ClosedByEscape = $observed.ClosedByEscape; Error = $observed.Error }
                }
                finally
                {
                    $timer.Stop()
                }
            }
        }
        $result.Error | Should -BeNullOrEmpty
        $result.ClosedByEscape | Should -BeTrue
        $result.Cancelled | Should -BeTrue
    }
}
