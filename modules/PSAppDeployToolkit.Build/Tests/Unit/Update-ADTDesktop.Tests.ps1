BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest -Force

    Mock -ModuleName PSAppDeployToolkit Exit-ADTInvocation { }
    Initialize-ADTTestModule -Path $TestDrive

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}

AfterAll {
    Import-ADTModuleUnderTest -Force
}
Describe 'Update-ADTDesktop' {
    Context 'Functionality' {
        It 'Returns nothing' {
            # It asks Explorer to re-read its environment and redraw, which is a request rather than
            # something with a result, and deployments call it for effect alone.
            Update-ADTDesktop | Should -BeNullOrEmpty
        }

        It 'Does not object' {
            { Update-ADTDesktop } | Should -Not -Throw
        }

        It 'Says what it is doing' {
            Update-ADTDesktop
            Should -Invoke -ModuleName PSAppDeployToolkit Write-ADTLogEntry -ParameterFilter { $Message -like '*Refreshing*' }
        }

        It 'Can be called repeatedly' {
            # Deployments call it after each installer that changes file associations, so it has to stay
            # a request rather than accumulating anything.
            { 1..3 | ForEach-Object { Update-ADTDesktop } } | Should -Not -Throw
        }
    }
}
