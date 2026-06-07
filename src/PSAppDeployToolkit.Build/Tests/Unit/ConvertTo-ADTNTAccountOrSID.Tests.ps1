BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'ConvertTo-ADTNTAccountOrSID' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

        # Pre-compute the expected NTAccount name for S-1-5-18 from the runtime, avoiding locale-specific hard-coding.
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'LocalSystemNTAccount', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
        $LocalSystemNTAccount = [System.Security.Principal.SecurityIdentifier]::new('S-1-5-18').Translate([System.Security.Principal.NTAccount])

        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'LocalSystemSid', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
        $LocalSystemSid = 'S-1-5-18'
    }

    Context 'SIDToNTAccount parameter set' {
        It 'Converts S-1-5-18 to the NT AUTHORITY\SYSTEM NTAccount' {
            $result = ConvertTo-ADTNTAccountOrSID -SID ([System.Security.Principal.SecurityIdentifier]::new('S-1-5-18'))
            $result | Should -BeOfType ([System.Security.Principal.NTAccount])
            $result.Value | Should -Be $LocalSystemNTAccount.Value
        }

        It 'Returns a SecurityIdentifier type for the NTAccountToSID parameter set' {
            $sid = ConvertTo-ADTNTAccountOrSID -AccountName ([System.Security.Principal.NTAccount]$LocalSystemNTAccount.Value)
            $sid | Should -BeOfType ([System.Security.Principal.SecurityIdentifier])
            $sid.Value | Should -Be $LocalSystemSid
        }

        It 'Converts NT AUTHORITY\NETWORK SERVICE (S-1-5-20) correctly' {
            $expectedNTAccount = [System.Security.Principal.SecurityIdentifier]::new('S-1-5-20').Translate([System.Security.Principal.NTAccount])
            $result = ConvertTo-ADTNTAccountOrSID -SID ([System.Security.Principal.SecurityIdentifier]::new('S-1-5-20'))
            $result | Should -BeOfType ([System.Security.Principal.NTAccount])
            $result.Value | Should -Be $expectedNTAccount.Value
        }
    }

    Context 'NTAccountToSID parameter set' {
        It 'Converts NT AUTHORITY\SYSTEM NTAccount to S-1-5-18 SID' {
            $result = ConvertTo-ADTNTAccountOrSID -AccountName ([System.Security.Principal.NTAccount]$LocalSystemNTAccount.Value)
            $result | Should -BeOfType ([System.Security.Principal.SecurityIdentifier])
            $result.Value | Should -Be $LocalSystemSid
        }

        It 'Round-trips SID -> NTAccount -> SID for BUILTIN\Administrators (S-1-5-32-544)' {
            $originalSid = [System.Security.Principal.SecurityIdentifier]::new('S-1-5-32-544')
            $ntAccount = ConvertTo-ADTNTAccountOrSID -SID $originalSid
            $roundTripped = ConvertTo-ADTNTAccountOrSID -AccountName $ntAccount
            $roundTripped.Value | Should -Be $originalSid.Value
        }
    }

    Context 'WellKnownSIDName parameter set' {
        It 'Converts LocalSystemSid well-known name to S-1-5-18 SID' {
            $result = ConvertTo-ADTNTAccountOrSID -WellKnownSIDName LocalSystemSid -LocalHost
            $result | Should -BeOfType ([System.Security.Principal.SecurityIdentifier])
            $result.Value | Should -Be $LocalSystemSid
        }

        It 'Converts LocalSystemSid well-known name to NTAccount when WellKnownToNTAccount is specified' {
            $result = ConvertTo-ADTNTAccountOrSID -WellKnownSIDName LocalSystemSid -LocalHost -WellKnownToNTAccount
            $result | Should -BeOfType ([System.Security.Principal.NTAccount])
            $result.Value | Should -Be $LocalSystemNTAccount.Value
        }

        It 'Converts NetworkServiceSid well-known name to correct SID (S-1-5-20)' {
            $result = ConvertTo-ADTNTAccountOrSID -WellKnownSIDName NetworkServiceSid -LocalHost
            $result | Should -BeOfType ([System.Security.Principal.SecurityIdentifier])
            $result.Value | Should -Be 'S-1-5-20'
        }

        It 'Converts BuiltinAdministratorsSid well-known name to correct SID (S-1-5-32-544)' {
            $result = ConvertTo-ADTNTAccountOrSID -WellKnownSIDName BuiltinAdministratorsSid -LocalHost
            $result | Should -BeOfType ([System.Security.Principal.SecurityIdentifier])
            $result.Value | Should -Be 'S-1-5-32-544'
        }
    }

    Context 'Input Validation' {
        It 'Should have a mandatory SID parameter in SIDToNTAccount parameter set' {
            (Get-Command ConvertTo-ADTNTAccountOrSID).Parameters['SID'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Contain $true
        }

        It 'Should have a mandatory AccountName parameter in NTAccountToSID parameter set' {
            (Get-Command ConvertTo-ADTNTAccountOrSID).Parameters['AccountName'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Contain $true
        }

        It 'Should have a mandatory WellKnownSIDName parameter in WellKnownName parameter set' {
            (Get-Command ConvertTo-ADTNTAccountOrSID).Parameters['WellKnownSIDName'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Contain $true
        }

        It 'Throws ParameterArgumentTransformationError when SID is an invalid SID string' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
            }
            { ConvertTo-ADTNTAccountOrSID -SID 'not-a-valid-sid' } | Should @shouldParams
        }

        It 'Throws ParameterArgumentTransformationError when WellKnownSIDName is an invalid enum value' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
            }
            { ConvertTo-ADTNTAccountOrSID -WellKnownSIDName 'NotARealSidName' } | Should @shouldParams
        }

        It 'LdapUri only accepts LDAP:// or LDAPS://' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentValidationError,ConvertTo-ADTNTAccountOrSID'
            }
            { ConvertTo-ADTNTAccountOrSID -WellKnownSIDName LocalSystemSid -LdapUri 'HTTP://' } | Should @shouldParams
        }
    }
}
