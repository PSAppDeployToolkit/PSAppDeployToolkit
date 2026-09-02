BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}
Describe 'Start-ADTMspProcessAsUser' {
    # Contract only, for the same reason as Start-ADTMspProcess.
    Context 'Input Validation' {
        It 'Refuses a patch that is not there' {
            { Start-ADTMspProcessAsUser -FilePath "$TestDrive\NeverExisted.msp" } | Should -Throw -ErrorId 'FilePathNotFound,Start-ADTMspProcessAsUser'
        }

        It 'Requires a patch to apply' {
            Test-ADTMandatoryParameter -Command (Get-Command Start-ADTMspProcessAsUser) -Parameter FilePath | Should -BeTrue
        }
    }
}
