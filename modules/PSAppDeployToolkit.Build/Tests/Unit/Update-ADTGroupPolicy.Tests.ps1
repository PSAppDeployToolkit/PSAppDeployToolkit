BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}
Describe 'Update-ADTGroupPolicy' {
    # Contract only. Refreshing policy applies whatever the machine's management has pending for it, which
    # is a change to the machine rather than something to assert against.
    Context 'Input Validation' {
        It 'Refuses a target it does not know' {
            # Computer and User are the two halves of policy, and there is no third.
            { Update-ADTGroupPolicy -Target 'Everything' } | Should -Throw -ErrorId 'ParameterArgumentValidationError,Update-ADTGroupPolicy'
        }
    }
}
