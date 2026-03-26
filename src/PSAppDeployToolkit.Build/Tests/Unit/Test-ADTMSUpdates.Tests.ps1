BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}

Describe 'Test-ADTMSUpdates' {
    BeforeAll {
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables { }
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Update found via Get-HotFix' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Get-HotFix { return [PSCustomObject]@{ HotFixID = 'KB9999991' } }
        }

        It 'Returns $true when Get-HotFix finds the update' {
            Test-ADTMSUpdates -KbNumber 'KB9999991' | Should -Be $true
        }

        It 'Returns a System.Boolean' {
            Test-ADTMSUpdates -KbNumber 'KB9999991' | Should -BeOfType [System.Boolean]
        }

        It 'Does not throw' {
            { Test-ADTMSUpdates -KbNumber 'KB9999991' } | Should -Not -Throw
        }

        It 'Can be called multiple times consecutively without error' {
            { Test-ADTMSUpdates -KbNumber 'KB9999991'; Test-ADTMSUpdates -KbNumber 'KB9999991' } | Should -Not -Throw
        }
    }

    Context 'Logging' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Get-HotFix { return [PSCustomObject]@{ HotFixID = 'KB0000001' } }
        }

        It 'Calls Write-ADTLogEntry at least once per invocation' {
            Test-ADTMSUpdates -KbNumber 'KB0000001'
            Should -Invoke Write-ADTLogEntry -ModuleName PSAppDeployToolkit -Scope It
        }
    }

    Context 'Input validation' {
        It 'Throws when -KbNumber is an empty string' {
            { Test-ADTMSUpdates -KbNumber '' } | Should -Throw
        }
    }
}
