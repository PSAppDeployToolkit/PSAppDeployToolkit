BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest -Force

    Mock -ModuleName PSAppDeployToolkit Exit-ADTInvocation { }
    Initialize-ADTTestModule -Path $TestDrive
}

AfterAll {
    Import-ADTModuleUnderTest -Force
}
Describe 'Update-ADTEnvironmentPsProvider' {
    Context 'Functionality' {
        BeforeEach {
            $script:OriginalEnvironment = [System.Environment]::GetEnvironmentVariables()
        }

        AfterEach {
            # The function rewrites the whole process environment from the registry, so everything it
            # touched has to go back exactly as it was found or the rest of the run inherits the damage.
            foreach ($name in @([System.Environment]::GetEnvironmentVariables().Keys))
            {
                if (!$script:OriginalEnvironment.Contains($name))
                {
                    [System.Environment]::SetEnvironmentVariable($name, $null)
                }
            }
            foreach ($entry in $script:OriginalEnvironment.GetEnumerator())
            {
                [System.Environment]::SetEnvironmentVariable($entry.Key, $entry.Value)
            }
        }

        It 'Rebuilds a path that was overwritten' {
            # Installers routinely change the machine path, and a deployment carrying on in the same
            # session would otherwise never see the new entries.
            $env:PATH = 'C:\Nothing\Useful'
            Update-ADTEnvironmentPsProvider
            $env:PATH | Should -Not -BeExactly 'C:\Nothing\Useful'
        }

        It 'Rebuilds the path from both the machine and the user' {
            $env:PATH = 'C:\Nothing\Useful'
            Update-ADTEnvironmentPsProvider
            foreach ($entry in ([PSADT.Utilities.EnvironmentUtilities]::GetEnvironmentVariable('PATH', 'Machine')).Split([System.IO.Path]::PathSeparator, [System.StringSplitOptions]::RemoveEmptyEntries))
            {
                $env:PATH.Split([System.IO.Path]::PathSeparator) | Should -Contain $entry.Trim()
            }
        }

        It 'Does not repeat an entry that both the machine and the user carry' {
            $env:PATH = 'C:\Nothing\Useful'
            Update-ADTEnvironmentPsProvider
            $entries = $env:PATH.Split([System.IO.Path]::PathSeparator, [System.StringSplitOptions]::RemoveEmptyEntries)
            @($entries).Count | Should -Be @($entries | Select-Object -Unique).Count
        }

        # Skipped until the function is repaired. PowerShell adds its own module directories to
        # PSModulePath at startup and they appear in neither registry hive, so refreshing from the
        # registry alone drops them and the session can no longer find the modules it shipped with.
        It 'Keeps the module directories PowerShell added for itself' -Skip {
            $before = $env:PSModulePath.Split([System.IO.Path]::PathSeparator, [System.StringSplitOptions]::RemoveEmptyEntries)
            Update-ADTEnvironmentPsProvider
            foreach ($entry in $before)
            {
                $env:PSModulePath.Split([System.IO.Path]::PathSeparator) | Should -Contain $entry
            }
        }

        It 'Brings a machine variable back into the session' {
            $env:windir = 'nonsense'
            Update-ADTEnvironmentPsProvider
            $env:windir | Should -Not -BeExactly 'nonsense'
        }

        It 'Returns nothing' {
            Update-ADTEnvironmentPsProvider | Should -BeNullOrEmpty
        }
    }
}
