BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest -Force

    Mock -ModuleName PSAppDeployToolkit Exit-ADTInvocation { }
}

AfterAll {
    Import-ADTModuleUnderTest -Force
}
Describe 'Get-ADTDefaultConfig' {
    Context 'Without an initialized module' {
        It 'Returns every section the config carries' {
            InModuleScope -ModuleName PSAppDeployToolkit {
                $config = Get-ADTDefaultConfig
                $config | Should -BeOfType ([System.Collections.Hashtable])
                $config.Keys | Should -Contain 'Assets'
                $config.Keys | Should -Contain 'MSI'
                $config.Keys | Should -Contain 'Toolkit'
                $config.Keys | Should -Contain 'UI'
            }
        }

        It 'Leaves the module uninitialized' {
            # Answering without paying for an initialization is the entire point, so a caller that only
            # needs one value must not end up seating the module as a side effect.
            InModuleScope -ModuleName PSAppDeployToolkit {
                $null = Get-ADTDefaultConfig
                Test-ADTModuleInitialized | Should -BeFalse
            }
        }

        It 'Builds no environment table for a config that names none' {
            # A table costs around a fifth of a second the first time in a process, which is the cost that
            # removing Initialize-ADTModuleIfUninitialized took out of this path. The shipped defaults name
            # environment variables through the provider, which needs nothing in scope.
            InModuleScope -ModuleName PSAppDeployToolkit {
                Mock New-ADTEnvironmentTable { }
                $null = Get-ADTDefaultConfig
                Should -Invoke New-ADTEnvironmentTable -Times 0 -Exactly
            }
        }

        It 'Expands the environment variables the shipped paths are authored with' {
            # Authored as $env:SystemRoot\Logs\Software and the like, which unexpanded would have a caller
            # creating a directory named literally $env:SystemRoot under wherever it happened to be.
            InModuleScope -ModuleName PSAppDeployToolkit {
                $config = Get-ADTDefaultConfig
                foreach ($path in $config.Toolkit.LogPath, $config.Toolkit.CachePath, $config.Toolkit.TempPath)
                {
                    $path | Should -Not -Match '\$env:'
                    [PSADT.FileSystem.FileSystemUtilities]::IsPathFullyQualified($path) | Should -BeTrue
                }
            }
        }

        It 'Appends the toolkit name onto the temp path' {
            # A bare %TEMP% is shared with everything else on the machine, so the toolkit takes a folder
            # of its own underneath it just as an initialized config does.
            InModuleScope -ModuleName PSAppDeployToolkit {
                [System.IO.Path]::GetFileName((Get-ADTDefaultConfig).Toolkit.TempPath) | Should -BeExactly $ExecutionContext.SessionState.Module.Name
            }
        }

        It 'Hands back the no-admin-rights paths when the caller does not own the configured ones' {
            # Asserted as an equivalence rather than for one case, so it holds whatever account the tests run under.
            InModuleScope -ModuleName PSAppDeployToolkit {
                $config = Get-ADTDefaultConfig
                if (!(Test-ADTCallerOwnsConfiguredPaths -Config $config))
                {
                    $config.Toolkit.LogPath | Should -BeExactly $config.Toolkit.LogPathNoAdminRights
                    $config.Toolkit.CachePath | Should -BeExactly $config.Toolkit.CachePathNoAdminRights
                    $config.Toolkit.RegPath | Should -BeExactly $config.Toolkit.RegPathNoAdminRights
                }
                else
                {
                    $config.Toolkit.LogPath | Should -Not -BeExactly $config.Toolkit.LogPathNoAdminRights
                }
            }
        }
    }

    Context 'Machine policy' {
        BeforeEach {
            # A fallback for every policy key, as the versioned keys are found by listing the root and Pester
            # refuses a call no filter covers once the command is mocked at all. The per-test mocks win over it.
            Mock -ModuleName PSAppDeployToolkit Get-ChildItem { } -ParameterFilter { $LiteralPath -like '*Policies\PSAppDeployToolkit*' }
        }

        It 'Reads the policy key the ADMX template writes to' {
            InModuleScope -ModuleName PSAppDeployToolkit {
                Mock Get-ChildItem { } -ParameterFilter { $LiteralPath -like '*Policies\PSAppDeployToolkit\Config*' }
                $null = Get-ADTDefaultConfig
                Should -Invoke Get-ChildItem -Times 1 -ParameterFilter { $LiteralPath -like '*Policies\PSAppDeployToolkit\Config' }
            }
        }

        It 'Super-imposes a policy value over the shipped default' {
            # MutexWaitTime and FileCopyMode are both policy-backed in the shipped ADMX, so reading the
            # defaults straight out of the module would quietly ignore an administrator's setting.
            InModuleScope -ModuleName PSAppDeployToolkit {
                (Get-ADTDefaultConfig).MSI.MutexWaitTime | Should -Be 600
                Mock Get-ChildItem { 'policy-key' } -ParameterFilter { $LiteralPath -like '*Policies\PSAppDeployToolkit\Config*' }
                Mock Convert-ADTRegistryKeyToHashtable { @{ MSI = @{ MutexWaitTime = 42 }; Toolkit = @{ FileCopyMode = 'Robocopy' } } }
                $config = Get-ADTDefaultConfig
                $config.MSI.MutexWaitTime | Should -Be 42
                $config.Toolkit.FileCopyMode | Should -BeExactly 'Robocopy'
            }
        }

        It 'Expands a policy value naming one of the toolkit variables' {
            # Import-ADTConfig expands with the environment table in scope, so a config naming one of its
            # values resolves there. This path expands before the module is initialized and had nothing to
            # resolve them against, so the expansion threw rather than returning a config.
            InModuleScope -ModuleName PSAppDeployToolkit {
                Mock Get-ChildItem { 'policy-key' } -ParameterFilter { $LiteralPath -like '*Policies\PSAppDeployToolkit\Config*' }
                # Both forms carry the same value, so the answer is the same whether or not the account
                # running the tests owns the configured path and gets redirected to the other one.
                Mock Convert-ADTRegistryKeyToHashtable { @{ Toolkit = @{ LogPath = '$envWinDir\ADTProbeLogs'; LogPathNoAdminRights = '$envWinDir\ADTProbeLogs' } } }
                (Get-ADTDefaultConfig).Toolkit.LogPath | Should -BeExactly "$((New-ADTEnvironmentTable).envWinDir.FullName)\ADTProbeLogs"
            }
        }

        It 'Builds no environment table for a braced environment variable' {
            # ${env:X} reaches the provider exactly as $env:X does, so it needs nothing in scope either and
            # must not be read as a name only the environment table could supply.
            InModuleScope -ModuleName PSAppDeployToolkit {
                Mock New-ADTEnvironmentTable { }
                Mock Get-ChildItem { 'policy-key' } -ParameterFilter { $LiteralPath -like '*Policies\PSAppDeployToolkit\Config*' }
                Mock Convert-ADTRegistryKeyToHashtable { @{ Toolkit = @{ LogPath = '${env:ProgramData}\ADTProbeLogs'; LogPathNoAdminRights = '${env:ProgramData}\ADTProbeLogs' } } }
                (Get-ADTDefaultConfig).Toolkit.LogPath | Should -BeExactly "$([System.Environment]::GetEnvironmentVariable('ProgramData'))\ADTProbeLogs"
                Should -Invoke New-ADTEnvironmentTable -Times 0 -Exactly
            }
        }

        It 'Leaves the keys the policy says nothing about alone' {
            InModuleScope -ModuleName PSAppDeployToolkit {
                Mock Get-ChildItem { 'policy-key' } -ParameterFilter { $LiteralPath -like '*Policies\PSAppDeployToolkit\Config*' }
                Mock Convert-ADTRegistryKeyToHashtable { @{ MSI = @{ MutexWaitTime = 42 } } }
                (Get-ADTDefaultConfig).UI.DefaultTimeout | Should -Be 3300
            }
        }
    }

    Context 'Against an initialized module' {
        BeforeAll {
            # Get-ADTDefaultConfig answers only for a module that has not been initialized, so the default
            # config is taken first and compared with the seated one afterwards.
            #
            # TEMP is moved aside across both, since the two only part company over the temporary path where
            # the environment and .NET do: C:\Windows\Temp against the hardened C:\Windows\SystemTemp under
            # SYSTEM. .NET reads TMP ahead of TEMP and so stays put, which is what separates them here.
            #
            # Deliberately not Initialize-ADTTestModule, which repoints the log, temp and cache paths at
            # TestDrive afterwards and so has nothing left to compare against. Initializing on its own
            # reads the config without writing anywhere.
            $original = $env:TEMP
            try
            {
                $env:TEMP = 'C:\ADTNotTheHardenedTemp'
                $script:DefaultConfig = InModuleScope -ModuleName PSAppDeployToolkit { Get-ADTDefaultConfig }
                InModuleScope -ModuleName PSAppDeployToolkit { Initialize-ADTModule -InformationAction SilentlyContinue }
                $script:SeatedConfig = InModuleScope -ModuleName PSAppDeployToolkit { Get-ADTConfig }
            }
            finally
            {
                $env:TEMP = $original
            }
        }

        It 'Agrees with the initialized config on <Section>.<Name>' -ForEach @(
            @{ Section = 'Toolkit'; Name = 'TempPath' }
            @{ Section = 'Toolkit'; Name = 'LogPath' }
            @{ Section = 'Toolkit'; Name = 'CachePath' }
            @{ Section = 'Toolkit'; Name = 'RegPath' }
            @{ Section = 'MSI'; Name = 'LogPath' }
        ) {
            # Callers pick one or the other depending on whether the module is seated, so the two have to
            # land on the same answer for anything a deployment has not overridden.
            $script:DefaultConfig.$Section.$Name | Should -BeExactly $script:SeatedConfig.$Section.$Name
        }

        It 'Resolves the temporary path to the hardened folder on both' {
            # Both answers have to be the folder .NET reports rather than the one TEMP was moved to, which
            # is what makes the agreement above mean the hardening happened rather than that neither did it.
            $expected = [System.IO.Path]::GetTempPath().TrimEnd('\')
            $script:DefaultConfig.Toolkit.TempPath | Should -BeLike "$expected\*"
            $script:SeatedConfig.Toolkit.TempPath | Should -BeLike "$expected\*"
        }

        It 'Refuses to answer for a module that has been initialized' {
            # Every caller is gated on the module not being initialized, so reaching here with one that is
            # means a caller that should have asked Get-ADTConfig and would otherwise be handed a config
            # built without the directories the initialized module was given.
            InModuleScope -ModuleName PSAppDeployToolkit {
                { Get-ADTDefaultConfig } | Should -Throw -ErrorId 'ModuleAlreadyInitialized'
            }
        }
    }
}
