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
