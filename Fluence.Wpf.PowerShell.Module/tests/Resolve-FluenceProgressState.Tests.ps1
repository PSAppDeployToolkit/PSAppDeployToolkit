# Imported at discovery time, not in BeforeAll: InModuleScope below needs the module loaded
# while Pester is still discovering the cases.
Import-Module "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1" -Force

#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }
# The subject is a module-private helper. AGENTS.md forbids dot-sourcing a copy of one, so the
# cases run inside the imported module instead, where the private functions already resolve.
InModuleScope 'Fluence.Wpf.PowerShell' {
    BeforeAll {
        # The production callers pass $PSBoundParameters, which is a PSBoundParametersDictionary rather
        # than a hashtable; this shim reproduces that exact input shape.
        function script:Invoke-ViaBoundParameter
        {
            [CmdletBinding()]
            param([string]$Message, [string]$Detail, [double]$PercentComplete, [switch]$Indeterminate, [hashtable]$Current)
            Resolve-FluenceProgressState -Current $Current -Bound $PSBoundParameters
        }
    }

    Describe 'Resolve-FluenceProgressState' {
        Context 'Bound-parameter dictionary input' {
            It 'reads a real PSBoundParameters dictionary' {
                $s = script:Invoke-ViaBoundParameter -Message 'Copying' -PercentComplete 120 -Current @{ Detail = 'kept' }
                $s.Message | Should -Be 'Copying'
                $s.Detail | Should -Be 'kept'
                $s.PercentComplete | Should -Be 100
                $s.Indeterminate | Should -BeFalse
            }
        }
        Context 'Defaults' {
            It 'starts indeterminate with empty text when nothing is bound' {
                $s = Resolve-FluenceProgressState -Bound @{}
                $s.Message | Should -Be ''
                $s.Detail | Should -Be ''
                $s.PercentComplete | Should -Be 0
                $s.Indeterminate | Should -BeTrue
            }
            It 'keeps the current values that are not rebound' {
                $current = @{ Message = 'Installing'; Detail = 'step 1'; PercentComplete = 25; Indeterminate = $false }
                $s = Resolve-FluenceProgressState -Current $current -Bound @{ Detail = 'step 2' }
                $s.Message | Should -Be 'Installing'
                $s.Detail | Should -Be 'step 2'
                $s.PercentComplete | Should -Be 25
                $s.Indeterminate | Should -BeFalse
            }
        }
        Context 'Percent clamping' {
            It 'clamps <Given> to <Expected> and switches to determinate' -ForEach @(
                @{ Given = -10; Expected = 0 }
                @{ Given = 0; Expected = 0 }
                @{ Given = 42.5; Expected = 42.5 }
                @{ Given = 100; Expected = 100 }
                @{ Given = 250; Expected = 100 }
            ) {
                $s = Resolve-FluenceProgressState -Bound @{ PercentComplete = $Given }
                $s.PercentComplete | Should -Be $Expected
                $s.Indeterminate | Should -BeFalse
            }
            It 'treats NaN as 0' {
                (Resolve-FluenceProgressState -Bound @{ PercentComplete = [double]::NaN }).PercentComplete | Should -Be 0
            }
            It '-Indeterminate wins over a bound percentage' {
                $s = Resolve-FluenceProgressState -Bound @{ PercentComplete = 50; Indeterminate = $true }
                $s.Indeterminate | Should -BeTrue
                $s.PercentComplete | Should -Be 50
            }
            It 'a bound percentage makes a previously indeterminate bar determinate' {
                $s = Resolve-FluenceProgressState -Current @{ Indeterminate = $true } -Bound @{ PercentComplete = 10 }
                $s.Indeterminate | Should -BeFalse
            }
        }
    }

}
