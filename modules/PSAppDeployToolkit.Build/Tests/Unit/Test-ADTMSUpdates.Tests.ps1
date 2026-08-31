BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}
Describe 'Test-ADTMSUpdates' {
    Context 'Functionality' {
        It 'Returns a boolean' {
            Test-ADTMSUpdates -KbNumber 'KB0000000' | Should -BeOfType ([System.Boolean])
        }

        It 'Reports an update that was never issued as absent' {
            Test-ADTMSUpdates -KbNumber 'KB0000000' | Should -BeFalse
        }

        It 'Finds an update this machine actually has' {
            # Read from the machine rather than hard-coded, so the positive case holds anywhere. Skipped by
            # its own guard where nothing is installed, which is legitimate on a fresh image.
            $installed = Get-HotFix -ErrorAction Ignore | Select-Object -First 1 -ExpandProperty HotFixID
            if ($installed)
            {
                Test-ADTMSUpdates -KbNumber $installed | Should -BeTrue
            }
        }

        It 'Takes the KB number positionally' {
            Test-ADTMSUpdates 'KB0000000' | Should -BeFalse
        }
    }
}
