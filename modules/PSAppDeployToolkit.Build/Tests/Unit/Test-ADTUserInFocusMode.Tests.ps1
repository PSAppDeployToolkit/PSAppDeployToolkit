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
            # Focus mode belongs to a user, so with none there is nothing to report and nothing to ask the
            # client about. Deliberately not compared against the toast notification mode, which reads as
            # though it were the same thing and is not: the two are separate queries against separate
            # facilities, which is why Test-ADTUserIsBusy takes each of them as its own signal.
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { }
            Test-ADTUserInFocusMode | Should -BeNullOrEmpty
            Should -Invoke -ModuleName PSAppDeployToolkit Write-ADTLogEntry -ParameterFilter { $Message.StartsWith('Bypassing') } -Times 1 -Exactly
        }
    }
}
