BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Test-ADTRegistryValue' {
    BeforeAll {
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'TestRegistry', Justification = 'This variable is used within scriptblocks that PSScriptAnalyzer has no visibility of.')]
        $TestRegistry = (New-Item -Path 'TestRegistry:\TestLocation' -ItemType Directory).PSPath
        New-ItemProperty -LiteralPath $TestRegistry -Name 'Test' -Value 0 -PropertyType DWord | Out-Null

        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Functionality' {
        It 'Should return $true' {
            Test-ADTRegistryValue -Key $TestRegistry -Name 'Test' | Should -BeTrue
        }
        It 'Should return $false' {
            Test-ADTRegistryValue -Key $TestRegistry -Name 'DoesNotExist' | Should -BeFalse
        }
    }

    Context 'Input Validation' {
        It 'Should verify that Key is not null, empty or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,Test-ADTRegistryValue'
            }
            { Test-ADTRegistryValue -Key $null -Name 'Anything' } | Should @shouldParams
            { Test-ADTRegistryValue -Key '' -Name 'Anything' } | Should @shouldParams
            { Test-ADTRegistryValue -Key ' ' -Name 'Anything' } | Should @shouldParams
        }
        It 'Should verify that Name is not null, empty or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,Test-ADTRegistryValue'
            }
            { Test-ADTRegistryValue -Key 'Anything' -Name $null } | Should @shouldParams
            { Test-ADTRegistryValue -Key 'Anything' -Name '' } | Should @shouldParams
            { Test-ADTRegistryValue -Key 'Anything' -Name ' ' } | Should @shouldParams
        }
        It 'Should verify that SID is not null, empty or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
            }
            { Test-ADTRegistryValue -Key 'Anything' -Name 'Test' -SID $null } | Should @shouldParams -ErrorId 'ParameterArgumentValidationError,Test-ADTRegistryValue'
            { Test-ADTRegistryValue -Key 'Anything' -Name 'Test' -SID '' } | Should @shouldParams -ErrorId 'ParameterArgumentTransformationError,Test-ADTRegistryValue'
            { Test-ADTRegistryValue -Key 'Anything' -Name 'Test' -SID ' ' } | Should @shouldParams -ErrorId 'ParameterArgumentTransformationError,Test-ADTRegistryValue'
        }
    }
}
