BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Invoke-ADTObjectMethod' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Positional parameter set' {
        It 'Invokes a no-argument method and returns the correct result' {
            $result = Invoke-ADTObjectMethod -InputObject 'Hello' -MethodName 'ToUpper'
            $result | Should -Be 'HELLO'
        }
        It 'Invokes a method with positional ArgumentList and returns the correct result' {
            $result = Invoke-ADTObjectMethod -InputObject 'Hello' -MethodName 'Substring' -ArgumentList @(1, 3)
            $result | Should -Be 'ell'
        }
        It 'Returns the correct type from the invoked method' {
            $result = Invoke-ADTObjectMethod -InputObject 'Hello' -MethodName 'ToUpper'
            $result | Should -BeOfType ([System.String])
        }
    }

    Context 'Named parameter set' {
        It 'Invokes a method using named -Parameter hashtable and returns the correct result' {
            $result = Invoke-ADTObjectMethod -InputObject 'Hello World' -MethodName 'Replace' -Parameter @{ oldValue = 'World'; newValue = 'Pester' }
            $result | Should -Be 'Hello Pester'
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
                ErrorId       = 'ParameterArgumentValidationError,Invoke-ADTObjectMethod'
            }
            { Invoke-ADTObjectMethod -InputObject $Value -MethodName 'ToUpper' } | Should @shouldParams
        }
        It 'Throws ParameterArgumentValidationError when MethodName is <Label>' -ForEach @(
            @{ Label = 'null';       Value = $null }
            @{ Label = 'empty';      Value = '' }
            @{ Label = 'whitespace'; Value = " `f`n`r`t`v" }
        ) {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentValidationError,Invoke-ADTObjectMethod'
            }
            { Invoke-ADTObjectMethod -InputObject 'Hello' -MethodName $Value } | Should @shouldParams
        }
        It 'Throws ParameterArgumentValidationError when Parameter hashtable is empty' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentValidationError,Invoke-ADTObjectMethod'
            }
            { Invoke-ADTObjectMethod -InputObject 'Hello' -MethodName 'ToUpper' -Parameter @{} } | Should @shouldParams
        }
    }
}
