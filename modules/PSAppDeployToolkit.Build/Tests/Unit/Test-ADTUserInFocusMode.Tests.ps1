BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}
Describe 'Test-ADTUserInFocusMode' {
    Context 'Functionality' {
        It 'Answers with a boolean, or with nothing where it cannot tell' {
            # Nothing is one of its documented answers: the API is absent on older builds of Windows, and
            # there is nobody to ask about when no user is logged on.
            $focusMode = Test-ADTUserInFocusMode
            if ($null -ne $focusMode)
            {
                $focusMode | Should -BeOfType ([System.Boolean])
            }
        }

        It 'Bypasses itself when nobody is logged on' {
            # Focus mode belongs to a user, so with none there is nothing to ask the client. Deliberately
            # not compared against toast notification mode: separate queries against separate facilities.
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { }
            Test-ADTUserInFocusMode | Should -BeNullOrEmpty
            Should -Invoke -ModuleName PSAppDeployToolkit Write-ADTLogEntry -ParameterFilter { $Message.StartsWith('Bypassing') } -Times 1 -Exactly
        }
    }
}
