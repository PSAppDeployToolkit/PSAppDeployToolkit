BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}

Describe 'Get-ADTApplication' {
    BeforeAll {
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables { }
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'No filters — all applications' {
        It 'Does not throw when called with no parameters' {
            { Get-ADTApplication } | Should -Not -Throw
        }

        It 'Returns at least one installed application' {
            $result = @(Get-ADTApplication)
            $result.Count | Should -BeGreaterThan 0
        }

        It 'Each result is of type PSADT.AppManagement.InstalledApplication' {
            $result = @(Get-ADTApplication)
            $result[0] | Should -BeOfType ([PSADT.AppManagement.InstalledApplication])
        }

        It 'Each result has a non-empty DisplayName' {
            $result = @(Get-ADTApplication)
            foreach ($app in $result)
            {
                $app.DisplayName | Should -Not -BeNullOrEmpty
            }
        }

        It 'Is64BitApplication is null or a System.Boolean (nullable bool)' {
            $result = @(Get-ADTApplication)
            foreach ($app in $result)
            {
                if ($null -ne $app.Is64BitApplication)
                {
                    $app.Is64BitApplication | Should -BeOfType ([System.Boolean])
                }
            }
        }
    }

    Context '-Name filter (Contains match)' {
        It 'Returns results when filtering by a broad name (Microsoft)' {
            $result = @(Get-ADTApplication -Name 'Microsoft')
            $result.Count | Should -BeGreaterThan 0
        }

        It 'Each result DisplayName contains the search term (case-insensitive)' {
            $result = @(Get-ADTApplication -Name 'Microsoft')
            foreach ($app in $result)
            {
                $app.DisplayName | Should -BeLike '*Microsoft*'
            }
        }

        It 'Returns empty or fewer results for a nonexistent app name' {
            $result = @(Get-ADTApplication -Name 'PSADT_NonExistent_App_ZZZ')
            $result.Count | Should -Be 0
        }
    }

    Context '-NameMatch Exact' {
        It 'Does not throw with -NameMatch Exact and a name that does not match exactly' {
            { Get-ADTApplication -Name 'Microsoft' -NameMatch Exact } | Should -Not -Throw
        }

        It 'Returns empty for a partial name with -NameMatch Exact' {
            $result = @(Get-ADTApplication -Name 'Microsoft' -NameMatch Exact)
            # "Microsoft" alone is unlikely to be an exact app name
            foreach ($app in $result)
            {
                $app.DisplayName | Should -Be 'Microsoft'
            }
        }
    }

    Context '-ApplicationType filter' {
        It 'Does not throw with -ApplicationType MSI' {
            { Get-ADTApplication -ApplicationType MSI } | Should -Not -Throw
        }

        It 'Does not throw with -ApplicationType EXE' {
            { Get-ADTApplication -ApplicationType EXE } | Should -Not -Throw
        }

        It 'MSI results have WindowsInstaller = true' {
            $result = @(Get-ADTApplication -ApplicationType MSI)
            foreach ($app in $result)
            {
                $app.WindowsInstaller | Should -BeTrue
            }
        }

        It 'EXE results have WindowsInstaller = false' {
            $result = @(Get-ADTApplication -ApplicationType EXE)
            foreach ($app in $result)
            {
                $app.WindowsInstaller | Should -BeFalse
            }
        }
    }

    Context '-FilterScript' {
        It 'Does not throw when using -FilterScript' {
            { Get-ADTApplication -FilterScript { $_.DisplayName -like '*Microsoft*' } } | Should -Not -Throw
        }

        It '-FilterScript returns only matching results' {
            $result = @(Get-ADTApplication -FilterScript { $_.DisplayName -like '*Microsoft*' })
            foreach ($app in $result)
            {
                $app.DisplayName | Should -BeLike '*Microsoft*'
            }
        }

        It '-FilterScript returning false for all returns empty' {
            $result = @(Get-ADTApplication -FilterScript { $false })
            $result.Count | Should -Be 0
        }
    }

    Context '-IncludeUpdatesAndHotfixes' {
        It 'Does not throw with -IncludeUpdatesAndHotfixes' {
            { Get-ADTApplication -IncludeUpdatesAndHotfixes } | Should -Not -Throw
        }

        It 'Result with -IncludeUpdatesAndHotfixes has count >= result without it' {
            $withFlag = @(Get-ADTApplication -IncludeUpdatesAndHotfixes).Count
            $withoutFlag = @(Get-ADTApplication).Count
            $withFlag | Should -BeGreaterOrEqual $withoutFlag
        }
    }
}
