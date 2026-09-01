BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}
Describe 'Unregister-ADTDll' {
    # Contract only, for the same reason as Register-ADTDll: unregistering removes class registrations
    # from the machine.
    Context 'Input Validation' {
        It 'Refuses a library that is not there' {
            { Unregister-ADTDll -FilePath "$TestDrive\NeverExisted.dll" } | Should -Throw -ErrorId 'InvalidFilePathParameterValue,Unregister-ADTDll'
        }

        It 'Refuses a blank path' {
            { Unregister-ADTDll -FilePath '   ' } | Should -Throw -ErrorId 'InvalidFilePathParameterValue,Unregister-ADTDll'
        }

        It 'Requires a library to unregister' {
            { Unregister-ADTDll } | Should -Throw -ErrorId 'MissingMandatoryParameter,Unregister-ADTDll'
        }
    }
}
