BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest -Force

    # Nothing in this file may be allowed to reach [Environment]::Exit, which closing the last session
    # would otherwise do from a ConsoleHost with a client process open.
    Mock -ModuleName PSAppDeployToolkit Exit-ADTInvocation { }
}

AfterAll {
    Import-ADTModuleUnderTest -Force
}

Describe 'Initialize-ADTModule' {
    Context 'Functionality' {
        It 'Leaves the module unusable until it is called' {
            InModuleScope PSAppDeployToolkit { $ADT.Config } | Should -BeNullOrEmpty
            InModuleScope PSAppDeployToolkit { $ADT.Strings } | Should -BeNullOrEmpty
            InModuleScope PSAppDeployToolkit { $ADT.Environment } | Should -BeNullOrEmpty
        }

        It 'Populates the config, strings and environment' {
            Initialize-ADTModule -InformationAction SilentlyContinue
            InModuleScope PSAppDeployToolkit { $ADT.Config.Count } | Should -BeGreaterThan 0
            InModuleScope PSAppDeployToolkit { $ADT.Strings.Count } | Should -BeGreaterThan 0
            InModuleScope PSAppDeployToolkit { $ADT.Environment } | Should -Not -BeNullOrEmpty
        }

        It 'Marks the module as initialised' {
            Test-ADTModuleInitialized | Should -BeTrue
        }

        It 'Records how long it took' {
            InModuleScope PSAppDeployToolkit { $ADT.Durations.ModuleInit.TotalMilliseconds } | Should -BeGreaterThan 0
        }

        It 'Resets the last exit code' {
            InModuleScope PSAppDeployToolkit { $ADT.LastExitCode } | Should -Be 0
        }

        It 'Can be called again to reload the config' {
            # Re-initialising is how a caller picks up a different script directory, so it must not object
            # to already being initialised.
            { Initialize-ADTModule -InformationAction SilentlyContinue } | Should -Not -Throw
            Test-ADTModuleInitialized | Should -BeTrue
        }

        It 'Adds the variables it is given to the environment' {
            Initialize-ADTModule -AdditionalEnvironmentVariables @{ PesterAddedVariable = 'added-by-test' } -InformationAction SilentlyContinue
            (Get-ADTEnvironmentTable).PesterAddedVariable | Should -BeExactly 'added-by-test'
        }
    }

    Context 'Input Validation' {
        It 'Rejects a script directory that does not exist' {
            { Initialize-ADTModule -ScriptDirectory "$TestDrive\NoSuchDirectory" } | Should -Throw -ErrorId 'InvalidScriptDirectoryParameterValue,Initialize-ADTModule'
        }

        It 'Rejects an empty script directory' {
            # Reaches New-ADTValidateScriptErrorRecord with the empty value the validator rejected, which is
            # the case that used to report a failure against an internal parameter instead.
            { Initialize-ADTModule -ScriptDirectory '' } | Should -Throw -ErrorId 'InvalidScriptDirectoryParameterValue,Initialize-ADTModule'
        }

        It 'Rejects the same script directory twice' {
            { Initialize-ADTModule -ScriptDirectory $TestDrive, $TestDrive } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }
    }

    Context 'With an open session' {
        BeforeAll {
            Initialize-ADTTestModule -Path $TestDrive
            $null = Open-ADTSession -SessionState $ExecutionContext.SessionState -AppName 'InitProbe' -DeployMode Silent -PassThru -InformationAction SilentlyContinue
        }

        AfterAll {
            Close-ADTSession -ExitCode 0 -NoShellExit -InformationAction SilentlyContinue
        }

        It 'Refuses to reinitialise mid-deployment' {
            # Reloading config underneath a running deployment would change the rules it started under.
            { Initialize-ADTModule } | Should -Throw -ErrorId 'InitWithActiveSessionError,Initialize-ADTModule'
        }
    }
}
