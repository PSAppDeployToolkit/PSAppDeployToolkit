BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Remove-ADTInvalidFileNameChars' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Functionality' {
        It 'Returns a clean name unchanged' {
            Remove-ADTInvalidFileNameChars -Name 'ValidFileName' | Should -Be 'ValidFileName'
        }
        It 'Removes backslash and forward-slash (both are invalid filename chars): <Name> -> <Expected>' -ForEach @(
            @{ Name = 'Filename/\1';   Expected = 'Filename1' }
            @{ Name = 'path\file.txt'; Expected = 'pathfile.txt' }
            @{ Name = 'a/b/c';        Expected = 'abc' }
        ) {
            Remove-ADTInvalidFileNameChars -Name $Name | Should -Be $Expected
        }
        It 'Removes other invalid filename characters' {
            $invalid = [System.IO.Path]::GetInvalidFileNameChars() -join ''
            $result = Remove-ADTInvalidFileNameChars -Name ('prefix' + $invalid + 'suffix')
            $result | Should -Be 'prefixsuffix'
        }
        It 'Returns a [System.String]' {
            Remove-ADTInvalidFileNameChars -Name 'test' | Should -BeOfType ([System.String])
        }
        It 'Accepts pipeline input' {
            'File/Name' | Remove-ADTInvalidFileNameChars | Should -Be 'FileName'
        }
        It 'Accepts pipeline input bound by property name' {
            [PSCustomObject]@{ Name = 'File\Name' } | Remove-ADTInvalidFileNameChars | Should -Be 'FileName'
        }
    }

    Context 'Input Validation' {
        It 'Throws ParameterArgumentValidationError for invalid Name value: <Label>' -ForEach @(
            @{ Label = 'null';       Value = $null }
            @{ Label = 'empty';      Value = '' }
            @{ Label = 'whitespace'; Value = " `f`n`r`t`v" }
        ) {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentValidationError,Remove-ADTInvalidFileNameChars'
            }
            { Remove-ADTInvalidFileNameChars -Name $Value } | Should @shouldParams
        }
    }
}
