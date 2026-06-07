BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Enable-ADTTerminalServerInstallMode' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

        # Mock the private collaborator so no real change.exe invocation occurs.
        Mock -ModuleName PSAppDeployToolkit Invoke-ADTTerminalServerModeChange { }
    }

    Context 'Functionality' {
        It 'Delegates to Invoke-ADTTerminalServerModeChange with Mode Install when not already in install mode' {
            # InAppInstallMode() is a static .NET call that returns $false on non-RDS hosts.
            # When it returns $false the function proceeds to call Invoke-ADTTerminalServerModeChange.
            if ([PSADT.TerminalServices.TerminalServerUtilities]::InAppInstallMode())
            {
                Set-ItResult -Skipped -Because 'Host is already in terminal server install mode; the delegation path cannot be exercised.'
                return
            }
            Enable-ADTTerminalServerInstallMode
            Should -Invoke -CommandName Invoke-ADTTerminalServerModeChange -ModuleName PSAppDeployToolkit -Times 1 -Exactly -ParameterFilter {
                $Mode -eq 'Install'
            }
        }

        It 'Produces no output' {
            $result = Enable-ADTTerminalServerInstallMode
            $result | Should -BeNullOrEmpty
        }

        It 'Does not call Invoke-ADTTerminalServerModeChange when already in install mode' {
            if (![PSADT.TerminalServices.TerminalServerUtilities]::InAppInstallMode())
            {
                Set-ItResult -Skipped -Because 'Host is not in terminal server install mode; the already-enabled early-return path cannot be exercised.'
                return
            }
            Enable-ADTTerminalServerInstallMode
            Should -Invoke -CommandName Invoke-ADTTerminalServerModeChange -ModuleName PSAppDeployToolkit -Times 0 -Exactly
        }
    }

    Context 'Input Validation' {
        It 'Accepts no positional parameters' {
            $cmd = Get-Command Enable-ADTTerminalServerInstallMode
            $positional = $cmd.Parameters.Values | Where-Object {
                $_.Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] -and $_.Position -ge 0 })
            }
            $positional | Should -BeNullOrEmpty
        }

        It 'Supports ShouldProcess' {
            $cmd = Get-Command Enable-ADTTerminalServerInstallMode
            $cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
            $cmd.Parameters.ContainsKey('Confirm') | Should -BeTrue
        }
    }
}
