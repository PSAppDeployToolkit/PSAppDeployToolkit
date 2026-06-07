BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Get-ADTLoggedOnUser' {
    BeforeAll {
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Functionality' {
        It 'Should not throw' {
            { Get-ADTLoggedOnUser } | Should -Not -Throw
        }

        It 'Should return PSADT.TerminalServices.SessionInfo objects when sessions exist' {
            $sessions = @(Get-ADTLoggedOnUser)
            if ($sessions.Count -eq 0)
            {
                Set-ItResult -Skipped -Because 'No logged-on user sessions found on this machine.'
                return
            }
            $sessions[0] | Should -BeOfType ([PSADT.TerminalServices.SessionInfo])
        }

        It 'Should return at least one session with a non-empty UserName' {
            $sessions = @(Get-ADTLoggedOnUser)
            if ($sessions.Count -eq 0)
            {
                Set-ItResult -Skipped -Because 'No logged-on user sessions found on this machine.'
                return
            }
            $sessions | Where-Object { -not [string]::IsNullOrEmpty($_.UserName) } | Should -Not -BeNullOrEmpty
        }

        It 'Should return sessions with a non-empty NTAccount when a user is present' {
            $sessions = @(Get-ADTLoggedOnUser)
            if ($sessions.Count -eq 0)
            {
                Set-ItResult -Skipped -Because 'No logged-on user sessions found on this machine.'
                return
            }
            $userSessions = $sessions | Where-Object { $_.IsUserSession }
            if (!$userSessions)
            {
                Set-ItResult -Skipped -Because 'No user sessions (IsUserSession = true) found.'
                return
            }
            $userSessions | ForEach-Object { $_.NTAccount | Should -Not -BeNullOrEmpty }
        }

        It 'Should return Boolean values for session-type flags' {
            $sessions = @(Get-ADTLoggedOnUser)
            if ($sessions.Count -eq 0)
            {
                Set-ItResult -Skipped -Because 'No logged-on user sessions found on this machine.'
                return
            }
            $s = $sessions[0]
            $s.IsCurrentSession    | Should -BeOfType ([System.Boolean])
            $s.IsConsoleSession    | Should -BeOfType ([System.Boolean])
            $s.IsUserSession       | Should -BeOfType ([System.Boolean])
            $s.IsActiveUserSession | Should -BeOfType ([System.Boolean])
            $s.IsRdpSession        | Should -BeOfType ([System.Boolean])
        }

        It 'Should have at most one session marked IsCurrentSession = true' {
            $sessions = @(Get-ADTLoggedOnUser)
            if ($sessions.Count -eq 0)
            {
                Set-ItResult -Skipped -Because 'No logged-on user sessions found on this machine.'
                return
            }
            ($sessions | Where-Object { $_.IsCurrentSession }).Count | Should -BeLessOrEqual 1
        }

        It 'Should return a LogonTime in the past for user sessions' {
            $sessions = @(Get-ADTLoggedOnUser)
            $userSessions = $sessions | Where-Object { $_.IsUserSession -and $null -ne $_.LogonTime }
            if (!$userSessions)
            {
                Set-ItResult -Skipped -Because 'No user sessions with a LogonTime found.'
                return
            }
            $userSessions | ForEach-Object { $_.LogonTime | Should -BeLessThan ([System.DateTime]::Now) }
        }
    }

    Context 'Metadata' {
        It 'Should declare OutputType of PSADT.TerminalServices.SessionInfo' {
            $outputTypes = (Get-Command Get-ADTLoggedOnUser).OutputType.Type
            $outputTypes | Should -Contain ([PSADT.TerminalServices.SessionInfo])
        }
    }
}
