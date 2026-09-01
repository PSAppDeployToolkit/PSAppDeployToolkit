BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest -Force

    Mock -ModuleName PSAppDeployToolkit Exit-ADTInvocation { }

    $script:RegistryRoot = (New-Item -Path 'TestRegistry:\ResetDeferHistoryProbe' -ItemType Directory).PSPath
    Initialize-ADTTestModule -Path $TestDrive -RegistryPath $script:RegistryRoot
}

AfterAll {
    Import-ADTModuleUnderTest -Force
}

Describe 'Reset-ADTDeferHistory' {
    Context 'With no session open' {
        It 'Tells the caller to open one first' {
            { Reset-ADTDeferHistory } | Should -Throw -ErrorId 'ADTSessionBufferEmpty,Reset-ADTDeferHistory'
        }
    }

    Context 'With a session open' {
        BeforeAll {
            $null = Open-ADTSession -SessionState $ExecutionContext.SessionState -AppName 'ResetDeferProbe' -DeployMode Silent -PassThru -InformationAction SilentlyContinue
        }

        AfterAll {
            Close-ADTSession -ExitCode 0 -NoShellExit -InformationAction SilentlyContinue
        }

        It 'Clears a history that was recorded' {
            # Called once a deployment finally goes ahead, so the next one starts with its full allowance
            # rather than the exhausted count from last time.
            Set-ADTDeferHistory -DeferTimesRemaining 1 -InformationAction SilentlyContinue
            Get-ADTDeferHistory | Should -Not -BeNullOrEmpty

            Reset-ADTDeferHistory -InformationAction SilentlyContinue
            Get-ADTDeferHistory | Should -BeNullOrEmpty
        }

        It 'Removes the registry key rather than blanking its values' {
            Set-ADTDeferHistory -DeferTimesRemaining 1 -DeferDeadline ([System.DateTime]::new(2030, 1, 1)) -InformationAction SilentlyContinue
            Reset-ADTDeferHistory -InformationAction SilentlyContinue

            $remaining = @(Get-ChildItem -LiteralPath $script:RegistryRoot -Recurse | & { process { if ($_.Property.Count) { return $_ } } })
            $remaining.Count | Should -Be 0
        }

        It 'Says nothing when there is no history to clear' {
            # Reset runs unconditionally at the end of a deployment, so a first run must not fail on it.
            Reset-ADTDeferHistory -InformationAction SilentlyContinue
            { Reset-ADTDeferHistory -InformationAction SilentlyContinue } | Should -Not -Throw
        }

        It 'Leaves the session able to record a new history afterwards' {
            Reset-ADTDeferHistory -InformationAction SilentlyContinue
            Set-ADTDeferHistory -DeferTimesRemaining 5 -InformationAction SilentlyContinue
            (Get-ADTDeferHistory).DeferTimesRemaining | Should -Be 5
            Reset-ADTDeferHistory -InformationAction SilentlyContinue
        }
    }
}
