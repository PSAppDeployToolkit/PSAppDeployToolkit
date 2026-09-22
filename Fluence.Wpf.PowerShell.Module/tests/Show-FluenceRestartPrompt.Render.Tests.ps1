#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }

# Opt-in render test for the restart prompt. Tagged UI and skipped unless FLUENCE_PS_UI=1. The prompt
# is driven by its own one-second countdown, so it opens, counts down and closes on every host shape
# without a harness timer.

BeforeAll {
    Import-Module "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1" -Force
}

Describe 'Show-FluenceRestartPrompt render' -Tag UI {
    It 'counts down and returns TimedOut when nobody answers' -Skip:($env:FLUENCE_PS_UI -ne '1') {
        $started = [System.Diagnostics.Stopwatch]::StartNew()
        $outcome = Show-FluenceRestartPrompt -Message 'Render restart prompt' -Countdown 1 -Theme Light -Backdrop None
        $outcome | Should -Be 'TimedOut'
        $started.ElapsedMilliseconds | Should -BeLessThan 9000
    }
}
