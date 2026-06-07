BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Get-ADTStringTable' {
    BeforeAll {
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    # This context runs before the module is initialized so the uninitialized
    # contract is observed against genuinely empty module state.
    Context 'Uninitialized module' {
        It 'Throws an InvalidOperationException when the string table is not initialized' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.InvalidOperationException]
                ErrorId = 'ADTStringTableNotInitialized,Get-ADTStringTable'
            }
            { Get-ADTStringTable } | Should @shouldParams
        }
    }

    Context 'Initialized module' {
        BeforeAll {
            Initialize-ADTModule
        }

        It 'Returns a hashtable' {
            $result = Get-ADTStringTable
            $result | Should -BeOfType ([System.Collections.Hashtable])
        }

        It 'Returns a non-empty table' {
            (Get-ADTStringTable).Count | Should -BeGreaterThan 0
        }

        It 'Contains the well-known top-level key <Key>' -ForEach @(
            @{ Key = 'BalloonTip' }
            @{ Key = 'CloseAppsPrompt' }
            @{ Key = 'InstallationPrompt' }
            @{ Key = 'ProgressPrompt' }
            @{ Key = 'RestartPrompt' }
            @{ Key = 'DiskSpaceText' }
            @{ Key = 'BlockExecutionText' }
            @{ Key = 'ListSelectionPrompt' }
        ) {
            (Get-ADTStringTable).ContainsKey($Key) | Should -BeTrue
        }

        It 'Returns the same reference on consecutive calls when no SessionState is provided' {
            $first = Get-ADTStringTable
            $second = Get-ADTStringTable
            [System.Object]::ReferenceEquals($first, $second) | Should -BeTrue
        }

        It 'Returns a distinct copy (not the cached reference) when a SessionState is provided' {
            $cached = Get-ADTStringTable
            $expanded = Get-ADTStringTable -SessionState $ExecutionContext.SessionState
            [System.Object]::ReferenceEquals($cached, $expanded) | Should -BeFalse
        }

        It 'Returns a hashtable with the same top-level keys when a SessionState is provided' {
            $cached = Get-ADTStringTable
            $expanded = Get-ADTStringTable -SessionState $ExecutionContext.SessionState
            ($expanded.Keys | Sort-Object) | Should -Be ($cached.Keys | Sort-Object)
        }
    }

    Context 'Input Validation' {
        It 'Has a non-mandatory SessionState parameter' {
            (Get-Command Get-ADTStringTable).Parameters['SessionState'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Not -Contain $true
        }

        It 'Declares an OutputType of System.Collections.Hashtable' {
            (Get-Command Get-ADTStringTable).OutputType.Type | Should -Contain ([System.Collections.Hashtable])
        }
    }
}
