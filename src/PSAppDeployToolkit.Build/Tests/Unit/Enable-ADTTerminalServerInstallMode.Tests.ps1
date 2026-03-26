BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}

Describe 'Enable-ADTTerminalServerInstallMode' {
    BeforeAll {
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables { }
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'On a standard workstation (not in install mode)' {
        # On a workstation, InAppInstallMode() = $false.
        # Enable checks InAppInstallMode() — if false, proceeds to ShouldProcess.
        # With -WhatIf, ShouldProcess returns $false → early return without mode change.

        It 'Does not throw when -WhatIf is specified on a workstation' {
            $inInstallMode = [PSADT.TerminalServices.TerminalServerUtilities]::InAppInstallMode()
            if ($inInstallMode)
            {
                Set-ItResult -Skipped -Because 'This system is already in Terminal Server install mode'
                return
            }
            { Enable-ADTTerminalServerInstallMode -WhatIf } | Should -Not -Throw
        }
    }

    Context 'On a Terminal Server already in install mode' {
        It 'Does not throw when already in install mode (returns early)' {
            $inInstallMode = [PSADT.TerminalServices.TerminalServerUtilities]::InAppInstallMode()
            if (!$inInstallMode)
            {
                Set-ItResult -Skipped -Because 'This system is not currently in Terminal Server install mode'
                return
            }
            # InAppInstallMode() = $true → "already in install mode" → returns early without throw.
            { Enable-ADTTerminalServerInstallMode } | Should -Not -Throw
        }
    }

    Context 'WhatIf is always safe' {
        It 'Does not throw with -WhatIf regardless of current mode' {
            { Enable-ADTTerminalServerInstallMode -WhatIf } | Should -Not -Throw
        }
    }
}
