BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}

Describe 'Remove-ADTItem' {

    Context 'Files and folders' {
        It 'Should remove a file via LiteralPath' {
            $filePath = Join-Path -Path $TestDrive -ChildPath 'remove-file.txt'
            New-Item -Path $filePath -ItemType File -Force | Out-Null

            Remove-ADTItem -LiteralPath $filePath

            $filePath | Should -Not -Exist
        }

        It 'Should remove a folder recursively when Recurse is specified' {
            $folderPath = Join-Path -Path $TestDrive -ChildPath 'recursive-folder'
            $subFolderPath = Join-Path -Path $folderPath -ChildPath 'child'
            New-Item -Path $subFolderPath -ItemType Directory -Force | Out-Null
            Set-Content -Path (Join-Path -Path $subFolderPath -ChildPath 'file.txt') -Value 'data' -Encoding Ascii -Force

            Remove-ADTItem -LiteralPath $folderPath -Recurse

            $folderPath | Should -Not -Exist
        }

        It 'Should skip folders when no folder recursion mode is specified' {
            $folderPath = Join-Path -Path $TestDrive -ChildPath 'skip-folder'
            $subFolderPath = Join-Path -Path $folderPath -ChildPath 'child'
            New-Item -Path $subFolderPath -ItemType Directory -Force | Out-Null

            Remove-ADTItem -LiteralPath $folderPath

            $folderPath | Should -Exist
        }

        It 'Should remove an empty folder when Recurse is not specified' {
            $folderPath = Join-Path -Path $TestDrive -ChildPath 'empty-folder'
            New-Item -Path $folderPath -ItemType Directory -Force | Out-Null

            Remove-ADTItem -LiteralPath $folderPath

            $folderPath | Should -Not -Exist
        }

        It 'Should remove nested empty folders bottom-up when Recurse and OnlyIfEmpty are specified' {
            $folderPath = Join-Path -Path $TestDrive -ChildPath 'only-empty-root'
            $subFolderPath = Join-Path -Path $folderPath -ChildPath 'a'
            $leafFolderPath = Join-Path -Path $subFolderPath -ChildPath 'b'
            New-Item -Path $leafFolderPath -ItemType Directory -Force | Out-Null

            Remove-ADTItem -LiteralPath $folderPath -Recurse -OnlyIfEmpty

            $leafFolderPath | Should -Not -Exist
            $subFolderPath | Should -Not -Exist
            $folderPath | Should -Not -Exist
        }

        It 'Should keep folders that contain files when Recurse and OnlyIfEmpty are specified' {
            $folderPath = Join-Path -Path $TestDrive -ChildPath 'only-empty-mixed'
            $subFolderPath = Join-Path -Path $folderPath -ChildPath 'a'
            $leafFolderPath = Join-Path -Path $subFolderPath -ChildPath 'b'
            New-Item -Path $leafFolderPath -ItemType Directory -Force | Out-Null
            Set-Content -Path (Join-Path -Path $leafFolderPath -ChildPath 'keep.txt') -Value 'data' -Encoding Ascii -Force

            Remove-ADTItem -LiteralPath $folderPath -Recurse -OnlyIfEmpty

            $folderPath | Should -Exist
            $subFolderPath | Should -Exist
            $leafFolderPath | Should -Exist
            (Join-Path -Path $leafFolderPath -ChildPath 'keep.txt') | Should -Exist
        }
    }

    Context 'InputObject and validation' {
        It 'Should remove a file from pipeline input' {
            $filePath = Join-Path -Path $TestDrive -ChildPath 'pipeline-file.txt'
            New-Item -Path $filePath -ItemType File -Force | Out-Null

            Get-Item -LiteralPath $filePath | Remove-ADTItem

            $filePath | Should -Not -Exist
        }

        It 'Should handle non-existent paths gracefully' {
            { Remove-ADTItem -LiteralPath (Join-Path -Path $TestDrive -ChildPath 'does-not-exist') } | Should -Not -Throw
        }

        It 'Should require Recurse when OnlyIfEmpty is specified' {
            $folderPath = Join-Path -Path $TestDrive -ChildPath 'binding-validation'
            New-Item -Path $folderPath -ItemType Directory -Force | Out-Null

            { Remove-ADTItem -LiteralPath $folderPath -OnlyIfEmpty } | Should -Throw
        }
    }
}
