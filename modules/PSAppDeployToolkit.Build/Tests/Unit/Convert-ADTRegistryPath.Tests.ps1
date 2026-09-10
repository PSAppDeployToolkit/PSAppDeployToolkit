BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest
}
Describe 'Convert-ADTRegistryPath' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

        # A SID that belongs to nobody, which is the point: converting a key for the account already behind
        # HKEY_CURRENT_USER is a no-op by design, so naming the caller's own SID skips both the rewrite and
        # the validation that goes with it. 'S-1-5-18' used to stand in for another user here and stopped
        # doing so the moment the suite was run as LocalSystem.
        $script:OtherUserSid = 'S-1-5-21-1111111111-2222222222-3333333333-1001'
    }

    Context 'Functionality' {
        It 'Should return Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE' {
            Convert-ADTRegistryPath -Key 'HKLM\SOFTWARE' | Should -Be 'Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE'
            Convert-ADTRegistryPath -Key 'HKLM:\SOFTWARE' | Should -Be 'Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE'
            Convert-ADTRegistryPath -Key 'HKEY_LOCAL_MACHINE\SOFTWARE' | Should -Be 'Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE'
        }
        It 'Should rewrite a user key to the HKEY_USERS hive of the SID it was given' {
            $expected = "Microsoft.PowerShell.Core\Registry::HKEY_USERS\$script:OtherUserSid\SOFTWARE"
            Convert-ADTRegistryPath -Key 'HKCU\SOFTWARE' -SID $script:OtherUserSid | Should -Be $expected
            Convert-ADTRegistryPath -Key 'HKCU:\SOFTWARE' -SID $script:OtherUserSid | Should -Be $expected
            Convert-ADTRegistryPath -Key 'HKEY_CURRENT_USER\SOFTWARE' -SID $script:OtherUserSid | Should -Be $expected
        }

        It 'Should leave a user key alone for the SID already behind it' {
            # HKEY_CURRENT_USER is that account's hive already, so there is nothing to rewrite. This is the
            # case the tests above used to hit by accident whenever the suite ran as the named account.
            $caller = (Get-ADTCallerSid).Value
            Convert-ADTRegistryPath -Key 'HKCU\SOFTWARE' -SID $caller | Should -Be 'Microsoft.PowerShell.Core\Registry::HKEY_CURRENT_USER\SOFTWARE'
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
            { Convert-ADTRegistryPath -Key 'HKLM\SOFTWARE' -SID $script:OtherUserSid } | Should @shouldParams
            { Convert-ADTRegistryPath -Key 'HKLM:\SOFTWARE' -SID $script:OtherUserSid } | Should @shouldParams
            { Convert-ADTRegistryPath -Key 'HKEY_LOCAL_MACHINE\SOFTWARE' -SID $script:OtherUserSid } | Should @shouldParams
            { Convert-ADTRegistryPath -Key 'HKEY_LOCAL_MACHINE:\SOFTWARE' -SID $script:OtherUserSid } | Should @shouldParams
            { Convert-ADTRegistryPath -Key 'HKEY_CURRENT_USER:\SOFTWARE' -SID $script:OtherUserSid } | Should @shouldParams
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
