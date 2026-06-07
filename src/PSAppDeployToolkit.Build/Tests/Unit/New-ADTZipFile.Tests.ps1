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

        It 'Overwrites an existing archive when -Force is specified' {
            New-ADTZipFile -LiteralPath $SourceDir -DestinationPath $ZipPath
            $firstSize = (Get-Item -LiteralPath $ZipPath).Length
            New-ADTZipFile -LiteralPath $SourceDir -DestinationPath $ZipPath -Force
            $secondSize = (Get-Item -LiteralPath $ZipPath).Length
            $secondSize | Should -Be $firstSize
            Test-Path -LiteralPath $ZipPath | Should -BeTrue
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
