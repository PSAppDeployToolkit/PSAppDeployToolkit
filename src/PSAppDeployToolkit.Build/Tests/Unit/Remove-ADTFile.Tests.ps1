BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Remove-ADTFile' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Functionality' {
        BeforeEach {
            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'TestFile', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
            $TestFile = "$TestDrive\file.txt"
            New-Item -Path $TestFile -ItemType File -Force | Out-Null

            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'TestDir', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
            $TestDir = "$TestDrive\SubDir"
            New-Item -Path "$TestDir\nested.txt" -ItemType File -Force | Out-Null
        }

        It 'Removes a single file via -LiteralPath' {
            Remove-ADTFile -LiteralPath $TestFile
            Test-Path -LiteralPath $TestFile | Should -BeFalse
        }

        It 'Removes files matching a wildcard via -Path' {
            New-Item -Path "$TestDrive\extra.txt" -ItemType File -Force | Out-Null
            Remove-ADTFile -Path "$TestDrive\*.txt"
            Get-ChildItem -Path $TestDrive -Filter '*.txt' | Should -BeNullOrEmpty
        }

        It 'Skips a directory when -Recurse is not specified' {
            Remove-ADTFile -LiteralPath $TestDir
            Test-Path -LiteralPath $TestDir -PathType Container | Should -BeTrue
        }

        It 'Removes a directory and its contents when -Recurse is specified' {
            Remove-ADTFile -LiteralPath $TestDir -Recurse
            Test-Path -LiteralPath $TestDir | Should -BeFalse
        }

        It 'Does not throw when a missing path is supplied and -ErrorAction SilentlyContinue is used' {
            { Remove-ADTFile -LiteralPath "$TestDrive\DoesNotExist.txt" -ErrorAction SilentlyContinue } | Should -Not -Throw
        }

        It 'Does not throw when a missing path is supplied and -ErrorAction Stop is used (warns and continues)' {
            # Remove-ADTFile explicitly catches ItemNotFoundException and logs a warning instead of propagating.
            { Remove-ADTFile -LiteralPath "$TestDrive\DoesNotExist.txt" -ErrorAction Stop } | Should -Not -Throw
        }
    }

    Context 'Input Validation' {
        It 'Should verify that LiteralPath is not null, empty or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,Remove-ADTFile'
            }
            { Remove-ADTFile -LiteralPath $null } | Should @shouldParams
            { Remove-ADTFile -LiteralPath '' } | Should @shouldParams
            { Remove-ADTFile -LiteralPath " `f`n`r`t`v" } | Should @shouldParams
        }

        It 'Should verify that Path is not null, empty or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,Remove-ADTFile'
            }
            { Remove-ADTFile -Path $null } | Should @shouldParams
            { Remove-ADTFile -Path '' } | Should @shouldParams
            { Remove-ADTFile -Path " `f`n`r`t`v" } | Should @shouldParams
        }
    }
}
