BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'New-ADTValidateScriptErrorRecord' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Functionality' {
        It 'Returns an [ErrorRecord] with InvalidArgument category and derived ErrorId' {
            $record = New-ADTValidateScriptErrorRecord -ParameterName 'FilePath' -ProvidedValue 'C:\bad' -ExceptionMessage 'Path is invalid'
            $record | Should -BeOfType ([System.Management.Automation.ErrorRecord])
            $record.CategoryInfo.Category | Should -Be ([System.Management.Automation.ErrorCategory]::InvalidArgument)
            $record.FullyQualifiedErrorId | Should -Be 'InvalidFilePathParameterValue'
        }
        It 'Wraps an [ArgumentException] whose ParamName matches ParameterName' {
            $record = New-ADTValidateScriptErrorRecord -ParameterName 'FilePath' -ProvidedValue 'C:\bad' -ExceptionMessage 'Path is invalid'
            $record.Exception | Should -BeOfType ([System.ArgumentException])
            $record.Exception.ParamName | Should -Be 'FilePath'
        }
        It 'Sets TargetObject to the ProvidedValue' {
            $record = New-ADTValidateScriptErrorRecord -ParameterName 'FilePath' -ProvidedValue 'C:\bad' -ExceptionMessage 'Path is invalid'
            $record.TargetObject | Should -Be 'C:\bad'
        }
        It 'Wraps the InnerException when supplied' {
            $inner = [System.IO.IOException]::new('inner error')
            $record = New-ADTValidateScriptErrorRecord -ParameterName 'FilePath' -ProvidedValue 'C:\bad' -ExceptionMessage 'Path is invalid' -InnerException $inner
            $record.Exception.InnerException | Should -BeOfType ([System.IO.IOException])
        }
    }

    Context 'Input Validation' {
        It 'Throws ParameterArgumentValidationError when ParameterName is <Label>' -ForEach @(
            @{ Label = 'null';       Value = $null }
            @{ Label = 'empty';      Value = '' }
            @{ Label = 'whitespace'; Value = " `f`n`r`t`v" }
        ) {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentValidationError,New-ADTValidateScriptErrorRecord'
            }
            { New-ADTValidateScriptErrorRecord -ParameterName $Value -ProvidedValue 'val' -ExceptionMessage 'msg' } | Should @shouldParams
        }
        It 'Throws ParameterArgumentValidationError when ExceptionMessage is <Label>' -ForEach @(
            @{ Label = 'null';       Value = $null }
            @{ Label = 'empty';      Value = '' }
            @{ Label = 'whitespace'; Value = " `f`n`r`t`v" }
        ) {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentValidationError,New-ADTValidateScriptErrorRecord'
            }
            { New-ADTValidateScriptErrorRecord -ParameterName 'Param' -ProvidedValue 'val' -ExceptionMessage $Value } | Should @shouldParams
        }
    }
}
