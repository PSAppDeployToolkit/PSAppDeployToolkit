BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}

Describe 'Remove-ADTFolder' {
    BeforeAll {
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables { }
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    BeforeEach {
        # Fresh folder tree per test: root with two files and a non-empty subfolder.
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'TestRoot', Justification = "This variable is used within scriptblocks that PSScriptAnalyzer has no visibility of.")]
        $Script:TestRoot = (New-Item -Path "$TestDrive\TestFolder-$(New-Guid)" -ItemType Directory -Force).FullName
        New-Item -Path "$Script:TestRoot\FileA.txt" -ItemType File -Force | Out-Null
        New-Item -Path "$Script:TestRoot\FileB.txt" -ItemType File -Force | Out-Null
        New-Item -Path "$Script:TestRoot\SubFolder\FileC.txt" -ItemType File -Force | Out-Null
    }

    Context 'LiteralPath Parameter Set' {
        It 'Removes a folder and all its contents recursively' {
            Remove-ADTFolder -LiteralPath $Script:TestRoot
            $Script:TestRoot | Should -Not -Exist
        }

        It 'Removes a deeply nested folder recursively' {
            $deep = (New-Item -Path "$TestDrive\Deep-$(New-Guid)\A\B\C" -ItemType Directory -Force).FullName
            $root = Split-Path -Path (Split-Path -Path (Split-Path -Path $deep))
            Remove-ADTFolder -LiteralPath $root
            $root | Should -Not -Exist
        }

        It 'Does not throw when the target folder does not exist' {
            { Remove-ADTFolder -LiteralPath "$TestDrive\NonExistent-$(New-Guid)" } | Should -Not -Throw
        }
    }

    Context 'Path Parameter Set (wildcards)' {
        It 'Removes folders matching a wildcard pattern' {
            $prefix = "WildTest-$(New-Guid)"
            $folder = (New-Item -Path "$TestDrive\$prefix" -ItemType Directory -Force).FullName
            Remove-ADTFolder -Path "$TestDrive\$prefix"
            $folder | Should -Not -Exist
        }
    }

    Context 'InputObject Parameter Set (pipeline)' {
        It 'Removes a folder supplied as a DirectoryInfo via pipeline' {
            $di = [System.IO.DirectoryInfo]$Script:TestRoot
            $di | Remove-ADTFolder
            $Script:TestRoot | Should -Not -Exist
        }

        It 'Does not throw when a non-existent DirectoryInfo is piped' {
            $di = [System.IO.DirectoryInfo]"$TestDrive\PipeNonExistent-$(New-Guid)"
            { $di | Remove-ADTFolder } | Should -Not -Throw
        }
    }

    Context 'DisableRecursion' {
        It 'Deletes files at the root level even when non-empty subfolders exist' {
            # DisableRecursion deletes root-level files but writes a non-terminating error
            # for non-empty subfolders; suppress it so assertions can be checked.
            Remove-ADTFolder -LiteralPath $Script:TestRoot -DisableRecursion -ErrorAction SilentlyContinue
            "$Script:TestRoot\FileA.txt" | Should -Not -Exist
            "$Script:TestRoot\FileB.txt" | Should -Not -Exist
        }

        It 'Skips non-empty subfolders when DisableRecursion is specified' {
            Remove-ADTFolder -LiteralPath $Script:TestRoot -DisableRecursion -ErrorAction SilentlyContinue
            "$Script:TestRoot\SubFolder" | Should -Exist
        }

        It 'Removes a folder that contains only files (no subfolders) with DisableRecursion' {
            $flat = (New-Item -Path "$TestDrive\Flat-$(New-Guid)" -ItemType Directory -Force).FullName
            New-Item -Path "$flat\file1.txt" -ItemType File -Force | Out-Null
            New-Item -Path "$flat\file2.txt" -ItemType File -Force | Out-Null
            Remove-ADTFolder -LiteralPath $flat -DisableRecursion
            $flat | Should -Not -Exist
        }
    }

    Context 'WhatIf Support' {
        It 'Does not remove the folder when -WhatIf is specified' {
            Remove-ADTFolder -LiteralPath $Script:TestRoot -WhatIf
            $Script:TestRoot | Should -Exist
        }
    }

    Context 'Input Validation' {
        It 'Throws when LiteralPath is null' {
            { Remove-ADTFolder -LiteralPath $null } | Should -Throw
        }

        It 'Throws when LiteralPath is an empty string' {
            { Remove-ADTFolder -LiteralPath '' } | Should -Throw
        }
    }
}
