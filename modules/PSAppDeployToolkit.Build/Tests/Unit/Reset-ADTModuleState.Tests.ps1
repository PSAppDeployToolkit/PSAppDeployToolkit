BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest -Force

    # Nothing here may reach [Environment]::Exit, which closing the last session would otherwise do.
    Mock -ModuleName PSAppDeployToolkit Exit-ADTInvocation { }
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}

AfterAll {
    Import-ADTModuleUnderTest -Force
}

Describe 'Reset-ADTModuleState' {
    Context 'With no session open' {
        BeforeEach {
            Initialize-ADTTestModule -Path $TestDrive
        }

        It 'Leaves the module uninitialized' {
            # The state is what "initialized" means, so dropping it is the whole operation.
            Test-ADTModuleInitialized | Should -BeTrue
            InModuleScope -ModuleName PSAppDeployToolkit { Reset-ADTModuleState }
            Test-ADTModuleInitialized | Should -BeFalse
        }

        It 'Takes the config, strings and environment with it' {
            # They hang off the state rather than sitting beside it, which is what stops the module ever
            # reporting itself uninitialized while still answering from a config it no longer has.
            InModuleScope -ModuleName PSAppDeployToolkit { Reset-ADTModuleState }
            { Get-ADTConfig } | Should -Throw -ErrorId 'ADTModuleNotInitialized,Get-ADTConfig'
            { Get-ADTStringTable } | Should -Throw -ErrorId 'ADTModuleNotInitialized,Get-ADTStringTable'
            { Get-ADTEnvironmentTable } | Should -Throw -ErrorId 'ADTModuleNotInitialized,Get-ADTEnvironmentTable'
        }

        It 'Does not object to being called twice' {
            # Callers reset without first asking whether there is anything to reset, so a second call has to
            # be a no-op rather than a failure.
            InModuleScope -ModuleName PSAppDeployToolkit { Reset-ADTModuleState }
            { InModuleScope -ModuleName PSAppDeployToolkit { Reset-ADTModuleState } } | Should -Not -Throw
            Test-ADTModuleInitialized | Should -BeFalse
        }
    }

    Context 'With a session open' {
        BeforeEach {
            Initialize-ADTTestModule -Path $TestDrive
            $null = Open-ADTSession -SessionState $ExecutionContext.SessionState -AppName 'ResetProbe' -DeployMode Silent -PassThru -InformationAction SilentlyContinue
        }

        AfterEach {
            if (Test-ADTSessionActive)
            {
                Close-ADTSession -ExitCode 0 -NoShellExit -InformationAction SilentlyContinue
            }
        }

        It 'Refuses to discard a session that is still running' {
            # The state holds the open sessions, so resetting would drop a deployment mid-flight and leave
            # nothing able to close it.
            InModuleScope -ModuleName PSAppDeployToolkit {
                { Reset-ADTModuleState } | Should -Throw -ErrorId 'ModuleStateResetWhileInUse,Reset-ADTModuleState'
            }
            Test-ADTModuleInitialized | Should -BeTrue
        }

        It 'Discards it anyway when forced' {
            # Open-ADTSession re-enters from the console with whatever the last run left behind, and a
            # dangling session there has to be cleared rather than block every later deployment.
            InModuleScope -ModuleName PSAppDeployToolkit { Reset-ADTModuleState -Force }
            Test-ADTModuleInitialized | Should -BeFalse
            Test-ADTSessionActive | Should -BeFalse
        }
    }
}
