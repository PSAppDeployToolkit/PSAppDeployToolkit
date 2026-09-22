#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }

# Logic-lane contract of Show-FluenceRestartPrompt: the outcome mapping (a pure private helper) and
# the parameter validation that fails before any window opens. The live prompt is rendered in
# Show-FluenceRestartPrompt.Render.Tests.ps1.

BeforeAll {
    Import-Module "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1" -Force
    $script:Resolve = { param($r) & (Get-Module Fluence.Wpf.PowerShell) { param($x) Resolve-FluenceRestartOutcome -Result $x } $r }
}

Describe 'Show-FluenceRestartPrompt' {
    Context 'Outcome mapping' {
        It 'maps the Restart flag to Restart' {
            & $script:Resolve @{ Restart = $true; Later = $false; Cancelled = $false; TimedOut = $false } | Should -Be 'Restart'
        }
        It 'maps a timeout to TimedOut' {
            & $script:Resolve @{ Restart = $false; Later = $false; Cancelled = $false; TimedOut = $true } | Should -Be 'TimedOut'
        }
        It 'maps the Restart later button (a cancel dismissal) to Later' {
            & $script:Resolve @{ Restart = $false; Later = $false; Cancelled = $true; TimedOut = $false } | Should -Be 'Later'
        }
        It 'maps a Fluence.DialogResult object the same way' {
            $result = & (Get-Module Fluence.Wpf.PowerShell) { ConvertTo-FluenceResult -Result @{ Restart = $false; Later = $false; Cancelled = $false; TimedOut = $true } }
            & $script:Resolve $result | Should -Be 'TimedOut'
        }
        It 'prefers Restart over TimedOut when both are set' {
            & $script:Resolve @{ Restart = $true; TimedOut = $true } | Should -Be 'Restart'
        }
    }
    Context 'Parameter validation' {
        It 'rejects -Countdown together with -NoCountdown' {
            { Show-FluenceRestartPrompt -Countdown 10 -NoCountdown } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }
        It 'rejects a -Countdown of <Value>' -ForEach @(
            @{ Value = 0 }
            @{ Value = 90000 }
        ) {
            { Show-FluenceRestartPrompt -Countdown $Value } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }
    }
}
