BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}
Describe 'Remove-ADTFile' {
    Context 'Deleting files' {
        It 'Deletes the file it is given' {
            $file = "$TestDrive\single.txt"
            Set-Content -LiteralPath $file -Value 'content'
            Remove-ADTFile -LiteralPath $file
            Test-Path -LiteralPath $file | Should -BeFalse
        }

        It 'Deletes every file it is given' {
            $files = 1..3 | ForEach-Object { $p = "$TestDrive\many$_.txt"; Set-Content -LiteralPath $p -Value 'content'; $p }
            Remove-ADTFile -LiteralPath $files
            $files | ForEach-Object { Test-Path -LiteralPath $_ | Should -BeFalse }
        }

        It 'Resolves a wildcard' {
            # Deployments clean up by pattern far more often than by name.
            $null = New-Item -Path "$TestDrive\Wild" -ItemType Directory -Force
            1..3 | ForEach-Object { Set-Content -LiteralPath "$TestDrive\Wild\file$_.tmp" -Value 'content' }
            Set-Content -LiteralPath "$TestDrive\Wild\keep.txt" -Value 'content'
            Remove-ADTFile -Path "$TestDrive\Wild\*.tmp"
            @(Get-ChildItem -LiteralPath "$TestDrive\Wild" -File).Name | Should -Be 'keep.txt'
        }

        It 'Takes a file from the pipeline' {
            $file = "$TestDrive\piped.txt"
            Set-Content -LiteralPath $file -Value 'content'
            Get-Item -LiteralPath $file | Remove-ADTFile
            Test-Path -LiteralPath $file | Should -BeFalse
        }
    }

    Context 'Deleting folders' {
        It 'Leaves a folder alone unless asked to recurse' {
            # Guards against a stray wildcard taking a whole directory tree with it.
            $folder = "$TestDrive\Keep"
            $null = New-Item -Path $folder -ItemType Directory -Force
            Set-Content -LiteralPath "$folder\inside.txt" -Value 'content'
            Remove-ADTFile -LiteralPath $folder
            Test-Path -LiteralPath $folder | Should -BeTrue
        }

        It 'Says why it skipped the folder' {
            $folder = "$TestDrive\Explained"
            $null = New-Item -Path $folder -ItemType Directory -Force
            Remove-ADTFile -LiteralPath $folder
            Should -Invoke -ModuleName PSAppDeployToolkit Write-ADTLogEntry -ParameterFilter { $Message -like '*Recurse switch was not specified*' }
        }

        It 'Deletes the folder when asked to recurse' {
            $folder = "$TestDrive\Recursed"
            $null = New-Item -Path "$folder\Nested" -ItemType Directory -Force
            Set-Content -LiteralPath "$folder\Nested\inside.txt" -Value 'content'
            Remove-ADTFile -LiteralPath $folder -Recurse
            Test-Path -LiteralPath $folder | Should -BeFalse
        }
    }

    Context 'Paths that are not there' {
        It 'Warns rather than failing when a path does not exist' {
            # This runs in the cleanup half of a deployment, where a file already being gone is the
            # outcome that was wanted anyway.
            { Remove-ADTFile -LiteralPath "$TestDrive\never-existed.txt" } | Should -Not -Throw
            Should -Invoke -ModuleName PSAppDeployToolkit Write-ADTLogEntry -ParameterFilter { $Severity -eq 'Warning' -and $Message -like '*does not exist*' }
        }

        It 'Warns when the drive does not exist' {
            { Remove-ADTFile -LiteralPath 'NoSuchADTDrive:\somewhere\file.txt' } | Should -Not -Throw
            Should -Invoke -ModuleName PSAppDeployToolkit Write-ADTLogEntry -ParameterFilter { $Severity -eq 'Warning' -and $Message -like '*drive does not exist*' }
        }

        It 'Reports a piped file that has already gone' {
            $file = "$TestDrive\vanishes.txt"
            Set-Content -LiteralPath $file -Value 'content'
            $item = Get-Item -LiteralPath $file
            Remove-Item -LiteralPath $file -Force
            { $item | Remove-ADTFile } | Should -Not -Throw
            Should -Invoke -ModuleName PSAppDeployToolkit Write-ADTLogEntry -ParameterFilter { $Message -like '*does not exist*' }
        }
    }

    Context 'WhatIf' {
        It 'Leaves the file where it is' {
            $file = "$TestDrive\whatif.txt"
            Set-Content -LiteralPath $file -Value 'content'
            Remove-ADTFile -LiteralPath $file -WhatIf
            Test-Path -LiteralPath $file | Should -BeTrue
        }
    }

    Context 'Input Validation' {
        It 'Refuses the same path twice' {
            { Remove-ADTFile -LiteralPath "$TestDrive\a.txt", "$TestDrive\a.txt" } | Should -Throw -ErrorId 'ParameterArgumentValidationError,Remove-ADTFile'
        }

        It 'Refuses a blank path' {
            { Remove-ADTFile -LiteralPath '   ' } | Should -Throw -ErrorId 'ParameterArgumentValidationError,Remove-ADTFile'
        }

        It 'Refuses a wildcard path and a literal one together' {
            # They are separate parameter sets, since one resolves wildcards and the other must not.
            { Remove-ADTFile -Path "$TestDrive\a.txt" -LiteralPath "$TestDrive\b.txt" } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }
    }
}
