BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest -Force

    Mock -ModuleName PSAppDeployToolkit Exit-ADTInvocation { }
}

AfterAll {
    Import-ADTModuleUnderTest -Force
}

Describe 'Get-ADTModuleDirectories' {
    Context 'Before initialisation' {
        It 'Refuses rather than handing back empty lists' {
            # The directories are settled during initialization, so empty ones would read as a deployment
            # that ships no config or strings of its own rather than as a module that has not started.
            InModuleScope -ModuleName PSAppDeployToolkit {
                { Get-ADTModuleDirectories } | Should -Throw -ErrorId 'ADTModuleNotInitialized,Get-ADTModuleDirectories'
            }
        }
    }

    Context 'After initialisation' {
        BeforeAll {
            Initialize-ADTTestModule -Path $TestDrive
        }

        It 'Hands back the three lists a seated module keeps' {
            InModuleScope -ModuleName PSAppDeployToolkit {
                Get-ADTModuleDirectories | Should -BeOfType ([PSAppDeployToolkit.Foundation.ModuleDirectories])
            }
        }

        It 'Falls back to the module''s own directory when the caller named none' {
            # Initialize-ADTModule defaults the script directory to Get-ADTModuleDirectory, which is how an
            # uninitialized module finds the config and strings it ships with.
            InModuleScope -ModuleName PSAppDeployToolkit {
                (Get-ADTModuleDirectories).Script.FullName | Should -Contain (Get-ADTModuleDirectory)
            }
        }

        It 'Reports the script directory the caller named' {
            # This is what Copy-ADTContentToCache and the zero-config MSI detection read to find a
            # deployment's Files and SupportFiles, so a caller naming one has to see it land here rather
            # than the module's own directory.
            Initialize-ADTModule -ScriptDirectory $TestDrive -InformationAction SilentlyContinue
            InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Expected = "$TestDrive" } {
                (Get-ADTModuleDirectories).Script.FullName | Should -Contain $Expected
            }
        }

        It 'Reports no config or strings directory for a deployment shipping neither' {
            # The ordinary case, and the one that has to arrive as an empty list rather than as nothing at
            # all, because every caller enumerates it without checking.
            InModuleScope -ModuleName PSAppDeployToolkit {
                $directories = Get-ADTModuleDirectories
                $directories.Config | Should -BeNullOrEmpty
                $directories.Strings | Should -BeNullOrEmpty
                { $directories.Config.Count } | Should -Not -Throw
            }
        }
    }
}
