BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}
Describe 'Start-ADTMspProcess' {
    # Contract only. Applying a patch changes installed software on the machine running the tests.
    Context 'Input Validation' {
        It 'Refuses a patch that is not there' {
            { Start-ADTMspProcess -FilePath "$TestDrive\NeverExisted.msp" } | Should -Throw -ErrorId 'FilePathNotFound,Start-ADTMspProcess'
        }

        It 'Requires a patch to apply' {
            { Start-ADTMspProcess } | Should -Throw -ErrorId 'MissingMandatoryParameter,Start-ADTMspProcess'
        }

        It 'Refuses a blank path' {
            { Start-ADTMspProcess -FilePath '   ' } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }
    }
}
