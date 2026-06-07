BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Get-ADTRunningProcesses' {
    BeforeAll {
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'currentProcessName', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
        $currentProcessName = [System.Diagnostics.Process]::GetCurrentProcess().ProcessName

        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'currentProcessDef', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
        $currentProcessDef = [PSADT.ProcessManagement.ProcessDefinition[]]@(
            [PSADT.ProcessManagement.ProcessDefinition]::new($currentProcessName)
        )

        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'bogusProcessDef', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
        $bogusProcessDef = [PSADT.ProcessManagement.ProcessDefinition[]]@(
            [PSADT.ProcessManagement.ProcessDefinition]::new('zzz_bogus_process_xyz_99999')
        )
    }

    Context 'Functionality' {
        It 'Should return results for the current PowerShell process' {
            $result = Get-ADTRunningProcesses -ProcessObjects $currentProcessDef
            @($result).Count | Should -BeGreaterThan 0
        }

        It 'Should return objects of type PSADT.ProcessManagement.RunningProcessInfo' {
            $result = Get-ADTRunningProcesses -ProcessObjects $currentProcessDef
            $result | Select-Object -First 1 | Should -BeOfType ([PSADT.ProcessManagement.RunningProcessInfo])
        }

        It 'Should return a non-null Process property on each result' {
            $result = Get-ADTRunningProcesses -ProcessObjects $currentProcessDef
            $result | ForEach-Object { $null -ne $_.Process | Should -BeTrue }
        }

        It 'Should return results whose Process.ProcessName matches the queried name' {
            $result = Get-ADTRunningProcesses -ProcessObjects $currentProcessDef
            $result | ForEach-Object { $_.Process.ProcessName | Should -Be $currentProcessName }
        }

        It 'Should return a non-empty FileName property on each result' {
            $result = Get-ADTRunningProcesses -ProcessObjects $currentProcessDef
            $result | ForEach-Object { $_.FileName | Should -Not -BeNullOrEmpty }
        }

        It 'Should return nothing (null) for a process that is not running' {
            $result = Get-ADTRunningProcesses -ProcessObjects $bogusProcessDef
            $result | Should -BeNullOrEmpty
        }
    }

    Context 'Input Validation' {
        It 'Should require the -ProcessObjects parameter (Mandatory = true)' {
            $isMandatory = (Get-Command Get-ADTRunningProcesses).Parameters['ProcessObjects'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory
            $isMandatory | Should -Contain $true
        }

        It 'Should throw when -ProcessObjects is null' {
            { Get-ADTRunningProcesses -ProcessObjects $null } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }
    }

    Context 'Metadata' {
        It 'Should declare OutputType of PSADT.ProcessManagement.RunningProcessInfo' {
            $outputTypes = (Get-Command Get-ADTRunningProcesses).OutputType.Type
            $outputTypes | Should -Contain ([PSADT.ProcessManagement.RunningProcessInfo])
        }
    }
}
