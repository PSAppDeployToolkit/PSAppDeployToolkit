#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }

# Opt-in render tests for the list selection dialog. Tagged UI and skipped unless FLUENCE_PS_UI=1.
# The dialogs close on their own one-second timeout, which returns $null; the selection contract
# itself (single item, array for -MultiSelect) is asserted through the private control builder on the
# UI thread, where the ListView selection can be driven without a click.

BeforeAll {
    Import-Module "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1" -Force
}

Describe 'Show-FluenceListSelection render' -Tag UI {
    It 'opens, times out, and returns $null' -Skip:($env:FLUENCE_PS_UI -ne '1') {
        $result = Show-FluenceListSelection -Message 'Render list' -Items 'Alpha', 'Beta', 'Gamma' -Timeout 1 -Theme Light -Backdrop None
        $result | Should -BeNullOrEmpty
    }

    It 'a List prompt seeds and captures a single selection or an array for MultiSelect' -Skip:($env:FLUENCE_PS_UI -ne '1') {
        $facts = & (Get-Module Fluence.Wpf.PowerShell) {
            Invoke-OnFluenceUi -Script {
                $single = New-FluencePrompt -Name One -Message 'one' -InputType List -ValidateSet 'A', 'B', 'C' -DefaultValue 'B'
                $multi = New-FluencePrompt -Name Many -Message 'many' -InputType List -ValidateSet 'A', 'B', 'C' -MultiSelect -DefaultValue 'C'
                $state = @{ Result = @{} }
                $singleControl = New-FluenceInputControl -Prompt $single -State $state
                $multiControl = New-FluenceInputControl -Prompt $multi -State $state

                $seededSingle = $state.Result['One']
                $seededMany = $state.Result['Many']

                # Drive the selections the way a click would.
                $singleControl.SelectedItem = 'A'
                $multiControl.SelectedItems.Add('A')

                @{
                    SeededSingle   = $seededSingle
                    SeededManyType = $seededMany.GetType().FullName
                    SeededMany     = @($seededMany)
                    Single         = $state.Result['One']
                    Many           = @($state.Result['Many'])
                    ManyIsArray    = ($state.Result['Many'] -is [System.Array])
                    MultiMode      = $multiControl.SelectionMode.ToString()
                    SingleMode     = $singleControl.SelectionMode.ToString()
                }
            }
        }

        $facts.SeededSingle | Should -Be 'B'
        $facts.SeededManyType | Should -Be 'System.Object[]'
        $facts.SeededMany | Should -Be @('C')
        $facts.Single | Should -Be 'A'
        $facts.ManyIsArray | Should -BeTrue
        ($facts.Many | Sort-Object) | Should -Be @('A', 'C')
        $facts.MultiMode | Should -Be 'Extended'
        $facts.SingleMode | Should -Be 'Single'
    }
}
