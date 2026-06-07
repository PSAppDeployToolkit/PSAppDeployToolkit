BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Update-ADTGroupPolicy' {
    BeforeAll {
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

        # Fake gpupdate result: ExitCode 0 (success) with a no-op Dispose().
        function script:New-FakeGpResult
        {
            return [PSCustomObject]@{ ExitCode = 0 } | Add-Member -MemberType ScriptMethod -Name Dispose -Value { } -PassThru
        }
    }

    Context 'Input Validation' {
        It 'Should accept only Computer or User for the Target parameter' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentValidationError,Update-ADTGroupPolicy'
            }
            { Update-ADTGroupPolicy -Target 'Bogus' } | Should @shouldParams
        }

        It 'Should reject duplicate Target entries (ValidateUnique)' {
            { Update-ADTGroupPolicy -Target @('Computer', 'Computer') } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }

        It 'Should have a non-mandatory Target parameter' {
            (Get-Command Update-ADTGroupPolicy).Parameters['Target'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Not -Contain $true
        }
    }

    Context 'Computer target' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Start-ADTProcess { return script:New-FakeGpResult }
            Mock -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation { return [PSCustomObject]@{ ExitCode = 0 } }
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser {
                $nt = [System.Security.Principal.NTAccount]::new('TEST\user')
                $sid = [System.Security.Principal.SecurityIdentifier]::new('S-1-5-21-1-2-3-1001')
                return [PSADT.Foundation.RunAsActiveUser]::new($nt, $sid, [uint32]1, $null)
            }
        }

        It 'Should invoke gpupdate.exe via Start-ADTProcess for the Computer target' {
            Update-ADTGroupPolicy -Target Computer
            Should -Invoke -ModuleName PSAppDeployToolkit Start-ADTProcess -Times 1 -Exactly -ParameterFilter { $FilePath -match 'gpupdate\.exe$' }
        }

        It 'Should pass /Target:Computer in the argument list' {
            Update-ADTGroupPolicy -Target Computer
            Should -Invoke -ModuleName PSAppDeployToolkit Start-ADTProcess -Times 1 -Exactly -ParameterFilter { $ArgumentList -contains '/Target:Computer' }
        }

        It 'Should add /Force to the argument list when -Force is supplied' {
            Update-ADTGroupPolicy -Target Computer -Force
            Should -Invoke -ModuleName PSAppDeployToolkit Start-ADTProcess -Times 1 -Exactly -ParameterFilter { $ArgumentList -contains '/Force' }
        }

        It 'Should not pass /Force when -Force is omitted' {
            Update-ADTGroupPolicy -Target Computer
            Should -Invoke -ModuleName PSAppDeployToolkit Start-ADTProcess -Times 1 -Exactly -ParameterFilter { $ArgumentList -notcontains '/Force' }
        }

        It 'Should call Start-ADTProcess with -NoWait when -NoWait is supplied' {
            Update-ADTGroupPolicy -Target Computer -NoWait
            Should -Invoke -ModuleName PSAppDeployToolkit Start-ADTProcess -Times 1 -Exactly -ParameterFilter { $NoWait -eq $true }
        }
    }

    Context 'User target' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Start-ADTProcess { return script:New-FakeGpResult }
            Mock -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation { return [PSCustomObject]@{ ExitCode = 0 } }
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser {
                $nt = [System.Security.Principal.NTAccount]::new('TEST\user')
                $sid = [System.Security.Principal.SecurityIdentifier]::new('S-1-5-21-1-2-3-1001')
                return [PSADT.Foundation.RunAsActiveUser]::new($nt, $sid, [uint32]1, $null)
            }
        }

        It 'Should invoke a Group Policy update for the User via the client/server operation' {
            Update-ADTGroupPolicy -Target User
            Should -Invoke -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation -Times 1 -Exactly -ParameterFilter { $GroupPolicyUpdate -eq $true }
        }

        It 'Should not invoke Start-ADTProcess for a User-only update' {
            Update-ADTGroupPolicy -Target User
            Should -Invoke -ModuleName PSAppDeployToolkit Start-ADTProcess -Times 0 -Exactly
        }
    }

    Context 'User target - no active user (regression: missing return guard)' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Start-ADTProcess { return script:New-FakeGpResult }
            Mock -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation { return [PSCustomObject]@{ ExitCode = 0 } }
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { return $null }
        }

        It 'Should not throw when there is no active user and Target is User' {
            { Update-ADTGroupPolicy -Target User } | Should -Not -Throw
        }

        It 'Should not invoke Invoke-ADTClientServerOperation when there is no active user' {
            Update-ADTGroupPolicy -Target User
            Should -Invoke -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation -Times 0 -Exactly
        }

        It 'Should log the bypass message when there is no active user' {
            Update-ADTGroupPolicy -Target User
            Should -Invoke -ModuleName PSAppDeployToolkit Write-ADTLogEntry -ParameterFilter { $Message -like '*Bypassing Group Policy update for the User*' }
        }
    }

    Context 'Default (both targets)' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Start-ADTProcess { return script:New-FakeGpResult }
            Mock -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation { return [PSCustomObject]@{ ExitCode = 0 } }
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser {
                $nt = [System.Security.Principal.NTAccount]::new('TEST\user')
                $sid = [System.Security.Principal.SecurityIdentifier]::new('S-1-5-21-1-2-3-1001')
                return [PSADT.Foundation.RunAsActiveUser]::new($nt, $sid, [uint32]1, $null)
            }
        }

        It 'Should update both Computer and User by default' {
            Update-ADTGroupPolicy
            Should -Invoke -ModuleName PSAppDeployToolkit Start-ADTProcess -Times 1 -Exactly
            Should -Invoke -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation -Times 1 -Exactly -ParameterFilter { $GroupPolicyUpdate -eq $true }
        }
    }
}
