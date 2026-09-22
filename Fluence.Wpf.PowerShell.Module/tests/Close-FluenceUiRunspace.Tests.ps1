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
