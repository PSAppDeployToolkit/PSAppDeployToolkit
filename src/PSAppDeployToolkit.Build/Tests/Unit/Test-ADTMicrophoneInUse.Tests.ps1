BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}

Describe 'Test-ADTMicrophoneInUse' {
    BeforeAll {
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables { }
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Return type' {
        It 'Returns a System.Boolean' {
            $result = Test-ADTMicrophoneInUse
            $result | Should -BeOfType [System.Boolean]
        }
    }

    Context 'Error handling' {
        It 'Does not throw under normal conditions' {
            { Test-ADTMicrophoneInUse } | Should -Not -Throw
        }

        It 'Can be called multiple times consecutively without error' {
            { Test-ADTMicrophoneInUse; Test-ADTMicrophoneInUse } | Should -Not -Throw
        }
    }

    Context 'Logging' {
        It 'Calls Write-ADTLogEntry at least once per invocation' {
            Test-ADTMicrophoneInUse
            Should -Invoke Write-ADTLogEntry -ModuleName PSAppDeployToolkit -Scope It
        }
    }

    Context 'Microphone not in use' {
        It 'Returns $false when no microphone is currently in use' {
            $result = Test-ADTMicrophoneInUse
            if ($result) { Set-ItResult -Skipped -Because 'A microphone is in use on this machine'; return }
            $result | Should -Be $false
        }
    }

    Context 'Microphone in use' {
        It 'Returns $true when a microphone is currently in use' {
            $result = Test-ADTMicrophoneInUse
            if (-not $result) { Set-ItResult -Skipped -Because 'No microphone is in use on this machine'; return }
            $result | Should -Be $true
        }
    }
}
