BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Close-ADTNotifyIcon' {
    BeforeAll {
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

        # Helper to build a RunAsActiveUser object for mocks that require one.
        function script:New-MockRunAsActiveUser
        {
            $nt = [System.Security.Principal.NTAccount]::new('TEST\user')
            $sid = [System.Security.Principal.SecurityIdentifier]::new('S-1-5-21-1-2-3-1001')
            return [PSADT.Foundation.RunAsActiveUser]::new($nt, $sid, [System.UInt32]1, $null)
        }
    }

    Context 'Parameters' {
        It 'Should take no parameters beyond the common ones' {
            $common = [System.Management.Automation.PSCmdlet]::CommonParameters
            (Get-Command Close-ADTNotifyIcon).Parameters.Keys.Where({ $common -notcontains $_ }) | Should -BeNullOrEmpty
        }
    }

    Context 'No active user' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Initialize-ADTModuleIfUninitialized { return $null }
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { return $null }
            Mock -ModuleName PSAppDeployToolkit Test-ADTNotifyIconOpen { return $true }
            Mock -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation { }
        }

        It 'Should not throw when there is no active user' {
            { Close-ADTNotifyIcon } | Should -Not -Throw
        }

        It 'Should not call Invoke-ADTClientServerOperation when there is no active user' {
            Close-ADTNotifyIcon
            Should -Invoke -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation -Times 0 -Exactly
        }
    }

    Context 'Active user, no notification icon open' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Initialize-ADTModuleIfUninitialized { return $null }
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { New-MockRunAsActiveUser }
            Mock -ModuleName PSAppDeployToolkit Test-ADTNotifyIconOpen { return $false }
            Mock -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation { }
        }

        It 'Should not call Invoke-ADTClientServerOperation when no notification icon is open' {
            Close-ADTNotifyIcon
            Should -Invoke -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation -Times 0 -Exactly
        }
    }

    Context 'Active user with an open notification icon' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Initialize-ADTModuleIfUninitialized { return $null }
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { New-MockRunAsActiveUser }
            Mock -ModuleName PSAppDeployToolkit Test-ADTNotifyIconOpen { return $true }
            Mock -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation { }
            Mock -ModuleName PSAppDeployToolkit Remove-ADTModuleCallback { }
            Mock -ModuleName PSAppDeployToolkit Test-ADTInstallationProgressOpen { return $false }
            Mock -ModuleName PSAppDeployToolkit Close-ADTClientServerProcess { }
        }

        It 'Should forward a CloseNotifyIcon operation for the active user' {
            Close-ADTNotifyIcon
            Should -Invoke -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation -Times 1 -Exactly -ParameterFilter { $CloseNotifyIcon -and ($null -ne $User) }
        }

        It 'Should remove the lingering OnFinish module callback' {
            Close-ADTNotifyIcon
            Should -Invoke -ModuleName PSAppDeployToolkit Remove-ADTModuleCallback -Times 1 -Exactly -ParameterFilter { $Hookpoint -eq 'OnFinish' }
        }

        It 'Should close the client/server process when running sessionless and no progress dialog is open' {
            Close-ADTNotifyIcon
            Should -Invoke -ModuleName PSAppDeployToolkit Close-ADTClientServerProcess -Times 1 -Exactly
        }

        It 'Should not close the client/server process when an installation progress dialog is open' {
            Mock -ModuleName PSAppDeployToolkit Test-ADTInstallationProgressOpen { return $true }
            Close-ADTNotifyIcon
            Should -Invoke -ModuleName PSAppDeployToolkit Close-ADTClientServerProcess -Times 0 -Exactly
        }
    }

    Context 'Sessionless guard' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { New-MockRunAsActiveUser }
            Mock -ModuleName PSAppDeployToolkit Test-ADTNotifyIconOpen { return $true }
            Mock -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation { }
            Mock -ModuleName PSAppDeployToolkit Remove-ADTModuleCallback { }
            Mock -ModuleName PSAppDeployToolkit Test-ADTInstallationProgressOpen { return $false }
            Mock -ModuleName PSAppDeployToolkit Close-ADTClientServerProcess { }
        }

        It 'Should not close the client/server process when an active session exists' {
            Mock -ModuleName PSAppDeployToolkit Initialize-ADTModuleIfUninitialized { return [PSCustomObject]@{ InstallName = 'TestApp' } }
            Close-ADTNotifyIcon
            Should -Invoke -ModuleName PSAppDeployToolkit Close-ADTClientServerProcess -Times 0 -Exactly
        }
    }
}
