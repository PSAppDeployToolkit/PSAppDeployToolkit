BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}
Describe 'Get-ADTMountedWimFile' {
    Context 'Functionality' {
        It 'Returns nothing for an image that is not mounted' {
            InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Drive = $TestDrive } {
                Get-ADTMountedWimFile -ImagePath "$Drive\notmounted.wim" | Should -BeNullOrEmpty
            }
        }

        It 'Returns nothing for a path nothing is mounted at' {
            InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Drive = $TestDrive } {
                Get-ADTMountedWimFile -Path $Drive | Should -BeNullOrEmpty
            }
        }

        It 'Requires either an image or a path' {
            {
                InModuleScope -ModuleName PSAppDeployToolkit { Get-ADTMountedWimFile }
            } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }

        It 'Rejects the image and the path together' {
            {
                InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Drive = $TestDrive } { Get-ADTMountedWimFile -ImagePath "$Drive\a.wim" -Path $Drive }
            } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }
    }
}
