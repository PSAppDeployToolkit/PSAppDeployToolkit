BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Set-ADTServiceStartMode' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Input Validation' {
        It 'Should verify that -Name is not null, empty or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,Set-ADTServiceStartMode'
            }
            { Set-ADTServiceStartMode -Name $null -StartMode 'Automatic' } | Should @shouldParams
            { Set-ADTServiceStartMode -Name '' -StartMode 'Automatic' } | Should @shouldParams
            { Set-ADTServiceStartMode -Name " `f`n`r`t`v" -StartMode 'Automatic' } | Should @shouldParams
        }
        It 'Should verify that -DisplayName is not null, empty or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,Set-ADTServiceStartMode'
            }
            { Set-ADTServiceStartMode -DisplayName $null -StartMode 'Automatic' } | Should @shouldParams
            { Set-ADTServiceStartMode -DisplayName '' -StartMode 'Automatic' } | Should @shouldParams
            { Set-ADTServiceStartMode -DisplayName " `f`n`r`t`v" -StartMode 'Automatic' } | Should @shouldParams
        }
        It 'Should verify that -InputObject is not null, empty or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
            }
            { Set-ADTServiceStartMode -InputObject $null -StartMode 'Automatic' } | Should @shouldParams -ErrorId 'ParameterArgumentValidationError,Set-ADTServiceStartMode'
            { Set-ADTServiceStartMode -InputObject '' -StartMode 'Automatic' } | Should @shouldParams -ErrorId 'ParameterArgumentTransformationError,Set-ADTServiceStartMode'
            { Set-ADTServiceStartMode -InputObject " `f`n`r`t`v" -StartMode 'Automatic' } | Should @shouldParams -ErrorId 'ParameterArgumentValidationError,Set-ADTServiceStartMode'
        }
    }
}
