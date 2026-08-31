BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}

Describe 'ConvertTo-ADTNTAccountOrSID' {
    Context 'Functionality' {
        It 'Turns <AccountName> into <Sid>' -ForEach @(
            @{ AccountName = 'BUILTIN\Administrators'; Sid = 'S-1-5-32-544' }
            @{ AccountName = 'NT AUTHORITY\SYSTEM'; Sid = 'S-1-5-18' }
            @{ AccountName = 'BUILTIN\Users'; Sid = 'S-1-5-32-545' }
        ) {
            # Well-known accounts, so the expected values hold on any Windows install regardless of locale.
            $result = ConvertTo-ADTNTAccountOrSID -AccountName $AccountName
            $result | Should -BeOfType ([System.Security.Principal.SecurityIdentifier])
            $result.Value | Should -BeExactly $Sid
        }

        It 'Turns <Sid> back into an account' -ForEach @(
            @{ Sid = 'S-1-5-32-544' }
            @{ Sid = 'S-1-5-18' }
        ) {
            # Round-tripped rather than compared against a literal, because the account names are localised.
            $account = ConvertTo-ADTNTAccountOrSID -SID $Sid
            $account | Should -BeOfType ([System.Security.Principal.NTAccount])
            (ConvertTo-ADTNTAccountOrSID -AccountName $account.Value).Value | Should -BeExactly $Sid
        }

        It 'Resolves the well-known name <WellKnownSIDName> to <Sid>' -ForEach @(
            @{ WellKnownSIDName = 'BuiltinAdministratorsSid'; Sid = 'S-1-5-32-544' }
            @{ WellKnownSIDName = 'LocalSystemSid'; Sid = 'S-1-5-18' }
            @{ WellKnownSIDName = 'BuiltinUsersSid'; Sid = 'S-1-5-32-545' }
        ) {
            (ConvertTo-ADTNTAccountOrSID -WellKnownSIDName $WellKnownSIDName).Value | Should -BeExactly $Sid
        }

        It 'Returns the account rather than the SID with -WellKnownToNTAccount' {
            ConvertTo-ADTNTAccountOrSID -WellKnownSIDName 'LocalSystemSid' -WellKnownToNTAccount | Should -BeOfType ([System.Security.Principal.NTAccount])
        }

        It 'Rejects a SID that is not one' {
            { ConvertTo-ADTNTAccountOrSID -SID 'not-a-sid' } | Should -Throw -ErrorId 'ParameterArgumentTransformationError,ConvertTo-ADTNTAccountOrSID'
        }

        It 'Reports an account it cannot resolve' {
            # The failure the caller needs to see is the translation one. Without a process block on the
            # group policy fallback, the body ran once with a null $_ and buried it under a missing
            # 'Username' property instead.
            { ConvertTo-ADTNTAccountOrSID -AccountName 'NoSuchAccountHere12345' -ErrorAction Stop } | Should -Throw -ErrorId 'IdentityNotMappedException,ConvertTo-ADTNTAccountOrSID'
        }

        It 'Reports a SID it cannot resolve the same way' {
            # The mirror direction, which already had its process block. Both should fail alike.
            { ConvertTo-ADTNTAccountOrSID -SID 'S-1-5-21-1111111111-2222222222-3333333333-4444' -ErrorAction Stop } | Should -Throw -ErrorId 'IdentityNotMappedException,ConvertTo-ADTNTAccountOrSID'
        }
    }
}
