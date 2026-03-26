BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Test-ADTRegistryValue' {
    BeforeAll {
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'TestRegistry', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
        $TestRegistry = (New-Item -Path 'TestRegistry:\TestLocation' -ItemType Directory).PSPath
        New-ItemProperty -LiteralPath $TestRegistry -Name 'Test' -Value 0 -PropertyType DWord | Out-Null

        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables {}
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

        # Test-ADTRegistryValue calls Convert-ADTRegistryPath internally, which references
        # [PSADT.AccountManagement.AccountUtilities]::CallerSid at compile time.
        # PowerShell resolves all type literals at compile time, requiring admin rights.
        $script:IsAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    }

    BeforeEach {
        if (!$script:IsAdmin) { Set-ItResult -Skipped -Because 'Requires admin rights (AccountUtilities static constructor triggered at compile time)'; return }
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
            { Test-ADTRegistryValue -Key " `f`n`r`t`v" -Name 'Anything' } | Should @shouldParams
        }
        It 'Should verify that Name is not null, empty or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,Test-ADTRegistryValue'
            }
            { Test-ADTRegistryValue -Key 'Anything' -Name $null } | Should @shouldParams
            { Test-ADTRegistryValue -Key 'Anything' -Name '' } | Should @shouldParams
            { Test-ADTRegistryValue -Key 'Anything' -Name " `f`n`r`t`v" } | Should @shouldParams
        }
        It 'Should verify that SID is not null, empty or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
            }
            { Test-ADTRegistryValue -Key 'Anything' -Name 'Test' -SID $null } | Should @shouldParams -ErrorId 'ParameterArgumentValidationError,Test-ADTRegistryValue'
            { Test-ADTRegistryValue -Key 'Anything' -Name 'Test' -SID '' } | Should @shouldParams -ErrorId 'ParameterArgumentTransformationError,Test-ADTRegistryValue'
            { Test-ADTRegistryValue -Key 'Anything' -Name 'Test' -SID " `f`n`r`t`v" } | Should @shouldParams -ErrorId 'ParameterArgumentTransformationError,Test-ADTRegistryValue'
        }
    }
}
