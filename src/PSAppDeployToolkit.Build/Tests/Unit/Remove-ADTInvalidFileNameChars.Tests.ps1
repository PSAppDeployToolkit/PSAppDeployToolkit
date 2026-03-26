BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}

Describe 'Remove-ADTInvalidFileNameChars' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry {}
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables {}
    }

    Context 'Clean Names (no invalid characters)' {
        It 'Returns a name with no invalid characters unchanged' {
            Remove-ADTInvalidFileNameChars -Name 'ValidFileName' | Should -Be 'ValidFileName'
        }

        It 'Returns a name with numbers and underscores unchanged' {
            Remove-ADTInvalidFileNameChars -Name 'File_Name_123' | Should -Be 'File_Name_123'
        }

        It 'Returns a name with a dot extension unchanged' {
            Remove-ADTInvalidFileNameChars -Name 'setup.exe' | Should -Be 'setup.exe'
        }

        It 'Returns a name with hyphens and parentheses unchanged' {
            Remove-ADTInvalidFileNameChars -Name 'App (v1.0) - Setup' | Should -Be 'App (v1.0) - Setup'
        }
    }

    Context 'Removing Invalid Characters' {
        It 'Removes a forward slash' {
            Remove-ADTInvalidFileNameChars -Name 'File/Name' | Should -Be 'FileName'
        }

        It 'Removes a backslash' {
            Remove-ADTInvalidFileNameChars -Name 'File\Name' | Should -Be 'FileName'
        }

        It 'Removes a colon' {
            Remove-ADTInvalidFileNameChars -Name 'File:Name' | Should -Be 'FileName'
        }

        It 'Removes an asterisk' {
            Remove-ADTInvalidFileNameChars -Name 'File*Name' | Should -Be 'FileName'
        }

        It 'Removes a question mark' {
            Remove-ADTInvalidFileNameChars -Name 'File?Name' | Should -Be 'FileName'
        }

        It 'Removes angle brackets' {
            Remove-ADTInvalidFileNameChars -Name 'File<Name>' | Should -Be 'FileName'
        }

        It 'Removes a pipe character' {
            Remove-ADTInvalidFileNameChars -Name 'File|Name' | Should -Be 'FileName'
        }

        It 'Removes multiple different invalid characters at once' {
            Remove-ADTInvalidFileNameChars -Name 'File/\:*?<>|Name' | Should -Be 'FileName'
        }

        It 'Returns an empty string when the name consists entirely of invalid characters' {
            Remove-ADTInvalidFileNameChars -Name '/\:*?<>|' | Should -BeNullOrEmpty
        }
    }

    Context 'Whitespace Trimming' {
        It 'Trims leading whitespace from the result' {
            Remove-ADTInvalidFileNameChars -Name '   FileName' | Should -Be 'FileName'
        }

        It 'Trims trailing whitespace from the result' {
            Remove-ADTInvalidFileNameChars -Name 'FileName   ' | Should -Be 'FileName'
        }

        It 'Preserves internal whitespace' {
            Remove-ADTInvalidFileNameChars -Name 'My File Name' | Should -Be 'My File Name'
        }
    }

    Context 'Pipeline Input' {
        It 'Accepts a single value from the pipeline' {
            $result = 'Hello/World' | Remove-ADTInvalidFileNameChars
            $result | Should -Be 'HelloWorld'
        }

        It 'Processes multiple values from the pipeline independently' {
            $results = @('File/A', 'File*B') | Remove-ADTInvalidFileNameChars
            $results | Should -HaveCount 2
            $results[0] | Should -Be 'FileA'
            $results[1] | Should -Be 'FileB'
        }
    }

    Context 'Return Value Type' {
        It 'Returns a System.String for a normal name' {
            $result = Remove-ADTInvalidFileNameChars -Name 'TestFile'
            $result | Should -BeOfType [System.String]
        }
    }

    Context 'Input Validation' {
        It 'Throws when Name is null' {
            { Remove-ADTInvalidFileNameChars -Name $null } | Should -Throw
        }

        It 'Throws when Name is an empty string' {
            { Remove-ADTInvalidFileNameChars -Name '' } | Should -Throw
        }
    }
}
