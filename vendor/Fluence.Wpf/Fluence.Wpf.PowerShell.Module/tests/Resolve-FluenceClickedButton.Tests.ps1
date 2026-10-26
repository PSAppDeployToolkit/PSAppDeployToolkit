<#
Copyright (c) 2026, Dan Cunningham. All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice,
   this list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

3. Neither the name of the copyright holder nor the names of its
   contributors may be used to endorse or promote products derived from
   this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
#>


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
