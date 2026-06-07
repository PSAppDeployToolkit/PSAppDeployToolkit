BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'New-ADTZipFile' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Functionality' {
        BeforeEach {
            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'SourceDir', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
            $SourceDir = "$TestDrive\Source"
            New-Item -Path "$SourceDir\a.txt" -ItemType File -Force | Out-Null
            New-Item -Path "$SourceDir\b.txt" -ItemType File -Force | Out-Null
            New-Item -Path "$SourceDir\Sub\c.txt" -ItemType File -Force | Out-Null
            Set-Content -Path "$SourceDir\a.txt" -Value 'content-a'
            Set-Content -Path "$SourceDir\b.txt" -Value 'content-b'
            Set-Content -Path "$SourceDir\Sub\c.txt" -Value 'content-c'

            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'ZipPath', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
            $ZipPath = "$TestDrive\archive_$(New-Guid).zip"
        }

        It 'Creates a zip archive at the specified DestinationPath' {
            New-ADTZipFile -LiteralPath $SourceDir -DestinationPath $ZipPath
            Test-Path -LiteralPath $ZipPath -PathType Leaf | Should -BeTrue
        }

        It 'Zip archive contains the expected entries' {
            New-ADTZipFile -LiteralPath $SourceDir -DestinationPath $ZipPath
            $zip = [System.IO.Compression.ZipFile]::OpenRead($ZipPath)
            try
            {
                $entryNames = $zip.Entries | Select-Object -ExpandProperty Name
                $entryNames | Should -Contain 'a.txt'
                $entryNames | Should -Contain 'b.txt'
                $entryNames | Should -Contain 'c.txt'
            }
            finally
            {
                $zip.Dispose()
            }
        }

        It 'Overwrites an existing archive when -Force is specified (replaces, not appends)' {
            # First archive: a.txt + b.txt + Sub/c.txt
            New-ADTZipFile -LiteralPath $SourceDir -DestinationPath $ZipPath

            # Add a new file to the source so the second archive is observably different
            New-Item -Path "$SourceDir\d.txt" -ItemType File -Force | Out-Null

            # Second call with -Force must delete-then-recreate (source line 130), not append
            New-ADTZipFile -LiteralPath $SourceDir -DestinationPath $ZipPath -Force

            $zip = [System.IO.Compression.ZipFile]::OpenRead($ZipPath)
            try
            {
                $entryNames = $zip.Entries | Select-Object -ExpandProperty Name
                # New entry must be present — proves the archive was rebuilt with current source
                $entryNames | Should -Contain 'd.txt'
                # Pre-existing entry must appear exactly once — proves no duplication from append
                ($entryNames | Where-Object { $_ -eq 'a.txt' }).Count | Should -Be 1
            }
            finally
            {
                $zip.Dispose()
            }
        }

        It 'Archives a single file via -Path' {
            New-ADTZipFile -Path "$SourceDir\a.txt" -DestinationPath $ZipPath
            Test-Path -LiteralPath $ZipPath -PathType Leaf | Should -BeTrue
            $zip = [System.IO.Compression.ZipFile]::OpenRead($ZipPath)
            try
            {
                $zip.Entries.Count | Should -Be 1
                $zip.Entries[0].Name | Should -Be 'a.txt'
            }
            finally
            {
                $zip.Dispose()
            }
        }

        It 'Removes the source path after archiving when -RemoveSourceAfterArchiving is specified' {
            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'RemoveSourceFile', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
            $RemoveSourceFile = "$TestDrive\toremove.txt"
            New-Item -Path $RemoveSourceFile -ItemType File -Force | Out-Null

            New-ADTZipFile -LiteralPath $RemoveSourceFile -DestinationPath $ZipPath -RemoveSourceAfterArchiving
            Test-Path -LiteralPath $ZipPath | Should -BeTrue
            Test-Path -LiteralPath $RemoveSourceFile | Should -BeFalse
        }
    }

    Context 'Input Validation' {
        It 'Should verify that LiteralPath is not null, empty or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,New-ADTZipFile'
            }
            { New-ADTZipFile -LiteralPath $null -DestinationPath "$TestDrive\out.zip" } | Should @shouldParams
            { New-ADTZipFile -LiteralPath '' -DestinationPath "$TestDrive\out.zip" } | Should @shouldParams
            { New-ADTZipFile -LiteralPath " `f`n`r`t`v" -DestinationPath "$TestDrive\out.zip" } | Should @shouldParams
        }

        It 'Should verify that DestinationPath is not null, empty or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,New-ADTZipFile'
            }
            { New-ADTZipFile -LiteralPath "$TestDrive\Source" -DestinationPath $null } | Should @shouldParams
            { New-ADTZipFile -LiteralPath "$TestDrive\Source" -DestinationPath '' } | Should @shouldParams
            { New-ADTZipFile -LiteralPath "$TestDrive\Source" -DestinationPath " `f`n`r`t`v" } | Should @shouldParams
        }

        It 'Should verify that Path is not null, empty or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,New-ADTZipFile'
            }
            { New-ADTZipFile -Path $null -DestinationPath "$TestDrive\out.zip" } | Should @shouldParams
            { New-ADTZipFile -Path '' -DestinationPath "$TestDrive\out.zip" } | Should @shouldParams
            { New-ADTZipFile -Path " `f`n`r`t`v" -DestinationPath "$TestDrive\out.zip" } | Should @shouldParams
        }
    }
}
