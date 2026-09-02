BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}
Describe 'Mount-ADTWimFile' {
    # Contract only. A mounted image is machine state that outlives the run, and mounting one needs an
    # image to mount, so only what is refused before anything is mounted is covered.
    Context 'Input Validation' {
        It 'Refuses an image that is not there' {
            { Mount-ADTWimFile -ImagePath "$TestDrive\NeverExisted.wim" -Path "$TestDrive\MountPoint" -Index 1 } | Should -Throw -ErrorId 'InvalidImagePathParameterValue,Mount-ADTWimFile'
        }

        It 'Requires an image to mount' {
            Test-ADTParameterSetSatisfied -Command (Get-Command Mount-ADTWimFile) -Parameter Path, Index | Should -BeFalse
        }

        It 'Requires somewhere to mount it' {
            # The image is given one that exists, so that the refusal is about the missing mount point
            # rather than the image validation firing first.
            $image = "$TestDrive\Dummy.wim"
            Set-Content -LiteralPath $image -Value 'not an image'
            Test-ADTParameterSetSatisfied -Command (Get-Command Mount-ADTWimFile) -Parameter ImagePath, Index | Should -BeFalse
        }

        It 'Requires the image to be chosen by index or by name' {
            # A WIM holds several images, so which one is wanted has to be said one way or the other.
            { Mount-ADTWimFile -ImagePath "$TestDrive\NeverExisted.wim" -Path "$TestDrive\MountPoint" } | Should -Throw
        }
    }
}
