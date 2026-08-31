BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}
Describe 'Get-ADTForegroundWindowProcessId' {
    Context 'Functionality' {
        BeforeAll {
            $script:ForegroundId = InModuleScope -ModuleName PSAppDeployToolkit { Get-ADTForegroundWindowProcessId }
        }

        It 'Returns a process id' {
            $script:ForegroundId | Should -BeOfType ([System.UInt32])
        }

        It 'Names a process that is running' {
            # Zero means no foreground window was found, which is legitimate on a machine with nothing on
            # screen, so that case is allowed rather than asserted away.
            if ($script:ForegroundId -gt 0)
            {
                { [System.Diagnostics.Process]::GetProcessById($script:ForegroundId) } | Should -Not -Throw
            }
        }
    }
}
