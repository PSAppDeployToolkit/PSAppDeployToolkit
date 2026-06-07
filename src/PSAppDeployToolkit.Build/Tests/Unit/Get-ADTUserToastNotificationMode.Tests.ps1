BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Get-ADTUserToastNotificationMode' {
    BeforeAll {
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Parameters' {
        It 'Should take no parameters beyond the common ones' {
            $common = [System.Management.Automation.PSCmdlet]::CommonParameters
            (Get-Command Get-ADTUserToastNotificationMode).Parameters.Keys.Where({ $common -notcontains $_ }) | Should -BeNullOrEmpty
        }

        It 'Should declare an OutputType of Windows.UI.Notifications.ToastNotificationMode' {
            (Get-Command Get-ADTUserToastNotificationMode).OutputType.Type | Should -Contain ([Windows.UI.Notifications.ToastNotificationMode])
        }
    }

    Context 'No active user' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { return $null }
        }

        It 'Should not throw when there is no active user' {
            { Get-ADTUserToastNotificationMode } | Should -Not -Throw
        }

        It 'Should return nothing when there is no active user' {
            Get-ADTUserToastNotificationMode | Should -BeNullOrEmpty
        }

        It 'Should not query the client/server when there is no active user' {
            Mock -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation { }
            Get-ADTUserToastNotificationMode
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

        It 'Should return the mode cast to ToastNotificationMode when the API reports a value >= 0' {
            Mock -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation { return 0 }
            $result = Get-ADTUserToastNotificationMode
            $result | Should -Be ([Windows.UI.Notifications.ToastNotificationMode]0)
        }

        It 'Should return nothing when the API is unavailable (negative value)' {
            Mock -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation { return -1 }
            Get-ADTUserToastNotificationMode | Should -BeNullOrEmpty
        }

        It 'Should forward the active user with -GetUserToastNotificationMode' {
            Mock -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation { return 0 }
            Get-ADTUserToastNotificationMode
            Should -Invoke -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation -Times 1 -Exactly -ParameterFilter { $GetUserToastNotificationMode -and ($null -ne $User) }
        }
    }
}
