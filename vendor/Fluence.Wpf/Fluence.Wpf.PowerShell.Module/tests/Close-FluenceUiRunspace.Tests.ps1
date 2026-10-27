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
BeforeAll { Import-Module "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1" -Force }

Describe 'Close-FluenceUiRunspace' {
    It 'disposes and nulls an open STA runspace, idempotently' {
        InModuleScope Fluence.Wpf.PowerShell {
            $script:StaRunspace = [runspacefactory]::CreateRunspace()
            $script:StaRunspace.ApartmentState = 'STA'
            $script:StaRunspace.Open()
            Close-FluenceUiRunspace
            $script:StaRunspace | Should -BeNullOrEmpty
            { Close-FluenceUiRunspace } | Should -Not -Throw
        }
    }
    It 'is a no-op when no runspace was ever opened' {
        InModuleScope Fluence.Wpf.PowerShell {
            $script:StaRunspace = $null
            { Close-FluenceUiRunspace } | Should -Not -Throw
        }
    }
    It 'keeps the runspace that hosts the WPF Application alive across a module re-import (MTA only)' -Skip:([System.Threading.Thread]::CurrentThread.GetApartmentState() -ne [System.Threading.ApartmentState]::MTA) {
        # Creating the Application shows no window, so this stays in the logic lane. WPF allows one
        # Application per AppDomain for the life of the process, so the thread that created it must
        # survive Remove-Module and be adopted by the next import rather than replaced.
        $before = & (Get-Module Fluence.Wpf.PowerShell) {
            $null = Invoke-OnFluenceUi -Script { Initialize-FluenceApplication -Theme Light -Backdrop None }
            $script:StaRunspace.InstanceId
        }
        Import-Module "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1" -Force
        $after = & (Get-Module Fluence.Wpf.PowerShell) {
            $null = Invoke-OnFluenceUi -Script { Initialize-FluenceApplication -Theme Light -Backdrop None }
            $script:StaRunspace.InstanceId
        }
        $after | Should -Be $before
    }
}
