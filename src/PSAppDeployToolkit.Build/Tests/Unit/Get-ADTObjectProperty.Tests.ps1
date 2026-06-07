BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Get-ADTObjectProperty' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Functionality' {
        It 'Returns the value of a known property from an object' {
            $fi = [System.IO.FileInfo]::new('C:\x.txt')
            Get-ADTObjectProperty -InputObject $fi -PropertyName 'Name' | Should -Be 'x.txt'
        }
        It 'Returns the correct type for a known property' {
            $fi = [System.IO.FileInfo]::new('C:\x.txt')
            Get-ADTObjectProperty -InputObject $fi -PropertyName 'Name' | Should -BeOfType ([System.String])
        }
        It 'Returns the character at the given index via -ArgumentList' {
            Get-ADTObjectProperty -InputObject 'hello' -PropertyName 'Chars' -ArgumentList @(1) | Should -Be 'e'
        }
        It 'Throws MissingMethodException when the property does not exist' {
            $fi = [System.IO.FileInfo]::new('C:\x.txt')
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.MethodInvocationException]
                ErrorId       = 'MissingMethodException,Get-ADTObjectProperty'
            }
            { Get-ADTObjectProperty -InputObject $fi -PropertyName 'NonExistentProperty' -ErrorAction Stop } | Should @shouldParams
        }
    }

    Context 'Input Validation' {
        It 'Throws ParameterArgumentValidationError when InputObject is <Label>' -ForEach @(
            @{ Label = 'null';       Value = $null }
            @{ Label = 'empty';      Value = '' }
            @{ Label = 'whitespace'; Value = " `f`n`r`t`v" }
        ) {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentValidationError,Get-ADTObjectProperty'
            }
            { Get-ADTObjectProperty -InputObject $Value -PropertyName 'Name' } | Should @shouldParams
        }
        It 'Throws ParameterArgumentValidationError when PropertyName is <Label>' -ForEach @(
            @{ Label = 'null';       Value = $null }
            @{ Label = 'empty';      Value = '' }
            @{ Label = 'whitespace'; Value = " `f`n`r`t`v" }
        ) {
            $fi = [System.IO.FileInfo]::new('C:\x.txt')
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentValidationError,Get-ADTObjectProperty'
            }
            { Get-ADTObjectProperty -InputObject $fi -PropertyName $Value } | Should @shouldParams
        }
    }
}
