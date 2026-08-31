BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}
Describe 'Test-ADTUserInFocusMode' {
    Context 'Functionality' {
        It 'Returns a boolean' {
            Test-ADTUserInFocusMode | Should -BeOfType ([System.Boolean])
        }

        It 'Agrees with the toast notification mode it is derived from' {
            # Focus assist surfaces through the same notification mode, so anything other than Unrestricted
            # means the user has asked not to be interrupted.
            Test-ADTUserInFocusMode | Should -Be ((Get-ADTUserToastNotificationMode) -ne [Windows.UI.Notifications.ToastNotificationMode]::Unrestricted)
        }
    }
}
