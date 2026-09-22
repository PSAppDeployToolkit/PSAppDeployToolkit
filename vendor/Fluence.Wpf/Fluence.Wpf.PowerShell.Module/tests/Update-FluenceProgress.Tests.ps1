#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }

BeforeAll {
    Import-Module "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1" -Force
}

Describe 'Update-FluenceProgress' {
    It 'rejects an object that is not a Fluence.ProgressHandle' {
        { Update-FluenceProgress -Handle ([pscustomobject]@{ IsOpen = $true }) -Message 'x' } |
            Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
    }
    It 'refuses to update a closed handle before any UI work' {
        $closed = [pscustomobject]@{ PSTypeName = 'Fluence.ProgressHandle'; Id = [guid]::NewGuid(); Mode = 'Inline'; IsOpen = $false; State = @{}; Spec = @{}; Parts = $null }
        { Update-FluenceProgress -Handle $closed -Message 'x' } | Should -Throw '*already been closed*'
    }
}
