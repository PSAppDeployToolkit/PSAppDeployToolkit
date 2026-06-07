BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Test-ADTMicrophoneInUse' {
    BeforeAll {
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Functionality' {
        It 'Should not throw' {
            { Test-ADTMicrophoneInUse } | Should -Not -Throw
        }

        It 'Should return a Boolean value regardless of microphone state' {
            $result = Test-ADTMicrophoneInUse
            $result | Should -BeOfType ([System.Boolean])
        }

        It 'Should return exactly one value' {
            $result = @(Test-ADTMicrophoneInUse)
            $result.Count | Should -Be 1
        }
    }

    Context 'Metadata' {
        It 'Should declare OutputType of System.Boolean' {
            $outputTypes = (Get-Command Test-ADTMicrophoneInUse).OutputType.Type
            $outputTypes | Should -Contain ([System.Boolean])
        }

        It 'Should accept no parameters' {
            $params = (Get-Command Test-ADTMicrophoneInUse).Parameters.Keys
            # Only common/risk parameters should be present; no custom parameters
            $customParams = $params | Where-Object { $_ -notin [System.Management.Automation.Cmdlet]::CommonParameters }
            $customParams | Should -BeNullOrEmpty
        }
    }
}
