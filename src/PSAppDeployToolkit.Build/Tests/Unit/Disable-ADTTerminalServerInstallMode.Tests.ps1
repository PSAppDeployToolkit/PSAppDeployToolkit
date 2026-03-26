BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}

Describe 'Disable-ADTTerminalServerInstallMode' {
    BeforeAll {
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables { }
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'On a standard workstation (not in install mode)' {
        # On a standard workstation, InAppInstallMode() returns $false.
        # Disable checks InAppInstallMode() — if false (already in execute mode) it returns early.

        It 'Does not throw when called with no parameters on a workstation' {
            # InAppInstallMode() = $false → already in execute mode → logs and returns.
            $inInstallMode = [PSADT.TerminalServices.TerminalServerUtilities]::InAppInstallMode()
            if ($inInstallMode)
            {
                Set-ItResult -Skipped -Because 'This system is currently in Terminal Server install mode'
                return
            }
            { Disable-ADTTerminalServerInstallMode } | Should -Not -Throw
        }

        It 'Does not throw when -WhatIf is specified' {
            { Disable-ADTTerminalServerInstallMode -WhatIf } | Should -Not -Throw
        }
    }

    Context 'On a Terminal Server in install mode' {
        It 'Does not throw when -WhatIf is used in install mode' {
            $inInstallMode = [PSADT.TerminalServices.TerminalServerUtilities]::InAppInstallMode()
            if (!$inInstallMode)
            {
                Set-ItResult -Skipped -Because 'This system is not currently in Terminal Server install mode'
                return
            }
            # WhatIf prevents Invoke-ADTTerminalServerModeChange from being called.
            { Disable-ADTTerminalServerInstallMode -WhatIf } | Should -Not -Throw
        }
    }
}
