BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Remove-ADTFolder' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Functionality' {
        BeforeEach {
            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'EmptyFolder', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
            $EmptyFolder = "$TestDrive\EmptyFolder"
            New-Item -Path $EmptyFolder -ItemType Directory -Force | Out-Null

            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'FolderWithContent', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
            $FolderWithContent = "$TestDrive\FolderWithContent"
            New-Item -Path "$FolderWithContent\SubDir\Nested" -ItemType Directory -Force | Out-Null
            New-Item -Path "$FolderWithContent\file.txt" -ItemType File -Force | Out-Null
            New-Item -Path "$FolderWithContent\SubDir\sub.txt" -ItemType File -Force | Out-Null
            New-Item -Path "$FolderWithContent\SubDir\Nested\deep.txt" -ItemType File -Force | Out-Null
        }

        It 'Removes an empty folder via -LiteralPath' {
            Remove-ADTFolder -LiteralPath $EmptyFolder
            Test-Path -LiteralPath $EmptyFolder | Should -BeFalse
        }

        It 'Removes a folder and all its contents recursively by default' {
            Remove-ADTFolder -LiteralPath $FolderWithContent
            Test-Path -LiteralPath $FolderWithContent | Should -BeFalse
        }

        It 'Removes files in a folder without recursion via -DisableRecursion when no non-empty subfolders exist' {
            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'FlatFolder', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
            $FlatFolder = "$TestDrive\FlatFolder"
            New-Item -Path "$FlatFolder\a.txt" -ItemType File -Force | Out-Null
            New-Item -Path "$FlatFolder\b.txt" -ItemType File -Force | Out-Null

            Remove-ADTFolder -LiteralPath $FlatFolder -DisableRecursion
            Test-Path -LiteralPath $FlatFolder | Should -BeFalse
        }

        It 'Throws with NonEmptySubfolderError when -DisableRecursion is used on a folder with non-empty subfolders' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.IO.IOException]
                ErrorId = 'NonEmptySubfolderError,Remove-ADTFolder'
            }
            { Remove-ADTFolder -LiteralPath $FolderWithContent -DisableRecursion -ErrorAction Stop } | Should @shouldParams
        }

        It 'Does not throw when a missing path is supplied (logs and continues)' {
            { Remove-ADTFolder -LiteralPath "$TestDrive\DoesNotExist" } | Should -Not -Throw
        }

        It 'Accepts pipeline input via InputObject parameter' {
            $dirInfo = [System.IO.DirectoryInfo]::new($EmptyFolder)
            $dirInfo | Remove-ADTFolder
            Test-Path -LiteralPath $EmptyFolder | Should -BeFalse
        }
    }

    Context 'Input Validation' {
        It 'Should verify that LiteralPath is not null, empty or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,Remove-ADTFolder'
            }
            { Remove-ADTFolder -LiteralPath $null } | Should @shouldParams
            { Remove-ADTFolder -LiteralPath '' } | Should @shouldParams
            { Remove-ADTFolder -LiteralPath " `f`n`r`t`v" } | Should @shouldParams
        }

        It 'Should verify that Path is not null, empty or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,Remove-ADTFolder'
            }
            { Remove-ADTFolder -Path $null } | Should @shouldParams
            { Remove-ADTFolder -Path '' } | Should @shouldParams
            { Remove-ADTFolder -Path " `f`n`r`t`v" } | Should @shouldParams
        }
    }
}
