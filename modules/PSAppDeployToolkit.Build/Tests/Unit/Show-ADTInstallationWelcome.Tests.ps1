BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest -Force

    Mock -ModuleName PSAppDeployToolkit Exit-ADTInvocation { }
    Initialize-ADTTestModule -Path $TestDrive

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}

AfterAll {
    Import-ADTModuleUnderTest -Force
}
Describe 'Show-ADTInstallationWelcome' {
    # Only the silent path against processes that cannot be running is exercised. Everything else either
    # puts a dialog on screen or closes whatever the machine happens to have open, both of which are left
    # to the user interface effort.
    Context 'In a silent deployment' {
        BeforeAll {
            $null = Open-ADTSession -SessionState $ExecutionContext.SessionState -AppName 'WelcomeSilent' -DeployMode Silent -PassThru -InformationAction SilentlyContinue
            $script:Absent = @{ Name = "adtnosuchprocess$([System.Guid]::NewGuid().ToString('N'))" }
        }

        AfterAll {
            Close-ADTSession -ExitCode 0 -NoShellExit -InformationAction SilentlyContinue
        }

        It 'Closes nothing and shows nothing' {
            { Show-ADTInstallationWelcome -CloseProcesses $script:Absent -Silent } | Should -Not -Throw
        }

        It 'Returns nothing' {
            # Callers branch on a deferral result, so anything returned from the silent path would have a
            # deployment take a branch nobody chose.
            Show-ADTInstallationWelcome -CloseProcesses $script:Absent -Silent | Should -BeNullOrEmpty
        }

        It 'Can be called more than once' {
            { 1..2 | ForEach-Object { Show-ADTInstallationWelcome -CloseProcesses $script:Absent -Silent } } | Should -Not -Throw
        }

        It 'Accepts several processes to look for' {
            { Show-ADTInstallationWelcome -CloseProcesses $script:Absent, @{ Name = 'adtnosuchprocesseither'; Description = 'Not Running' } -Silent } | Should -Not -Throw
        }
    }

    Context 'Input Validation' {
        It 'Requires something to do' {
            Test-ADTParameterSetSatisfied -Command (Get-Command Show-ADTInstallationWelcome) | Should -BeFalse
        }

        It 'Refuses deferral in a silent run' {
            # There is nobody to defer, so asking for both is a contradiction rather than a preference.
            { Show-ADTInstallationWelcome -CloseProcesses @{ Name = 'anything' } -Silent -AllowDefer } | Should -Throw -ErrorId 'AmbiguousParameterSet,Show-ADTInstallationWelcome'
        }

        It 'Refuses a countdown longer than a day' {
            { Show-ADTInstallationWelcome -CloseProcesses @{ Name = 'anything' } -CloseProcessesCountdown ([System.TimeSpan]::FromDays(2)) } | Should -Throw -ErrorId 'InvalidCloseProcessesCountdownParameterValue,Show-ADTInstallationWelcome'
        }

        It 'Refuses a forced countdown longer than a day' {
            { Show-ADTInstallationWelcome -CloseProcesses @{ Name = 'anything' } -ForceCloseProcessesCountdown ([System.TimeSpan]::FromDays(2)) } | Should -Throw -ErrorId 'InvalidForceCloseProcessesCountdownParameterValue,Show-ADTInstallationWelcome'
        }
    }
}
