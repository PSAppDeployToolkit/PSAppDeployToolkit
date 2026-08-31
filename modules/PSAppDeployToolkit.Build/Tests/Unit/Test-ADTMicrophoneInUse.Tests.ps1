BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}
Describe 'Test-ADTMicrophoneInUse' {
    Context 'Functionality' {
        It 'Returns a boolean' {
            Test-ADTMicrophoneInUse | Should -BeOfType ([System.Boolean])
        }

        It 'Agrees with the native call it wraps' {
            Test-ADTMicrophoneInUse | Should -Be ([PSADT.DeviceManagement.DeviceUtilities]::IsMicrophoneInUse())
        }

        It 'Answers without needing a logged-on user' {
            # Unlike the other user-state checks, this reads a machine-wide capability rather than going
            # through the client process, so it must not bypass itself.
            { Test-ADTMicrophoneInUse -ErrorAction Stop } | Should -Not -Throw
        }
    }
}
