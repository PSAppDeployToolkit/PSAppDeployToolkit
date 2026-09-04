BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest -Force

    Mock -ModuleName PSAppDeployToolkit Exit-ADTInvocation { }
    Initialize-ADTTestModule -Path $TestDrive
}

AfterAll {
    Import-ADTModuleUnderTest -Force
}
Describe 'New-ADTEnvironmentTable' {
    Context 'Functionality' {
        BeforeAll {
            $script:Environment = InModuleScope -ModuleName PSAppDeployToolkit { New-ADTEnvironmentTable }
        }

        It 'Returns an environment table' {
            $script:Environment | Should -BeOfType ([PSAppDeployToolkit.Foundation.EnvironmentTable])
        }

        It 'Names the toolkit it was built by' {
            $script:Environment.AppDeployToolkitName | Should -Not -BeNullOrEmpty
            $script:Environment.AppDeployMainScriptVersion | Should -Not -BeNullOrEmpty
        }

        It 'Reports the running process rather than a cached view of it' {
            $script:Environment.Culture | Should -Be ([System.Globalization.CultureInfo]::CurrentCulture)
            $script:Environment.UICulture | Should -Be ([System.Globalization.CultureInfo]::CurrentUICulture)
        }

        It 'Agrees with the table the module is already holding' {
            # Open-ADTSession builds its own, so a second call has to describe the same machine.
            $script:Environment.IsAdmin | Should -Be (Get-ADTEnvironmentTable).IsAdmin
            $script:Environment.EnvComputerName | Should -BeExactly (Get-ADTEnvironmentTable).EnvComputerName
        }

        It 'Presents its values as read-only' {
            # Callers get handed this table directly, so nothing they do can be allowed to rewrite the
            # module's view of the machine.
            { $script:Environment.IsAdmin = !$script:Environment.IsAdmin } | Should -Throw
        }
    }

    Context 'Additional environment variables' {
        It 'Adds what the caller supplied' {
            # Frontends pass their own values through so that deployment scripts can read them alongside
            # the toolkit's own.
            InModuleScope -ModuleName PSAppDeployToolkit {
                (New-ADTEnvironmentTable -AdditionalEnvironmentVariables @{ TestOnlyValue = 'supplied' }).TestOnlyValue | Should -BeExactly 'supplied'
            }
        }

        It 'Leaves the built-in values in place' {
            InModuleScope -ModuleName PSAppDeployToolkit {
                (New-ADTEnvironmentTable -AdditionalEnvironmentVariables @{ TestOnlyValue = 'supplied' }).AppDeployToolkitName | Should -Not -BeNullOrEmpty
            }
        }

        It 'Does not leak the caller''s additions into the next table' {
            InModuleScope -ModuleName PSAppDeployToolkit {
                $null = New-ADTEnvironmentTable -AdditionalEnvironmentVariables @{ TestOnlyValue = 'supplied' }
                (New-ADTEnvironmentTable).PSObject.Properties.Name | Should -Not -Contain 'TestOnlyValue'
            }
        }

        It 'Refuses an empty dictionary' {
            # Supplying nothing is what omitting the parameter is for, so an empty one is a caller mistake.
            InModuleScope -ModuleName PSAppDeployToolkit {
                { New-ADTEnvironmentTable -AdditionalEnvironmentVariables @{} } | Should -Throw -ErrorId 'ParameterArgumentValidationError,New-ADTEnvironmentTable'
            }
        }
    }
}
