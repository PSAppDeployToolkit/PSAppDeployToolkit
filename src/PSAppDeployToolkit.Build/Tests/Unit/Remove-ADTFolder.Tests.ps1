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
        It 'Removes a folder tree when -OnlyIfEmpty finds only empty folders' {
            $path = New-Item -Path "$TestDrive\OnlyEmpty\Child\Grandchild" -ItemType Directory -Force
            $rootPath = $path.Parent.Parent.FullName

            Remove-ADTFolder -LiteralPath $rootPath -OnlyIfEmpty

            $rootPath | Should -Not -Exist
        }

        It 'Does not remove a folder tree when -OnlyIfEmpty finds a file' {
            $path = New-Item -Path "$TestDrive\ContainsFile\Child\Grandchild" -ItemType Directory -Force
            $rootPath = $path.Parent.Parent.FullName
            New-Item -Path "$rootPath\Child\Grandchild\test.txt" -ItemType File -Force | Out-Null

            { Remove-ADTFolder -LiteralPath $rootPath -OnlyIfEmpty -ErrorAction Stop } | Should -Throw -ErrorId 'NonEmptyFolderError,Remove-ADTFolder'
            $rootPath | Should -Exist
            "$rootPath\Child\Grandchild\test.txt" | Should -Exist
        }

        It 'Removes an empty folder when -OnlyIfEmpty has no child folders to evaluate' {
            $rootPath = (New-Item -Path "$TestDrive\EmptyRoot" -ItemType Directory -Force).FullName

            Remove-ADTFolder -LiteralPath $rootPath -OnlyIfEmpty

            $rootPath | Should -Not -Exist
        }
    }

    Context 'Input Validation' {
        It 'Does not allow -DisableRecursion and -OnlyIfEmpty together' {
            $rootPath = (New-Item -Path "$TestDrive\InvalidCombination" -ItemType Directory -Force).FullName

            { Remove-ADTFolder -LiteralPath $rootPath -DisableRecursion -OnlyIfEmpty } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException]) -ErrorId 'AmbiguousParameterSet,Remove-ADTFolder'
        }
    }
}
