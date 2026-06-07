BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}

Describe 'Start-ADTMspProcessAsUser' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

        # A real (synthetic) RunAsActiveUser used as the resolved-user sentinel. It must be the
        # genuine type so it binds to Start-ADTMspProcess's typed -RunAsActiveUser parameter.
        $script:FakeActiveUser = [PSADT.Foundation.RunAsActiveUser]::new(
            [System.Security.Principal.NTAccount]::new('CONTOSO\TestUser'),
            [System.Security.Principal.SecurityIdentifier]::new('S-1-5-21-1111111111-2222222222-3333333333-1001'),
            [System.UInt32]2,
            $null
        )
    }

    Context 'Real cross-user launch' {
        It 'Patches in a secondary user session' {
            # The genuine cross-user MSP launch requires a second interactive logon session, an
            # active client/server channel and SYSTEM-level privileges unavailable in the unit
            # test host. This is covered by integration testing instead.
            Set-ItResult -Skipped -Because 'requires a secondary interactive user session'
        }
    }

    Context 'Input Validation' {
        It 'Has a mandatory FilePath parameter' {
            (Get-Command Start-ADTMspProcessAsUser).Parameters['FilePath'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Contain $true
        }

        It 'Throws a parameter binding error when FilePath has an unsupported extension' {
            { Start-ADTMspProcessAsUser -FilePath 'patch.msi' } | Should -Throw -ErrorId 'ParameterArgumentValidationError,Start-ADTMspProcessAsUser'
        }

        It 'Throws a parameter binding error when AdditionalArgumentList contains a whitespace-only value' {
            { Start-ADTMspProcessAsUser -FilePath 'patch.msp' -AdditionalArgumentList '   ' } | Should -Throw -ErrorId 'ParameterArgumentValidationError,Start-ADTMspProcessAsUser'
        }
    }

    Context 'Forwarding to Start-ADTMspProcess' {
        BeforeEach {
            $injectedUser = $script:FakeActiveUser
            Mock -ModuleName PSAppDeployToolkit Update-ADTProcessAsUserBoundParameters {
                $BoundParameters['RunAsActiveUser'] = $injectedUser
                return $true
            }
            Mock -ModuleName PSAppDeployToolkit Start-ADTMspProcess { }
        }

        It 'Forwards to Start-ADTMspProcess once when a user session is resolved' {
            Start-ADTMspProcessAsUser -FilePath 'patch.msp'
            Should -Invoke -CommandName Start-ADTMspProcess -ModuleName PSAppDeployToolkit -Times 1 -Exactly
        }

        It 'Forwards the FilePath to Start-ADTMspProcess unchanged' {
            Start-ADTMspProcessAsUser -FilePath 'patch.msp'
            Should -Invoke -CommandName Start-ADTMspProcess -ModuleName PSAppDeployToolkit -Times 1 -Exactly -ParameterFilter {
                $FilePath -eq 'patch.msp'
            }
        }

        It 'Returns the Start-ADTMspProcess result when PassThru is specified' {
            Mock -ModuleName PSAppDeployToolkit Start-ADTMspProcess { return [PSADT.ProcessManagement.ProcessResult]::new(0) }
            $result = Start-ADTMspProcessAsUser -FilePath 'patch.msp' -PassThru
            $result | Should -BeOfType ([PSADT.ProcessManagement.ProcessResult])
            $result.ExitCode | Should -Be 0
        }
    }

    Context 'No user logged on' {
        It 'Returns without invoking Start-ADTMspProcess when no active user is resolved' {
            Mock -ModuleName PSAppDeployToolkit Update-ADTProcessAsUserBoundParameters { return $false }
            Mock -ModuleName PSAppDeployToolkit Start-ADTMspProcess { }
            Start-ADTMspProcessAsUser -FilePath 'patch.msp' -ContinueWhenNoUserLoggedOn
            Should -Invoke -CommandName Start-ADTMspProcess -ModuleName PSAppDeployToolkit -Times 0 -Exactly
        }
    }
}
