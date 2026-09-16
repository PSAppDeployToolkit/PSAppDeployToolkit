BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}

Describe 'Test-ADTPathIsReparsePoint' {
    BeforeAll {
        # Junctions rather than symbolic links, as creating one needs no privilege and is therefore the
        # form a standard user can leave behind for an elevated process to walk into.
        $script:RealDirectory = (New-Item -Path "$TestDrive\RealDirectory" -ItemType Directory -Force).FullName
        $script:LinkTarget = (New-Item -Path "$TestDrive\LinkTarget" -ItemType Directory -Force).FullName
        $script:DanglingTarget = (New-Item -Path "$TestDrive\DanglingTarget" -ItemType Directory -Force).FullName
        $script:RealFile = "$TestDrive\RealFile.txt"
        Set-Content -LiteralPath $script:RealFile -Value 'a file is not a reparse point'

        $script:Junction = "$TestDrive\Junction"
        $script:DanglingJunction = "$TestDrive\DanglingJunction"
        $null = cmd.exe /c mklink /J "$script:Junction" "$script:LinkTarget"
        $null = cmd.exe /c mklink /J "$script:DanglingJunction" "$script:DanglingTarget"
        Remove-Item -LiteralPath $script:DanglingTarget -Recurse -Force

        # A real directory reached through the junction, so that nothing along the way is a reparse point
        # except the ancestor. This is what a standard user leaves behind when the path an elevated process
        # was told to write to is itself unremarkable.
        $script:BeneathJunction = (New-Item -Path "$script:LinkTarget\Beneath" -ItemType Directory -Force).FullName.Replace($script:LinkTarget, $script:Junction)
    }

    AfterAll {
        # Removed through cmd.exe so that the link goes and whatever it points at stays.
        $null = cmd.exe /c rmdir "$script:Junction"
        $null = cmd.exe /c rmdir "$script:DanglingJunction"
    }

    Context 'Functionality' {
        It 'Reports a junction' {
            InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Path = $script:Junction } {
                Test-ADTPathIsReparsePoint -LiteralPath $Path | Should -BeTrue
            }
        }

        It 'Reports a junction whose target has gone' {
            # Test-Path still calls this a container, which is what makes it worth asserting separately.
            InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Path = $script:DanglingJunction } {
                Test-ADTPathIsReparsePoint -LiteralPath $Path | Should -BeTrue
            }
        }

        It 'Does not report a real directory' {
            InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Path = $script:RealDirectory } {
                Test-ADTPathIsReparsePoint -LiteralPath $Path | Should -BeFalse
            }
        }

        It 'Does not report the directory a junction points at' {
            InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Path = $script:LinkTarget } {
                Test-ADTPathIsReparsePoint -LiteralPath $Path | Should -BeFalse
            }
        }

        It 'Does not report a file' {
            InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Path = $script:RealFile } {
                Test-ADTPathIsReparsePoint -LiteralPath $Path | Should -BeFalse
            }
        }

        It 'Does not report a path that is not there' {
            # Nothing to redirect through, and the caller deals with the absence on its own terms.
            InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Path = "$TestDrive\NeverExisted" } {
                Test-ADTPathIsReparsePoint -LiteralPath $Path | Should -BeFalse
            }
        }

        It 'Reports a real directory reached through a junction' {
            # The directory itself carries no reparse point, so testing only the path named would let an
            # elevated write through to wherever the junction above it points.
            InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Path = $script:BeneathJunction } {
                Test-ADTPathIsReparsePoint -LiteralPath $Path | Should -BeTrue
            }
        }

        It 'Reports a path that is not there yet beneath a junction' {
            # What the mount path looks like before anything creates it, which is when it is first checked.
            InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Path = "$script:Junction\NotCreatedYet" } {
                Test-ADTPathIsReparsePoint -LiteralPath $Path | Should -BeTrue
            }
        }

        It 'Reports a path beneath a junction whose target has gone' {
            InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Path = "$script:DanglingJunction\Beneath" } {
                Test-ADTPathIsReparsePoint -LiteralPath $Path | Should -BeTrue
            }
        }
    }

    Context 'Input Validation' {
        It 'Refuses a <Name> path' -ForEach @(@{ Name = 'empty'; Value = '' }, @{ Name = 'whitespace'; Value = '   ' }) {
            InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Value = $Value } {
                { Test-ADTPathIsReparsePoint -LiteralPath $Value } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
            }
        }

        It 'Requires a path' {
            Test-ADTMandatoryParameter -Command (InModuleScope PSAppDeployToolkit { Get-Command Test-ADTPathIsReparsePoint }) -Parameter LiteralPath | Should -BeTrue
        }
    }
}
