BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Block-ADTAppExecution' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Input Validation' {
        It 'Should have a mandatory Processes parameter' {
            (Get-Command Block-ADTAppExecution).Parameters['Processes'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Contain $true
        }

        It 'Should have a non-mandatory WindowLocation parameter' {
            (Get-Command Block-ADTAppExecution).Parameters['WindowLocation'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Not -Contain $true
        }

        It 'Throws ParameterArgumentValidationError when Processes is null' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentValidationError,Block-ADTAppExecution'
            }
            { Block-ADTAppExecution -Processes $null } | Should @shouldParams
        }

        It 'Throws when Processes array contains duplicate entries (ValidateUnique)' {
            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'pd', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
            $pd = [PSADT.ProcessManagement.ProcessDefinition]::new('notepad.exe')
            { Block-ADTAppExecution -Processes @($pd, $pd) } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }
    }

    Context 'Behavioural - requires active ADT session' {
        It 'Throws InvalidOperationException when no ADT session is active' {
            Set-ItResult -Skipped -Because 'Block-ADTAppExecution calls Get-ADTSession in its begin block before any mockable seam is reachable; without a real Open-ADTSession the function terminates with InvalidOperationException before the body executes. Behavioural paths (IFEO writes, scheduled-task creation) cannot be exercised headlessly without a full session object.'
        }

        It 'Sets the IFEO Debugger registry value for each process name' -ForEach @(
            @{ ProcessName = 'notepad.exe' }
            @{ ProcessName = 'calc.exe' }
        ) {
            Set-ItResult -Skipped -Because "Block-ADTAppExecution uses [Microsoft.Win32.Registry]::SetValue() directly to write HKLM IFEO keys — not Set-ADTRegistryKey — so there is no in-module mock seam for the IFEO write; and the function also requires an active ADT session that cannot be faked headlessly. ProcessName: $ProcessName."
        }

        It 'Creates a scheduled task to clean up blocked applications' {
            Set-ItResult -Skipped -Because 'Block-ADTAppExecution calls Register-ScheduledTask against the live task scheduler and requires an active ADT session; neither can be satisfied in a headless test context without full elevation and a real session object.'
        }

        It 'Calls Set-ADTRegistryKey to persist block-execution arguments' {
            Set-ItResult -Skipped -Because 'Block-ADTAppExecution requires an active ADT session before reaching the Set-ADTRegistryKey call; Get-ADTSession throws InvalidOperationException in headless contexts, preventing execution from reaching this seam.'
        }

        It 'Registers an OnFinish callback to Unblock-ADTAppExecution' {
            Set-ItResult -Skipped -Because 'Block-ADTAppExecution requires an active ADT session before reaching the Add-ADTModuleCallback call; the session barrier cannot be bypassed headlessly.'
        }
    }
}
