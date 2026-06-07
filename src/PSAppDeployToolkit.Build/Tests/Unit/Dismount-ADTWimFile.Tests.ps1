BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Dismount-ADTWimFile' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

        # Mock Invoke-ADTCommandWithRetries to intercept the Dismount-WindowsImage call.
        Mock -ModuleName PSAppDeployToolkit Invoke-ADTCommandWithRetries { }

        # Create temp paths in TestDrive for fake wim file and mount point.
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'WimFilePath', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
        $WimFilePath = Join-Path $TestDrive 'test.wim'
        [System.IO.File]::WriteAllBytes($WimFilePath, [System.Byte[]]@())

        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'MountPath', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
        $MountPath = Join-Path $TestDrive 'Mount'
        $null = [System.IO.Directory]::CreateDirectory($MountPath)

        # Build a fake MountedImageInfoObject-like PSObject that Get-ADTMountedWimFile would return.
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'FakeMountInfo', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
        $FakeMountInfo = [PSCustomObject]@{
            ImagePath = $WimFilePath
            Path      = $MountPath
            ImageIndex = 1
            MountMode = 'ReadOnly'
        }

        # Mock Get-ADTMountedWimFile to return the fake mount info by default.
        Mock -ModuleName PSAppDeployToolkit Get-ADTMountedWimFile { return $FakeMountInfo }
    }

    Context 'Functionality - dismount by ImagePath' {
        It 'Invokes Invoke-ADTCommandWithRetries once when dismounting by ImagePath' {
            Dismount-ADTWimFile -ImagePath $WimFilePath
            Should -Invoke -CommandName Invoke-ADTCommandWithRetries -ModuleName PSAppDeployToolkit -Times 1 -Exactly
        }

        It 'Passes the mount path from Get-ADTMountedWimFile to Invoke-ADTCommandWithRetries' {
            Dismount-ADTWimFile -ImagePath $WimFilePath
            # -Path and -Discard are forwarded via the ValueFromRemainingArguments $Parameters collection.
            Should -Invoke -CommandName Invoke-ADTCommandWithRetries -ModuleName PSAppDeployToolkit -Times 1 -Exactly -ParameterFilter {
                $Parameters -contains $MountPath.ToString()
            }
        }

        It 'Passes the Discard switch to Invoke-ADTCommandWithRetries' {
            Dismount-ADTWimFile -ImagePath $WimFilePath
            # -Discard is forwarded as a switch into the ValueFromRemainingArguments $Parameters collection.
            Should -Invoke -CommandName Invoke-ADTCommandWithRetries -ModuleName PSAppDeployToolkit -Times 1 -Exactly -ParameterFilter {
                $Parameters -contains '-Discard'
            }
        }

        It 'Produces no output' {
            $result = Dismount-ADTWimFile -ImagePath $WimFilePath
            $result | Should -BeNullOrEmpty
        }
    }

    Context 'Functionality - dismount by Path' {
        It 'Invokes Invoke-ADTCommandWithRetries once when dismounting by Path' {
            Dismount-ADTWimFile -Path $MountPath
            Should -Invoke -CommandName Invoke-ADTCommandWithRetries -ModuleName PSAppDeployToolkit -Times 1 -Exactly
        }

        It 'Passes the mount path from Get-ADTMountedWimFile to Invoke-ADTCommandWithRetries when using Path parameter' {
            Dismount-ADTWimFile -Path $MountPath
            Should -Invoke -CommandName Invoke-ADTCommandWithRetries -ModuleName PSAppDeployToolkit -Times 1 -Exactly -ParameterFilter {
                $Parameters -contains $MountPath.ToString()
            }
        }
    }

    Context 'No-op when no mounted WIM found' {
        It 'Does not invoke Invoke-ADTCommandWithRetries when Get-ADTMountedWimFile returns nothing' {
            Mock -ModuleName PSAppDeployToolkit Get-ADTMountedWimFile { return $null }
            Dismount-ADTWimFile -ImagePath $WimFilePath
            Should -Invoke -CommandName Invoke-ADTCommandWithRetries -ModuleName PSAppDeployToolkit -Times 0 -Exactly
        }
    }

    Context 'WhatIf support' {
        BeforeAll {
            # Ensure the default mock is active for this context.
            Mock -ModuleName PSAppDeployToolkit Get-ADTMountedWimFile { return $FakeMountInfo }
        }

        It 'Does not throw when -WhatIf is specified' {
            { Dismount-ADTWimFile -ImagePath $WimFilePath -WhatIf } | Should -Not -Throw
        }

        It 'Does not invoke Invoke-ADTCommandWithRetries when -WhatIf is specified' {
            Dismount-ADTWimFile -ImagePath $WimFilePath -WhatIf
            Should -Invoke -CommandName Invoke-ADTCommandWithRetries -ModuleName PSAppDeployToolkit -Times 0 -Exactly
        }

        It 'Still invokes Get-ADTMountedWimFile when -WhatIf is specified (enumeration runs regardless)' {
            Dismount-ADTWimFile -ImagePath $WimFilePath -WhatIf
            Should -Invoke -CommandName Get-ADTMountedWimFile -ModuleName PSAppDeployToolkit -Times 1 -Exactly
        }
    }

    Context 'Input Validation' {
        BeforeAll {
            # Restore the default mock so validation tests have a working state.
            Mock -ModuleName PSAppDeployToolkit Get-ADTMountedWimFile { return $FakeMountInfo }
        }

        It 'Should have a mandatory ImagePath parameter in the ImagePath parameter set' {
            (Get-Command Dismount-ADTWimFile).Parameters['ImagePath'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] -and $_.ParameterSetName -eq 'ImagePath' }).Mandatory | Should -Contain $true
        }

        It 'Should have a mandatory Path parameter in the Path parameter set' {
            (Get-Command Dismount-ADTWimFile).Parameters['Path'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] -and $_.ParameterSetName -eq 'Path' }).Mandatory | Should -Contain $true
        }

        It 'Throws ParameterArgumentValidationError when ImagePath is null' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentValidationError,Dismount-ADTWimFile'
            }
            { Dismount-ADTWimFile -ImagePath $null } | Should @shouldParams
        }
    }
}
