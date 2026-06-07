BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Close-ADTInstallationProgress' {
    BeforeAll {
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

        # Build a real RunAsActiveUser so the typed -RunAsActiveUser parameter on
        # Test-ADTInstallationProgressOpen binds without a real logon session.
        function script:New-MockRunAsActiveUser
        {
            return [PSADT.Foundation.RunAsActiveUser]::new(
                [System.Security.Principal.NTAccount]::new('TEST\user'),
                [System.Security.Principal.SecurityIdentifier]::new('S-1-5-21-1-2-3-1001'),
                1,
                $null
            )
        }
    }

    Context 'Parameter surface' {
        It 'Declares no parameters of its own beyond the common parameters' {
            $common = [System.Management.Automation.PSCmdlet]::CommonParameters
            $declared = (Get-Command Close-ADTInstallationProgress).Parameters.Keys | Where-Object { $common -notcontains $_ }
            $declared | Should -BeNullOrEmpty
        }
    }

    Context 'No active user logged on' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Test-ADTSessionActive { return $false }
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { return $null }
            Mock -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation { }
        }

        It 'Logs a bypass message that mentions no active user' {
            Close-ADTInstallationProgress
            Should -Invoke -CommandName Write-ADTLogEntry -ModuleName PSAppDeployToolkit -Times 1 -Exactly -ParameterFilter {
                $Message -like '*no active user*'
            }
        }

        It 'Does not forward a close operation to the presenter' {
            Close-ADTInstallationProgress
            Should -Invoke -CommandName Invoke-ADTClientServerOperation -ModuleName PSAppDeployToolkit -Times 0 -Exactly -ParameterFilter {
                $CloseProgressDialog -eq $true
            }
        }

        It 'Produces no output' {
            $result = Close-ADTInstallationProgress
            $result | Should -BeNullOrEmpty
        }
    }

    Context 'Active user but no progress dialog open' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Test-ADTSessionActive { return $false }
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { return (New-MockRunAsActiveUser) }
            Mock -ModuleName PSAppDeployToolkit Test-ADTInstallationProgressOpen { return $false }
            Mock -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation { }
        }

        It 'Logs a bypass message that mentions no progress dialog open' {
            Close-ADTInstallationProgress
            Should -Invoke -CommandName Write-ADTLogEntry -ModuleName PSAppDeployToolkit -Times 1 -Exactly -ParameterFilter {
                $Message -like '*no progress dialog open*'
            }
        }

        It 'Does not forward a close operation to the presenter when nothing is open' {
            Close-ADTInstallationProgress
            Should -Invoke -CommandName Invoke-ADTClientServerOperation -ModuleName PSAppDeployToolkit -Times 0 -Exactly -ParameterFilter {
                $CloseProgressDialog -eq $true
            }
        }
    }

    Context 'Active user with a progress dialog open (sessionless)' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Test-ADTSessionActive { return $false }
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { return (New-MockRunAsActiveUser) }
            Mock -ModuleName PSAppDeployToolkit Test-ADTInstallationProgressOpen { return $true }
            Mock -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation { }
            Mock -ModuleName PSAppDeployToolkit Remove-ADTModuleCallback { }
            Mock -ModuleName PSAppDeployToolkit Test-ADTNotifyIconOpen { return $false }
            Mock -ModuleName PSAppDeployToolkit Close-ADTClientServerProcess { }
        }

        It 'Forwards a CloseProgressDialog operation to the presenter' {
            Close-ADTInstallationProgress
            Should -Invoke -CommandName Invoke-ADTClientServerOperation -ModuleName PSAppDeployToolkit -Times 1 -Exactly -ParameterFilter {
                $CloseProgressDialog -eq $true
            }
        }

        It 'Removes the lingering OnFinish module callback in the finally block' {
            Close-ADTInstallationProgress
            Should -Invoke -CommandName Remove-ADTModuleCallback -ModuleName PSAppDeployToolkit -Times 1 -Exactly -ParameterFilter {
                $Hookpoint -eq 'OnFinish'
            }
        }

        It 'Closes the client/server process when running sessionless with no notify icon' {
            Close-ADTInstallationProgress
            Should -Invoke -CommandName Close-ADTClientServerProcess -ModuleName PSAppDeployToolkit -Times 1 -Exactly
        }
    }
}
