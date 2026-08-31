BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

    function Get-Probe
    {
        param
        (
            [Parameter(Mandatory = $false)]
            [System.Collections.Hashtable]$Splat = @{}
        )

        InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ S = $Splat } {
            Get-ADTClientServerUser @S
        }
    }
}

Describe 'Get-ADTClientServerUser' {
    Context 'Functionality' {
        BeforeAll {
            $script:User = Get-Probe
        }

        It 'Returns the user the client process would run as' {
            # Everything that shows UI or runs in the user's context resolves the target this way, so a null
            # here is what makes those functions bypass themselves.
            $script:User | Should -BeOfType ([PSADT.Foundation.RunAsActiveUser])
        }

        It 'Agrees with Get-ADTLoggedOnUser about who that is' {
            $wantedSid = $script:User.SID.Value
            $session = Get-ADTLoggedOnUser | & { process { if ($_.SID.Value.Equals($wantedSid)) { return $_ } } } | Select-Object -First 1
            $session | Should -Not -BeNullOrEmpty
            $session.NTAccount.Value | Should -BeExactly $script:User.NTAccount.Value
            $session.SessionId | Should -Be $script:User.SessionId
        }

        It 'Splits the account into its domain and user parts' {
            $script:User.NTAccount.Value | Should -BeExactly "$($script:User.DomainName)\$($script:User.UserName)"
        }

        It 'Reports whether that user is a local administrator' {
            $script:User.IsLocalAdmin | Should -BeOfType ([System.Boolean])
        }

        It 'Resolves a named user' {
            $named = Get-Probe -Splat @{ Username = $script:User.UserName; AllowAnyValidSession = $true }
            $named.SID.Value | Should -BeExactly $script:User.SID.Value
        }

        It 'Returns nothing for a user who is not logged on' {
            Get-Probe -Splat @{ Username = 'NoSuchUserIsLoggedOn12345'; AllowAnyValidSession = $true } | Should -BeNullOrEmpty
        }
    }
}
