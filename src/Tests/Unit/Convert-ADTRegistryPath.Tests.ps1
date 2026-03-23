BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Convert-ADTRegistryPath' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Functionality' {
        It 'Should return Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE' {
            Convert-ADTRegistryPath -Key 'HKLM\SOFTWARE' | Should -Be 'Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE'
            Convert-ADTRegistryPath -Key 'HKLM:\SOFTWARE' | Should -Be 'Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE'
            Convert-ADTRegistryPath -Key 'HKEY_LOCAL_MACHINE\SOFTWARE' | Should -Be 'Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE'
        }
        It 'Should return Microsoft.PowerShell.Core\Registry::HKEY_USERS\S-1-5-18\SOFTWARE' {
            Convert-ADTRegistryPath -Key 'HKCU\SOFTWARE' -SID 'S-1-5-18' | Should -Be 'Microsoft.PowerShell.Core\Registry::HKEY_USERS\S-1-5-18\SOFTWARE'
            Convert-ADTRegistryPath -Key 'HKCU:\SOFTWARE' -SID 'S-1-5-18' | Should -Be 'Microsoft.PowerShell.Core\Registry::HKEY_USERS\S-1-5-18\SOFTWARE'
            Convert-ADTRegistryPath -Key 'HKEY_CURRENT_USER\SOFTWARE' -SID 'S-1-5-18' | Should -Be 'Microsoft.PowerShell.Core\Registry::HKEY_USERS\S-1-5-18\SOFTWARE'
        }
        It 'Should return Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node' {
            Convert-ADTRegistryPath -Key 'HKLM\SOFTWARE' -Wow6432Node | Should -Be 'Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node'
            Convert-ADTRegistryPath -Key 'HKLM:\SOFTWARE' -Wow6432Node | Should -Be 'Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node'
            Convert-ADTRegistryPath -Key 'HKEY_LOCAL_MACHINE\SOFTWARE' -Wow6432Node | Should -Be 'Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node'
        }
    }

    Context 'Input Validation' {
        It 'Should verify that Key is not null, empty or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,Convert-ADTRegistryPath'
            }
            { Convert-ADTRegistryPath -Key $null } | Should @shouldParams
            { Convert-ADTRegistryPath -Key '' } | Should @shouldParams
            { Convert-ADTRegistryPath -Key " `f`n`r`t`v" } | Should @shouldParams
        }
        It 'Should verify that SID is not null, empty or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
            }
            { Convert-ADTRegistryPath -Key 'Anything' -SID $null } | Should @shouldParams -ErrorId 'ParameterArgumentValidationError,Convert-ADTRegistryPath'
            { Convert-ADTRegistryPath -Key 'Anything' -SID '' } | Should @shouldParams -ErrorId 'ParameterArgumentTransformationError,Convert-ADTRegistryPath'
            { Convert-ADTRegistryPath -Key 'Anything' -SID " `f`n`r`t`v" } | Should @shouldParams -ErrorId 'ParameterArgumentTransformationError,Convert-ADTRegistryPath'
        }
        It 'Should verify that the registry hive is HKEY_CURRENT_USER when the -SID parameter is provided' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.InvalidOperationException]
                ErrorId = 'SidSpecifiedForNonUserRegistryHive,Convert-ADTRegistryPath'
            }
            { Convert-ADTRegistryPath -Key 'HKLM\SOFTWARE' -SID 'S-1-5-18' } | Should @shouldParams
            { Convert-ADTRegistryPath -Key 'HKLM:\SOFTWARE' -SID 'S-1-5-18' } | Should @shouldParams
            { Convert-ADTRegistryPath -Key 'HKEY_LOCAL_MACHINE\SOFTWARE' -SID 'S-1-5-18' } | Should @shouldParams
            { Convert-ADTRegistryPath -Key 'HKEY_LOCAL_MACHINE:\SOFTWARE' -SID 'S-1-5-18' } | Should @shouldParams
            { Convert-ADTRegistryPath -Key 'HKEY_CURRENT_USER:\SOFTWARE' -SID 'S-1-5-18' } | Should @shouldParams
        }
        It 'Should verify that the registry hive provided is a valid registry hive' {
            { Convert-ADTRegistryPath -Key 'HKCC:\TestLocation' } | Should -Not -Throw
            { Convert-ADTRegistryPath -Key 'HKCU:\TestLocation' } | Should -Not -Throw
            { Convert-ADTRegistryPath -Key 'HKCR:\TestLocation' } | Should -Not -Throw
            { Convert-ADTRegistryPath -Key 'HKLM:\TestLocation' } | Should -Not -Throw
            { Convert-ADTRegistryPath -Key 'HKPD:\TestLocation' } | Should -Not -Throw
            { Convert-ADTRegistryPath -Key 'HKU:\TestLocation' } | Should -Not -Throw
            { Convert-ADTRegistryPath -Key 'TestRegistry:\TestLocation' } | Should -Throw -ExceptionType ([System.ArgumentException]) -ErrorId 'RegistryKeyValueInvalid,Convert-ADTRegistryPath'
        }
    }
}
