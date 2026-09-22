# Imported at discovery time, not in BeforeAll: InModuleScope below needs the module loaded
# while Pester is still discovering the cases.
Import-Module "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1" -Force

#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }
# The subject is a module-private helper. AGENTS.md forbids dot-sourcing a copy of one, so the
# cases run inside the imported module instead, where the private functions already resolve.
InModuleScope 'Fluence.Wpf.PowerShell' {
    Describe 'Resolve-FluenceClickedButton' {

        It 'returns the name of the clicked non-cancel button when its flag is true' {
            $buttons = @(New-FluenceButton 'OK' -IsDefault)
            $result = @{ OK = $true; Cancelled = $false }
            Resolve-FluenceClickedButton -Result $result -Buttons $buttons | Should -Be 'OK'
        }

        It 'returns the cancel button name when result is Cancelled and a cancel button exists' {
            $buttons = @(
                New-FluenceButton 'OK' -IsDefault
                New-FluenceButton 'Cancel' -IsCancel
            )
            $result = @{ OK = $false; Cancel = $false; Cancelled = $true }
            Resolve-FluenceClickedButton -Result $result -Buttons $buttons | Should -Be 'Cancel'
        }

        It 'returns the name of the clicked button even when it is not the first button' {
            $buttons = @(
                New-FluenceButton 'Yes' -IsDefault
                New-FluenceButton 'No'
            )
            $result = @{ Yes = $false; No = $true; Cancelled = $false }
            Resolve-FluenceClickedButton -Result $result -Buttons $buttons | Should -Be 'No'
        }

        It 'maps a dismissal to the last (safe) button when no cancel button exists' {
            $buttons = @(
                New-FluenceButton 'Yes' -IsDefault
                New-FluenceButton 'No'
            )
            $result = @{ Yes = $false; No = $false; Cancelled = $true }
            Resolve-FluenceClickedButton -Result $result -Buttons $buttons | Should -Be 'No'
        }

        It 'maps a dismissal to OK for a single-button OK preset' {
            $buttons = @(New-FluenceButton 'OK' -IsDefault)
            $result = @{ OK = $false; Cancelled = $true }
            Resolve-FluenceClickedButton -Result $result -Buttons $buttons | Should -Be 'OK'
        }

        # --- Runtime shape: [pscustomobject] / Fluence.DialogResult via ConvertTo-FluenceResult ---

        It 'returns the clicked button name when Result is a pscustomobject with the flag true' {
            $buttons = @(New-FluenceButton 'OK' -IsDefault)
            $result = ConvertTo-FluenceResult -Result @{ OK = $true; Cancelled = $false }
            Resolve-FluenceClickedButton -Result $result -Buttons $buttons | Should -Be 'OK'
        }

        It 'returns the cancel button name when pscustomobject result has Cancelled true' {
            $buttons = @(
                New-FluenceButton 'Yes' -IsDefault
                New-FluenceButton 'Cancel' -IsCancel
            )
            $result = ConvertTo-FluenceResult -Result @{ Yes = $false; No = $false; Cancelled = $true }
            Resolve-FluenceClickedButton -Result $result -Buttons $buttons | Should -Be 'Cancel'
        }

        It 'maps a dismissal to the last (safe) button for a pscustomobject result with no cancel button' {
            $buttons = @(
                New-FluenceButton 'Yes' -IsDefault
                New-FluenceButton 'No'
            )
            $result = ConvertTo-FluenceResult -Result @{ Yes = $false; No = $false; Cancelled = $true }
            Resolve-FluenceClickedButton -Result $result -Buttons $buttons | Should -Be 'No'
        }

        # --- Timeout path: TimedOut is set, Cancelled stays false, no button flag is true ---

        It 'maps a timeout to -DefaultButton when one is named' {
            $buttons = @(
                New-FluenceButton 'Yes'
                New-FluenceButton 'No' -IsDefault
            )
            $result = @{ Yes = $false; No = $false; Cancelled = $false; TimedOut = $true }
            Resolve-FluenceClickedButton -Result $result -Buttons $buttons -DefaultButton 'No' | Should -Be 'No'
        }

        It 'maps a timeout without -DefaultButton to the cancel button when one exists' {
            $buttons = @(
                New-FluenceButton 'OK' -IsDefault
                New-FluenceButton 'Cancel' -IsCancel
            )
            $result = @{ OK = $false; Cancel = $false; Cancelled = $false; TimedOut = $true }
            Resolve-FluenceClickedButton -Result $result -Buttons $buttons | Should -Be 'Cancel'
        }

        It 'maps a timeout without -DefaultButton to the last (safe) button when no cancel button exists' {
            $buttons = @(
                New-FluenceButton 'Yes' -IsDefault
                New-FluenceButton 'No'
            )
            $result = ConvertTo-FluenceResult -Result @{ Yes = $false; No = $false; Cancelled = $false; TimedOut = $true }
            Resolve-FluenceClickedButton -Result $result -Buttons $buttons | Should -Be 'No'
        }

        It 'prefers a clicked button over -DefaultButton even when TimedOut is set' {
            $buttons = @(
                New-FluenceButton 'Yes' -IsDefault
                New-FluenceButton 'No'
            )
            $result = @{ Yes = $true; No = $false; Cancelled = $false; TimedOut = $true }
            Resolve-FluenceClickedButton -Result $result -Buttons $buttons -DefaultButton 'No' | Should -Be 'Yes'
        }
    }

}
