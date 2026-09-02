BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}
Describe 'Set-ADTClientServerProcessPermissions' {
    # Contract only. It grants the logged-on user access to the toolkit's own client binaries, which means
    # rewriting the permissions on files inside the installed module.
    Context 'Input Validation' {
        It 'Requires a user to grant access to' {
            Test-ADTMandatoryParameter -Command (InModuleScope PSAppDeployToolkit { Get-Command Set-ADTClientServerProcessPermissions }) -Parameter User | Should -BeTrue
        }

        It 'Refuses something that is not a user' {
            InModuleScope -ModuleName PSAppDeployToolkit {
                { Set-ADTClientServerProcessPermissions -User 'not a user' } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
            }
        }
    }
}
