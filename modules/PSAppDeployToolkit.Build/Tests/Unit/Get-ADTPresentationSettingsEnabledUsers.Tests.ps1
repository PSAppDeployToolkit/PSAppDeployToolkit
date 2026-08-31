BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}
Describe 'Get-ADTPresentationSettingsEnabledUsers' {
    Context 'Functionality' {
        It 'Returns nothing when no user is presenting' {
            # Presentation mode is off unless somebody turns it on, so the empty result is the case that
            # matters: callers treat anything returned as a reason not to interrupt.
            Get-ADTPresentationSettingsEnabledUsers | Should -BeNullOrEmpty
        }

        It 'Warns that it is deprecated' {
            $null = Get-ADTPresentationSettingsEnabledUsers
            Should -Invoke -ModuleName PSAppDeployToolkit Write-ADTLogEntry -ParameterFilter {
                $Severity -eq 'Warning' -and $Message -match 'deprecated and will be removed'
            }
        }
    }
}
