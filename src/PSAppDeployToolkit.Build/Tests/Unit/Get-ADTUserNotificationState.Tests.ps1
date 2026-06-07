BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Get-ADTUserNotificationState' {
    BeforeAll {
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Parameters' {
        It 'Should take no parameters beyond the common ones' {
            $common = [System.Management.Automation.PSCmdlet]::CommonParameters
            (Get-Command Get-ADTUserNotificationState).Parameters.Keys.Where({ $common -notcontains $_ }) | Should -BeNullOrEmpty
        }

        It 'Should declare an OutputType of PSADT.Interop.QUERY_USER_NOTIFICATION_STATE' {
            (Get-Command Get-ADTUserNotificationState).OutputType.Type | Should -Contain ([PSADT.Interop.QUERY_USER_NOTIFICATION_STATE])
        }
    }

    Context 'No active user' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { return $null }
        }

        It 'Should not throw when there is no active user' {
            { Get-ADTUserNotificationState } | Should -Not -Throw
        }

        It 'Should return nothing when there is no active user' {
            Get-ADTUserNotificationState | Should -BeNullOrEmpty
        }

        It 'Should not query the client/server when there is no active user' {
            Mock -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation { }
            Get-ADTUserNotificationState
            Should -Invoke -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation -Times 0 -Exactly
        }
    }

    Context 'Active user present' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser {
                $nt = [System.Security.Principal.NTAccount]::new('TEST\user')
                $sid = [System.Security.Principal.SecurityIdentifier]::new('S-1-5-21-1-2-3-1001')
                return [PSADT.Foundation.RunAsActiveUser]::new($nt, $sid, [uint32]1, $null)
            }
        }

        It 'Should return the notification state reported by the client/server operation' {
            Mock -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation { return [PSADT.Interop.QUERY_USER_NOTIFICATION_STATE]::QUNS_ACCEPTS_NOTIFICATIONS }
            $result = Get-ADTUserNotificationState
            $result | Should -Be ([PSADT.Interop.QUERY_USER_NOTIFICATION_STATE]::QUNS_ACCEPTS_NOTIFICATIONS)
        }

        It 'Should forward the active user to Invoke-ADTClientServerOperation with -GetUserNotificationState' {
            Mock -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation { return [PSADT.Interop.QUERY_USER_NOTIFICATION_STATE]::QUNS_BUSY }
            Get-ADTUserNotificationState
            Should -Invoke -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation -Times 1 -Exactly -ParameterFilter { $GetUserNotificationState -and ($null -ne $User) }
        }
    }
}
