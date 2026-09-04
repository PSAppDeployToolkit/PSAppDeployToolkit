BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}
Describe 'Register-ADTDll' {
    # Contract only. Registering a library writes its class registrations to the machine, so only what is
    # refused before regsvr32 is reached is covered.
    Context 'Input Validation' {
        It 'Refuses a library that is not there' {
            { Register-ADTDll -FilePath "$TestDrive\NeverExisted.dll" } | Should -Throw -ErrorId 'InvalidFilePathParameterValue,Register-ADTDll'
        }

        It 'Refuses a blank path' {
            { Register-ADTDll -FilePath '   ' } | Should -Throw -ErrorId 'InvalidFilePathParameterValue,Register-ADTDll'
        }

        It 'Requires a library to register' {
            Test-ADTMandatoryParameter -Command (Get-Command Register-ADTDll) -Parameter FilePath | Should -BeTrue
        }
    }
}
