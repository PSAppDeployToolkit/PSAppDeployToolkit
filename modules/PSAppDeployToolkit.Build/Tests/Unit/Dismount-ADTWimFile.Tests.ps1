BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}
Describe 'Dismount-ADTWimFile' {
    # Mounting an image needs one to mount, and a mounted image is machine state rather than something
    # confined to a test. Only paths and images that are not mounted are asked about here.
    Context 'When nothing is mounted there' {
        It 'Does not object to a path with no image on it' {
            { Dismount-ADTWimFile -Path "$TestDrive\NotAMountPoint" } | Should -Not -Throw
        }

        It 'Does not object to an image that is not mounted' {
            { Dismount-ADTWimFile -ImagePath "$TestDrive\NotAnImage.wim" } | Should -Not -Throw
        }

        It 'Returns nothing' {
            Dismount-ADTWimFile -Path "$TestDrive\NotAMountPoint" | Should -BeNullOrEmpty
        }
    }

    Context 'Input Validation' {
        It 'Requires something to dismount' {
            Test-ADTParameterSetSatisfied -Command (Get-Command Dismount-ADTWimFile) | Should -BeFalse
        }

        It 'Refuses a path and an image together' {
            # They are the two ways of naming the same mount, and one of them has to win.
            { Dismount-ADTWimFile -Path "$TestDrive\A" -ImagePath "$TestDrive\B.wim" } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }
    }
}
