BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}
Describe 'Show-ADTHelpConsole' {
    # Only the bypass is covered. Showing the console opens a window and leaves it open, which is a change
    # to the machine rather than a query of it, so the path that does so is deliberately not exercised.
    Context 'No active user' {
        It 'Answers with nothing rather than failing' {
            # The user is handed to a mandatory ValidateNotNullOrEmpty parameter, so an unguarded empty
            # result is a terminating error rather than the quiet bypass every sibling performs.
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { }
            Show-ADTHelpConsole | Should -BeNullOrEmpty
        }

        It 'Does not reach the client/server process' {
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { }
            Mock -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation { }
            Show-ADTHelpConsole
            Should -Invoke -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation -Times 0 -Exactly
        }
    }
}
