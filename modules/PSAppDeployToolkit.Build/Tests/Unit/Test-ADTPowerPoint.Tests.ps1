BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}
Describe 'Test-ADTPowerPoint' {
    Context 'Functionality' {
        It 'Returns a boolean' {
            Test-ADTPowerPoint | Should -BeOfType ([System.Boolean])
        }

        It 'Reports no presentation when PowerPoint is not running' {
            # The check looks for a POWERPNT process before anything else, so with none running the answer
            # has to be false regardless of window state.
            if (![System.Diagnostics.Process]::GetProcessesByName('POWERPNT').Length)
            {
                Test-ADTPowerPoint | Should -BeFalse
            }
        }
    }
}
