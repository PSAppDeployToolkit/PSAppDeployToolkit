BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}
Describe 'Block-ADTAppExecution' {
    # Contract only. Blocking registers a scheduled task and writes Image File Execution Options entries
    # for the machine, neither of which can be confined to a test, so only what is refused before any of
    # that happens is covered.
    Context 'Input Validation' {
        It 'Requires processes to block' {
            { Block-ADTAppExecution } | Should -Throw -ErrorId 'MissingMandatoryParameter,Block-ADTAppExecution'
        }

        It 'Requires a session to block them for' {
            # The scheduled task it registers is named after the deployment, and unblocking finds it again
            # by that name, so there is nothing to key the block to without one.
            { Block-ADTAppExecution -Processes @{ Name = 'anything' } } | Should -Throw -ErrorId 'ADTSessionBufferEmpty,Block-ADTAppExecution'
        }

        It 'Refuses a window position it does not know' {
            { Block-ADTAppExecution -Processes @{ Name = 'anything' } -WindowLocation 'Nowhere' } | Should -Throw -ErrorId 'ParameterArgumentTransformationError,Block-ADTAppExecution'
        }
    }
}
