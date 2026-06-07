BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}

Describe 'Start-ADTMsiProcessAsUser' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

        # A real (synthetic) RunAsActiveUser used as the resolved-user sentinel. It must be the
        # genuine type so it binds to Start-ADTMsiProcess's typed -RunAsActiveUser parameter.
        $script:FakeActiveUser = [PSADT.Foundation.RunAsActiveUser]::new(
            [System.Security.Principal.NTAccount]::new('CONTOSO\TestUser'),
            [System.Security.Principal.SecurityIdentifier]::new('S-1-5-21-1111111111-2222222222-3333333333-1001'),
            [System.UInt32]2,
            $null
        )
    }

    Context 'Real cross-user launch' {
        It 'Launches msiexec in a secondary user session' {
            # The genuine cross-user MSI launch requires a second interactive logon session, an
            # active client/server channel and SYSTEM-level privileges unavailable in the unit
            # test host. This is covered by integration testing instead.
            Set-ItResult -Skipped -Because 'requires a secondary interactive user session'
        }
    }

    Context 'Input Validation' {
        It 'Has a mandatory FilePath parameter in the FilePath parameter set' {
            (Get-Command Start-ADTMsiProcessAsUser).Parameters['FilePath'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] -and $_.ParameterSetName -eq 'FilePath' }).Mandatory | Should -Contain $true
        }

        It 'Throws a parameter binding error when FilePath has an unsupported extension' {
            { Start-ADTMsiProcessAsUser -Action Install -FilePath 'installer.exe' } | Should -Throw -ErrorId 'ParameterArgumentValidationError,Start-ADTMsiProcessAsUser'
        }

        It 'Throws a parameter binding error when Action is not a valid value' {
            { Start-ADTMsiProcessAsUser -Action 'Frobnicate' -FilePath 'fixture.msi' } | Should -Throw -ErrorId 'ParameterArgumentValidationError,Start-ADTMsiProcessAsUser'
        }

        It 'Throws a parameter binding error when ProductCode is not a valid GUID' {
            { Start-ADTMsiProcessAsUser -Action Uninstall -ProductCode 'not-a-guid' } | Should -Throw -ErrorId 'ParameterArgumentTransformationError,Start-ADTMsiProcessAsUser'
        }
    }

    Context 'Forwarding to Start-ADTMsiProcess' {
        BeforeEach {
            $injectedUser = $script:FakeActiveUser
            Mock -ModuleName PSAppDeployToolkit Update-ADTProcessAsUserBoundParameters {
                $BoundParameters['RunAsActiveUser'] = $injectedUser
                return $true
            }
            Mock -ModuleName PSAppDeployToolkit Start-ADTMsiProcess { }
        }

        It 'Forwards to Start-ADTMsiProcess once when a user session is resolved' {
            Start-ADTMsiProcessAsUser -Action Install -FilePath 'fixture.msi'
            Should -Invoke -CommandName Start-ADTMsiProcess -ModuleName PSAppDeployToolkit -Times 1 -Exactly
        }

        It 'Forwards the Action and FilePath to Start-ADTMsiProcess unchanged' {
            Start-ADTMsiProcessAsUser -Action Uninstall -FilePath 'fixture.msi'
            Should -Invoke -CommandName Start-ADTMsiProcess -ModuleName PSAppDeployToolkit -Times 1 -Exactly -ParameterFilter {
                $Action -eq 'Uninstall' -and $FilePath -eq 'fixture.msi'
            }
        }

        It 'Returns the Start-ADTMsiProcess result when PassThru is specified' {
            Mock -ModuleName PSAppDeployToolkit Start-ADTMsiProcess { return [PSADT.ProcessManagement.ProcessResult]::new(0) }
            $result = Start-ADTMsiProcessAsUser -Action Install -FilePath 'fixture.msi' -PassThru
            $result | Should -BeOfType ([PSADT.ProcessManagement.ProcessResult])
            $result.ExitCode | Should -Be 0
        }
    }

    Context 'No user logged on' {
        It 'Returns without invoking Start-ADTMsiProcess when no active user is resolved' {
            Mock -ModuleName PSAppDeployToolkit Update-ADTProcessAsUserBoundParameters { return $false }
            Mock -ModuleName PSAppDeployToolkit Start-ADTMsiProcess { }
            Start-ADTMsiProcessAsUser -Action Install -FilePath 'fixture.msi' -ContinueWhenNoUserLoggedOn
            Should -Invoke -CommandName Start-ADTMsiProcess -ModuleName PSAppDeployToolkit -Times 0 -Exactly
        }
    }
}
