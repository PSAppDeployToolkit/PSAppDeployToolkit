BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}
Describe 'Test-ADTEspActive' {
    Context 'Functionality' {
        It 'Returns a boolean' {
            Test-ADTEspActive | Should -BeOfType ([System.Boolean])
        }

        It 'Reports no enrolment status page when wwahost is not running' {
            # wwahost is what draws the page, and the function short-circuits on its absence, so this is the
            # first thing it decides.
            if (![System.Diagnostics.Process]::GetProcessesByName('wwahost').Length)
            {
                Test-ADTEspActive | Should -BeFalse
            }
        }

        It 'Cannot be active once the out-of-box experience has finished and no wwahost is running' {
            if ((Test-ADTOobeCompleted) -and ![System.Diagnostics.Process]::GetProcessesByName('wwahost').Length)
            {
                Test-ADTEspActive | Should -BeFalse
            }
        }
    }
}
