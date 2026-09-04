BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest
}

Describe 'Remove-ADTFolder' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Functionality' {
        It 'Removes a folder and everything in it' {
            # The default, and what a deployment reaches for most often, which was the one path these
            # tests did not cover.
            $rootPath = (New-Item -Path "$TestDrive\Tree\Child\Grandchild" -ItemType Directory -Force).Parent.Parent.FullName
            Set-Content -LiteralPath "$rootPath\top.txt" -Value 'content'
            Set-Content -LiteralPath "$rootPath\Child\Grandchild\deep.txt" -Value 'content'

            Remove-ADTFolder -LiteralPath $rootPath

            $rootPath | Should -Not -Exist
        }

        It 'Removes the files at the top but refuses a subfolder that is not empty with -DisableRecursion' {
            # Not recursing means the contents of a subfolder are not its to delete, so it takes what it
            # can and reports what it could not.
            $rootPath = (New-Item -Path "$TestDrive\NoRecurse\Child" -ItemType Directory -Force).Parent.FullName
            Set-Content -LiteralPath "$rootPath\top.txt" -Value 'content'
            Set-Content -LiteralPath "$rootPath\Child\deep.txt" -Value 'content'

            { Remove-ADTFolder -LiteralPath $rootPath -DisableRecursion -ErrorAction Stop } | Should -Throw

            "$rootPath\top.txt" | Should -Not -Exist
            "$rootPath\Child\deep.txt" | Should -Exist
        }

        It 'Does not object to a folder that is not there' {
            # Deployments remove folders a previous version may never have created.
            { Remove-ADTFolder -LiteralPath "$TestDrive\NeverExisted" } | Should -Not -Throw
        }

        It 'Removes nothing with -WhatIf' {
            $rootPath = (New-Item -Path "$TestDrive\Untouched" -ItemType Directory -Force).FullName
            Set-Content -LiteralPath "$rootPath\top.txt" -Value 'content'

            Remove-ADTFolder -LiteralPath $rootPath -WhatIf

            "$rootPath\top.txt" | Should -Exist
        }
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
