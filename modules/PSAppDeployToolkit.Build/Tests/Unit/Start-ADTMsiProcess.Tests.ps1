BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}
Describe 'Start-ADTMsiProcess' {
    # Contract only. Every path beyond validation hands a package to Windows Installer, which installs,
    # repairs or removes software on the machine running the tests.
    Context 'Input Validation' {
        It 'Refuses a package that is not there' {
            { Start-ADTMsiProcess -Action Install -FilePath "$TestDrive\NeverExisted.msi" } | Should -Throw -ErrorId 'FilePathNotFound,Start-ADTMsiProcess'
        }

        It 'Refuses an action it does not know' {
            { Start-ADTMsiProcess -Action 'Frobnicate' -FilePath "$TestDrive\NeverExisted.msi" } | Should -Throw -ErrorId 'ParameterArgumentValidationError,Start-ADTMsiProcess'
        }

        It 'Requires something to work on' {
            # A package can be named by path or by product code, and neither has a default.
            { Start-ADTMsiProcess -Action Install } | Should -Throw -ErrorId 'AmbiguousParameterSet,Start-ADTMsiProcess'
        }

        It 'Refuses a product code that is not a GUID' {
            { Start-ADTMsiProcess -Action Uninstall -ProductCode 'not-a-guid' } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }
    }
}
