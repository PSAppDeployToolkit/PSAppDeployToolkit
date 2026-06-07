BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Disable-ADTTerminalServerInstallMode' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

        # Mock the private collaborator so no real change.exe invocation occurs.
        Mock -ModuleName PSAppDeployToolkit Invoke-ADTTerminalServerModeChange { }
    }

    Context 'Functionality' {
        It 'Does not throw when the host is not in terminal server install mode' {
            # On non-RDS hosts InAppInstallMode() returns $false, so the function
            # takes the early-return "already in execute mode" path and should not throw.
            if ([PSADT.TerminalServices.TerminalServerUtilities]::InAppInstallMode())
            {
                Set-ItResult -Skipped -Because 'Host is in terminal server install mode; the already-disabled early-return path cannot be exercised.'
                return
            }
            { Disable-ADTTerminalServerInstallMode } | Should -Not -Throw
        }

        It 'Produces no output' {
            $result = Disable-ADTTerminalServerInstallMode
            $result | Should -BeNullOrEmpty
        }

        It 'Does not call Invoke-ADTTerminalServerModeChange when not in install mode' {
            # On non-RDS hosts the function exits early before reaching the collaborator.
            if ([PSADT.TerminalServices.TerminalServerUtilities]::InAppInstallMode())
            {
                Set-ItResult -Skipped -Because 'Host is in terminal server install mode; the early-return path cannot be exercised.'
                return
            }
            Disable-ADTTerminalServerInstallMode
            Should -Invoke -CommandName Invoke-ADTTerminalServerModeChange -ModuleName PSAppDeployToolkit -Times 0 -Exactly
        }

        It 'Delegates to Invoke-ADTTerminalServerModeChange with Mode Execute when in install mode' {
            # InAppInstallMode() is a static .NET call that cannot be mocked.
            # This path is only reachable on a host that is currently in install mode.
            if (![PSADT.TerminalServices.TerminalServerUtilities]::InAppInstallMode())
            {
                Set-ItResult -Skipped -Because 'Host is not in terminal server install mode; the delegation path cannot be exercised without a real RDS environment.'
                return
            }
            Disable-ADTTerminalServerInstallMode
            Should -Invoke -CommandName Invoke-ADTTerminalServerModeChange -ModuleName PSAppDeployToolkit -Times 1 -Exactly -ParameterFilter {
                $Mode -eq 'Execute'
            }
        }
    }

    Context 'Input Validation' {
        It 'Accepts no positional parameters' {
            $cmd = Get-Command Disable-ADTTerminalServerInstallMode
            $positional = $cmd.Parameters.Values | Where-Object {
                $_.Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] -and $_.Position -ge 0 })
            }
            $positional | Should -BeNullOrEmpty
        }

        It 'Supports ShouldProcess' {
            $cmd = Get-Command Disable-ADTTerminalServerInstallMode
            $cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
            $cmd.Parameters.ContainsKey('Confirm') | Should -BeTrue
        }
    }
}
