BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}

Describe 'ConvertTo-ADTNTAccountOrSID' {
    BeforeAll {
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables { }
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'NTAccount to SID' {
        It 'Converts BUILTIN\Administrators to S-1-5-32-544' {
            $result = ConvertTo-ADTNTAccountOrSID -AccountName 'BUILTIN\Administrators'
            $result.Value | Should -Be 'S-1-5-32-544'
        }

        It 'Converts NT AUTHORITY\SYSTEM to S-1-5-18' {
            $result = ConvertTo-ADTNTAccountOrSID -AccountName 'NT AUTHORITY\SYSTEM'
            $result.Value | Should -Be 'S-1-5-18'
        }

        It 'Returns a SecurityIdentifier' {
            ConvertTo-ADTNTAccountOrSID -AccountName 'NT AUTHORITY\SYSTEM' | Should -BeOfType [System.Security.Principal.SecurityIdentifier]
        }

        It 'Does not throw for a well-known local account' {
            { ConvertTo-ADTNTAccountOrSID -AccountName 'BUILTIN\Administrators' } | Should -Not -Throw
        }
    }

    Context 'SID to NTAccount' {
        It 'Converts S-1-5-32-544 to BUILTIN\Administrators' {
            $sid = [System.Security.Principal.SecurityIdentifier]::new('S-1-5-32-544')
            $result = ConvertTo-ADTNTAccountOrSID -SID $sid
            $result.Value | Should -Be 'BUILTIN\Administrators'
        }

        It 'Converts S-1-5-18 to NT AUTHORITY\SYSTEM' {
            $sid = [System.Security.Principal.SecurityIdentifier]::new('S-1-5-18')
            $result = ConvertTo-ADTNTAccountOrSID -SID $sid
            $result.Value | Should -Be 'NT AUTHORITY\SYSTEM'
        }

        It 'Returns an NTAccount' {
            $sid = [System.Security.Principal.SecurityIdentifier]::new('S-1-5-18')
            ConvertTo-ADTNTAccountOrSID -SID $sid | Should -BeOfType [System.Security.Principal.NTAccount]
        }
    }

    Context 'Well-Known SID with -LocalHost' {
        It 'Returns a SecurityIdentifier for BuiltinAdministratorsSid' {
            ConvertTo-ADTNTAccountOrSID -WellKnownSIDName BuiltinAdministratorsSid -LocalHost | Should -BeOfType [System.Security.Principal.SecurityIdentifier]
        }

        It 'BuiltinAdministratorsSid resolves to S-1-5-32-544' {
            $result = ConvertTo-ADTNTAccountOrSID -WellKnownSIDName BuiltinAdministratorsSid -LocalHost
            $result.Value | Should -Be 'S-1-5-32-544'
        }

        It 'Returns an NTAccount when -WellKnownToNTAccount is specified' {
            ConvertTo-ADTNTAccountOrSID -WellKnownSIDName BuiltinAdministratorsSid -LocalHost -WellKnownToNTAccount | Should -BeOfType [System.Security.Principal.NTAccount]
        }

        It 'NTAccount for BuiltinAdministratorsSid is BUILTIN\Administrators' {
            $result = ConvertTo-ADTNTAccountOrSID -WellKnownSIDName BuiltinAdministratorsSid -LocalHost -WellKnownToNTAccount
            $result.Value | Should -Be 'BUILTIN\Administrators'
        }
    }

    Context 'Round-Trip Conversion' {
        It 'AccountName to SID to AccountName round-trips correctly' {
            $original = 'BUILTIN\Administrators'
            $sid = ConvertTo-ADTNTAccountOrSID -AccountName $original
            $back = ConvertTo-ADTNTAccountOrSID -SID $sid
            $back.Value | Should -Be $original
        }

        It 'SID to AccountName to SID round-trips correctly' {
            $original = [System.Security.Principal.SecurityIdentifier]::new('S-1-5-18')
            $name = ConvertTo-ADTNTAccountOrSID -SID $original
            $back = ConvertTo-ADTNTAccountOrSID -AccountName $name.Value
            $back.Value | Should -Be $original.Value
        }
    }
}
