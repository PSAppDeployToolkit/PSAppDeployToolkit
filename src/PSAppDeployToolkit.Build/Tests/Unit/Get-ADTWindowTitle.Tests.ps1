BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}

Describe 'Get-ADTWindowTitle' {
    BeforeAll {
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables { }
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
        # Returning $null simulates no logged-on user, triggering the early-return bypass path.
        Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { }
    }

    Context 'No active user — bypass path' {
        It 'Does not throw when there is no active user (all-windows call)' {
            { Get-ADTWindowTitle } | Should -Not -Throw
        }

        It 'Returns nothing when there is no active user' {
            $result = Get-ADTWindowTitle
            $result | Should -BeNull
        }

        It 'Does not throw when -WindowTitle is specified and no active user' {
            { Get-ADTWindowTitle -WindowTitle 'Notepad' } | Should -Not -Throw
        }

        It 'Does not throw when -ParentProcess is specified and no active user' {
            { Get-ADTWindowTitle -ParentProcess 'notepad' } | Should -Not -Throw
        }

        It 'Does not throw when -ParentProcessId is specified and no active user' {
            { Get-ADTWindowTitle -ParentProcessId 1 } | Should -Not -Throw
        }
    }

    Context 'Input validation' {
        It 'Throws when -WindowTitle is an empty array' {
            { Get-ADTWindowTitle -WindowTitle @() } | Should -Throw
        }

        It 'Throws when -ParentProcess is an empty array' {
            { Get-ADTWindowTitle -ParentProcess @() } | Should -Throw
        }
    }
}
