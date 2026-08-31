BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}
Describe 'Test-ADTOobeCompleted' {
    Context 'Functionality' {
        It 'Returns a boolean' {
            Test-ADTOobeCompleted | Should -BeOfType ([System.Boolean])
        }

        It 'Reports the out-of-box experience as finished on a machine in use' {
            # A machine running tests has been through setup, so anything else would mean the check is
            # reading the wrong thing. The toolkit uses this to hold deployments back during provisioning.
            Test-ADTOobeCompleted | Should -BeTrue
        }

        It 'Agrees with the native call it wraps' {
            Test-ADTOobeCompleted | Should -Be ([PSADT.DeviceManagement.DeviceUtilities]::IsOOBEComplete())
        }
    }
}
