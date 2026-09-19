BeforeDiscovery {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"

    # Get-WindowsImage needs elevation even to answer that nothing is mounted. A runtime skip rather
    # than #Requires -RunAsAdministrator, which fails the container. The parameter tests below need none.
    $script:IsElevated = Test-ADTCallerElevated
}
BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}
Describe 'Get-ADTMountedWimFile' {
    Context 'Functionality' {
        It 'Returns nothing for an image that is not mounted' -Skip:(!$script:IsElevated) {
            InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Drive = $TestDrive } {
                Get-ADTMountedWimFile -ImagePath "$Drive\notmounted.wim" | Should -BeNullOrEmpty
            }
        }

        It 'Returns nothing for a path nothing is mounted at' -Skip:(!$script:IsElevated) {
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

    Context 'Matching a mounted image' {
        # Driven through a mocked Get-WindowsImage so that the comparison can be tested against known mounts
        # without elevation, and without a real image being mounted to compare against.
        BeforeEach {
            Mock -ModuleName PSAppDeployToolkit Get-WindowsImage {
                [PSCustomObject]@{ ImagePath = 'C:\Images\install.wim'; Path = 'C:\Mount' }
                [PSCustomObject]@{ ImagePath = 'C:\Images\other.wim'; Path = 'C:\Other Mount' }
            }
        }

        It 'Finds the image mounted at a path' {
            InModuleScope -ModuleName PSAppDeployToolkit { (Get-ADTMountedWimFile -Path 'C:\Mount').ImagePath | Should -BeExactly 'C:\Images\install.wim' }
        }

        It 'Finds it whatever the case of the path' {
            # Windows does not distinguish them, so neither can a lookup that decides whether a path is free.
            InModuleScope -ModuleName PSAppDeployToolkit { (Get-ADTMountedWimFile -Path 'c:\MOUNT').ImagePath | Should -BeExactly 'C:\Images\install.wim' }
        }

        It 'Finds it whether or not the path was given a trailing separator' {
            InModuleScope -ModuleName PSAppDeployToolkit { (Get-ADTMountedWimFile -Path 'C:\Mount\').ImagePath | Should -BeExactly 'C:\Images\install.wim' }
        }

        It 'Does not match a path that merely starts with a mounted one' {
            # The reason this is not a substring test: a mount at C:\Mount would otherwise answer for
            # C:\MountPoint, and Mount-ADTWimFile refuses a path this says is taken.
            InModuleScope -ModuleName PSAppDeployToolkit { Get-ADTMountedWimFile -Path 'C:\MountPoint' | Should -BeNullOrEmpty }
        }

        It 'Finds every path it was given, in any case' {
            # Several paths compared as an array rather than one compared as a string, which is the other
            # half of the same defect: that comparison was ordinal and missed a path spelt differently.
            InModuleScope -ModuleName PSAppDeployToolkit {
                (Get-ADTMountedWimFile -Path 'c:\mount', 'C:\OTHER MOUNT').ImagePath | Should -Be @('C:\Images\install.wim', 'C:\Images\other.wim')
            }
        }

        It 'Finds the path an image is mounted at' {
            InModuleScope -ModuleName PSAppDeployToolkit { (Get-ADTMountedWimFile -ImagePath 'C:\Images\other.wim').Path | Should -BeExactly 'C:\Other Mount' }
        }

        It 'Finds an image whatever the case of its path' {
            InModuleScope -ModuleName PSAppDeployToolkit { (Get-ADTMountedWimFile -ImagePath 'c:\images\OTHER.wim').Path | Should -BeExactly 'C:\Other Mount' }
        }

        It 'Returns nothing when neither is mounted' {
            InModuleScope -ModuleName PSAppDeployToolkit { Get-ADTMountedWimFile -Path 'C:\Nowhere' | Should -BeNullOrEmpty }
        }
    }
}
