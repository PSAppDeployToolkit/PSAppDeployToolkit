BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}

Describe 'Update-ADTDesktop' {
    BeforeAll {
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables { }
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
        Mock -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation { }
    }

    Context 'No active user (bypass path)' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { }
        }

        It 'Does not throw when no active user is logged on' {
            { Update-ADTDesktop } | Should -Not -Throw
        }

        It 'Returns no output when no active user is logged on' {
            $result = Update-ADTDesktop
            $result | Should -BeNull
        }

        It 'Does not call Invoke-ADTClientServerOperation when no active user is logged on' {
            Update-ADTDesktop
            Should -Not -Invoke Invoke-ADTClientServerOperation -ModuleName PSAppDeployToolkit -Scope It
        }

        It 'Calls Write-ADTLogEntry with a bypass message when no active user is logged on' {
            Update-ADTDesktop
            Should -Invoke Write-ADTLogEntry -ModuleName PSAppDeployToolkit -Scope It -ParameterFilter { $Message -like '*no active user*' }
        }
    }

    Context 'Active user present' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser {
                return [PSADT.Foundation.RunAsActiveUser]::new(
                    [System.Security.Principal.NTAccount]::new('NT AUTHORITY\SYSTEM'),
                    [System.Security.Principal.SecurityIdentifier]::new('S-1-5-18'),
                    [uint32]0,
                    $null
                )
            }
        }

        It 'Does not throw when an active user is logged on' {
            { Update-ADTDesktop } | Should -Not -Throw
        }

        It 'Calls Invoke-ADTClientServerOperation when an active user is logged on' {
            Update-ADTDesktop
            Should -Invoke Invoke-ADTClientServerOperation -ModuleName PSAppDeployToolkit -Scope It
        }

        It 'Returns no output when an active user is logged on' {
            $result = Update-ADTDesktop
            $result | Should -BeNull
        }
    }

    Context 'Error handling' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser {
                return [PSCustomObject]@{ UserName = 'DOMAIN\TestUser' }
            }
        }

        It 'Throws when Invoke-ADTClientServerOperation fails' {
            Mock -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation {
                throw [System.InvalidOperationException]::new('Client server operation failed.')
            }
            { Update-ADTDesktop } | Should -Throw
        }
    }
}
