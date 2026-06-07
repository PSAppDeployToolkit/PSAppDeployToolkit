BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Update-ADTDesktop' {
    BeforeAll {
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Parameters' {
        It 'Should take no parameters beyond the common ones' {
            $common = [System.Management.Automation.PSCmdlet]::CommonParameters
            (Get-Command Update-ADTDesktop).Parameters.Keys.Where({ $common -notcontains $_ }) | Should -BeNullOrEmpty
        }
    }

    Context 'No active user' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { return $null }
            Mock -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation { }
        }

        It 'Should not throw when there is no active user' {
            { Update-ADTDesktop } | Should -Not -Throw
        }

        It 'Should not refresh the shell when there is no active user' {
            Update-ADTDesktop
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
            Mock -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation { }
        }

        It 'Should not throw when refreshing the desktop' {
            { Update-ADTDesktop } | Should -Not -Throw
        }

        It 'Should request a desktop and environment refresh for the active user' {
            Update-ADTDesktop
            Should -Invoke -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation -Times 1 -Exactly -ParameterFilter { $RefreshDesktopAndEnvironmentVariables -and ($null -ne $User) }
        }
    }
}
