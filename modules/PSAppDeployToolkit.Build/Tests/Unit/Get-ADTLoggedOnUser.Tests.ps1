BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}

Describe 'Get-ADTLoggedOnUser' {
    Context 'Functionality' {
        BeforeAll {
            # Typed rather than wrapped, so that Count is the array's own and not an ETS member that
            # StrictMode withholds from a lone session.
            [PSADT.TerminalServices.SessionInfo[]]$script:Sessions = Get-ADTLoggedOnUser
        }

        It 'Returns session information' {
            $script:Sessions.Count | Should -BeGreaterThan 0
            $script:Sessions[0] | Should -BeOfType ([PSADT.TerminalServices.SessionInfo])
        }

        It 'Finds the session this test is running in' {
            # There is exactly one current session by definition, and it has to be the one the test process
            # belongs to.
            $current = @($script:Sessions | & { process { if ($_.IsCurrentSession) { return $_ } } })
            $current.Count | Should -Be 1
            $process = [System.Diagnostics.Process]::GetCurrentProcess()
            try
            {
                $current[0].SessionId | Should -Be $process.SessionId
            }
            finally
            {
                $process.Dispose()
            }
        }

        It 'Identifies the account signed into the session the test is running in' {
            # Not always the account running the test. A deployment runs as LocalSystem inside somebody
            # else's session: the process belongs to the machine and the session belongs to them, so what
            # comes back describes them rather than the caller.
            $current = $script:Sessions | & { process { if ($_.IsCurrentSession) { return $_ } } } | Select-Object -First 1
            $current.SID.Value | Should -Not -BeNullOrEmpty
            $current.UserName | Should -Not -BeNullOrEmpty
            if ((Get-ADTCallerSid).Value -ne 'S-1-5-18')
            {
                $current.SID.Value | Should -BeExactly ((Get-ADTCallerSid).Value)
                $current.UserName | Should -BeExactly (Get-ADTCallerUserName)
            }
        }

        It 'Splits the account into its domain and user parts' {
            $current = $script:Sessions | & { process { if ($_.IsCurrentSession) { return $_ } } } | Select-Object -First 1
            $current.NTAccount.Value | Should -BeExactly "$($current.DomainName)\$($current.UserName)"
        }

        It 'Nominates at most one console session' {
            # More than one would mean the console session could not be identified, which is what the
            # toolkit uses to decide where to show a dialog.
            @($script:Sessions | & { process { if ($_.IsConsoleSession) { return $_ } } }).Count | Should -BeLessOrEqual 1
        }

        It 'Gives every session a distinct id' {
            ($script:Sessions.SessionId | Select-Object -Unique | Measure-Object).Count | Should -Be $script:Sessions.Count
        }
    }
}
