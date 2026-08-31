BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}
Describe 'Test-ADTUserIsBusy' {
    Context 'Functionality' {
        It 'Returns a boolean' {
            Test-ADTUserIsBusy | Should -BeOfType ([System.Boolean])
        }

        It 'Says the user is busy exactly when one of its inputs says so' {
            # The function is the union of the individual checks, so it must not report quiet while any one
            # of them reports busy, nor busy while all of them are quiet.
            $notificationState = Get-ADTUserNotificationState
            $notificationsBlocked = ($notificationState -ne [PSADT.Interop.QUERY_USER_NOTIFICATION_STATE]::QUNS_ACCEPTS_NOTIFICATIONS) -and ($notificationState -ne [PSADT.Interop.QUERY_USER_NOTIFICATION_STATE]::QUNS_APP)
            $expected = (Test-ADTMicrophoneInUse) -or (Test-ADTUserInFocusMode) -or ((Get-ADTUserToastNotificationMode) -gt 0) -or $notificationsBlocked -or (Test-ADTPowerPoint)
            Test-ADTUserIsBusy | Should -Be $expected
        }
    }
}
