BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Get-ADTSession' {
    BeforeAll {
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'No active session' {
        It 'Throws InvalidOperationException with ErrorId ADTSessionBufferEmpty when no session is open' {
            # Guard: ensure no session is active before asserting the empty-buffer path.
            if (Test-ADTSessionActive)
            {
                Set-ItResult -Skipped -Because 'A session is unexpectedly active; cannot assert the empty-buffer path.'
                return
            }
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.InvalidOperationException]
                ErrorId       = 'ADTSessionBufferEmpty,Get-ADTSession'
            }
            { Get-ADTSession } | Should @shouldParams
        }
    }

    Context 'With an active session' {
        BeforeAll {
            # Stand up a real minimal session in Silent mode (no UI) without an exit-on-close shell.
            $script:adtSession = Open-ADTSession -DeploymentType Install -DeployMode Silent -AppName 'GetSessionApp' -AppVendor 'GetSessionVendor' -AppVersion '1.0.0' -ScriptDirectory $env:TEMP -NoSessionDetection -PassThru
        }

        AfterAll {
            # Tear down the session we opened so nothing leaks to sibling test files.
            if (Test-ADTSessionActive)
            {
                Close-ADTSession -ExitCode 0 -NoShellExit
            }
        }

        It 'Returns the active DeploymentSession object' {
            $result = Get-ADTSession
            $result | Should -BeOfType ([PSAppDeployToolkit.Foundation.DeploymentSession])
        }

        It 'Returns the most recently opened session' {
            $result = Get-ADTSession
            $result | Should -Be $script:adtSession
        }
    }

    Context 'Metadata' {
        It 'Declares an OutputType of PSAppDeployToolkit.Foundation.DeploymentSession' {
            $outputTypes = (Get-Command Get-ADTSession).OutputType.Type
            $outputTypes | Should -Contain ([PSAppDeployToolkit.Foundation.DeploymentSession])
        }
    }
}
