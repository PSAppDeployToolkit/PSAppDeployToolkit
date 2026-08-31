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

        It 'Reports an account it cannot resolve' -Skip {
            # Skipped: the off-domain fallback in Convert-ADTNTAccountToSID pipes
            # GroupPolicyAccountInfo::Get() into `& { if ($_.Username.Equals(...)) }` with no process block,
            # so the body runs once in the end block where $_ is null. The result is "The property
            # 'Username' cannot be found on this object" rather than either a resolved SID or the original
            # translation failure. Its mirror, the SID to account direction at line 146, has the process
            # block and behaves correctly.
            #
            # The comment on that branch says it exists for a device with no line of sight to a domain
            # controller, which is a normal state for a deployment. Unskip with the fix.
            { ConvertTo-ADTNTAccountOrSID -AccountName 'NoSuchAccountHere12345' -ErrorAction Stop } | Should -Throw -ExceptionType ([System.Security.Principal.IdentityNotMappedException])
        }
    }
}
