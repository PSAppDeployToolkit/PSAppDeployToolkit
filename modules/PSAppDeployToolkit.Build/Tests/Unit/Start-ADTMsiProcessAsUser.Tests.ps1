BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}
Describe 'Start-ADTMsiProcessAsUser' {
    # Contract only, for the same reason as Start-ADTMsiProcess: it hands a package to Windows Installer,
    # here in the logged-on user's session.
    Context 'Input Validation' {
        It 'Refuses a package that is not there' {
            { Start-ADTMsiProcessAsUser -Action Install -FilePath "$TestDrive\NeverExisted.msi" } | Should -Throw -ErrorId 'FilePathNotFound,Start-ADTMsiProcessAsUser'
        }

        It 'Refuses an action it does not know' {
            { Start-ADTMsiProcessAsUser -Action 'Frobnicate' -FilePath "$TestDrive\NeverExisted.msi" } | Should -Throw -ErrorId 'ParameterArgumentValidationError,Start-ADTMsiProcessAsUser'
        }

        It 'Requires something to work on' {
            { Start-ADTMsiProcessAsUser -Action Install } | Should -Throw -ErrorId 'AmbiguousParameterSet,Start-ADTMsiProcessAsUser'
        }
    }
}
