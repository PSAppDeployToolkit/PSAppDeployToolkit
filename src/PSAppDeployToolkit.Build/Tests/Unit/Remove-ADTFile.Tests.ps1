BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}

Describe 'Remove-ADTFile' {
    BeforeAll {
        # Mock Set-ADTPreferenceVariables due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables { }
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry {}
    }

    BeforeEach {
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'TestRoot', Justification = "This variable is used within scriptblocks that PSScriptAnalyzer has no visibility of.")]
        $TestRoot = (New-Item -Path "$TestDrive\TestFiles-$(New-Guid)" -ItemType Directory -Force).FullName

        New-Item -ItemType File -Force -Path @(
            "$TestRoot\file1.txt"
            "$TestRoot\file2.txt"
            "$TestRoot\file3.log"
            "$TestRoot\Subfolder1\child1.txt"
            "$TestRoot\Subfolder1\child2.txt"
            "$TestRoot\Subfolder2\child3.txt"
        ) | Out-Null

        Set-ItemProperty -Path "$TestRoot\file1.txt" -Name Attributes -Value 'Hidden'
        Set-ItemProperty -Path "$TestRoot\Subfolder2" -Name Attributes -Value 'Hidden'
    }

    Context 'Single File Removal' {
        It 'Removes a single file via -Path' {
            Remove-ADTFile -Path "$TestRoot\file2.txt"
            "$TestRoot\file2.txt" | Should -Not -Exist
        }

        It 'Removes a single file via -LiteralPath' {
            Remove-ADTFile -LiteralPath "$TestRoot\file2.txt"
            "$TestRoot\file2.txt" | Should -Not -Exist
        }

        It 'Removes a hidden file' {
            Remove-ADTFile -LiteralPath "$TestRoot\file1.txt"
            "$TestRoot\file1.txt" | Should -Not -Exist
        }

        It 'Leaves sibling files intact when removing a single file' {
            Remove-ADTFile -Path "$TestRoot\file2.txt"
            "$TestRoot\file1.txt" | Should -Exist
            "$TestRoot\file3.log" | Should -Exist
        }

        It 'Removes a file in a subfolder' {
            Remove-ADTFile -LiteralPath "$TestRoot\Subfolder1\child1.txt"
            "$TestRoot\Subfolder1\child1.txt" | Should -Not -Exist
            "$TestRoot\Subfolder1\child2.txt" | Should -Exist
        }
    }

    Context 'Wildcard Removal' {
        It 'Removes only files matching a wildcard extension pattern' {
            Remove-ADTFile -Path "$TestRoot\*.txt"
            "$TestRoot\file1.txt" | Should -Not -Exist
            "$TestRoot\file2.txt" | Should -Not -Exist
            "$TestRoot\file3.log" | Should -Exist
        }

        It 'Removes all top-level files when using * wildcard' {
            Remove-ADTFile -Path "$TestRoot\*"
            Get-ChildItem -Path $TestRoot -File | Should -BeNullOrEmpty
        }

        It 'Removes files matching a partial name wildcard' {
            Remove-ADTFile -Path "$TestRoot\file?.txt"
            "$TestRoot\file1.txt" | Should -Not -Exist
            "$TestRoot\file2.txt" | Should -Not -Exist
            "$TestRoot\file3.log" | Should -Exist
        }
    }

    Context 'Folder Removal' {
        It 'Skips a folder and does not throw when -Recurse is not specified' {
            { Remove-ADTFile -LiteralPath "$TestRoot\Subfolder1" -ErrorAction SilentlyContinue } | Should -Not -Throw
            "$TestRoot\Subfolder1" | Should -Exist
        }

        It 'Leaves folder contents intact when -Recurse is not specified' {
            Remove-ADTFile -LiteralPath "$TestRoot\Subfolder1" -ErrorAction SilentlyContinue
            "$TestRoot\Subfolder1\child1.txt" | Should -Exist
            "$TestRoot\Subfolder1\child2.txt" | Should -Exist
        }

        It 'Removes a folder and all its contents when -Recurse is specified' {
            Remove-ADTFile -LiteralPath "$TestRoot\Subfolder1" -Recurse
            "$TestRoot\Subfolder1" | Should -Not -Exist
        }

        It 'Removes a hidden folder recursively' {
            Remove-ADTFile -LiteralPath "$TestRoot\Subfolder2" -Recurse
            "$TestRoot\Subfolder2" | Should -Not -Exist
        }

        It 'Removes multiple folders recursively when using a wildcard with -Recurse' {
            Remove-ADTFile -Path "$TestRoot\Subfolder*" -Recurse
            "$TestRoot\Subfolder1" | Should -Not -Exist
            "$TestRoot\Subfolder2" | Should -Not -Exist
        }

        It 'Removes only the contents of a folder when * wildcard is used with -Recurse' {
            Remove-ADTFile -Path "$TestRoot\Subfolder1\*" -Recurse
            "$TestRoot\Subfolder1" | Should -Exist
            Get-ChildItem -Path "$TestRoot\Subfolder1" | Should -BeNullOrEmpty
        }
    }

    Context 'Non-Existent Paths' {
        It 'Does not throw when the specified file does not exist via -LiteralPath' {
            { Remove-ADTFile -LiteralPath "$TestRoot\DoesNotExist.txt" } | Should -Not -Throw
        }

        It 'Does not throw when a wildcard pattern matches no files' {
            { Remove-ADTFile -Path "$TestRoot\*.xyz" } | Should -Not -Throw
        }

        It 'Does not throw when -ErrorAction SilentlyContinue is specified for a non-existent path' {
            { Remove-ADTFile -LiteralPath "$TestRoot\DoesNotExist.txt" -ErrorAction SilentlyContinue } | Should -Not -Throw
        }

        It 'Continues removing remaining items after a non-existent path in an array' {
            Remove-ADTFile -LiteralPath @("$TestRoot\DoesNotExist.txt", "$TestRoot\file2.txt") -ErrorAction SilentlyContinue
            "$TestRoot\file2.txt" | Should -Not -Exist
        }
    }

    Context 'Array of Paths' {
        It 'Removes multiple files provided as an array via -LiteralPath' {
            Remove-ADTFile -LiteralPath @("$TestRoot\file1.txt", "$TestRoot\file2.txt")
            "$TestRoot\file1.txt" | Should -Not -Exist
            "$TestRoot\file2.txt" | Should -Not -Exist
            "$TestRoot\file3.log" | Should -Exist
        }

        It 'Removes multiple files provided as an array via -Path' {
            Remove-ADTFile -Path @("$TestRoot\file2.txt", "$TestRoot\file3.log")
            "$TestRoot\file2.txt" | Should -Not -Exist
            "$TestRoot\file3.log" | Should -Not -Exist
            "$TestRoot\file1.txt" | Should -Exist
        }

        It 'Removes a mix of files and folders provided as an array via -LiteralPath' {
            Remove-ADTFile -LiteralPath @("$TestRoot\file2.txt", "$TestRoot\Subfolder1") -Recurse
            "$TestRoot\file2.txt" | Should -Not -Exist
            "$TestRoot\Subfolder1" | Should -Not -Exist
            "$TestRoot\file1.txt" | Should -Exist
        }
    }

    Context 'WhatIf Support' {
        It 'Does not delete a file when -WhatIf is specified' {
            Remove-ADTFile -LiteralPath "$TestRoot\file2.txt" -WhatIf
            "$TestRoot\file2.txt" | Should -Exist
        }

        It 'Does not delete a folder when -WhatIf is specified with -Recurse' {
            Remove-ADTFile -LiteralPath "$TestRoot\Subfolder1" -Recurse -WhatIf
            "$TestRoot\Subfolder1" | Should -Exist
            "$TestRoot\Subfolder1\child1.txt" | Should -Exist
        }
    }

    Context 'Input Validation' {
        It 'Throws when -Path is null' {
            { Remove-ADTFile -Path $null } | Should -Throw
        }

        It 'Throws when -Path is an empty string' {
            { Remove-ADTFile -Path '' } | Should -Throw
        }

        It 'Throws when -LiteralPath is null' {
            { Remove-ADTFile -LiteralPath $null } | Should -Throw
        }

        It 'Throws when -LiteralPath is an empty string' {
            { Remove-ADTFile -LiteralPath '' } | Should -Throw
        }

        It 'Throws when both -Path and -LiteralPath are supplied' {
            { Remove-ADTFile -Path "$TestRoot\file1.txt" -LiteralPath "$TestRoot\file2.txt" } | Should -Throw
        }
    }
}
