BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}

Describe 'Start-ADTProcessAsUser' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

        # A real (synthetic) RunAsActiveUser used as the resolved-user sentinel. It must be the
        # genuine type so it binds to Start-ADTProcess's typed -RunAsActiveUser parameter.
        $script:FakeActiveUser = [PSADT.Foundation.RunAsActiveUser]::new(
            [System.Security.Principal.NTAccount]::new('CONTOSO\TestUser'),
            [System.Security.Principal.SecurityIdentifier]::new('S-1-5-21-1111111111-2222222222-3333333333-1001'),
            [System.UInt32]2,
            $null
        )
    }

    Context 'Real cross-user launch' {
        It 'Launches a process in a secondary user session' {
            # The genuine cross-user launch path requires a second interactive logon session, an
            # active client/server channel and SYSTEM-level privileges that are unavailable in the
            # unit test host. This is covered by integration testing instead.
            Set-ItResult -Skipped -Because 'requires a secondary interactive user session'
        }
    }

    Context 'Input Validation' {
        It 'Has a mandatory FilePath parameter' {
            (Get-Command Start-ADTProcessAsUser).Parameters['FilePath'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Contain $true
        }

        It 'Throws ParameterArgumentValidationError when FilePath is <Name>' -ForEach @(
            @{ Name = 'empty'; Value = '' }
            @{ Name = 'whitespace'; Value = " `f`n`r`t`v" }
        ) {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentValidationError,Start-ADTProcessAsUser'
            }
            { Start-ADTProcessAsUser -FilePath $Value -CreateNoWindow } | Should @shouldParams
        }

        It 'Throws ParameterArgumentValidationError when FilePath is null' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentValidationError,Start-ADTProcessAsUser'
            }
            { Start-ADTProcessAsUser -FilePath $null -CreateNoWindow } | Should @shouldParams
        }

        It 'Throws ParameterArgumentTransformationError when Timeout is not a TimeSpan' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentTransformationError,Start-ADTProcessAsUser'
            }
            { Start-ADTProcessAsUser -FilePath 'setup.exe' -CreateNoWindow -Timeout 'not-a-timespan' } | Should @shouldParams
        }
    }

    Context 'Forwarding to Start-ADTProcess' {
        BeforeEach {
            # Simulate a resolved active user being injected into the bound parameters so the
            # function proceeds past its no-user-logged-on guard, then assert it forwards to
            # Start-ADTProcess. The genuine resolution/launch seams are mocked.
            $injectedUser = $script:FakeActiveUser
            Mock -ModuleName PSAppDeployToolkit Update-ADTProcessAsUserBoundParameters {
                $BoundParameters['RunAsActiveUser'] = $injectedUser
                return $true
            }
            Mock -ModuleName PSAppDeployToolkit Start-ADTProcess { }
        }

        It 'Forwards to Start-ADTProcess once when a user session is resolved' {
            Start-ADTProcessAsUser -FilePath 'setup.exe' -CreateNoWindow
            Should -Invoke -CommandName Start-ADTProcess -ModuleName PSAppDeployToolkit -Times 1 -Exactly
        }

        It 'Forwards the FilePath to Start-ADTProcess unchanged' {
            Start-ADTProcessAsUser -FilePath 'setup.exe' -ArgumentList '/S' -CreateNoWindow
            Should -Invoke -CommandName Start-ADTProcess -ModuleName PSAppDeployToolkit -Times 1 -Exactly -ParameterFilter {
                $FilePath -eq 'setup.exe' -and ($ArgumentList -contains '/S')
            }
        }

        It 'Returns the Start-ADTProcess result when PassThru is specified' {
            Mock -ModuleName PSAppDeployToolkit Start-ADTProcess { return [PSADT.ProcessManagement.ProcessResult]::new(0) }
            $result = Start-ADTProcessAsUser -FilePath 'setup.exe' -CreateNoWindow -PassThru
            $result | Should -BeOfType ([PSADT.ProcessManagement.ProcessResult])
            $result.ExitCode | Should -Be 0
        }
    }

    Context 'No user logged on' {
        It 'Returns without invoking Start-ADTProcess when no active user is resolved' {
            Mock -ModuleName PSAppDeployToolkit Update-ADTProcessAsUserBoundParameters { return $false }
            Mock -ModuleName PSAppDeployToolkit Start-ADTProcess { }
            Start-ADTProcessAsUser -FilePath 'setup.exe' -CreateNoWindow -ContinueWhenNoUserLoggedOn
            Should -Invoke -CommandName Start-ADTProcess -ModuleName PSAppDeployToolkit -Times 0 -Exactly
        }
    }
}
