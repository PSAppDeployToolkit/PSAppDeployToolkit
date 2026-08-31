BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}
Describe 'Get-ADTUserToastNotificationMode' {
    Context 'Functionality' {
        It 'Returns a toast notification mode' {
            Get-ADTUserToastNotificationMode | Should -BeOfType ([Windows.UI.Notifications.ToastNotificationMode])
        }

        It 'Returns a mode the enumeration declares' {
            [System.Enum]::IsDefined([Windows.UI.Notifications.ToastNotificationMode], (Get-ADTUserToastNotificationMode)) | Should -BeTrue
        }
    }
}
