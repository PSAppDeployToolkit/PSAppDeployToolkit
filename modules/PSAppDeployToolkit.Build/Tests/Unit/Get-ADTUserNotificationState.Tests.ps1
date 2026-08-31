BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}
Describe 'Get-ADTUserNotificationState' {
    Context 'Functionality' {
        It 'Returns a notification state' {
            Get-ADTUserNotificationState | Should -BeOfType ([PSADT.Interop.QUERY_USER_NOTIFICATION_STATE])
        }

        It 'Returns a state the enumeration declares' {
            [System.Enum]::IsDefined([PSADT.Interop.QUERY_USER_NOTIFICATION_STATE], (Get-ADTUserNotificationState)) | Should -BeTrue
        }

        It 'Never reports the state as not-present while a user is logged on' {
            # QUNS_NOT_PRESENT means no interactive session at all, which contradicts having resolved a
            # client/server user to ask in the first place.
            Get-ADTUserNotificationState | Should -Not -Be ([PSADT.Interop.QUERY_USER_NOTIFICATION_STATE]::QUNS_NOT_PRESENT)
        }
    }
}
