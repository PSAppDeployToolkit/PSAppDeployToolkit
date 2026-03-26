BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force

    # Build ProcessDefinition objects used across tests.
    # The current process is guaranteed to be running regardless of PS edition.
    $script:CurrentProcessName = [System.Diagnostics.Process]::GetCurrentProcess().ProcessName
    $script:PwshDef = [PSADT.ProcessManagement.ProcessDefinition]::new($script:CurrentProcessName)
    $script:GhostDef = [PSADT.ProcessManagement.ProcessDefinition]::new('ADTTestProcessThatDoesNotExist')
}

Describe 'Get-ADTRunningProcesses' {
    BeforeAll {
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables {}
        # No Initialize-ADTFunction call in this function — only Write-ADTLogEntry needs mocking.
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Running Process' {
        It 'Returns a non-empty result when the process is running' {
            $result = Get-ADTRunningProcesses -ProcessObjects @($script:PwshDef)
            $result | Should -Not -BeNullOrEmpty
        }

        It 'Result elements are RunningProcessInfo objects' {
            $result = Get-ADTRunningProcesses -ProcessObjects @($script:PwshDef)
            $result[0] | Should -BeOfType ([PSADT.ProcessManagement.RunningProcessInfo])
        }

        It 'Does not throw for a running process' {
            { Get-ADTRunningProcesses -ProcessObjects @($script:PwshDef) } | Should -Not -Throw
        }

        It 'Returned RunningProcessInfo has a Process property' {
            $result = Get-ADTRunningProcesses -ProcessObjects @($script:PwshDef)
            $result[0].Process | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Non-Running Process' {
        It 'Returns nothing when no matching process is running' {
            $result = Get-ADTRunningProcesses -ProcessObjects @($script:GhostDef)
            $result | Should -BeNullOrEmpty
        }

        It 'Does not throw when no matching process is running' {
            { Get-ADTRunningProcesses -ProcessObjects @($script:GhostDef) } | Should -Not -Throw
        }
    }

    Context 'Mixed Input' {
        It 'Returns only the running processes from a mixed list' {
            $result = Get-ADTRunningProcesses -ProcessObjects @($script:PwshDef, $script:GhostDef)
            # Should include pwsh but not the ghost process.
            $result | Should -Not -BeNullOrEmpty
            $names = $result | ForEach-Object { $_.Process.ProcessName }
            $names | Should -Contain $script:CurrentProcessName
        }
    }

    Context 'Input Validation' {
        It 'Throws when ProcessObjects is null' {
            { Get-ADTRunningProcesses -ProcessObjects $null } | Should -Throw
        }
    }
}
