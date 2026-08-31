BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}
Describe 'Test-ADTNonNativeCaller' {
    Context 'Functionality' {
        It 'Returns a boolean' {
            InModuleScope -ModuleName PSAppDeployToolkit { Test-ADTNonNativeCaller } | Should -BeOfType ([System.Boolean])
        }

        It 'Reports false for a caller that is not the v3 front end' {
            InModuleScope -ModuleName PSAppDeployToolkit { Test-ADTNonNativeCaller } | Should -BeFalse
        }

        It 'Reports true when called through AppDeployToolkitMain.ps1' {
            # The check looks for that script in the call stack, which is how the v3 compatibility front end
            # is recognised. Standing one up is the only way to reach the true branch.
            $shim = "$TestDrive\AppDeployToolkitMain.ps1"
            Set-Content -LiteralPath $shim -Value 'InModuleScope -ModuleName PSAppDeployToolkit { Test-ADTNonNativeCaller }'
            & $shim | Should -BeTrue
        }
    }
}
