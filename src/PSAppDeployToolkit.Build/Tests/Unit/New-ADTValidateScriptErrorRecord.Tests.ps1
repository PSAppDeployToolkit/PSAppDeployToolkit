BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}

Describe 'New-ADTValidateScriptErrorRecord' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry {}
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables {}
    }

    Context 'Return Value Type' {
        It 'Returns a System.Management.Automation.ErrorRecord' {
            $result = New-ADTValidateScriptErrorRecord -ParameterName 'FilePath' -ProvidedValue 'C:\Bad' -ExceptionMessage 'Path does not exist.'
            $result | Should -BeOfType [System.Management.Automation.ErrorRecord]
        }
    }

    Context 'ErrorId Format' {
        It 'Sets FullyQualifiedErrorId to "Invalid{ParameterName}ParameterValue"' {
            $result = New-ADTValidateScriptErrorRecord -ParameterName 'FilePath' -ProvidedValue 'C:\Bad' -ExceptionMessage 'Bad path.'
            $result.FullyQualifiedErrorId | Should -Match 'InvalidFilePathParameterValue'
        }

        It 'Incorporates any ParameterName into the ErrorId' {
            $result = New-ADTValidateScriptErrorRecord -ParameterName 'Drive' -ProvidedValue 'Q:' -ExceptionMessage 'Drive not found.'
            $result.FullyQualifiedErrorId | Should -Match 'InvalidDriveParameterValue'
        }
    }

    Context 'Exception Properties' {
        It 'Exception type is System.ArgumentException' {
            $result = New-ADTValidateScriptErrorRecord -ParameterName 'Path' -ProvidedValue 'value' -ExceptionMessage 'msg'
            $result.Exception | Should -BeOfType [System.ArgumentException]
        }

        It 'Exception Message starts with the supplied ExceptionMessage' {
            # .NET appends " (Parameter '<name>')" to ArgumentException.Message in modern runtimes.
            $msg = 'The specified path does not exist.'
            $result = New-ADTValidateScriptErrorRecord -ParameterName 'Path' -ProvidedValue 'C:\Bad' -ExceptionMessage $msg
            $result.Exception.Message | Should -BeLike "$msg*"
        }

        It 'ArgumentException.ParamName is set to the supplied ParameterName' {
            $result = New-ADTValidateScriptErrorRecord -ParameterName 'MyParam' -ProvidedValue 'value' -ExceptionMessage 'msg'
            ([System.ArgumentException]$result.Exception).ParamName | Should -Be 'MyParam'
        }
    }

    Context 'Error Category' {
        It 'CategoryInfo.Category is InvalidArgument' {
            $result = New-ADTValidateScriptErrorRecord -ParameterName 'Path' -ProvidedValue 'value' -ExceptionMessage 'msg'
            $result.CategoryInfo.Category | Should -Be ([System.Management.Automation.ErrorCategory]::InvalidArgument)
        }
    }

    Context 'TargetObject' {
        It 'TargetObject is set to the supplied ProvidedValue (string)' {
            $result = New-ADTValidateScriptErrorRecord -ParameterName 'Path' -ProvidedValue 'C:\Bad' -ExceptionMessage 'msg'
            $result.TargetObject | Should -Be 'C:\Bad'
        }

        It 'TargetObject holds a numeric ProvidedValue correctly' {
            $result = New-ADTValidateScriptErrorRecord -ParameterName 'Count' -ProvidedValue 42 -ExceptionMessage 'msg'
            $result.TargetObject | Should -Be 42
        }
    }

    Context 'InnerException Parameter' {
        It 'Wraps the supplied InnerException inside the ArgumentException' {
            $inner = [System.Exception]::new('Root cause')
            $result = New-ADTValidateScriptErrorRecord -ParameterName 'Path' -ProvidedValue 'bad' -ExceptionMessage 'Outer.' -InnerException $inner
            $result.Exception.InnerException.Message | Should -Be 'Root cause'
        }

        It 'InnerException is null when the parameter is not supplied' {
            $result = New-ADTValidateScriptErrorRecord -ParameterName 'Path' -ProvidedValue 'bad' -ExceptionMessage 'msg'
            $result.Exception.InnerException | Should -BeNullOrEmpty
        }
    }

    Context 'Input Validation' {
        It 'Throws when ParameterName is null' {
            { New-ADTValidateScriptErrorRecord -ParameterName $null -ProvidedValue 'val' -ExceptionMessage 'msg' } | Should -Throw
        }

        It 'Throws when ParameterName is an empty string' {
            { New-ADTValidateScriptErrorRecord -ParameterName '' -ProvidedValue 'val' -ExceptionMessage 'msg' } | Should -Throw
        }

        It 'Throws when ExceptionMessage is null' {
            { New-ADTValidateScriptErrorRecord -ParameterName 'Path' -ProvidedValue 'val' -ExceptionMessage $null } | Should -Throw
        }

        It 'Throws when ExceptionMessage is an empty string' {
            { New-ADTValidateScriptErrorRecord -ParameterName 'Path' -ProvidedValue 'val' -ExceptionMessage '' } | Should -Throw
        }
    }
}
