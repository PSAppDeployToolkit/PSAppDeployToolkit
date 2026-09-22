#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }

BeforeAll {
    Import-Module "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1" -Force
}

Describe 'Close-FluenceProgress' {
    It 'rejects an object that is not a Fluence.ProgressHandle' {
        { Close-FluenceProgress -Handle ([pscustomobject]@{ IsOpen = $true }) } |
            Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
    }
    It 'is a no-op for a handle that is already closed' {
        $closed = [pscustomobject]@{ PSTypeName = 'Fluence.ProgressHandle'; Id = [guid]::NewGuid(); Mode = 'Inline'; IsOpen = $false; State = @{}; Spec = @{}; Parts = $null }
        { Close-FluenceProgress -Handle $closed } | Should -Not -Throw
        $closed.IsOpen | Should -BeFalse
    }
}
