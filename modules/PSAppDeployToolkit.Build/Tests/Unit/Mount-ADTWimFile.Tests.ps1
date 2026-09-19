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

    Context 'Mount paths that redirect' {
        BeforeAll {
            # A junction standing where the mount point is expected. Creating one needs no privilege, so
            # a standard user can leave it anywhere they can create a subdirectory and have the elevated
            # mount land wherever they chose. Nothing here mounts, so no image is needed.
            $script:JunctionTarget = (New-Item -Path "$TestDrive\JunctionTarget" -ItemType Directory -Force).FullName
            $script:JunctionMountPath = "$TestDrive\JunctionMountPath"
            $null = cmd.exe /c mklink /J "$script:JunctionMountPath" "$script:JunctionTarget"

            # ImagePath is validated before Path is, so it needs a file that exists for the mount path
            # validation to be reached at all. It never has to be a real image, as nothing gets that far.
            $script:DummyImage = "$TestDrive\DummyImage.wim"
            Set-Content -LiteralPath $script:DummyImage -Value 'not an image'
        }

        AfterAll {
            # Removed through cmd.exe so that the link goes and its target stays.
            $null = cmd.exe /c rmdir "$script:JunctionMountPath"
        }

        It 'Refuses a mount path that is a junction' {
            # Test-Path calls a junction a container and Get-ChildItem reads through it to an empty
            # target, so neither the emptiness check nor the create-if-absent check notices one.
            { Mount-ADTWimFile -ImagePath $script:DummyImage -Path $script:JunctionMountPath -Index 1 } | Should -Throw -ErrorId 'InvalidPathParameterValue,Mount-ADTWimFile'
        }

        It 'Refuses a mount path reached through a junction' {
            # The redirection is the same whether the junction stands at the mount point or above it, and
            # a mount point that does not exist yet is the state this is first checked in.
            { Mount-ADTWimFile -ImagePath $script:DummyImage -Path "$script:JunctionMountPath\Beneath" -Index 1 } | Should -Throw -ErrorId 'InvalidPathParameterValue,Mount-ADTWimFile'
        }

        It 'Accepts the directory the junction points at' {
            # The refusal has to be about the redirection rather than about the target being unusable,
            # so the same directory named directly must get past the mount path validation.
            $thrown = { Mount-ADTWimFile -ImagePath $script:DummyImage -Path $script:JunctionTarget -Index 1 } | Should -Throw -PassThru
            $thrown.FullyQualifiedErrorId | Should -Not -BeLike 'InvalidPathParameterValue*'
        }
    }
}
