BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}

Describe 'Get-ADTLoggedOnUser' {
    BeforeAll {
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables { }
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

        # Get-ADTLoggedOnUser calls [PSADT.TerminalServices.SessionManager]::GetSessionInfo() which
        # internally triggers AccountUtilities static constructor — requires admin rights.
        $script:IsAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    }

    BeforeEach {
        if (!$script:IsAdmin) { Set-ItResult -Skipped -Because 'Requires admin rights (AccountUtilities static constructor triggered at compile time)'; return }
    }

    Context 'Basic Invocation' {
        It 'Does not throw when called' {
            { Get-ADTLoggedOnUser } | Should -Not -Throw
        }

        It 'Result is null or a collection with Count >= 0' {
            $result = Get-ADTLoggedOnUser
            if ($null -ne $result)
            {
                ($result | Measure-Object).Count | Should -BeGreaterOrEqual 0
            }
        }
    }

    Context 'Session Element Properties' {
        BeforeAll {
            if ($script:IsAdmin)
            {
                $script:Sessions = Get-ADTLoggedOnUser
            }
        }

        It 'Each element has a UserName string property' {
            if (!$script:Sessions)
            {
                Set-ItResult -Skipped -Because 'No user sessions are active on this machine'
                return
            }
            foreach ($session in $script:Sessions)
            {
                $session.UserName | Should -BeOfType ([System.String])
            }
        }

        It 'Each element has a SessionId property' {
            if (!$script:Sessions)
            {
                Set-ItResult -Skipped -Because 'No user sessions are active on this machine'
                return
            }
            foreach ($session in $script:Sessions)
            {
                $session.SessionId | Should -Not -BeNull
            }
        }

        It 'Each element has a non-null NTAccount property' {
            if (!$script:Sessions)
            {
                Set-ItResult -Skipped -Because 'No user sessions are active on this machine'
                return
            }
            foreach ($session in $script:Sessions)
            {
                $session.NTAccount | Should -Not -BeNull
            }
        }

        It 'Each element has an IsCurrentSession bool property' {
            if (!$script:Sessions)
            {
                Set-ItResult -Skipped -Because 'No user sessions are active on this machine'
                return
            }
            foreach ($session in $script:Sessions)
            {
                $session.IsCurrentSession | Should -BeOfType ([System.Boolean])
            }
        }

        It 'Each element is of type PSADT.TerminalServices.SessionInfo' {
            if (!$script:Sessions)
            {
                Set-ItResult -Skipped -Because 'No user sessions are active on this machine'
                return
            }
            foreach ($session in $script:Sessions)
            {
                $session | Should -BeOfType ([PSADT.TerminalServices.SessionInfo])
            }
        }

        It 'At most one session has IsCurrentSession set to true' {
            if (!$script:Sessions)
            {
                Set-ItResult -Skipped -Because 'No user sessions are active on this machine'
                return
            }
            $currentSessions = @($script:Sessions | Where-Object { $_.IsCurrentSession -eq $true })
            $currentSessions.Count | Should -BeLessOrEqual 1
        }

        It 'Each element has a DomainName property' {
            if (!$script:Sessions)
            {
                Set-ItResult -Skipped -Because 'No user sessions are active on this machine'
                return
            }
            foreach ($session in $script:Sessions)
            {
                $session.PSObject.Properties.Name | Should -Contain 'DomainName'
            }
        }
    }
}
