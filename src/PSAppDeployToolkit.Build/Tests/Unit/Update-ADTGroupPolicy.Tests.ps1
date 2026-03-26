BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}

Describe 'Update-ADTGroupPolicy' {
    BeforeAll {
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables { }
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

        # Start-ADTProcess must return ExitCode = 0 to avoid the non-zero exit code error path.
        Mock -ModuleName PSAppDeployToolkit Start-ADTProcess { return [PSCustomObject]@{ ExitCode = 0 } }

        # Invoke-ADTClientServerOperation must return ExitCode = 0 likewise.
        Mock -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation { return [PSCustomObject]@{ ExitCode = 0 } }

        # Return a RunAsActiveUser object so the User target does not take the bypass path.
        # Pester's mock wrapper enforces the original parameter types, so we must return the correct type.
        Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser {
            return [PSADT.Foundation.RunAsActiveUser]::new(
                [System.Security.Principal.NTAccount]::new('NT AUTHORITY\SYSTEM'),
                [System.Security.Principal.SecurityIdentifier]::new('S-1-5-18'),
                [uint32]0,
                $null
            )
        }
    }

    Context 'Basic invocation' {
        It 'Does not throw with default parameters' {
            # Note: Initialize-ADTFunction is called with -ErrorAction SilentlyContinue,
            # so errors inside are logged but never propagated.
            { Update-ADTGroupPolicy } | Should -Not -Throw
        }

        It 'Returns no output' {
            $result = Update-ADTGroupPolicy
            $result | Should -BeNull
        }
    }

    Context '-Target Computer only' {
        It 'Calls Start-ADTProcess for the Computer target' {
            Update-ADTGroupPolicy -Target Computer
            Should -Invoke Start-ADTProcess -ModuleName PSAppDeployToolkit -Scope It
        }

        It 'Does not call Invoke-ADTClientServerOperation for the Computer-only target' {
            Update-ADTGroupPolicy -Target Computer
            Should -Not -Invoke Invoke-ADTClientServerOperation -ModuleName PSAppDeployToolkit -Scope It
        }
    }

    Context '-Target User only' {
        It 'Calls Invoke-ADTClientServerOperation for the User target' {
            Update-ADTGroupPolicy -Target User
            Should -Invoke Invoke-ADTClientServerOperation -ModuleName PSAppDeployToolkit -Scope It
        }

        It 'Does not call Start-ADTProcess for the User-only target' {
            Update-ADTGroupPolicy -Target User
            Should -Not -Invoke Start-ADTProcess -ModuleName PSAppDeployToolkit -Scope It
        }
    }

    Context '-NoWait' {
        It 'Calls Start-ADTProcess with -NoWait when -NoWait is specified' {
            Update-ADTGroupPolicy -Target Computer -NoWait
            Should -Invoke Start-ADTProcess -ModuleName PSAppDeployToolkit -Scope It -ParameterFilter { $NoWait -eq $true }
        }
    }

    Context '-Force' {
        It 'Passes /Force in the ArgumentList when -Force is specified' {
            Update-ADTGroupPolicy -Target Computer -Force
            Should -Invoke Start-ADTProcess -ModuleName PSAppDeployToolkit -Scope It -ParameterFilter { $ArgumentList -contains '/Force' }
        }

        It 'Does not include /Force in the ArgumentList when -Force is not specified' {
            Update-ADTGroupPolicy -Target Computer
            Should -Invoke Start-ADTProcess -ModuleName PSAppDeployToolkit -Scope It -ParameterFilter { $ArgumentList -notcontains '/Force' }
        }
    }

    Context 'Input validation' {
        It 'Throws when -Target contains an invalid value' {
            { Update-ADTGroupPolicy -Target 'InvalidTarget' } | Should -Throw
        }
    }

    Context 'Logging' {
        It 'Calls Write-ADTLogEntry at least once per invocation' {
            Update-ADTGroupPolicy -Target Computer
            Should -Invoke Write-ADTLogEntry -ModuleName PSAppDeployToolkit -Scope It
        }
    }
}
