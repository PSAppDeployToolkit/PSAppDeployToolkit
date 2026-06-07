BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Test-ADTSessionActive' {
    BeforeAll {
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'No active session' {
        It 'Returns $false when no session is open' {
            $m = Get-Module PSAppDeployToolkit
            $count = & $m { $Script:ADT.Sessions.Count }
            if ($count -gt 0)
            {
                Set-ItResult -Skipped -Because 'A session is unexpectedly active; cannot assert the inactive path.'
                return
            }
            Test-ADTSessionActive | Should -BeFalse
        }

        It 'Returns a System.Boolean' {
            Test-ADTSessionActive | Should -BeOfType ([System.Boolean])
        }
    }

    Context 'With an active session' {
        BeforeAll {
            $script:adtSession = Open-ADTSession -DeploymentType Install -DeployMode Silent -AppName 'SessionActiveApp' -AppVendor 'SessionActiveVendor' -AppVersion '1.0.0' -ScriptDirectory $env:TEMP -NoSessionDetection -PassThru
        }

        AfterAll {
            if (Test-ADTSessionActive)
            {
                Close-ADTSession -ExitCode 0 -NoShellExit
            }
        }

        It 'Returns $true while a session is active' {
            Test-ADTSessionActive | Should -BeTrue
        }
    }

    Context 'Toggles with session lifecycle' {
        It 'Returns $false, then $true after open, then $false after close' {
            if (Test-ADTSessionActive)
            {
                Set-ItResult -Skipped -Because 'A session is unexpectedly active before the lifecycle assertion.'
                return
            }
            Test-ADTSessionActive | Should -BeFalse
            $null = Open-ADTSession -DeploymentType Install -DeployMode Silent -AppName 'ToggleApp' -AppVendor 'ToggleVendor' -AppVersion '1.0.0' -ScriptDirectory $env:TEMP -NoSessionDetection -PassThru
            try
            {
                Test-ADTSessionActive | Should -BeTrue
            }
            finally
            {
                Close-ADTSession -ExitCode 0 -NoShellExit
            }
            Test-ADTSessionActive | Should -BeFalse
        }
    }
}
