BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    Initialize-ADTTestModule -Path $TestDrive
}
Describe 'Test-ADTCallerOwnsConfiguredPaths' {
    Context 'Functionality' {
        It 'Returns a boolean' {
            InModuleScope -ModuleName PSAppDeployToolkit { Test-ADTCallerOwnsConfiguredPaths } | Should -BeOfType ([System.Boolean])
        }

        It 'Follows <Decider> when PathsBasedOnSystemContext is <Mode>' -ForEach @(
            @{ Mode = $false; Decider = 'IsAdmin' }
            @{ Mode = $true; Decider = 'IsLocalSystemAccount' }
        ) {
            # Asserted as an equivalence rather than for one case, so it holds whatever account the tests run under.
            InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Mode = $Mode; Decider = $Decider } {
                $config = @{ Toolkit = @{ PathsBasedOnSystemContext = $Mode } }
                Test-ADTCallerOwnsConfiguredPaths -Config $config | Should -Be (Get-ADTEnvironmentTable).$Decider
            }
        }

        It 'Reads the seated config when none is supplied' {
            # Every caller but Import-ADTConfig relies on this, since the config is established by then.
            InModuleScope -ModuleName PSAppDeployToolkit {
                $ADT.Config.Toolkit.PathsBasedOnSystemContext | Should -BeFalse
                Test-ADTCallerOwnsConfiguredPaths | Should -Be (Get-ADTEnvironmentTable).IsAdmin
            }
        }

        It 'Refuses an empty config' {
            InModuleScope -ModuleName PSAppDeployToolkit {
                { Test-ADTCallerOwnsConfiguredPaths -Config @{} } | Should -Throw -ErrorId 'ParameterArgumentValidationError,Test-ADTCallerOwnsConfiguredPaths'
            }
        }
    }
}
