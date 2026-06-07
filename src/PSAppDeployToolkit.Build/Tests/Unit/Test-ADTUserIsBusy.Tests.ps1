BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Test-ADTUserIsBusy' {
    BeforeAll {
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Functionality' {
        It 'Should not throw' {
            { Test-ADTUserIsBusy } | Should -Not -Throw
        }

        It 'Should return a Boolean value regardless of user state' {
            $result = Test-ADTUserIsBusy
            $result | Should -BeOfType ([System.Boolean])
        }

        It 'Should return exactly one value' {
            $result = @(Test-ADTUserIsBusy)
            $result.Count | Should -Be 1
        }

        It 'Should return $false when no active user is logged on' {
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { return $null }
            $result = Test-ADTUserIsBusy
            $result | Should -BeFalse
        }

        It 'Should return $true when the microphone is in use' {
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { return [PSCustomObject]@{ UserName = 'TestUser' } }
            Mock -ModuleName PSAppDeployToolkit Test-ADTMicrophoneInUse { return $true }
            $result = Test-ADTUserIsBusy
            $result | Should -BeTrue
        }

        It 'Should return $true when the user is in focus mode' {
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { return [PSCustomObject]@{ UserName = 'TestUser' } }
            Mock -ModuleName PSAppDeployToolkit Test-ADTMicrophoneInUse { return $false }
            Mock -ModuleName PSAppDeployToolkit Test-ADTUserInFocusMode { return $true }
            $result = Test-ADTUserIsBusy
            $result | Should -BeTrue
        }

        It 'Should return $false when no busy indicators are active' {
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { return [PSCustomObject]@{ UserName = 'TestUser' } }
            Mock -ModuleName PSAppDeployToolkit Test-ADTMicrophoneInUse { return $false }
            Mock -ModuleName PSAppDeployToolkit Test-ADTUserInFocusMode { return $false }
            Mock -ModuleName PSAppDeployToolkit Get-ADTUserToastNotificationMode { return 0 }
            Mock -ModuleName PSAppDeployToolkit Get-ADTUserNotificationState { return [PSADT.Interop.QUERY_USER_NOTIFICATION_STATE]::QUNS_ACCEPTS_NOTIFICATIONS }
            Mock -ModuleName PSAppDeployToolkit Test-ADTPowerPoint { return $false }
            $result = Test-ADTUserIsBusy
            $result | Should -BeFalse
        }

        It 'Should return $false when notification state is QUNS_APP' {
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { return [PSCustomObject]@{ UserName = 'TestUser' } }
            Mock -ModuleName PSAppDeployToolkit Test-ADTMicrophoneInUse { return $false }
            Mock -ModuleName PSAppDeployToolkit Test-ADTUserInFocusMode { return $false }
            Mock -ModuleName PSAppDeployToolkit Get-ADTUserToastNotificationMode { return 0 }
            Mock -ModuleName PSAppDeployToolkit Get-ADTUserNotificationState { return [PSADT.Interop.QUERY_USER_NOTIFICATION_STATE]::QUNS_APP }
            Mock -ModuleName PSAppDeployToolkit Test-ADTPowerPoint { return $false }
            $result = Test-ADTUserIsBusy
            $result | Should -BeFalse
        }
    }

    Context 'Metadata' {
        It 'Should declare OutputType of System.Boolean' {
            $outputTypes = (Get-Command Test-ADTUserIsBusy).OutputType.Type
            $outputTypes | Should -Contain ([System.Boolean])
        }

        It 'Should accept no custom parameters' {
            $params = (Get-Command Test-ADTUserIsBusy).Parameters.Keys
            $customParams = $params | Where-Object { $_ -notin [System.Management.Automation.Cmdlet]::CommonParameters }
            $customParams | Should -BeNullOrEmpty
        }
    }
}
